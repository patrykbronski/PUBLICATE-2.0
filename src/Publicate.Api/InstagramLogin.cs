using System.Text.Json;
using Microsoft.EntityFrameworkCore;
namespace Publicate;

public partial class OAuthService {
 private string InstagramGraph=>$"https://graph.instagram.com/{Setting(Platform.Instagram,"Version")}";
 private string BeginInstagram(string browser) {
 if(InstagramUnified) return BeginInstagramUnified(browser);
 var args=new Dictionary<string,string>{{"client_id",Setting(Platform.Instagram,"ClientId")},{"redirect_uri",Callback(Platform.Instagram)},{"response_type","code"},{"scope",Scopes(Platform.Instagram).Replace(' ', ',')},{"state",states.Add(Platform.Instagram,"",browser,db.CurrentOwnerId)}};
 return "https://www.instagram.com/oauth/authorize?"+string.Join("&",args.Select(x=>ProviderHttp.Escape(x.Key)+"="+ProviderHttp.Escape(x.Value)));
 }
 private static JsonElement InstagramToken(JsonElement response) {
 if(response.TryGetProperty("data",out var data)) {
 if(response.TryGetProperty("access_token",out _)||data.ValueKind!=JsonValueKind.Array||data.GetArrayLength()!=1)
 throw new ProviderException(new(JobState.Unknown,Detail:"Niejednoznaczna odpowiedź tokenu Instagram."));
 return data[0];
 }
 return response;
 }
 private static string InstagramGrants(JsonElement token) {
 if(token.TryGetProperty("permissions",out var permissions)&&permissions.ValueKind==JsonValueKind.Array)
 return string.Join(" ",permissions.EnumerateArray().Select(x=>x.ToString()));
 return (ProviderHttp.Optional(token,"scope")??"").Replace(',',' ');
 }
 private static DateTimeOffset InstagramExpiry(JsonElement token) {
 if(!token.TryGetProperty("expires_in",out var value)||!double.TryParse(value.ToString(),out var seconds)||seconds<=0||seconds>60*24*3600+3600)
 throw new ProviderException(new(JobState.Unknown,Detail:"Instagram nie potwierdził ważności tokenu."));
 return DateTimeOffset.UtcNow.AddSeconds(seconds);
 }
 private async Task CompleteInstagram(string code) {
 if(InstagramUnified) {await CompleteInstagramUnified(code);return;}
 var shortToken=InstagramToken((await http.Send(HttpMethod.Post,"https://api.instagram.com/oauth/access_token",form:new() {
 {"client_id",Setting(Platform.Instagram,"ClientId")},{"client_secret",Setting(Platform.Instagram,"ClientSecret")},
 {"grant_type","authorization_code"},{"redirect_uri",Callback(Platform.Instagram)},{"code",code}
 })).Json);
 var shortAccess=ProviderHttp.Required(shortToken,"access_token");
 var tokens=(await http.Send(HttpMethod.Get,"https://graph.instagram.com/access_token?grant_type=ig_exchange_token&client_secret="+ProviderHttp.Escape(Setting(Platform.Instagram,"ClientSecret"))+"&access_token="+ProviderHttp.Escape(shortAccess))).Json;
 var access=ProviderHttp.Required(tokens,"access_token");
 var expiry=InstagramExpiry(tokens);
 var profile=(await http.Send(HttpMethod.Get,InstagramGraph+"/me?fields=user_id,username",access)).Json;
 var id=ProviderHttp.Required(profile,"user_id");
 if(ProviderHttp.Optional(shortToken,"user_id") is string tokenId && tokenId!=id)
 throw new ProviderException(new(JobState.Unknown,Detail:"Instagram zwrócił niespójne identyfikatory konta."));
 var grants=InstagramGrants(shortToken);
 var account=await db.ConnectedAccounts.SingleOrDefaultAsync(x=>x.OwnerId==db.CurrentOwnerId&&x.Platform==Platform.Instagram&&x.ExternalId==id&&x.AuthMethod!="InstagramUnified")
 ?? new ConnectedAccount{OwnerId=db.CurrentOwnerId,Platform=Platform.Instagram,ExternalId=id};
 if(db.Entry(account).State==EntityState.Detached) db.Add(account);
 account.AuthMethod="InstagramLogin";account.Name=ProviderHttp.Required(profile,"username");account.AccessToken=vault.Seal(access);account.RefreshToken=null;account.ExpiresAt=expiry;account.Disconnected=false;
 account.Scopes=string.IsNullOrWhiteSpace(grants)?"UNVERIFIED:"+Scopes(Platform.Instagram):grants;
 await UpsertInstagramTarget(account);
 await db.SaveChangesAsync();
 }
 private static void RequireInstagramLogin(ConnectedAccount account) {
 if(IsUnified(account)) return;
 if(!account.Scopes.Contains("instagram_business_",StringComparison.Ordinal))
 throw new ProviderException(new(JobState.RequiresAuthorization,Detail:"Połącz Instagram ponownie przez Instagram Login. Stare połączenie przez Facebooka nie jest obsługiwane.",SafeToRetry:true));
 }
 private async Task UpsertInstagramTarget(ConnectedAccount account) {
 var target=await db.PublishingTargets.SingleOrDefaultAsync(t=>t.AccountId==account.Id&&t.Platform==Platform.Instagram&&t.ExternalId==account.ExternalId);
 if(target is null) {target=new(){OwnerId=account.OwnerId,AccountId=account.Id,Platform=Platform.Instagram,ExternalId=account.ExternalId};db.Add(target);}
 target.Kind="InstagramLogin";target.Name=account.Name;target.AccessToken=null;
 }
 private async Task DiscoverInstagram(ConnectedAccount account) {
 if(IsUnified(account)) {await DiscoverInstagramUnified(account);return;}
 var access=await Access(account);
 var profile=(await http.Send(HttpMethod.Get,InstagramGraph+"/me?fields=user_id,username",access)).Json;
 if(ProviderHttp.Required(profile,"user_id")!=account.ExternalId) throw new ProviderException(new(JobState.RequiresAuthorization,Detail:"Identyfikator Instagram nie zgadza się z podłączonym kontem."));
 account.Name=ProviderHttp.Required(profile,"username");
 await UpsertInstagramTarget(account);await db.SaveChangesAsync();
 }
 private async Task RefreshInstagram(ConnectedAccount account) {
 if(IsUnified(account)) throw new ProviderException(new(JobState.RequiresAuthorization,Detail:"Unified Login wymaga ponownego połączenia zamiast ig_refresh_token. Sprawdź ważność przez Sprawdź cele.",SafeToRetry:true));
 RequireInstagramLogin(account);
 if(account.Disconnected||account.ExpiresAt is null||account.ExpiresAt<=DateTimeOffset.UtcNow)
 throw new ProviderException(new(JobState.RequiresAuthorization,Detail:"Token Instagram wygasł lub konto odłączono. Połącz konto ponownie.",SafeToRetry:true));
 // Long-lived tokens must be at least 24 hours old; a fresh 60-day token needs no refresh.
 if(account.ExpiresAt>DateTimeOffset.UtcNow.AddDays(59)) return;
 var token=(await http.Send(HttpMethod.Get,"https://graph.instagram.com/refresh_access_token?grant_type=ig_refresh_token&access_token="+ProviderHttp.Escape(vault.Open(account.AccessToken)))).Json;
 var access=ProviderHttp.Required(token,"access_token");var expiry=InstagramExpiry(token);
 account.AccessToken=vault.Seal(access);account.ExpiresAt=expiry;
 await db.SaveChangesAsync();
 }
}
