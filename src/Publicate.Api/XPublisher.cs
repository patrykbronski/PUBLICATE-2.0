using System.Text.Json;
namespace Publicate;

public class XPublisher(ProviderHttp h,OAuthService o,IMediaStorage s,IConfiguration config):SocialPublisher(h,o,s) {
 const string Root="https://api.x.com/2/";
 public override Platform Platform=>Platform.X;
 protected virtual Task Pause(TimeSpan delay,CancellationToken ct)=>Task.Delay(delay,ct);
 static JsonElement Data(ApiReply r){
 if(r.Json.TryGetProperty("errors",out var errors)&&errors.ValueKind==JsonValueKind.Array&&errors.GetArrayLength()>0)throw new ProviderException(new(JobState.Unknown,Detail:"X zwrócił błędy w odpowiedzi; brak jednoznacznego potwierdzenia.",HttpStatus:r.Status));
 return r.Json.TryGetProperty("data",out var d)?d:throw new ProviderException(new(JobState.Unknown,Detail:"X: brak data w odpowiedzi.",HttpStatus:r.Status));
 }
 static string Id(JsonElement d){var id=ProviderHttp.Required(d,"id");if(!System.Text.RegularExpressions.Regex.IsMatch(id,"^[0-9]{1,20}$"))throw new ProviderException(new(JobState.Unknown,Detail:"X: nieprawidłowy identyfikator w odpowiedzi."));return id;}
 static string? State(JsonElement d)=>d.TryGetProperty("processing_info",out var p)?ProviderHttp.Optional(p,"state"):null;
 static TimeSpan Delay(JsonElement d)=>TimeSpan.FromSeconds(d.TryGetProperty("processing_info",out var p)&&p.TryGetProperty("check_after_secs",out var n)&&n.TryGetInt32(out var v)?Math.Max(1,v):2);
 public override async Task<PublishResult> PublishPost(PublishContext c,CancellationToken ct){
 if(!config.GetValue<bool>("Providers:X:AllowPaidCalls"))return new(JobState.RequiresApproval,Detail:"Płatne API X zablokowane.",SafeToRetry:true);
 var errors=ValidatePost(c.Post,c.Media);if(errors.Count>0)return new(JobState.Failed,Detail:string.Join(" ",errors),SafeToRetry:true);
 if(c.Media.Count>0&&!c.Target.Account.Scopes.Split(' ').Contains("media.write"))return new(JobState.RequiresAuthorization,Detail:"Połącz X ponownie, aby nadać media.write dla zdjęć i filmu.",SafeToRetry:true);
 var posting=false;string? operation=null;
 try{
 var token=await OAuth.TargetToken(c.Target);var ids=new List<string>();
 foreach(var m in c.Media){
 var init=Data(await Http.Send(HttpMethod.Post,Root+"media/upload/initialize",token,new{media_type=m.ContentType,total_bytes=m.Bytes,media_category=m.ContentType=="video/mp4"?"tweet_video":"tweet_image"},ct:ct,operation:"x.media.initialize"));
 var id=Id(init);operation="x:upload:"+id;await c.Checkpoint(operation);
 await using var stream=await Storage.Read(m.Key,ct);long sent=0;var index=0;
 while(sent<m.Bytes){
 var bytes=new byte[(int)Math.Min(4*1024*1024,m.Bytes-sent)];await stream.ReadExactlyAsync(bytes,ct);
 using var body=new MultipartFormDataContent();body.Add(new StringContent((index++).ToString(System.Globalization.CultureInfo.InvariantCulture)),"segment_index");
 var part=new ByteArrayContent(bytes);part.Headers.ContentType=new("application/octet-stream");body.Add(part,"media","chunk.bin");
 var appended=await Http.Send(HttpMethod.Post,Root+"media/upload/"+id+"/append",token,content:body,ct:ct,operation:"x.media.append");
 if(appended.Json.TryGetProperty("errors",out var ae)&&ae.ValueKind==JsonValueKind.Array&&ae.GetArrayLength()>0)throw new ProviderException(new(JobState.Failed,Detail:"X odrzucił fragment materiału."));
 sent+=bytes.Length;
 }
 var data=Data(await Http.Send(HttpMethod.Post,Root+"media/upload/"+id+"/finalize",token,ct:ct,operation:"x.media.finalize"));
 operation="x:media:"+id;await c.Checkpoint(operation);
 var elapsed=System.Diagnostics.Stopwatch.StartNew();var polls=0;
 while(State(data) is "pending" or "in_progress"){
 var delay=Delay(data);if(++polls>12||elapsed.Elapsed+delay>TimeSpan.FromSeconds(120))return new(JobState.Processing,OperationId:operation,Detail:"X przetwarza materiał. Post nie został wysłany. Sprawdź status; dalsza publikacja wymaga ponownego zatwierdzenia.");
 await Pause(delay,ct);data=Data(await Http.Send(HttpMethod.Get,Root+"media/upload?command=STATUS&media_id="+id,token,ct:ct,operation:"x.media.status"));
 }
 if(State(data)=="failed")return new(JobState.Failed,OperationId:operation,Detail:"X odrzucił przetwarzanie materiału; post nie powstał.",SafeToRetry:true);
 if(State(data)!=null&&State(data)!="succeeded")return new(JobState.Failed,OperationId:operation,Detail:"X nie potwierdził gotowości materiału; post nie został wysłany.",SafeToRetry:true);
 ids.Add(id);
 }
 var payload=new Dictionary<string,object>();if(!string.IsNullOrWhiteSpace(c.Post.Text))payload["text"]=c.Post.Text.Normalize();if(ids.Count>0)payload["media"]=new{media_ids=ids};
 // Persist the boundary before the irreversible request; media status must never unlock an uncertain Post.
 operation="x:posting";await c.Checkpoint(operation);posting=true;
 var response=await Http.Send(HttpMethod.Post,Root+"tweets",token,payload,ct:ct,operation:"x.post.create");
 var postId=Id(Data(response));if(response.Status!=201)return new(JobState.Unknown,OperationId:operation,Detail:"X zwrócił nieoczekiwany status utworzenia posta.",HttpStatus:response.Status);
 return new(JobState.Published,ExternalId:postId,OperationId:operation,Url:"https://x.com/i/status/"+postId,HttpStatus:response.Status);
 }catch(ProviderException e) when(!posting){return e.Result with{State=e.Result.State==JobState.Unknown?JobState.Failed:e.Result.State,OperationId=operation,SafeToRetry=true};}
 catch(OperationCanceledException) when(!posting){return new(JobState.Failed,OperationId:operation,Detail:"Przerwano przygotowanie materiału X przed wysłaniem posta. Możesz ponowić po sprawdzeniu pliku.",SafeToRetry:true);}
 catch(HttpRequestException) when(!posting){return new(JobState.Failed,OperationId:operation,Detail:"Przerwano przesyłanie materiału X przed utworzeniem posta.",SafeToRetry:true);}
 }
 public override async Task<PublishResult> CheckPublishStatus(PublicationJob j,PublishingTarget t,CancellationToken ct){
 if(!config.GetValue<bool>("Providers:X:AllowPaidCalls"))return new(j.State,j.ExternalId,j.OperationId,j.Url,"Odczyt X zablokowany: brak zgody na koszt.");
 if(j.ExternalId!=null){var r=await Http.Send(HttpMethod.Get,Root+"tweets/"+ProviderHttp.Escape(j.ExternalId),await OAuth.TargetToken(t),ct:ct,operation:"x.post.status");return Id(Data(r))==j.ExternalId?new(JobState.Published,j.ExternalId,j.OperationId,"https://x.com/i/status/"+j.ExternalId,HttpStatus:r.Status):new(j.State,j.ExternalId,j.OperationId,Detail:"X nie potwierdził oczekiwanego posta.");}
 if(j.OperationId?.StartsWith("x:media:")==true&&j.State is JobState.Processing or JobState.Failed){
 var id=j.OperationId[8..];var d=Data(await Http.Send(HttpMethod.Get,Root+"media/upload?command=STATUS&media_id="+ProviderHttp.Escape(id),await OAuth.TargetToken(t),ct:ct,operation:"x.media.status"));var state=State(d);
 if(state is "succeeded" or "failed")return new(JobState.Failed,OperationId:j.OperationId,Detail:state=="succeeded"?"Materiał X gotowy, ale post nie został wysłany. Zatwierdź ponowną próbę; plik zostanie przesłany ponownie.":"X odrzucił materiał przed utworzeniem posta. Popraw plik.",SafeToRetry:true);
 return new(JobState.Processing,OperationId:j.OperationId,Detail:"Brak potwierdzenia zakończenia przetwarzania X. Post nie został wysłany.");
 }
 return await base.CheckPublishStatus(j,t,ct);
 }
}
