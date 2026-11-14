using System.Diagnostics;
using System.Text.Json;
using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.EntityFrameworkCore;
namespace Publicate;
public interface IMediaStorage {
 Task Put(string key,Stream data,string contentType,CancellationToken ct);
 Task<Stream> Read(string key,CancellationToken ct);
 Task<string> Expose(MediaAsset asset,CancellationToken ct);
 Task Delete(MediaAsset asset,CancellationToken ct);
}
public class LocalMediaStorage(IConfiguration config):IMediaStorage {
 public string Root=>Path.GetFullPath(config["Storage:Root"]??"../../.data/media");
 private string PathFor(string key) {
 if(!System.Text.RegularExpressions.Regex.IsMatch(key,"^[a-f0-9]{32}\\.(jpg|png|mp4)$")) throw new InvalidOperationException("Nieprawidłowy klucz pliku.");
 Directory.CreateDirectory(Root);return Path.Combine(Root,key);
 }
 public async Task Put(string key,Stream data,string contentType,CancellationToken ct) { await using var f=File.Create(PathFor(key));await data.CopyToAsync(f,ct); }
 public Task<Stream> Read(string key,CancellationToken ct)=>Task.FromResult<Stream>(File.OpenRead(PathFor(key)));
 public Task<string> Expose(MediaAsset asset,CancellationToken ct) {
 var b=config["Storage:PublicBaseUrl"]??"";
 if(!Uri.TryCreate(b,UriKind.Absolute,out var u)||u.Scheme!="https"||u.IsLoopback) throw new InvalidOperationException("Ustaw publiczny HTTPS Storage:PublicBaseUrl dla /media. Nie wystarczy localhost.");
 return Task.FromResult(b.TrimEnd('/')+"/media/"+asset.Key);
 }
 public Task Delete(MediaAsset asset,CancellationToken ct) { File.Delete(PathFor(asset.Key));return Task.CompletedTask; }
}
public class R2MediaStorage:IMediaStorage,IDisposable {
 private readonly AmazonS3Client client; private readonly IConfiguration config;
 public R2MediaStorage(IConfiguration c) {
 config=c;
 if(!c.GetValue<bool>("Storage:R2:AllowOperations"))throw new InvalidOperationException("R2: ustaw AllowOperations dopiero po zaakceptowaniu kosztów usługi.");
 var id=c["Storage:R2:AccountId"]??"";
 if(!System.Text.RegularExpressions.Regex.IsMatch(id,"^[a-f0-9]{32}$")) throw new InvalidOperationException("Brak prawidłowego AccountId R2.");
 client=new(c["Storage:R2:AccessKeyId"],c["Storage:R2:SecretAccessKey"],new AmazonS3Config{ServiceURL=$"https://{id}.r2.cloudflarestorage.com",ForcePathStyle=true,AuthenticationRegion="auto"});
 }
 private string Private=>config["Storage:R2:PrivateBucket"]??throw new InvalidOperationException("PrivateBucket wymagany");
 private string Public=>config["Storage:R2:PublicBucket"]??throw new InvalidOperationException("PublicBucket wymagany");
 public async Task Put(string key,Stream data,string contentType,CancellationToken ct) {
 await client.PutObjectAsync(new PutObjectRequest{BucketName=Private,Key=key,InputStream=data,ContentType=contentType,DisablePayloadSigning=true,DisableDefaultChecksumValidation=true,AutoCloseStream=false},ct);
 }
 public async Task<Stream> Read(string key,CancellationToken ct) {
 using var response=await client.GetObjectAsync(Private,key,ct);
 // Bounded by PoC upload limit; avoid holding SDK response beyond its lifetime.
 var stream=new MemoryStream();await response.ResponseStream.CopyToAsync(stream,ct);stream.Position=0;return stream;
 }
 public async Task<string> Expose(MediaAsset asset,CancellationToken ct) {
 if(Private==Public) throw new InvalidOperationException("Prywatny i publiczny bucket muszą być różne.");
 var b=config["Storage:R2:PublicBaseUrl"]??"";
 if(!Uri.TryCreate(b,UriKind.Absolute,out var u)||u.Scheme!="https"||u.IsLoopback) throw new InvalidOperationException("Wymagana publiczna domena HTTPS R2.");
 await client.CopyObjectAsync(new CopyObjectRequest{SourceBucket=Private,SourceKey=asset.Key,DestinationBucket=Public,DestinationKey=asset.Key},ct);
 return b.TrimEnd('/')+"/"+asset.Key;
 }
 public async Task Delete(MediaAsset a,CancellationToken ct) { await client.DeleteObjectAsync(Public,a.Key,ct);await client.DeleteObjectAsync(Private,a.Key,ct); }
 public void Dispose()=>client.Dispose();
}
public class MediaInspector(IConfiguration config) {
 public async Task Inspect(string path,MediaAsset a,CancellationToken ct) {
 var signature=new byte[12];await using(var file=File.OpenRead(path)){if(await file.ReadAsync(signature,ct)<12)throw new InvalidOperationException("Plik zbyt krótki.");}
 var matches=a.ContentType switch {"image/jpeg"=>signature[0]==255&&signature[1]==216&&signature[2]==255,"image/png"=>signature.AsSpan(0,8).SequenceEqual(new byte[]{137,80,78,71,13,10,26,10}),"video/mp4"=>System.Text.Encoding.ASCII.GetString(signature,4,4)=="ftyp",_=>false};
 if(!matches)throw new InvalidOperationException("Sygnatura pliku nie odpowiada rozszerzeniu.");
 var start=new ProcessStartInfo(config["Media:FfprobePath"]??"ffprobe"){RedirectStandardOutput=true,RedirectStandardError=true,UseShellExecute=false,CreateNoWindow=true};
 foreach(var arg in new[]{"-v","error","-protocol_whitelist","file,pipe","-show_streams","-show_format","-of","json",path}) start.ArgumentList.Add(arg);
 using var process=Process.Start(start)??throw new InvalidOperationException("Nie można uruchomić ffprobe.");
 using var timeout=CancellationTokenSource.CreateLinkedTokenSource(ct);timeout.CancelAfter(TimeSpan.FromSeconds(20));
 try {
 var output=process.StandardOutput.ReadToEndAsync(timeout.Token);var error=process.StandardError.ReadToEndAsync(timeout.Token);
 await process.WaitForExitAsync(timeout.Token);await error;
 if(process.ExitCode!=0) throw new InvalidOperationException("Nieprawidłowy plik multimedialny.");
 using var doc=JsonDocument.Parse(await output);
 ApplyProbe(doc.RootElement,a);
 } finally { if(!process.HasExited) process.Kill(true); }
 }
 public static void ApplyProbe(JsonElement probe,MediaAsset a){
 var streams=probe.GetProperty("streams").EnumerateArray().ToArray();
 var video=streams.FirstOrDefault(x=>ProviderHttp.Optional(x,"codec_type")=="video"&&!(x.TryGetProperty("disposition",out var disposition)&&disposition.TryGetProperty("attached_pic",out var attached)&&attached.GetInt32()==1));
 if(video.ValueKind==JsonValueKind.Undefined) throw new InvalidOperationException("Brak obrazu w pliku.");
 a.Width=video.GetProperty("width").GetInt32();a.Height=video.GetProperty("height").GetInt32();a.Codec=ProviderHttp.Required(video,"codec_name");
 var duration=ProviderHttp.Optional(probe.GetProperty("format"),"duration");
 a.Duration=double.TryParse(duration,System.Globalization.NumberStyles.Float,System.Globalization.CultureInfo.InvariantCulture,out var d)?d:0;
 if(a.ContentType=="image/jpeg"&&a.Codec!="mjpeg"||a.ContentType=="image/png"&&a.Codec!="png") throw new InvalidOperationException($"Zawartość nie odpowiada formatowi obrazu {a.ContentType}; wykryty kodek: {a.Codec}.");
 if(a.Width<=0||a.Height<=0)throw new InvalidOperationException("Nieprawidłowe wymiary obrazu filmu lub zdjęcia.");
 if(a.ContentType=="video/mp4"){
 if(!System.Text.RegularExpressions.Regex.IsMatch(a.Codec,"^[a-zA-Z0-9_]{1,64}$")||a.Codec=="unknown"||!double.IsFinite(a.Duration)||a.Duration<=0)throw new InvalidOperationException("Nie udało się rozpoznać kodeka lub czasu trwania filmu MP4.");
 if(!(ProviderHttp.Optional(probe.GetProperty("format"),"format_name")??"").Split(',').Contains("mp4"))throw new InvalidOperationException("Wykryty kontener nie jest MP4.");
 if(streams.Any(x=>ProviderHttp.Optional(x,"codec_type")=="audio"&&ProviderHttp.Optional(x,"codec_name")!="aac"))throw new InvalidOperationException("PUBLICATE obecnie przyjmuje w MP4 dźwięk AAC lub film bez dźwięku. To ograniczenie aplikacji, nie wspólny wymóg wszystkich API.");
 }
 }
}
public static class PostValidator {
 public static List<string> Validate(Platform p,ResolvedPost post,IReadOnlyList<MediaAsset> media) {
 var issues=new List<string>();var cap=Capabilities.Get(p);var rule=PublicationRules.Get(p);
 if(p is not (Platform.YouTube or Platform.X)&&post.Text.Length>cap.TextLimit) issues.Add($"Opis i CTA przekraczają limit PoC {cap.TextLimit} znaków.");
 if(post.MediaIds.Length!=media.Count||post.MediaIds.Distinct().Count()!=post.MediaIds.Length||media.Any(x=>x.DeletedAt!=null)) issues.Add("Multimedia brakujące, usunięte lub powtórzone.");
 if(media.Count==0&&(!cap.Text||string.IsNullOrWhiteSpace(post.Text))) issues.Add("Wymagane multimedia lub niepusty tekst dla tej platformy.");
 var videos=media.Count(x=>x.ContentType=="video/mp4");
 if(videos>0&&(!cap.Video||videos!=1||media.Count!=1)) issues.Add("PoC obsługuje jeden film bez zdjęć w tym wariancie.");
 if(videos==0&&media.Count>cap.Images) issues.Add($"Maksimum zdjęć w PoC: {cap.Images}.");
 foreach(var m in media) {
 if(m.Bytes<=0||m.Bytes>PublicationRules.MaxFileBytes) issues.Add("Limit pojedynczego pliku PoC: 64 MiB.");
 if(m.Width<=0||m.Height<=0) issues.Add("Nie udało się ustalić wymiarów.");
 if(m.ContentType is not ("image/jpeg" or "image/png" or "video/mp4")) issues.Add("Obsługiwane JPEG, PNG oraz MP4; kodek wideo sprawdzamy dla wybranej platformy.");
 if(m.ContentType.StartsWith("image/")&&m.Bytes>rule.MaxImageBytes) issues.Add($"{p}: limit obrazu w PUBLICATE wynosi {rule.MaxImageBytes} bajtów.");
 if(p==Platform.LinkedIn&&m.ContentType.StartsWith("image/")&&(long)m.Width*m.Height>=36152320) issues.Add("LinkedIn: obraz musi mieć mniej niż 36 152 320 pikseli.");
 if(p==Platform.Instagram&&m.ContentType.StartsWith("image/")) {
 if(m.ContentType!="image/jpeg") issues.Add("Instagram: wymagany JPEG.");
 var ratio=(double)m.Width/Math.Max(1,m.Height);
 if(ratio<0.8||ratio>1.91) issues.Add("Instagram: proporcje zdjęcia muszą mieścić się w zakresie 4:5–1,91:1.");
 }
 if(m.ContentType=="video/mp4") {
 var codecIssue=VideoCodecRules.Validate(p,m.Codec);if(codecIssue!=null)issues.Add(codecIssue);
 if(!double.IsFinite(m.Duration)||m.Duration<rule.VideoMinSeconds||(rule.VideoMaxSeconds is double max&&m.Duration>max)) issues.Add($"{p}: długość filmu od {rule.VideoMinSeconds} s"+(rule.VideoMaxSeconds is double limit?$" do {limit} s.":"; maksimum zależy od konta twórcy."));
 if(p==Platform.LinkedIn&&m.Bytes<75_000) issues.Add("LinkedIn Videos API: plik musi mieć co najmniej 75 kB.");
 if(p==Platform.TikTok&&(m.Width<360||m.Height<360||m.Width>4096||m.Height>4096)) issues.Add("TikTok: szerokość i wysokość filmu muszą mieścić się w zakresie 360–4096 px.");
 if(p==Platform.Instagram&&(m.Width>1920||(double)m.Width/Math.Max(1,m.Height)<0.01||(double)m.Width/Math.Max(1,m.Height)>10)) issues.Add("Instagram Reel: szerokość do 1920 px, proporcje 0,01:1–10:1; 9:16 jest zaleceniem.");
 }
 }
 if(p==Platform.X&&!string.IsNullOrEmpty(post.Text)){var text=XText.Analyze(post.Text);if(!text.Valid)issues.Add($"X: nieprawidłowy tekst lub przekroczony limit 280 znaków ważonych (obecnie {text.Length}).");}
 if(p==Platform.YouTube)issues.AddRange(YouTubeRules.Validate(post,media));
 return issues.Distinct().ToList();
 }
}
public static class RetentionPolicy {
 public static bool IsPending(JobState state)=>state is JobState.Draft or JobState.Validating or JobState.Ready or JobState.Uploading or JobState.Processing or JobState.Publishing or JobState.Unknown;
 public static bool CanDelete(MediaAsset a,IEnumerable<PublicationJob> jobs,bool referenced,DateTimeOffset now,TimeSpan retention,TimeSpan inactive) {
 var related=jobs.Where(x=>x.MediaIds.Contains(a.Id)).ToArray();
 if(a.DeletedAt!=null||related.Any(x=>IsPending(x.State))) return false;
 if(related.Any(x=>x.CompletedAt==null||x.CompletedAt+retention>now)) return false;
 if(referenced&&a.LastUsedAt+inactive>now) return false;
 return a.LastUsedAt+retention<now&&a.DeleteWarningAt is not null&&a.DeleteWarningAt.Value.AddHours(24)<now;
 }
}
public class CleanupWorker(IServiceScopeFactory scopes,IConfiguration config,ILogger<CleanupWorker> log):BackgroundService {
 protected override async Task ExecuteAsync(CancellationToken ct) {
 while(!ct.IsCancellationRequested) {
 try { using var scope=scopes.CreateScope();var gate=scope.ServiceProvider.GetRequiredService<OperationGate>().Mutex;await gate.WaitAsync(ct);try{await Clean(scope.ServiceProvider,ct);}finally{gate.Release();} }
 catch(OperationCanceledException) when(ct.IsCancellationRequested) { break; }
 catch(Exception) { log.LogWarning("Czyszczenie plików nie zakończyło się; sprawdź dostęp do bazy i magazynu."); }
 await Task.Delay(TimeSpan.FromHours(1),ct);
 }
 }
 public async Task Clean(IServiceProvider services,CancellationToken ct) {
 var db=services.GetRequiredService<AppDb>();var storage=services.GetRequiredService<IMediaStorage>();
 var assets=await db.MediaAssets.Where(x=>x.DeletedAt==null).ToListAsync(ct);
 var jobs=await db.PublicationJobs.ToListAsync(ct);var posts=await db.Posts.ToListAsync(ct);var variants=await db.PostVariants.ToListAsync(ct);
 var retention=TimeSpan.FromHours(Math.Max(48,config.GetValue("Storage:RetentionHours",48)));
 var inactive=TimeSpan.FromDays(Math.Max(3,config.GetValue("Storage:InactiveDays",30)));var now=DateTimeOffset.UtcNow;
 foreach(var a in assets) {
 var activePosts=posts.Where(p=>DraftService.Active(p,now)).ToArray();
 var activeIds=activePosts.Select(p=>p.Id).ToHashSet();
 var referenced=activePosts.Any(p=>p.MediaIds.Contains(a.Id))||variants.Any(v=>activeIds.Contains(v.PostId)&&v.MediaOverride?.Contains(a.Id)==true);
 var related=jobs.Where(j=>j.MediaIds.Contains(a.Id)).ToArray();
 var expiredReference=posts.Any(p=>!DraftService.Active(p,now)&&(p.MediaIds.Contains(a.Id)||variants.Any(v=>v.PostId==p.Id&&v.MediaOverride?.Contains(a.Id)==true)));
 if(!referenced&&(expiredReference||a.CreatedAt.AddHours(48)<=now)&&!related.Any(j=>RetentionPolicy.IsPending(j.State)||j.CompletedAt==null||j.CompletedAt+retention>now)){
 await storage.Delete(a,ct);a.DeletedAt=now;a.PublicUrl=null;continue;
 }
 if(referenced)continue;
 if(a.DeleteWarningAt==null&&a.LastUsedAt+(referenced?inactive:retention)<now) {a.DeleteWarningAt=now;continue;}
 if(RetentionPolicy.CanDelete(a,jobs,referenced,now,retention,inactive)) {await storage.Delete(a,ct);a.DeletedAt=now;a.PublicUrl=null;}
 }
 await db.SaveChangesAsync(ct);
 var staging=Path.GetFullPath(config["Storage:StagingRoot"]??"../../.data/staging");
 if(Directory.Exists(staging)) foreach(var path in Directory.EnumerateFiles(staging)) {
 var info=new FileInfo(path);
 if((info.Attributes&FileAttributes.ReparsePoint)==0&&System.Text.RegularExpressions.Regex.IsMatch(info.Name,"^[a-f0-9]{32}\\.(jpg|jpeg|png|mp4)$")&&info.LastWriteTimeUtc<DateTime.UtcNow-retention) File.Delete(path);
 }
 }
}
