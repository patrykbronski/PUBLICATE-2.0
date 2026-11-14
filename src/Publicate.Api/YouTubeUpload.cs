using System.Net.Http.Headers;
using System.Text.RegularExpressions;
namespace Publicate;

public partial class ProviderHttp {
 // Retries only within the SAME session. Never repeat videos.insert after uncertain delivery.
 // Session credentials remain in memory; process restart deliberately requires manual reconciliation.
 public async Task<ApiReply> UploadYouTube(string session,string token,MediaAsset media,IMediaStorage storage,CancellationToken ct){
 ValidateUploadUrl(session,"www.googleapis.com");
 long offset=0;bool probe=false;TimeSpan delay=TimeSpan.Zero;
 for(var attempt=0;attempt<6;attempt++){
 if(delay>TimeSpan.Zero)await Task.Delay(delay,ct);
 try{
 using var content=probe?new ByteArrayContent([]):(HttpContent)await RemainingVideo(offset);
 if(probe){content.Headers.ContentLength=0;content.Headers.ContentRange=new ContentRangeHeaderValue(media.Bytes);}
 var reply=await Send(HttpMethod.Put,session,token,content:content,ct:ct,operation:"youtube.upload");
 if(reply.Status is 200 or 201)return reply;
 if(reply.Status!=308)throw Uncertain();
 long next=0;
 if(reply.Range!=null){var match=Regex.Match(reply.Range,"^bytes=0-([0-9]+)$");if(!match.Success||!long.TryParse(match.Groups[1].Value,out var last)||last>=media.Bytes||last<0)throw Uncertain();next=last+1;}
 if(next<offset)throw Uncertain(); // Never replay bytes that Google previously acknowledged.
 offset=next;probe=offset==media.Bytes;
 delay=reply.RetryAfter??TimeSpan.FromSeconds(Math.Pow(2,attempt));
 if(delay>TimeSpan.FromSeconds(30))throw Uncertain();
 }catch(HttpRequestException){probe=true;delay=TimeSpan.FromSeconds(Math.Pow(2,attempt));}
 catch(OperationCanceledException) when(!ct.IsCancellationRequested){probe=true;delay=TimeSpan.FromSeconds(Math.Pow(2,attempt));}
 catch(ProviderException ex) when(ex.Result.HttpStatus is 500 or 502 or 503 or 504){probe=true;delay=TimeSpan.FromSeconds(Math.Pow(2,attempt));}
 catch(ProviderException ex){throw new ProviderException(ex.Result with{SafeToRetry=false});}
 }
 throw Uncertain();

 async Task<StreamContent> RemainingVideo(long start){
 var stream=await storage.Read(media.Key,ct);
 try{
 if(stream.CanSeek)stream.Seek(start,SeekOrigin.Begin);
 else {var buffer=new byte[65536];long remaining=start;while(remaining>0){var read=await stream.ReadAsync(buffer.AsMemory(0,(int)Math.Min(buffer.Length,remaining)),ct);if(read==0)throw Uncertain();remaining-=read;}}
 var content=new StreamContent(stream);content.Headers.ContentType=new("video/mp4");content.Headers.ContentLength=media.Bytes-start;
 content.Headers.ContentRange=new(start,media.Bytes-1,media.Bytes);return content;
 }catch{await stream.DisposeAsync();throw;}
 }
 static ProviderException Uncertain()=>new(new(JobState.Unknown,Detail:"Nie potwierdzono zakończenia uploadu YouTube. Sprawdź kanał w YouTube Studio przed nową próbą; nie utworzono automatycznie kolejnej sesji."));
 }
}
