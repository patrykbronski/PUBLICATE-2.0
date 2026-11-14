using System.Diagnostics;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
namespace Publicate;
public class OperationGate { public SemaphoreSlim Mutex { get; }=new(1,1); }
public class PublicationService(AppDb db,IEnumerable<ISocialPublisher> publishers,IMediaStorage storage,IConfiguration config) {
 public async Task<PostVariant> Variant(Guid id)=>await db.PostVariants.Include(v=>v.Post).Include(v=>v.Target).ThenInclude(t=>t.Account).SingleOrDefaultAsync(v=>v.Id==id&&v.OwnerId==db.CurrentOwnerId)??throw new KeyNotFoundException();
 public async Task<List<MediaAsset>> Media(Guid[] ids) {
 var found=await db.MediaAssets.Where(m=>m.OwnerId==db.CurrentOwnerId&&ids.Contains(m.Id)&&m.DeletedAt==null&&m.Uploaded).ToListAsync();
 return ids.Where(id=>found.Any(m=>m.Id==id)).Select(id=>found.First(m=>m.Id==id)).ToList();
 }
 public async Task<List<string>> Validate(PostVariant v) {
 var p=v.Resolve();var media=await Media(p.MediaIds);var issues=PostValidator.Validate(v.Target.Platform,p,media);
 var a=v.Target.Account;
 if(a.Disconnected) issues.Add("Konto odłączone.");
 if(v.Target.Platform==Platform.Facebook && string.IsNullOrWhiteSpace(v.Target.AccessToken)) issues.Add("Brak aktualnego tokenu celu Meta. Ponownie pobierz miejsca publikacji.");
 if(v.Target.Platform==Platform.Instagram&&v.Target.Kind!=(OAuthService.IsUnified(a)?"InstagramUnified":"InstagramLogin")) issues.Add("Połącz Instagram ponownie; niezgodna metoda autoryzacji celu.");
 var scopes=a.Scopes.Split(' ',StringSplitOptions.RemoveEmptyEntries);
 var required=OAuthService.IsUnified(a)?new[]{"instagram_basic","instagram_content_publish"}:Capabilities.Get(v.Target.Platform).RequiredScopes;
 foreach(var scope in required) if(!scopes.Contains(scope)) issues.Add("Brak potwierdzonego scope: "+scope+". Odśwież autoryzację; nie zakładamy zgody na podstawie samego żądania OAuth.");
 if(v.Target.Platform==Platform.X&&media.Count>0&&!scopes.Contains("media.write"))issues.Add("X: połącz konto ponownie i nadaj media.write dla zdjęć/filmu.");
 if(a.ExpiresAt<DateTimeOffset.UtcNow&&a.RefreshToken==null) issues.Add("Autoryzacja wygasła; połącz konto ponownie.");
 if(v.Target.Platform==Platform.X&&!config.GetValue<bool>("Providers:X:AllowPaidCalls")) issues.Add("X: brak zgody na płatne wywołania.");
 if(v.Target.Platform==Platform.TikTok&&!config.GetValue<bool>("Providers:TikTok:PublishingApproved")) issues.Add("TikTok: dostęp do video.publish niepotwierdzony.");
 if(v.Target.Platform==Platform.TikTok){
 issues.AddRange(TikTokRules.Settings(p.SettingsJson));
 var storageKey=config["Storage:Provider"]=="R2"?"Storage:R2:PublicBaseUrl":"Storage:PublicBaseUrl";
 if(!TikTokRules.VerifiedUrl(config[storageKey]?.TrimEnd('/')+"/validation.mp4",config["Providers:TikTok:VerifiedUrlPrefix"]))issues.Add("TikTok: publiczny magazyn HTTPS musi należeć do zweryfikowanego prefiksu URL.");
 }
 if(v.Target.Platform==Platform.Instagram) {
 var key=config["Storage:Provider"]=="R2"?"Storage:R2:PublicBaseUrl":"Storage:PublicBaseUrl";
 if(!Uri.TryCreate(config[key],UriKind.Absolute,out var u)||u.Scheme!="https"||u.IsLoopback) issues.Add("Instagram wymaga konfiguracji publicznego magazynu HTTPS.");
 }
 return issues;
 }
 public async Task TikTokPreflight(PostVariant v){
 if(v.Target.Platform!=Platform.TikTok)return;
 using var timeout=new CancellationTokenSource(TimeSpan.FromSeconds(30));
 var adapter=(TikTokPublisher)publishers.Single(x=>x.Platform==Platform.TikTok);
 var creator=await adapter.Creator(v.Target,timeout.Token);
 var post=v.Resolve();var errors=TikTokRules.Creator(creator,post.SettingsJson,await Media(post.MediaIds));
 if(errors.Count>0)throw new InvalidOperationException(string.Join(" ",errors));
 }
 public async Task<PublicationJob> Publish(Guid id,Guid requestKey,bool confirmed,bool exposeConfirmed) {
 if(!confirmed||requestKey==Guid.Empty) throw new InvalidOperationException("Wymagane jawne potwierdzenie i requestKey.");
 var existing=await db.PublicationJobs.SingleOrDefaultAsync(j=>j.OwnerId==db.CurrentOwnerId&&j.RequestKey==requestKey);
 if(existing!=null) return existing;
 var v=await Variant(id);
 if(!DraftService.Active(v.Post,DateTimeOffset.UtcNow))throw new InvalidOperationException("Szkic wygasł lub został zakończony.");
 var previous=await db.PublicationJobs.Where(j=>j.OwnerId==db.CurrentOwnerId&&j.VariantId==id).OrderByDescending(j=>j.CreatedAt).FirstOrDefaultAsync();
 if(previous!=null&&(RetentionPolicy.IsPending(previous.State)||previous.State==JobState.Published||!previous.SafeToRetry)) throw new InvalidOperationException("Ten wariant ma istniejącą publikację lub niepewny wynik. Najpierw sprawdź status. Nowy celowy post utwórz osobno.");
 var issues=await Validate(v);if(issues.Count>0) throw new InvalidOperationException(string.Join(" ",issues));
 if(v.Target.Platform is Platform.Instagram or Platform.TikTok&&!exposeConfirmed) throw new InvalidOperationException("Potwierdź tymczasowe publiczne udostępnienie plików.");
 await TikTokPreflight(v);
 var post=v.Resolve();var media=await Media(post.MediaIds);
 var j=new PublicationJob{VariantId=id,Platform=v.Target.Platform,TargetName=v.Target.Name,RequestKey=requestKey,SnapshotJson=JsonSerializer.Serialize(post),MediaIds=post.MediaIds,State=JobState.Uploading};
 db.Add(j);
 foreach(var m in media){m.LastUsedAt=DateTimeOffset.UtcNow;m.DeleteWarningAt=null;}
 await db.SaveChangesAsync();
 var watch=Stopwatch.StartNew();PublishResult result;
 using var timeout=new CancellationTokenSource(TimeSpan.FromMinutes(5));
 try {
 if(j.Platform is Platform.Instagram or Platform.TikTok) {foreach(var m in media) m.PublicUrl=await storage.Expose(m,timeout.Token);await db.SaveChangesAsync();}
 j.State=JobState.Publishing;await db.SaveChangesAsync();
 var adapter=publishers.Single(x=>x.Platform==j.Platform);
 result=await adapter.PublishPost(new(v.Target,post,media,async op=>{j.OperationId=op;await db.SaveChangesAsync();}),timeout.Token);
 } catch(ProviderException e) {result=e.Result;}
 catch(OperationCanceledException) {result=new(JobState.Unknown,Detail:"Przekroczono czas. Nie wiadomo, czy dostawca utworzył post; bez automatycznego retry.");}
 catch(HttpRequestException) {result=new(JobState.Unknown,Detail:"Przerwane połączenie. Sprawdź konto i status przed kolejną próbą.");}
 catch(Exception) {result=new(JobState.Unknown,Detail:"Błąd aplikacji podczas operacji; brak pewności co do wyniku. Nie ponawiaj automatycznie.");}
 Apply(j,result);
 db.Add(new PublicationAttempt{JobId=j.Id,State=result.State,HttpStatus=result.HttpStatus,ProviderCode=result.ProviderCode,DurationMs=watch.ElapsedMilliseconds,ExternalId=result.ExternalId,Detail=result.Detail,SafeToRetry=result.SafeToRetry});
 await db.SaveChangesAsync();return j;
 }
 public async Task<PublicationJob> Check(Guid id) {
 var j=await db.PublicationJobs.SingleOrDefaultAsync(j=>j.Id==id&&j.OwnerId==db.CurrentOwnerId)??throw new KeyNotFoundException();
 // Repair the historical status-check bug only from persisted evidence, never from FINISHED alone.
 if(j.Platform==Platform.Instagram&&j.State==JobState.Failed&&!j.SafeToRetry&&j.ExternalId==null){
 var attempts=await db.PublicationAttempts.Where(a=>a.JobId==j.Id&&a.OwnerId==db.CurrentOwnerId).ToListAsync();
 if(InstagramNeverPublished(attempts))j.SafeToRetry=true;
 }
 var v=await Variant(j.VariantId);var watch=Stopwatch.StartNew();PublishResult r;
 try {using var timeout=new CancellationTokenSource(TimeSpan.FromSeconds(45));r=await publishers.Single(x=>x.Platform==j.Platform).CheckPublishStatus(j,v.Target,timeout.Token);}
 catch(ProviderException e) {r=e.Result with{State=j.State,ExternalId=j.ExternalId,OperationId=j.OperationId,Url=j.Url,SafeToRetry=j.SafeToRetry};}
 catch(Exception) {r=new(j.State,j.ExternalId,j.OperationId,j.Url,"Nie udało się odczytać statusu; zachowano poprzedni wynik.",SafeToRetry:j.SafeToRetry);}
 Apply(j,r);db.Add(new PublicationAttempt{JobId=j.Id,Operation="Status",State=r.State,HttpStatus=r.HttpStatus,ProviderCode=r.ProviderCode,DurationMs=watch.ElapsedMilliseconds,Detail=r.Detail,ExternalId=r.ExternalId,SafeToRetry=r.SafeToRetry});
 await db.SaveChangesAsync();
 if(v.Post.ComposerJson!=null){var state=DraftService.Read(v.Post);var ids=state.Variants.Select(x=>x.Id).ToArray();var published=await db.PublicationJobs.Where(x=>x.OwnerId==db.CurrentOwnerId&&ids.Contains(x.VariantId)&&x.State==JobState.Published).Select(x=>x.VariantId).Distinct().ToListAsync();if(ids.Length>0&&ids.All(published.Contains)){v.Post.Archived=true;await db.SaveChangesAsync();}}
 return j;
 }
 public static void Apply(PublicationJob j,PublishResult r) {
 j.State=r.State;j.ExternalId=r.ExternalId??j.ExternalId;j.OperationId=r.OperationId??j.OperationId;j.Url=r.Url??j.Url;j.Diagnostic=r.Detail;j.SafeToRetry=r.SafeToRetry;
 if(!RetentionPolicy.IsPending(j.State)) j.CompletedAt=DateTimeOffset.UtcNow;
 }
 public static bool InstagramNeverPublished(IReadOnlyList<PublicationAttempt> attempts){
 var publish=attempts.Where(a=>a.Operation=="Publish").ToArray();
 return publish.Length==1&&publish[0].State==JobState.Processing&&publish[0].ExternalId==null
 &&publish[0].Detail is "Kontener przetwarzany; post nie został wysłany. Kontrola statusu nie publikuje automatycznie." or "Karuzela przetwarzana, jeszcze nie opublikowana."
 &&attempts.All(a=>a.ExternalId==null&&a.State is JobState.Processing or JobState.Failed);
 }
}
