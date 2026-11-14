using System.Text.Json;
namespace Publicate;
public record PublishContext(PublishingTarget Target,ResolvedPost Post,IReadOnlyList<MediaAsset> Media,Func<string,Task> Checkpoint);
public interface ISocialPublisher {
 Platform Platform { get; }
 Capability GetCapabilities();
 List<string> ValidatePost(ResolvedPost post,IReadOnlyList<MediaAsset> media);
 Task<PublishResult> PublishPost(PublishContext context,CancellationToken ct);
 Task<PublishResult> CheckPublishStatus(PublicationJob job,PublishingTarget target,CancellationToken ct);
}
// Account connection/target discovery are delegated to OAuthService; publishing has no credential DTOs.
public abstract class SocialPublisher(ProviderHttp http,OAuthService oauth,IMediaStorage storage):ISocialPublisher {
 protected ProviderHttp Http=>http; protected OAuthService OAuth=>oauth; protected IMediaStorage Storage=>storage;
 public abstract Platform Platform { get; }
 public Capability GetCapabilities()=>Capabilities.Get(Platform);
 public virtual List<string> ValidatePost(ResolvedPost p,IReadOnlyList<MediaAsset> m)=>PostValidator.Validate(Platform,p,m);
 public abstract Task<PublishResult> PublishPost(PublishContext c,CancellationToken ct);
 public virtual Task<PublishResult> CheckPublishStatus(PublicationJob j,PublishingTarget t,CancellationToken ct)=>Task.FromResult(new PublishResult(j.State,j.ExternalId,j.OperationId,j.Url,"Brak dostępnego odczytu statusu. Nie ponawiaj automatycznie."));
 protected async Task<HttpContent> FileContent(MediaAsset a,CancellationToken ct) {
 var content=new StreamContent(await Storage.Read(a.Key,ct));content.Headers.ContentType=new(a.ContentType);return content;
 }
 protected static PublishResult Confirm(ApiReply r,string key="id") {
 var id=ProviderHttp.Optional(r.Json,key);
 return string.IsNullOrWhiteSpace(id)?new(JobState.Unknown,Detail:"API nie zwróciło identyfikatora publikacji.",HttpStatus:r.Status):new(JobState.Published,ExternalId:id,HttpStatus:r.Status);
 }
}
public class LinkedInPublisher(ProviderHttp h,OAuthService o,IMediaStorage s):SocialPublisher(h,o,s) {
 public override Platform Platform=>Platform.LinkedIn;
 private Dictionary<string,string> Headers=>new(){{"Linkedin-Version",OAuth.Setting(Platform,"Version")},{"X-Restli-Protocol-Version","2.0.0"}};
 public override async Task<PublishResult> PublishPost(PublishContext c,CancellationToken ct) {
 var token=await OAuth.TargetToken(c.Target);var mediaIds=new List<string>();
 foreach(var m in c.Media) {
 if(m.ContentType=="video/mp4") {
 var init=(await Http.Send(HttpMethod.Post,"https://api.linkedin.com/rest/videos?action=initializeUpload",token,new{initializeUploadRequest=new{owner=c.Target.ExternalId,fileSizeBytes=m.Bytes,uploadCaptions=false,uploadThumbnail=false}},headers:Headers,ct:ct)).Json.GetProperty("value");
 var video=ProviderHttp.Required(init,"video");await c.Checkpoint(video);
 var partIds=new List<string>();
 foreach(var part in init.GetProperty("uploadInstructions").EnumerateArray()) {
 var url=ProviderHttp.Required(part,"uploadUrl");ProviderHttp.ValidateUploadUrl(url,"linkedin.com","licdn.com");
 var first=part.GetProperty("firstByte").GetInt64();var last=Math.Min(part.GetProperty("lastByte").GetInt64(),m.Bytes-1);
 if(first<0||last<first||last>=m.Bytes) throw new InvalidOperationException("Nieprawidłowy zakres uploadu LinkedIn.");
 await using var stream=await Storage.Read(m.Key,ct);stream.Seek(first,SeekOrigin.Begin);
 var bytes=new byte[checked((int)(last-first+1))];await stream.ReadExactlyAsync(bytes,ct);
 // Upload ETag is required by finalizeUpload; separate helper preserves response headers.
 partIds.Add(await UploadPart(url,bytes,ct));
 }
 await Http.Send(HttpMethod.Post,"https://api.linkedin.com/rest/videos?action=finalizeUpload",token,new{finalizeUploadRequest=new{video,uploadToken=ProviderHttp.Optional(init,"uploadToken")??"",uploadedPartIds=partIds}},headers:Headers,ct:ct);
 var available=false;
 for(var i=0;i<6;i++) {
 var status=(await Http.Send(HttpMethod.Get,"https://api.linkedin.com/rest/videos/"+ProviderHttp.Escape(video),token,headers:Headers,ct:ct)).Json;
 var value=ProviderHttp.Optional(status,"status");if(value=="AVAILABLE"){available=true;break;}
 if(value=="PROCESSING_FAILED") return new(JobState.Failed,OperationId:video,Detail:"LinkedIn odrzucił przetwarzanie filmu.");
 await Task.Delay(TimeSpan.FromSeconds(3),ct);
 }
 if(!available) return new(JobState.Processing,OperationId:video,Detail:"Film przetwarzany. Post nie został jeszcze utworzony; sprawdź status i utwórz nową próbę po ręcznej weryfikacji.");
 mediaIds.Add(video);
 } else {
 var init=(await Http.Send(HttpMethod.Post,"https://api.linkedin.com/rest/images?action=initializeUpload",token,new{initializeUploadRequest=new{owner=c.Target.ExternalId}},headers:Headers,ct:ct)).Json.GetProperty("value");
 var url=ProviderHttp.Required(init,"uploadUrl");ProviderHttp.ValidateUploadUrl(url,"linkedin.com","licdn.com");
 var id=ProviderHttp.Required(init,"image");await c.Checkpoint(id);
 await Http.Send(HttpMethod.Put,url,token,content:await FileContent(m,ct),ct:ct);mediaIds.Add(id);
 }
 }
 var body=new Dictionary<string,object>{{"author",c.Target.ExternalId},{"commentary",c.Post.Text},{"visibility","PUBLIC"},{"distribution",new{feedDistribution="MAIN_FEED",targetEntities=Array.Empty<string>(),thirdPartyDistributionChannels=Array.Empty<string>()}},{"lifecycleState","PUBLISHED"},{"isReshareDisabledByAuthor",false}};
 if(mediaIds.Count==1) body["content"]=new{media=new{id=mediaIds[0]}};
 if(mediaIds.Count>1) body["content"]=new{multiImage=new{images=mediaIds.Select(id=>new{id}).ToArray()}};
 var result=await Http.Send(HttpMethod.Post,"https://api.linkedin.com/rest/posts",token,body,headers:Headers,ct:ct);
 return result.Status==201&&!string.IsNullOrEmpty(result.Id)?new(JobState.Published,ExternalId:result.Id,HttpStatus:201):new(JobState.Unknown,Detail:"Brak 201 i x-restli-id.",HttpStatus:result.Status);
 }
 private async Task<string> UploadPart(string url,byte[] bytes,CancellationToken ct)=>await Http.UploadPart(url,bytes,ct);
 public override async Task<PublishResult> CheckPublishStatus(PublicationJob j,PublishingTarget t,CancellationToken ct) {
 if(j.ExternalId!=null) return new(j.State,j.ExternalId,Detail:"ID pochodzi z odpowiedzi tworzenia; odczyt profilu wymaga r_member_social.");
 if(j.State==JobState.Processing&&j.OperationId?.StartsWith("urn:li:video:")==true) {
 var r=(await Http.Send(HttpMethod.Get,"https://api.linkedin.com/rest/videos/"+ProviderHttp.Escape(j.OperationId),await OAuth.TargetToken(t),headers:Headers,ct:ct)).Json;
 var status=ProviderHttp.Optional(r,"status");
 return new(status=="AVAILABLE"?JobState.Failed:status=="PROCESSING_FAILED"?JobState.Failed:JobState.Processing,OperationId:j.OperationId,Detail:status=="AVAILABLE"?"Film gotowy, ale post nie był utworzony. Można ręcznie ponowić wariant.":"Status filmu: "+status,SafeToRetry:status=="AVAILABLE");
 }
 return await base.CheckPublishStatus(j,t,ct);
 }
}
public class FacebookPublisher(ProviderHttp h,OAuthService o,IMediaStorage s):SocialPublisher(h,o,s) {
 public override Platform Platform=>Platform.Facebook;
 private string Graph=>$"https://graph.facebook.com/{OAuth.Setting(Platform,"Version")}/";
 public override async Task<PublishResult> PublishPost(PublishContext c,CancellationToken ct) {
 var token=await OAuth.TargetToken(c.Target);var id=ProviderHttp.Escape(c.Target.ExternalId);
 if(c.Media.Count==0) return Confirm(await Http.Send(HttpMethod.Post,Graph+id+"/feed",token,form:new(){{"message",c.Post.Text}},ct:ct));
 if(c.Media[0].ContentType=="video/mp4") {
 var form=new MultipartFormDataContent();form.Add(await FileContent(c.Media[0],ct),"source","video.mp4");form.Add(new StringContent(c.Post.Text),"description");
 var r=await Http.Send(HttpMethod.Post,Graph+id+"/videos",token,content:form,ct:ct);
 return new(JobState.Processing,OperationId:ProviderHttp.Required(r.Json,"id"),Detail:"Film przyjęty, oczekuje potwierdzenia statusu.",HttpStatus:r.Status);
 }
 var ids=new List<string>();
 foreach(var m in c.Media) {
 var form=new MultipartFormDataContent();form.Add(await FileContent(m,ct),"source","image"+(m.ContentType=="image/png"?".png":".jpg"));form.Add(new StringContent("false"),"published");
 var r=await Http.Send(HttpMethod.Post,Graph+id+"/photos",token,content:form,ct:ct);ids.Add(ProviderHttp.Required(r.Json,"id"));await c.Checkpoint(ids.Last());
 }
 var fields=new Dictionary<string,string>{{"message",c.Post.Text},{"attached_media",JsonSerializer.Serialize(ids.Select(x=>new{media_fbid=x}))}};
 return Confirm(await Http.Send(HttpMethod.Post,Graph+id+"/feed",token,form:fields,ct:ct));
 }
 public override async Task<PublishResult> CheckPublishStatus(PublicationJob j,PublishingTarget t,CancellationToken ct) {
 var token=await OAuth.TargetToken(t);
 if(j.ExternalId!=null) {
 var r=(await Http.Send(HttpMethod.Get,Graph+ProviderHttp.Escape(j.ExternalId)+"?fields=id,permalink_url",token,ct:ct)).Json;
 return new(JobState.Published,ProviderHttp.Required(r,"id"),Url:ProviderHttp.Optional(r,"permalink_url"));
 }
 if(j.State==JobState.Processing&&j.OperationId!=null) {
 var r=(await Http.Send(HttpMethod.Get,Graph+ProviderHttp.Escape(j.OperationId)+"?fields=status,published,permalink_url",token,ct:ct)).Json;
 var ready=r.TryGetProperty("status",out var status)&&ProviderHttp.Optional(status,"video_status")=="ready";
 var published=r.TryGetProperty("published",out var p)&&p.ValueKind==JsonValueKind.True;
 return ready&&published?new(JobState.Published,j.OperationId,Url:ProviderHttp.Optional(r,"permalink_url")):new(JobState.Processing,OperationId:j.OperationId,Detail:"Brak jednoczesnego potwierdzenia ready i published.");
 }
 return await base.CheckPublishStatus(j,t,ct);
 }
}
public class InstagramPublisher(ProviderHttp h,OAuthService o,IMediaStorage s):SocialPublisher(h,o,s) {
 public override Platform Platform=>Platform.Instagram;
 private string Graph(PublishingTarget target)=>OAuth.InstagramGraphFor(target);
 public override async Task<PublishResult> PublishPost(PublishContext c,CancellationToken ct) {
 var token=await OAuth.TargetToken(c.Target);var path=Graph(c.Target)+ProviderHttp.Escape(c.Target.ExternalId);var children=new List<string>();
 foreach(var m in c.Media) {
 var url=m.PublicUrl??throw new InvalidOperationException("Plik nie został udostępniony do pobrania.");
 var fields=new Dictionary<string,string>();
 if(m.ContentType=="video/mp4"){fields["media_type"]="REELS";fields["video_url"]=url;}
 else fields["image_url"]=url;
 if(c.Media.Count>1) fields["is_carousel_item"]="true";else fields["caption"]=c.Post.Text;
 var id=ProviderHttp.Required((await Http.Send(HttpMethod.Post,path+"/media",token,form:fields,ct:ct)).Json,"id");
 children.Add(id);await c.Checkpoint(id);
 if(!await WaitReady(id,token,c.Target,ct)) return new(JobState.Processing,OperationId:id,Detail:"Kontener przetwarzany; post nie został wysłany. Kontrola statusu nie publikuje automatycznie.");
 }
 var container=children[0];
 if(children.Count>1) {
 container=ProviderHttp.Required((await Http.Send(HttpMethod.Post,path+"/media",token,form:new(){{"media_type","CAROUSEL"},{"children",string.Join(",",children)},{"caption",c.Post.Text}},ct:ct)).Json,"id");
 await c.Checkpoint(container);
 if(!await WaitReady(container,token,c.Target,ct)) return new(JobState.Processing,OperationId:container,Detail:"Karuzela przetwarzana, jeszcze nie opublikowana.");
 }
 var r=await Http.Send(HttpMethod.Post,path+"/media_publish",token,form:new(){{"creation_id",container}},ct:ct);
 return Confirm(r) with {OperationId=container};
 }
 private async Task<bool> WaitReady(string id,string token,PublishingTarget target,CancellationToken ct) {
 // Video processing commonly outlasts the former six checks (~18 seconds).
 var watch=System.Diagnostics.Stopwatch.StartNew();
 for(var i=0;i<36&&watch.Elapsed<TimeSpan.FromMinutes(3);i++) {
 var state=ProviderHttp.Optional((await Http.Send(HttpMethod.Get,Graph(target)+ProviderHttp.Escape(id)+"?fields=status_code",token,ct:ct)).Json,"status_code");
 if(state=="FINISHED") return true;
 if(state is "ERROR" or "EXPIRED") throw new ProviderException(new(JobState.Failed,OperationId:id,Detail:"Kontener odrzucony lub wygasły.",SafeToRetry:true));
 if(i<35)await PauseProcessing(ct);
 }return false;
 }
 protected virtual Task PauseProcessing(CancellationToken ct)=>Task.Delay(TimeSpan.FromSeconds(5),ct);
 public override async Task<PublishResult> CheckPublishStatus(PublicationJob j,PublishingTarget t,CancellationToken ct) {
 var token=await OAuth.TargetToken(t);
 if(j.ExternalId!=null) {
 var r=(await Http.Send(HttpMethod.Get,Graph(t)+ProviderHttp.Escape(j.ExternalId)+"?fields=id,permalink",token,ct:ct)).Json;
 return new(JobState.Published,ProviderHttp.Required(r,"id"),j.OperationId,ProviderHttp.Optional(r,"permalink"));
 }
 if(j.OperationId!=null) {
 var state=ProviderHttp.Optional((await Http.Send(HttpMethod.Get,Graph(t)+ProviderHttp.Escape(j.OperationId)+"?fields=status_code",token,ct:ct)).Json,"status_code");
 // Unknown after final POST must never become retryable merely because a container is FINISHED.
 var unpublished=j.State==JobState.Processing||(j.State==JobState.Failed&&j.SafeToRetry);
 var retry=unpublished&&state is "FINISHED" or "ERROR" or "EXPIRED";
 var detail=retry?(state=="FINISHED"?"Instagram przygotował kontener, ale wcześniejsze oczekiwanie zakończyło się przed publikacją. Możesz ponownie zatwierdzić publikację tego wariantu; pozostałe opublikowane platformy zostaną pominięte.":"Kontener Instagrama odrzucony lub wygasły przed publikacją. Popraw materiał i ponownie zatwierdź wariant."):state=="IN_PROGRESS"&&unpublished?"Instagram nadal przetwarza kontener. Post nie został opublikowany; sprawdź status później.":"Kontener: "+state+". Brak potwierdzonego ID posta; wynik niepewny, bez automatycznego ponawiania.";
 return new(retry?JobState.Failed:state=="PUBLISHED"?JobState.Unknown:j.State,OperationId:j.OperationId,Detail:detail,SafeToRetry:retry);
 }return await base.CheckPublishStatus(j,t,ct);
 }
}
public class TikTokPublisher(ProviderHttp h,OAuthService o,IMediaStorage s,IConfiguration config):SocialPublisher(h,o,s) {
 public override Platform Platform=>Platform.TikTok;
 public async Task<JsonElement> Creator(PublishingTarget t,CancellationToken ct)=>(await Http.Send(HttpMethod.Post,"https://open.tiktokapis.com/v2/post/publish/creator_info/query/",await OAuth.TargetToken(t),new{},ct:ct)).Json.GetProperty("data");
 public override async Task<PublishResult> PublishPost(PublishContext c,CancellationToken ct) {
 if(!config.GetValue<bool>("Providers:TikTok:PublishingApproved")) return new(JobState.RequiresApproval,Detail:"Brak potwierdzenia dostępu video.publish w konfiguracji.",SafeToRetry:true);
 using var document=JsonDocument.Parse(c.Post.SettingsJson);var settings=document.RootElement;
 var errors=TikTokRules.Settings(c.Post.SettingsJson);
 if(errors.Count>0)return new(JobState.RequiresApproval,Detail:string.Join(" ",errors),SafeToRetry:true);
 var creator=await Creator(c.Target,ct);
 errors=TikTokRules.Creator(creator,c.Post.SettingsJson,c.Media);
 if(errors.Count>0)return new(JobState.Failed,Detail:string.Join(" ",errors),SafeToRetry:true);
 var m=c.Media.Single();
 if(!TikTokRules.VerifiedUrl(m.PublicUrl,config["Providers:TikTok:VerifiedUrlPrefix"]))return new(JobState.RequiresApproval,Detail:"TikTok: adres materiału nie należy do skonfigurowanego zweryfikowanego prefiksu HTTPS.",SafeToRetry:true);
 var token=await OAuth.TargetToken(c.Target);
 var r=(await Http.Send(HttpMethod.Post,"https://open.tiktokapis.com/v2/post/publish/video/init/",token,new{post_info=new{title=c.Post.Text,privacy_level="SELF_ONLY",disable_duet=!TikTokRules.Flag(settings,"duet"),disable_stitch=!TikTokRules.Flag(settings,"stitch"),disable_comment=!TikTokRules.Flag(settings,"comment"),brand_content_toggle=false,brand_organic_toggle=TikTokRules.Flag(settings,"ownBrand"),is_aigc=TikTokRules.Flag(settings,"ai")},source_info=new{source="PULL_FROM_URL",video_url=m.PublicUrl}},ct:ct)).Json.GetProperty("data");
 var id=ProviderHttp.Required(r,"publish_id");await c.Checkpoint(id);
 return new(JobState.Processing,OperationId:id,Detail:"TikTok przyjął zlecenie pobrania filmu. Sprawdź status; nie ponawiaj wysyłki.");
 }
 public override async Task<PublishResult> CheckPublishStatus(PublicationJob j,PublishingTarget t,CancellationToken ct) {
 if(j.OperationId==null) return await base.CheckPublishStatus(j,t,ct);
 var r=(await Http.Send(HttpMethod.Post,"https://open.tiktokapis.com/v2/post/publish/status/fetch/",await OAuth.TargetToken(t),new{publish_id=j.OperationId},ct:ct)).Json.GetProperty("data");
 var state=ProviderHttp.Optional(r,"status");
 string? id=null;
 if(r.TryGetProperty("publicaly_available_post_id",out var ids)&&ids.ValueKind==JsonValueKind.Array) id=ids.EnumerateArray().FirstOrDefault().ToString();
 var result=state switch{"PUBLISH_COMPLETE"=>JobState.Published,"FAILED"=>JobState.Failed,"PROCESSING_UPLOAD" or "PROCESSING_DOWNLOAD"=>JobState.Processing,_=>JobState.Unknown};
 var reason=ProviderHttp.Optional(r,"fail_reason");
 var safeReason=reason!=null&&System.Text.RegularExpressions.Regex.IsMatch(reason,"^[a-z_]{1,100}$")?reason:null;
 return new(result,ExternalId:result==JobState.Published&&!string.IsNullOrEmpty(id)?id:null,OperationId:j.OperationId,Detail:result==JobState.Published?"TikTok potwierdził PUBLISH_COMPLETE. Prywatny post może nie zwracać publicznego ID.":result==JobState.Failed?"TikTok odrzucił publikację. Kod: "+(safeReason??"nieudostępniony") :result==JobState.Processing?"TikTok przetwarza film; sprawdź status później.":"TikTok zwrócił nieznany stan; nie ponawiaj automatycznie.",ProviderCode:safeReason,SafeToRetry:result==JobState.Failed);
 }
}
