using System.Text.Json;
using Microsoft.EntityFrameworkCore;
namespace Publicate;

public partial class OAuthService {
 public bool InstagramUnified=>Setting(Platform.Instagram,"LoginMode")=="Unified";
 public static bool IsUnified(ConnectedAccount account)=>account.Platform==Platform.Instagram&&account.AuthMethod=="InstagramUnified";
 public string InstagramGraphFor(PublishingTarget target)=>$"https://{(IsUnified(target.Account)?"graph.facebook.com":"graph.instagram.com")}/{Setting(Platform.Instagram,"Version")}/";
 private string UnifiedGraph=>$"https://graph.facebook.com/{Setting(Platform.Instagram,"Version")}";
 private string BeginInstagramUnified(string browser) {
 var args=new Dictionary<string,string>{{"client_id",Setting(Platform.Instagram,"ClientId")},{"config_id",Setting(Platform.Instagram,"ConfigId")},{"display","popup"},{"override_default_response_type","true"},{"redirect_uri",Callback(Platform.Instagram)},{"response_type","code"},{"state",states.Add(Platform.Instagram,"Unified",browser,db.CurrentOwnerId)},{"prompt","login"}};
 return "https://www.facebook.com/dialog/oauth?"+string.Join("&",args.Select(x=>ProviderHttp.Escape(x.Key)+"="+ProviderHttp.Escape(x.Value)));
 }
 private async Task<JsonElement> InspectUnified(string access) {
 var result=(await http.Send(HttpMethod.Get,UnifiedGraph+"/debug_token?input_token="+ProviderHttp.Escape(access),Setting(Platform.Instagram,"ClientId")+"|"+Setting(Platform.Instagram,"ClientSecret"),operation:"instagram.unified.token_inspection")).Json;
 if(!result.TryGetProperty("data",out var data)||!data.TryGetProperty("is_valid",out var valid)||valid.ValueKind!=JsonValueKind.True||ProviderHttp.Optional(data,"app_id")!=Setting(Platform.Instagram,"ClientId"))
 throw new ProviderException(new(JobState.RequiresAuthorization,Detail:"Meta nie potwierdziła ważnego tokenu dla tej aplikacji."));
 return data;
 }
 private static DateTimeOffset? UnifiedExpiry(JsonElement data) {
 if(!data.TryGetProperty("expires_at",out var value)||!long.TryParse(value.ToString(),out var seconds)||seconds<0)
 throw new ProviderException(new(JobState.Unknown,Detail:"Meta nie potwierdziła ważności tokenu Unified Login."));
 if(seconds==0)return null; // Only explicit provider zero means no scheduled expiry.
 var expiry=DateTimeOffset.FromUnixTimeSeconds(seconds);
 if(expiry<=DateTimeOffset.UtcNow)throw new ProviderException(new(JobState.RequiresAuthorization,Detail:"Token Meta wygasł."));
 return expiry;
 }
 private static string UnifiedScopes(JsonElement data)=>data.TryGetProperty("scopes",out var scopes)&&scopes.ValueKind==JsonValueKind.Array?string.Join(" ",scopes.EnumerateArray().Select(x=>x.ToString())):"";
 private async Task<List<(string Id,string Name)>> UnifiedProfiles(JsonElement data,string access) {
 var profiles=new List<(string,string)>();
 string? after=null;var cursors=new HashSet<string>();
 do {
 var reply=(await http.Send(HttpMethod.Get,UnifiedGraph+"/me/assigned_instagram_accounts?fields=id,username&limit=100"+(after is null?"":"&after="+ProviderHttp.Escape(after)),access,operation:"instagram.unified.assigned_accounts")).Json;
 if(!reply.TryGetProperty("data",out var accounts)||accounts.ValueKind!=JsonValueKind.Array)throw new ProviderException(new(JobState.Unknown,Detail:"Meta nie zwróciła listy przydzielonych kont Instagram."));
 foreach(var profile in accounts.EnumerateArray())profiles.Add((ProviderHttp.Required(profile,"id"),ProviderHttp.Required(profile,"username")));
 after=reply.TryGetProperty("paging",out var paging)&&paging.TryGetProperty("next",out _)&&paging.TryGetProperty("cursors",out var cursor)?ProviderHttp.Optional(cursor,"after"):null;
 if(after is not null&&!cursors.Add(after))throw new ProviderException(new(JobState.Unknown,Detail:"Meta powtórzyła kursor listy kont."));
 }while(after is not null);
 if(profiles.Count==0)throw new ProviderException(new(JobState.RequiresApproval,Detail:"Meta nie udostępniła żadnego konta Instagram. Sprawdź konto profesjonalne, wybrane portfolio biznesowe i przydział zasobów.",SafeToRetry:true));
 profiles=profiles.DistinctBy(p=>p.Item1).ToList();
 return profiles;
 }
 private async Task CompleteInstagramUnified(string code) {
 var token=(await http.Send(HttpMethod.Post,UnifiedGraph+"/oauth/access_token",form:new(){{"client_id",Setting(Platform.Instagram,"ClientId")},{"client_secret",Setting(Platform.Instagram,"ClientSecret")},{"redirect_uri",Callback(Platform.Instagram)},{"code",code}},operation:"instagram.unified.code_exchange")).Json;
 var access=ProviderHttp.Required(token,"access_token");var data=await InspectUnified(access);
 var id=ProviderHttp.Required(data,"user_id");var expiry=UnifiedExpiry(data);
 var business=await UnifiedBusiness(access,id);var profiles=await UnifiedProfiles(data,access);
 var account=await db.ConnectedAccounts.SingleOrDefaultAsync(a=>a.OwnerId==db.CurrentOwnerId&&a.Platform==Platform.Instagram&&a.ExternalId==id&&a.AuthMethod=="InstagramUnified")
 ?? new ConnectedAccount{OwnerId=db.CurrentOwnerId,Platform=Platform.Instagram,ExternalId=id,AuthMethod="InstagramUnified"};
 if(db.Entry(account).State==EntityState.Detached) db.Add(account);
 account.ClientBusinessId=business;account.Name=string.Join(", ",profiles.Select(p=>p.Name));account.AccessToken=vault.Seal(access);account.RefreshToken=null;account.Disconnected=false;account.ExpiresAt=expiry;account.Scopes=UnifiedScopes(data);
 await SaveUnifiedTargets(account,profiles);await db.SaveChangesAsync();
 }
 private async Task SaveUnifiedTargets(ConnectedAccount account,List<(string Id,string Name)> profiles) {
 var old=await db.PublishingTargets.Where(t=>t.AccountId==account.Id).ToListAsync();
 foreach(var t in old){t.Kind="InstagramUnifiedRevoked";t.AccessToken=null;}
 foreach(var p in profiles) {
 var t=old.SingleOrDefault(t=>t.ExternalId==p.Id);
 if(t is null){t=new(){OwnerId=account.OwnerId,AccountId=account.Id,Platform=Platform.Instagram,ExternalId=p.Id};db.Add(t);}
 t.Name=p.Name;t.Kind="InstagramUnified";t.AccessToken=null;
 }
 }
 private async Task DiscoverInstagramUnified(ConnectedAccount account) {
 CheckConfig(Platform.Instagram);var access=await Access(account);var data=await InspectUnified(access);
 if(ProviderHttp.Required(data,"user_id")!=account.ExternalId)throw new ProviderException(new(JobState.RequiresAuthorization,Detail:"Token Unified Login należy do innej tożsamości."));
 account.Scopes=UnifiedScopes(data);account.ExpiresAt=UnifiedExpiry(data);
 account.ClientBusinessId=await UnifiedBusiness(access,account.ExternalId);
 await SaveUnifiedTargets(account,[]);await db.SaveChangesAsync();
 var profiles=await UnifiedProfiles(data,access);await SaveUnifiedTargets(account,profiles);await db.SaveChangesAsync();
 }
 private async Task<string> UnifiedBusiness(string access,string expectedUser) {
 var me=(await http.Send(HttpMethod.Get,UnifiedGraph+"/me?fields=client_business_id",access,operation:"instagram.unified.client_business")).Json;
 // Identity has already been verified by debug_token (is_valid, app_id, user_id).
 // /me may return only the requested business field; do not require a duplicate id.
 if(ProviderHttp.Optional(me,"id") is string returnedId&&returnedId!=expectedUser)throw new ProviderException(new(JobState.RequiresAuthorization,Detail:"Tożsamość System User różni się od tokenu."));
 return ProviderHttp.Required(me,"client_business_id");
 }
}
