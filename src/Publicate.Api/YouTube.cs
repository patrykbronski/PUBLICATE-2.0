using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
namespace Publicate;

public static class YouTubeRules {
 public const double MaxSeconds=179.99;
 public static List<string> Validate(ResolvedPost post,IReadOnlyList<MediaAsset> media){
 var errors=new List<string>();
 if(media.Count!=1||media.Any(m=>m.ContentType!="video/mp4"))errors.Add("YouTube Shorts wymaga jednego filmu MP4; zdjęcia i sam tekst nie są obsługiwane.");
 foreach(var m in media)if(m.ContentType=="video/mp4"&&(m.Width>m.Height||!double.IsFinite(m.Duration)||m.Duration<=0||m.Duration>MaxSeconds))errors.Add("YouTube Shorts: film pionowy lub kwadratowy, maksymalnie 2:59,99 (179,99 s).");
 if(Encoding.UTF8.GetByteCount(post.Text)>5000)errors.Add("YouTube: opis z CTA przekracza 5000 bajtów UTF-8.");
 if(post.Text.Contains('<')||post.Text.Contains('>'))errors.Add("YouTube: opis nie może zawierać znaków < ani >.");
 try{using var doc=JsonDocument.Parse(post.SettingsJson);var s=doc.RootElement;
 var title=s.TryGetProperty("title",out var t)&&t.ValueKind==JsonValueKind.String?t.GetString()!:"";
 if(string.IsNullOrWhiteSpace(title)||title.EnumerateRunes().Count()>100||title.Contains('<')||title.Contains('>'))errors.Add("YouTube: wpisz tytuł (1–100 znaków, bez < i >).");
 if(!s.TryGetProperty("privacy",out var privacy)||privacy.ValueKind!=JsonValueKind.String||privacy.GetString() is not ("private" or "unlisted" or "public"))errors.Add("YouTube: wybierz widoczność filmu.");
 if(!s.TryGetProperty("madeForKids",out var kids)||kids.ValueKind is not (JsonValueKind.True or JsonValueKind.False))errors.Add("YouTube: określ, czy materiał jest przeznaczony dla dzieci.");
 if(!s.TryGetProperty("containsSyntheticMedia",out var synthetic)||synthetic.ValueKind is not (JsonValueKind.True or JsonValueKind.False))errors.Add("YouTube: określ, czy film zawiera realistyczne treści syntetyczne lub zmodyfikowane.");
 }catch(Exception e) when(e is JsonException or InvalidOperationException){errors.Add("YouTube: nieprawidłowe ustawienia filmu.");}
 return errors;
 }
}

public partial class OAuthService {
 internal const string YouTubeChannels="https://www.googleapis.com/youtube/v3/channels?part=snippet&mine=true";
 private async Task<JsonElement> YouTubeChannel(string access){
 var response=(await http.Send(HttpMethod.Get,YouTubeChannels,access,operation:"youtube.channels")).Json;
 if(!response.TryGetProperty("items",out var items)||items.GetArrayLength()!=1)throw new ProviderException(new(JobState.RequiresAuthorization,Detail:"Google nie zwrócił jednego kanału. Utwórz kanał YouTube lub ponów OAuth, wybierając konkretny kanał.",SafeToRetry:true));
 return items[0];
 }
 private async Task CompleteYouTube(JsonElement tokens,string access){
 var channel=await YouTubeChannel(access);var id=ProviderHttp.Required(channel,"id");
 var a=await db.ConnectedAccounts.SingleOrDefaultAsync(a=>a.OwnerId==db.CurrentOwnerId&&a.Platform==Platform.YouTube&&a.ExternalId==id)??new ConnectedAccount{OwnerId=db.CurrentOwnerId,Platform=Platform.YouTube,ExternalId=id};
 if(db.Entry(a).State==EntityState.Detached)db.Add(a);
 a.Name=ProviderHttp.Required(channel.GetProperty("snippet"),"title");a.AuthMethod="GoogleOAuth";a.Disconnected=false;a.AccessToken=vault.Seal(access);
 if(tokens.TryGetProperty("refresh_token",out var rt)&&!string.IsNullOrWhiteSpace(rt.GetString()))a.RefreshToken=vault.Seal(rt.GetString()!);
 a.ExpiresAt=tokens.TryGetProperty("expires_in",out var exp)?DateTimeOffset.UtcNow.AddSeconds(exp.GetDouble()):null;
 a.Scopes=ProviderHttp.Optional(tokens,"scope")??"UNVERIFIED";
 await UpsertYouTube(a);await db.SaveChangesAsync();
 }
 private async Task UpsertYouTube(ConnectedAccount a){
 var t=await db.PublishingTargets.SingleOrDefaultAsync(t=>t.AccountId==a.Id&&t.ExternalId==a.ExternalId&&t.Platform==Platform.YouTube);
 if(t==null){t=new PublishingTarget{OwnerId=a.OwnerId,AccountId=a.Id,ExternalId=a.ExternalId,Platform=Platform.YouTube};db.Add(t);}
 t.Name=a.Name;t.Kind="YouTubeChannel";
 }
 private async Task DiscoverYouTube(ConnectedAccount a){var channel=await YouTubeChannel(await Access(a));if(ProviderHttp.Required(channel,"id")!=a.ExternalId)throw new ProviderException(new(JobState.RequiresAuthorization,Detail:"Token wskazuje inny kanał. Połącz kanał ponownie."));a.Name=ProviderHttp.Required(channel.GetProperty("snippet"),"title");await UpsertYouTube(a);await db.SaveChangesAsync();}
}

