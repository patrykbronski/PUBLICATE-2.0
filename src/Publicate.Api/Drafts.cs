using System.Text.Json;
using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
namespace Publicate;

public record ComposerVariant(Guid Id,Platform Platform,Guid TargetId,string TargetName,string Description,string Cta,bool CtaEnabled,Guid[] MediaIds,string SettingsJson="{}");
public record ComposerState(int Step,string Description,string Cta,bool CtaEnabled,Guid[] MediaIds,Platform[] Platforms,ComposerVariant[] Variants);
public record SaveDraftInput(Guid Id,int Revision,ComposerState State);
public record BatchInput(int Revision,Guid RequestKey,bool Confirmed,bool ExposeConfirmed);
public record DraftValidation(Platform Platform,List<string> Errors);
public class DraftService(AppDb db,PublicationService publications) {
 public static readonly JsonSerializerOptions Json=new(JsonSerializerDefaults.Web){Converters={new System.Text.Json.Serialization.JsonStringEnumConverter()}};
 public static ComposerState Read(Post p)=>p.ComposerJson is null?new(1,p.Description,p.Cta,!string.IsNullOrEmpty(p.Cta),p.MediaIds,[],[]):JsonSerializer.Deserialize<ComposerState>(p.ComposerJson,Json)!;
 public static bool Active(Post p,DateTimeOffset now)=>!p.Archived&&p.CreatedAt.AddHours(48)>now;
 public async Task<Post> Find(Guid id,bool active=true){var p=await db.Posts.SingleOrDefaultAsync(p=>p.Id==id&&p.OwnerId==db.CurrentOwnerId)??throw new KeyNotFoundException();if(active&&!Active(p,DateTimeOffset.UtcNow))throw new InvalidOperationException("Kopia robocza wygasła lub została zakończona.");return p;}
 public async Task<Dictionary<Platform,PublishingTarget>> Targets(){
 var user=await db.Users.SingleAsync(u=>u.Id==db.CurrentOwnerId);
 var preferences=JsonSerializer.Deserialize<Dictionary<Platform,Guid>>(user.PreferredTargetsJson,Json)??[];
 if(user.PreferredFacebookTargetId is Guid fb)preferences[Platform.Facebook]=fb;
 var all=await db.PublishingTargets.Include(t=>t.Account).Where(t=>t.OwnerId==db.CurrentOwnerId&&!t.Account.Disconnected&&(t.Platform!=Platform.Facebook||t.AccessToken!=null)&&(t.Platform!=Platform.Instagram||t.Kind=="InstagramLogin"||t.Kind=="InstagramUnified")).ToListAsync();
 var result=new Dictionary<Platform,PublishingTarget>();
 foreach(var group in all.GroupBy(t=>t.Platform)){
 if(preferences.TryGetValue(group.Key,out var id)){var chosen=group.SingleOrDefault(t=>t.Id==id);if(chosen!=null)result[group.Key]=chosen;}
 else if(group.Count()==1&&group.Key!=Platform.Facebook)result[group.Key]=group.Single();
 }return result;
 }
 public async Task<bool> Locked(Guid variantId){var last=await db.PublicationJobs.Where(j=>j.OwnerId==db.CurrentOwnerId&&j.VariantId==variantId).OrderByDescending(j=>j.CreatedAt).FirstOrDefaultAsync();return last!=null&&(last.State==JobState.Published||RetentionPolicy.IsPending(last.State)||!last.SafeToRetry);}
 public async Task<ComposerState> ReadState(Post p){var state=Read(p);
 if(p.ComposerJson==null){var legacy=await db.PostVariants.Include(v=>v.Target).Where(v=>v.PostId==p.Id&&v.OwnerId==db.CurrentOwnerId).ToListAsync();if(legacy.Count>0){var chosen=legacy.GroupBy(v=>v.Target.Platform).Select(g=>g.First()).ToArray();state=state with{Step=2,Platforms=chosen.Select(v=>v.Target.Platform).ToArray(),Variants=chosen.Select(v=>new ComposerVariant(v.Id,v.Target.Platform,v.TargetId,v.Target.Name,v.DescriptionOverride??p.Description,v.CtaOverride??p.Cta,!string.IsNullOrEmpty(v.CtaOverride??p.Cta),v.MediaOverride??p.MediaIds,v.SettingsJson)).ToArray()};}}
 return state;}
 public async Task<object> View(Post p){var state=await ReadState(p);var ids=state.Variants.Select(v=>v.Id).ToArray();var jobs=await db.PublicationJobs.Where(j=>j.OwnerId==db.CurrentOwnerId&&ids.Contains(j.VariantId)).OrderByDescending(j=>j.CreatedAt).ToListAsync();return new{p.Id,p.Revision,p.CreatedAt,expiresAt=p.CreatedAt.AddHours(48),p.Archived,state,jobs};}
 public async Task<(Post Post,Guid? Evicted)> Save(SaveDraftInput input){
 var s=input.State;if(s.Step is not (1 or 2)||s.Platforms.Distinct().Count()!=s.Platforms.Length||s.Platforms.Any(p=>!Enum.IsDefined(p)))throw new InvalidOperationException("Nieprawidłowy etap lub platformy.");
 if(input.Id==Guid.Empty||s.Variants.Any(v=>v.Id==Guid.Empty)||s.Variants.Select(v=>v.Id).Distinct().Count()!=s.Variants.Length)throw new InvalidOperationException("Nieprawidłowe identyfikatory szkicu.");
 if(s.Description.Length>100000||s.Cta.Length>10000)throw new InvalidOperationException("Treść szkicu jest zbyt długa.");
 var p=await db.Posts.SingleOrDefaultAsync(p=>p.Id==input.Id&&p.OwnerId==db.CurrentOwnerId);Guid? evicted=null;
 if(p==null){if(input.Revision!=0||await db.Posts.AnyAsync(p=>p.Id==input.Id))throw new KeyNotFoundException();
 var active=await db.Posts.Where(p=>p.OwnerId==db.CurrentOwnerId&&!p.Archived&&p.CreatedAt>DateTimeOffset.UtcNow.AddHours(-48)).OrderBy(p=>p.CreatedAt).ThenBy(p=>p.Id).ToListAsync();
 if(active.Count>=10){foreach(var old in active.Take(active.Count-9))old.Archived=true;evicted=active[0].Id;}
 p=new Post{Id=input.Id};db.Add(p);
 }else{if(!Active(p,DateTimeOffset.UtcNow))throw new InvalidOperationException("Szkic wygasł.");if(p.Revision!=input.Revision)throw new InvalidOperationException("Szkic zmienił się w innym oknie. Otwórz go ponownie.");}
 var previous=await ReadState(p);
 var attempted=await db.PublicationJobs.Where(j=>j.OwnerId==db.CurrentOwnerId&&previous.Variants.Select(v=>v.Id).Contains(j.VariantId)).Select(j=>j.VariantId).Distinct().ToListAsync();
 if(attempted.Any(id=>!s.Variants.Any(v=>v.Id==id)))throw new InvalidOperationException("Nie można usuwać wariantów z historią publikacji. Utwórz osobny szkic dla nowej publikacji.");
 if(s.Step==1&&previous.Step==2){var previousIds=previous.Variants.Select(v=>v.Id).ToArray();if(!await db.PublicationJobs.AnyAsync(j=>previousIds.Contains(j.VariantId)))db.PostVariants.RemoveRange(await db.PostVariants.Where(v=>v.PostId==p.Id).ToListAsync());}
 if(s.Step==1&&previous.Step==2&&await db.PublicationJobs.AnyAsync(j=>j.OwnerId==db.CurrentOwnerId&&previous.Variants.Select(v=>v.Id).Contains(j.VariantId)))throw new InvalidOperationException("Zestaw ma już próby publikacji. Nie można go resetować; utwórz nowy szkic.");
 var ids=s.MediaIds.Concat(s.Variants.SelectMany(v=>v.MediaIds)).Distinct().ToArray();
 var assets=await db.MediaAssets.Where(m=>m.OwnerId==db.CurrentOwnerId&&ids.Contains(m.Id)&&m.Uploaded&&m.DeletedAt==null).ToListAsync();if(assets.Count!=ids.Length)throw new InvalidOperationException("Brak dostępu do części materiałów.");
 if(s.Step==1&&s.Variants.Length!=0||s.Step==2&&(s.Variants.Length!=s.Platforms.Length||s.Variants.Select(v=>v.Platform).Distinct().Count()!=s.Platforms.Length||s.Variants.Any(v=>!s.Platforms.Contains(v.Platform))))throw new InvalidOperationException("Warianty nie odpowiadają wybranym platformom.");
 foreach(var v in s.Variants){
 if(v.Description.Length>100000||v.Cta.Length>10000)throw new InvalidOperationException("Wariant jest zbyt długi.");
 using var settings=JsonDocument.Parse(v.SettingsJson);if(settings.RootElement.ValueKind!=JsonValueKind.Object)throw new InvalidOperationException("Nieprawidłowe ustawienia wariantu.");
 var entity=await db.PostVariants.SingleOrDefaultAsync(x=>x.Id==v.Id);
 if(entity!=null&&(entity.OwnerId!=db.CurrentOwnerId||entity.PostId!=p.Id))throw new KeyNotFoundException();
 if(entity!=null&&await Locked(v.Id)){
 var old=previous.Variants.SingleOrDefault(x=>x.Id==v.Id);if(JsonSerializer.Serialize(old,Json)!=JsonSerializer.Serialize(v,Json))throw new InvalidOperationException("Nie można edytować opublikowanego lub niepewnego wariantu.");continue;
 }
 // Target changes are explicit in the saved snapshot and rechecked before publishing.
 if(!await db.PublishingTargets.AnyAsync(t=>t.Id==v.TargetId&&t.OwnerId==db.CurrentOwnerId&&t.Platform==v.Platform))throw new InvalidOperationException("Nieprawidłowy cel wariantu.");
 if(entity==null){entity=new PostVariant{Id=v.Id,PostId=p.Id};db.Add(entity);}
 entity.TargetId=v.TargetId;entity.DescriptionOverride=v.Description;entity.CtaOverride=v.CtaEnabled?v.Cta:"";entity.MediaOverride=v.MediaIds.ToArray();entity.SettingsJson=v.SettingsJson;
 }
 p.Description=s.Description;p.Cta=s.CtaEnabled?s.Cta:"";p.MediaIds=s.MediaIds;p.ComposerJson=JsonSerializer.Serialize(s,Json);p.Revision++;p.UpdatedAt=DateTimeOffset.UtcNow;
 foreach(var a in assets){a.LastUsedAt=DateTimeOffset.UtcNow;a.DeleteWarningAt=null;}
 await db.SaveChangesAsync();return(p,evicted);
 }
 public async Task<DraftValidation[]> Validate(ComposerState s){var current=await Targets();var result=new List<DraftValidation>();foreach(var platform in s.Platforms){
 var variant=s.Variants.SingleOrDefault(v=>v.Platform==platform);var errors=new List<string>();
 if(!current.TryGetValue(platform,out var target))errors.Add("Brak skonfigurowanego, dostępnego celu publikacji.");
 else{
 if(variant!=null&&variant.TargetId!=target.Id)errors.Add("Konto docelowe zmieniło się. Odśwież cel i potwierdź go przed publikacją.");
 var v=new PostVariant{Target=target,Post=new Post(),DescriptionOverride=variant?.Description??s.Description,CtaOverride=variant!=null?(variant.CtaEnabled?variant.Cta:""):(s.CtaEnabled?s.Cta:""),MediaOverride=variant?.MediaIds??s.MediaIds,SettingsJson=variant?.SettingsJson??"{}"};
 errors.AddRange(await publications.Validate(v));
 if(platform==Platform.TikTok){using var settings=JsonDocument.Parse(v.SettingsJson);if(!settings.RootElement.TryGetProperty("privacy",out var privacy)||privacy.GetString()!="SELF_ONLY"||!settings.RootElement.TryGetProperty("consent",out var consent)||consent.ValueKind!=JsonValueKind.True)errors.Add("TikTok wymaga wyboru SELF_ONLY i potwierdzenia Music Usage Confirmation.");}
 }
 result.Add(new(platform,errors));}return result.ToArray();}
 public static Guid RequestId(Guid batch,Guid variant)=>new(SHA256.HashData(Encoding.UTF8.GetBytes(batch.ToString()+variant.ToString())).AsSpan(0,16));
}
public static class DraftEndpoints {
 public static void MapDrafts(this RouteGroupBuilder api){
 api.MapGet("/composer/rules",()=>PlatformOrder.All.Select(PublicationRules.Get));
 api.MapGet("/composer/storage",async(AppDb db,IConfiguration config)=>new{usedBytes=await db.MediaAssets.Where(m=>m.OwnerId==db.CurrentOwnerId&&m.DeletedAt==null).SumAsync(m=>m.Bytes),limitBytes=config.GetValue<long>("Storage:UserQuotaBytes",PublicationRules.UserQuotaBytes)});
 api.MapGet("/composer/targets",async(DraftService service)=> (await service.Targets()).Values.Select(t=>new{t.Id,t.Platform,t.Name,t.AccountId}));
 api.MapPut("/composer/targets/{id:guid}",async(Guid id,AppDb db)=>{
 var t=await db.PublishingTargets.Include(t=>t.Account).SingleOrDefaultAsync(t=>t.Id==id&&t.OwnerId==db.CurrentOwnerId&&!t.Account.Disconnected&&(t.Platform!=Platform.Facebook||t.AccessToken!=null))??throw new KeyNotFoundException();
 var u=await db.Users.SingleAsync(u=>u.Id==db.CurrentOwnerId);var map=JsonSerializer.Deserialize<Dictionary<Platform,Guid>>(u.PreferredTargetsJson,DraftService.Json)??[];map[t.Platform]=id;u.PreferredTargetsJson=JsonSerializer.Serialize(map,DraftService.Json);if(t.Platform==Platform.Facebook)u.PreferredFacebookTargetId=id;await db.SaveChangesAsync();return Results.Ok();});
 api.MapGet("/drafts",async(AppDb db)=>await db.Posts.Where(p=>p.OwnerId==db.CurrentOwnerId&&!p.Archived&&p.CreatedAt>DateTimeOffset.UtcNow.AddHours(-48)).OrderByDescending(p=>p.UpdatedAt).Select(p=>new{p.Id,p.Description,p.CreatedAt,p.UpdatedAt,expiresAt=p.CreatedAt.AddHours(48)}).ToListAsync());
 api.MapGet("/drafts/{id:guid}",async(Guid id,DraftService s)=>await s.View(await s.Find(id)));
 api.MapPut("/drafts",async(SaveDraftInput input,DraftService s)=>{var saved=await s.Save(input);return new{draft=await s.View(saved.Post),evictedId=saved.Evicted};});
 api.MapDelete("/drafts/{id:guid}",async(Guid id,DraftService s,AppDb db)=>{var p=await s.Find(id);p.Archived=true;await db.SaveChangesAsync();return Results.Ok();});
 api.MapPost("/composer/validate",async(ComposerState state,DraftService s)=>await s.Validate(state));
 api.MapPost("/drafts/{id:guid}/publish",async(Guid id,BatchInput input,DraftService s,AppDb db,IServiceScopeFactory scopes,PublicationService publications)=>{
 if(!input.Confirmed||input.RequestKey==Guid.Empty)throw new InvalidOperationException("Potwierdź publikację całego zestawu.");
 var p=await s.Find(id,false);var state=DraftService.Read(p);
 var keys=state.Variants.Select(v=>DraftService.RequestId(input.RequestKey,v.Id)).ToArray();
 var replay=await db.PublicationJobs.Where(j=>j.OwnerId==db.CurrentOwnerId&&keys.Contains(j.RequestKey)).ToListAsync();
 if(replay.Count>0)return Results.Ok(new{jobs=replay,archived=p.Archived});
 if(!DraftService.Active(p,DateTimeOffset.UtcNow)||p.Revision!=input.Revision)throw new InvalidOperationException("Szkic wygasł lub zmienił się. Otwórz podsumowanie ponownie.");
 if(state.Step!=2||state.Variants.Length==0)throw new InvalidOperationException("Brak wariantów do publikacji.");
 var todo=new List<ComposerVariant>();
 foreach(var v in state.Variants){
 var last=await db.PublicationJobs.Where(j=>j.OwnerId==db.CurrentOwnerId&&j.VariantId==v.Id).OrderByDescending(j=>j.CreatedAt).FirstOrDefaultAsync();
 if(last?.State==JobState.Published)continue;
 if(await s.Locked(v.Id))throw new InvalidOperationException("Najpierw wyjaśnij stan trwającej lub niepewnej publikacji.");todo.Add(v);
 }
 if(todo.Count==0)throw new InvalidOperationException("Wszystkie warianty zostały już opublikowane.");
 var checks=await s.Validate(state with{Platforms=todo.Select(v=>v.Platform).ToArray(),Variants=todo.ToArray()});
 if(checks.Any(c=>c.Errors.Count>0))throw new InvalidOperationException(string.Join(" ",checks.SelectMany(c=>c.Errors)));
 if(todo.Any(v=>v.Platform is Platform.Instagram or Platform.TikTok)&&!input.ExposeConfirmed)throw new InvalidOperationException("Potwierdź publiczne udostępnienie materiałów na czas publikacji.");
 foreach(var v in todo.Where(v=>v.Platform==Platform.TikTok))await publications.TikTokPreflight(await publications.Variant(v.Id));
 // Independent DbContexts; the existing operation gate prevents concurrent edits/cleanup.
 var jobs=await Task.WhenAll(todo.Select(async v=>{using var scope=scopes.CreateScope();return await scope.ServiceProvider.GetRequiredService<PublicationService>().Publish(v.Id,DraftService.RequestId(input.RequestKey,v.Id),true,input.ExposeConfirmed);}));
 var all=await db.PublicationJobs.Where(j=>j.OwnerId==db.CurrentOwnerId&&state.Variants.Select(v=>v.Id).Contains(j.VariantId)).ToListAsync();
 p.Archived=state.Variants.All(v=>all.Any(j=>j.VariantId==v.Id&&j.State==JobState.Published));await db.SaveChangesAsync();return Results.Ok(new{jobs,archived=p.Archived});
 });
 }
}