public class YouTubePublisher(ProviderHttp h,OAuthService o,IMediaStorage s):SocialPublisher(h,o,s){
 public override Platform Platform=>Platform.YouTube;
 public override async Task<PublishResult> PublishPost(PublishContext c,CancellationToken ct){
 var errors=ValidatePost(c.Post,c.Media);if(errors.Count>0)return new(JobState.Failed,Detail:string.Join(" ",errors),SafeToRetry:true);
 var token=await OAuth.TargetToken(c.Target);
 var channels=(await Http.Send(HttpMethod.Get,OAuthService.YouTubeChannels,token,ct:ct,operation:"youtube.channels")).Json.GetProperty("items");
 if(channels.GetArrayLength()!=1||ProviderHttp.Optional(channels[0],"id")!=c.Target.ExternalId)return new(JobState.RequiresAuthorization,Detail:"Kanał Google nie odpowiada wybranemu celowi. Połącz kanał ponownie.",SafeToRetry:true);
 using var settings=JsonDocument.Parse(c.Post.SettingsJson);var options=settings.RootElement;var media=c.Media.Single();
 var init=await Http.Send(HttpMethod.Post,"https://www.googleapis.com/upload/youtube/v3/videos?uploadType=resumable&part=snippet,status",token,
 new{snippet=new{title=options.GetProperty("title").GetString(),description=c.Post.Text,categoryId="22"},status=new{privacyStatus=options.GetProperty("privacy").GetString(),selfDeclaredMadeForKids=options.GetProperty("madeForKids").GetBoolean(),containsSyntheticMedia=options.GetProperty("containsSyntheticMedia").GetBoolean()}},
 headers:new(){{"X-Upload-Content-Type","video/mp4"},{"X-Upload-Content-Length",media.Bytes.ToString(System.Globalization.CultureInfo.InvariantCulture)}},ct:ct,operation:"youtube.upload_init");
 if(init.Status!=200||string.IsNullOrWhiteSpace(init.Location))return new(JobState.Unknown,Detail:"YouTube nie potwierdził sesji uploadu; nie wysłano filmu.");
 // Session URL contains an upload credential: never persist it or expose it in diagnostics.
 ProviderHttp.ValidateUploadUrl(init.Location,"www.googleapis.com");
 var uploaded=await Http.UploadYouTube(init.Location,token,media,Storage,ct);
 var id=ProviderHttp.Optional(uploaded.Json,"id");
 if(uploaded.Status is not (200 or 201)||string.IsNullOrWhiteSpace(id))return new(JobState.Unknown,Detail:"Upload bez potwierdzonego ID filmu. Nie ponawiaj automatycznie.");
 await c.Checkpoint(id);
 return new(JobState.Processing,OperationId:id,Detail:"YouTube odebrał film. Sprawdź status przetwarzania; upload nie oznacza jeszcze publikacji.");
 }
 public override async Task<PublishResult> CheckPublishStatus(PublicationJob j,PublishingTarget t,CancellationToken ct){
 var id=j.ExternalId??j.OperationId;if(id==null)return new(JobState.Unknown,Detail:"Brak ID filmu; sprawdź kanał przed ponowieniem.");
 var root=(await Http.Send(HttpMethod.Get,"https://www.googleapis.com/youtube/v3/videos?part=status,processingDetails,snippet&id="+ProviderHttp.Escape(id),await OAuth.TargetToken(t),ct:ct,operation:"youtube.status")).Json;
 if(!root.TryGetProperty("items",out var items)||items.GetArrayLength()!=1)return new(JobState.Unknown,OperationId:id,Detail:"YouTube nie zwrócił filmu. Nie ponawiaj uploadu automatycznie.");
 var video=items[0];if(ProviderHttp.Optional(video,"id")!=id||ProviderHttp.Optional(video.GetProperty("snippet"),"channelId")!=t.ExternalId)return new(JobState.Unknown,OperationId:id,Detail:"Nie potwierdzono kanału filmu.");
 var status=video.GetProperty("status");var state=ProviderHttp.Optional(status,"uploadStatus");
 if(state is "failed" or "rejected" or "deleted")return new(JobState.Failed,OperationId:id,Detail:"YouTube odrzucił lub usunął film. Sprawdź YouTube Studio; brak automatycznego ponowienia.");
 var processing=video.TryGetProperty("processingDetails",out var details)?ProviderHttp.Optional(details,"processingStatus"):null;
 if(processing is "failed" or "terminated")return new(JobState.Failed,OperationId:id,Detail:"Przetwarzanie YouTube nie zakończyło się pomyślnie.");
 if(state!="processed"||processing is not (null or "succeeded"))return new(JobState.Processing,OperationId:id,Detail:"YouTube nadal przetwarza film.");
 var snapshot=JsonSerializer.Deserialize<ResolvedPost>(j.SnapshotJson);using var settings=JsonDocument.Parse(snapshot?.SettingsJson??"{}");
 if(ProviderHttp.Optional(status,"privacyStatus")!=ProviderHttp.Optional(settings.RootElement,"privacy"))return new(JobState.Unknown,OperationId:id,Detail:"Widoczność na YouTube różni się od zatwierdzonej. Sprawdź YouTube Studio.");
 return new(JobState.Published,ExternalId:id,OperationId:id,Url:"https://www.youtube.com/watch?v="+ProviderHttp.Escape(id),Detail:"Film przetworzony; widoczność: "+ProviderHttp.Optional(status,"privacyStatus")+". Klasyfikację Shorts wykonuje YouTube.");
 }
}
