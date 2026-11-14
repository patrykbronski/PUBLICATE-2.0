using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
namespace Publicate;
public record OAuthPending(Platform Platform,string Verifier,string BrowserHash,DateTimeOffset Expires,Guid OwnerId);
public class OAuthStates(TimeProvider? clock=null) {
 private DateTimeOffset Now=>clock?.GetUtcNow()??DateTimeOffset.UtcNow;
 private readonly ConcurrentDictionary<string,OAuthPending> states=new();
 public string Add(Platform p,string verifier,string browser,Guid ownerId=default) {
 foreach(var x in states.Where(x=>x.Value.Expires<Now)) states.TryRemove(x.Key,out _);
 var key=Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
 states[key]=new(p,verifier,Hash(browser),Now.AddMinutes(10),ownerId); return key;
 }
 public OAuthPending Consume(string state,Platform p,string browser) {
 if(!states.TryRemove(state,out var s)||s.Platform!=p||s.Expires<Now||s.BrowserHash!=Hash(browser)) throw new InvalidOperationException("Nieprawidłowy, wygasły lub wykorzystany OAuth state.");
 return s;
 }
 public static string Hash(string s)=>Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(s)));
}
public class TokenVault(IDataProtectionProvider provider) {
 private readonly IDataProtector protector=provider.CreateProtector("PUBLICATE.tokens.v1");
 public string Seal(string v)=>protector.Protect(v);
 public string Open(string v)=>protector.Unprotect(v);
}
public partial class OAuthService(IConfiguration config,ProviderHttp http,TokenVault vault,AppDb db,OAuthStates states) {
 public string Group(Platform p)=>p == Platform.Facebook ? "Meta":p.ToString();
 public string Setting(Platform p,string key)=>config[$"Providers:{Group(p)}:{key}"]??"";
 public void CheckConfig(Platform p) {
 if(p==Platform.Facebook&&!string.IsNullOrEmpty(Setting(p,"ConfigId"))&&!System.Text.RegularExpressions.Regex.IsMatch(Setting(p,"ConfigId"),"^[0-9]+$")) throw new InvalidOperationException("Facebook ConfigId musi byc identyfikatorem konfiguracji Meta.");
 if(p==Platform.Instagram&&InstagramUnified&&!System.Text.RegularExpressions.Regex.IsMatch(Setting(p,"ConfigId"),"^[0-9]+$")) throw new InvalidOperationException("Instagram Unified Login wymaga ConfigId z panelu Meta.");
 if(p==Platform.X&&!config.GetValue<bool>("Providers:X:AllowPaidCalls")) throw new InvalidOperationException("X wyłączone: wymagana jawna zgoda na koszty w konfiguracji.");
 if(string.IsNullOrWhiteSpace(Setting(p,"ClientId"))||string.IsNullOrWhiteSpace(Setting(p,"ClientSecret"))) throw new InvalidOperationException("Brak ClientId/ClientSecret. Zobacz docs/SETUP_ACCOUNTS.md.");
 if(p is Platform.Facebook or Platform.Instagram && !System.Text.RegularExpressions.Regex.IsMatch(Setting(p,"Version"),"^v[0-9]+\\.0$")) throw new InvalidOperationException("Ustaw wersję Meta Graph API potwierdzoną w portalu.");
 }
 public string Callback(Platform p) {
 var b=config["OAuth:CallbackBase"]??"http://localhost:5080";
 if(!Uri.TryCreate(b,UriKind.Absolute,out var u)||(u.Scheme!="https"&&!(u.Scheme=="http"&&u.IsLoopback))||u.AbsolutePath!="/"||!string.IsNullOrEmpty(u.Query)||!string.IsNullOrEmpty(u.UserInfo)) throw new InvalidOperationException("CallbackBase musi być origin HTTPS (HTTP tylko loopback).");
 return b.TrimEnd('/')+"/api/oauth/"+p+"/callback";
 }
 public string Scopes(Platform p)=>p switch {
 Platform.LinkedIn=>"openid profile w_member_social",
 Platform.Facebook=>"pages_show_list pages_read_engagement pages_manage_posts",
 Platform.Instagram=>"instagram_business_basic instagram_business_content_publish",
 Platform.TikTok=>"user.info.basic video.publish",
 Platform.YouTube=>"https://www.googleapis.com/auth/youtube.upload https://www.googleapis.com/auth/youtube.readonly",
 _=>"tweet.read tweet.write users.read offline.access media.write"};
 public string Begin(Platform p,string browser) {
 CheckConfig(p);
 if(p==Platform.Instagram) return BeginInstagram(browser);
 var verifier=Convert.ToBase64String(RandomNumberGenerator.GetBytes(48)).TrimEnd('=').Replace('+','-').Replace('/','_');
 var state=states.Add(p,verifier,browser,db.CurrentOwnerId);
 var args=new Dictionary<string,string>{{p==Platform.TikTok?"client_key":"client_id",Setting(p,"ClientId")},{"redirect_uri",Callback(p)},{"response_type","code"},{"scope",p==Platform.TikTok?Scopes(p).Replace(' ', ','):Scopes(p)},{"state",state}};
 if(p==Platform.Facebook&&!string.IsNullOrEmpty(Setting(p,"ConfigId"))) {
 args.Remove("scope");
 args["config_id"]=Setting(p,"ConfigId");
 args["override_default_response_type"]="true";
 }
 if(p is Platform.X or Platform.YouTube) { args["code_challenge"]=Convert.ToBase64String(SHA256.HashData(Encoding.ASCII.GetBytes(verifier))).TrimEnd('=').Replace('+','-').Replace('/','_'); args["code_challenge_method"]="S256"; }
 if(p==Platform.YouTube){args["access_type"]="offline";args["prompt"]="consent select_account";}
 var endpoint=p switch {Platform.YouTube=>"https://accounts.google.com/o/oauth2/v2/auth",Platform.LinkedIn=>"https://www.linkedin.com/oauth/v2/authorization",Platform.TikTok=>"https://www.tiktok.com/v2/auth/authorize/",Platform.X=>"https://x.com/i/oauth2/authorize",_=>$"https://www.facebook.com/{Setting(p,"Version")}/dialog/oauth"};
 return endpoint+"?"+string.Join("&",args.Select(x=>ProviderHttp.Escape(x.Key)+"="+ProviderHttp.Escape(x.Value)));
 }
 private string TokenEndpoint(Platform p)=>p switch {Platform.YouTube=>"https://oauth2.googleapis.com/token",Platform.LinkedIn=>"https://www.linkedin.com/oauth/v2/accessToken",Platform.TikTok=>"https://open.tiktokapis.com/v2/oauth/token/",Platform.X=>"https://api.x.com/2/oauth2/token",_=>$"https://graph.facebook.com/{Setting(p,"Version")}/oauth/access_token"};
 private async Task<System.Text.Json.JsonElement> Token(Platform p,Dictionary<string,string> fields) {
 fields[p==Platform.TikTok?"client_key":"client_id"]=Setting(p,"ClientId");
 Dictionary<string,string>? headers=null;
 if(p==Platform.X) headers=new(){{"Authorization","Basic "+Convert.ToBase64String(Encoding.UTF8.GetBytes(Setting(p,"ClientId")+":"+Setting(p,"ClientSecret")))}};
 else fields["client_secret"]=Setting(p,"ClientSecret");
 return (await http.Send(HttpMethod.Post,TokenEndpoint(p),form:fields,headers:headers,operation:p.ToString().ToLowerInvariant()+(fields.GetValueOrDefault("grant_type")=="refresh_token"?".token_refresh":".code_exchange"))).Json;
 }
 public async Task Complete(Platform p,string state,string code,string browser,bool denied=false) {
 var pending=states.Consume(state,p,browser);
 if(pending.OwnerId!=db.CurrentOwnerId)throw new InvalidOperationException("Sesja OAuth należy do innego użytkownika. Połącz konto ponownie.");
 if(denied)throw new ProviderException(new(JobState.RequiresAuthorization,Detail:"Dostawca nie udzielił autoryzacji. Rozpocznij nowe połączenie, jeśli chcesz spróbować ponownie."));
 if(string.IsNullOrWhiteSpace(code))throw new InvalidOperationException("Brak kodu autoryzacji. Rozpocznij nowe połączenie.");
 CheckConfig(p);
 if(p==Platform.Instagram) { if(pending.Verifier!=(InstagramUnified?"Unified":""))throw new InvalidOperationException("Metoda logowania zmieniła się podczas OAuth. Zacznij ponownie."); await CompleteInstagram(code); return; }
 var fields=new Dictionary<string,string>{{"grant_type","authorization_code"},{"code",code},{"redirect_uri",Callback(p)}};
 if(p is Platform.X or Platform.YouTube) fields["code_verifier"]=pending.Verifier;
 var tokens=await Token(p,fields); var access=ProviderHttp.Required(tokens,"access_token");
 if(p==Platform.YouTube){await CompleteYouTube(tokens,access);return;}
 var profile=await http.Send(HttpMethod.Get,p switch { Platform.LinkedIn=>"https://api.linkedin.com/v2/userinfo",Platform.TikTok=>"https://open.tiktokapis.com/v2/user/info/?fields=open_id,display_name",Platform.X=>"https://api.x.com/2/users/me",_=>$"https://graph.facebook.com/{Setting(p,"Version")}/me?fields=id,name"},access,operation:p.ToString().ToLowerInvariant()+".profile");
 var user=profile.Json;
 if(p==Platform.TikTok) user=user.GetProperty("data").GetProperty("user");
 if(p==Platform.X) user=user.GetProperty("data");
 var id=ProviderHttp.Required(user,p==Platform.LinkedIn?"sub":p==Platform.TikTok?"open_id":"id");
 var account=await db.ConnectedAccounts.SingleOrDefaultAsync(x=>x.OwnerId==db.CurrentOwnerId&&x.Platform==p&&x.ExternalId==id) ?? new ConnectedAccount{OwnerId=db.CurrentOwnerId,Platform=p,ExternalId=id};
 if(db.Entry(account).State==EntityState.Detached) db.Add(account);
 account.Name=ProviderHttp.Optional(user,p==Platform.TikTok?"display_name":"name")??id;
 account.AccessToken=vault.Seal(access); account.Disconnected=false;
 account.RefreshToken=tokens.TryGetProperty("refresh_token",out var rt)?vault.Seal(rt.GetString()!):null;
 account.ExpiresAt=tokens.TryGetProperty("expires_in",out var exp)?DateTimeOffset.UtcNow.AddSeconds(double.Parse(exp.ToString())):null;
 account.Scopes=ProviderHttp.Optional(tokens,"scope")?.Replace(',',' ')??"";
 if(p==Platform.Facebook) {
 var permissions=(await http.Send(HttpMethod.Get,$"https://graph.facebook.com/{Setting(p,"Version")}/me/permissions",access)).Json;
 account.Scopes=string.Join(" ",permissions.GetProperty("data").EnumerateArray().Where(x=>ProviderHttp.Optional(x,"status")=="granted").Select(x=>ProviderHttp.Required(x,"permission")));
 } else if(p==Platform.LinkedIn) {
 var introspection=(await http.Send(HttpMethod.Post,"https://www.linkedin.com/oauth/v2/introspectToken",form:new(){{"client_id",Setting(p,"ClientId")},{"client_secret",Setting(p,"ClientSecret")},{"token",access}},operation:"linkedin.token_inspection")).Json;
 if(!introspection.TryGetProperty("active",out var active)||active.ValueKind!=System.Text.Json.JsonValueKind.True) throw new ProviderException(new(JobState.RequiresAuthorization,Detail:"LinkedIn nie potwierdził aktywnego tokenu."));
 account.Scopes=(ProviderHttp.Optional(introspection,"scope")??"").Replace(',',' ');
 } else if(string.IsNullOrWhiteSpace(account.Scopes)) {
 // Providers may omit scope on an unchanged grant. Mark unverified instead of inventing grants.
 account.Scopes="UNVERIFIED:"+Scopes(p);
 }
 await db.SaveChangesAsync(); await Discover(account);
 }
 public async Task Discover(ConnectedAccount account) {
 CheckConfig(account.Platform);
 if(account.Platform==Platform.Instagram) { await DiscoverInstagram(account); return; }
 if(account.Platform==Platform.YouTube){await DiscoverYouTube(account);return;}
 var access=await Access(account);
 async Task Upsert(Platform p,string id,string name,string kind,string? token) {
 var target=await db.PublishingTargets.SingleOrDefaultAsync(t=>t.AccountId==account.Id&&t.Platform==p&&t.ExternalId==id);
 if(target is null) { target=new(){OwnerId=account.OwnerId,AccountId=account.Id,Platform=p,ExternalId=id}; db.Add(target); }
 target.Name=name; target.Kind=kind; target.AccessToken=token is null?null:vault.Seal(token);
 }
 if(account.Platform==Platform.Facebook) {
 var permissions=(await http.Send(HttpMethod.Get,$"https://graph.facebook.com/{Setting(account.Platform,"Version")}/me/permissions",access)).Json;
 account.Scopes=string.Join(" ",permissions.GetProperty("data").EnumerateArray().Where(x=>ProviderHttp.Optional(x,"status")=="granted").Select(x=>ProviderHttp.Required(x,"permission")));
 foreach(var old in await db.PublishingTargets.Where(t=>t.AccountId==account.Id).ToListAsync()) old.AccessToken=null;
 string? after=null;
 do {
 var result=(await http.Send(HttpMethod.Get,$"https://graph.facebook.com/{Setting(account.Platform,"Version")}/me/accounts?fields=id,name,access_token,tasks&limit=100"+(after is null?"":"&after="+ProviderHttp.Escape(after)),access)).Json;
 foreach(var page in result.GetProperty("data").EnumerateArray()) {
 var pt=ProviderHttp.Required(page,"access_token");
 if(account.Platform==Platform.Facebook) await Upsert(Platform.Facebook,ProviderHttp.Required(page,"id"),ProviderHttp.Required(page,"name"),"Page",pt);
 }
 after=result.TryGetProperty("paging",out var pg)&&pg.TryGetProperty("next",out _)&&pg.TryGetProperty("cursors",out var curs)?ProviderHttp.Optional(curs,"after"):null;
 } while(after is not null);
 } else await Upsert(account.Platform,account.Platform==Platform.LinkedIn?"urn:li:person:"+account.ExternalId:account.ExternalId,account.Name,"Profile",null);
 await db.SaveChangesAsync();
 }
 public async Task<string> Access(ConnectedAccount a) {
 if(a.Platform==Platform.Instagram) RequireInstagramLogin(a);
 if(a.Disconnected) throw new ProviderException(new(JobState.RequiresAuthorization,Detail:"Konto odłączone.",SafeToRetry:true));
 if(a.ExpiresAt<DateTimeOffset.UtcNow.AddMinutes(2)) await Refresh(a);
 return vault.Open(a.AccessToken);
 }
 public async Task Refresh(ConnectedAccount a) {
 CheckConfig(a.Platform);
 if(a.Platform==Platform.Instagram) { await RefreshInstagram(a); return; }
 if(a.RefreshToken is null||a.Platform is Platform.Facebook or Platform.Instagram) throw new ProviderException(new(JobState.RequiresAuthorization,Detail:"Wymagane ponowne OAuth; dostawca nie udostępnił refresh token.",SafeToRetry:true));
 var t=await Token(a.Platform,new(){{"grant_type","refresh_token"},{"refresh_token",vault.Open(a.RefreshToken)}});
 a.AccessToken=vault.Seal(ProviderHttp.Required(t,"access_token"));
 if(t.TryGetProperty("refresh_token",out var rt)) a.RefreshToken=vault.Seal(rt.GetString()!);
 a.ExpiresAt=t.TryGetProperty("expires_in",out var exp)?DateTimeOffset.UtcNow.AddSeconds(double.Parse(exp.ToString())):null;
 if(t.TryGetProperty("scope",out var scopes)) a.Scopes=scopes.ToString().Replace(',',' ');
 await db.SaveChangesAsync();
 }
 public async Task Disconnect(ConnectedAccount a) {
 // Local disconnect always removes credentials. Remote revocation is documented separately.
 a.Disconnected=true; a.AccessToken=""; a.RefreshToken=null;
 foreach(var t in await db.PublishingTargets.Where(t=>t.AccountId==a.Id).ToListAsync()) t.AccessToken=null;
 await db.SaveChangesAsync();
 }
 public async Task<string> TargetToken(PublishingTarget t) {
 if(t.Platform==Platform.Instagram&&t.Kind!=(IsUnified(t.Account)?"InstagramUnified":"InstagramLogin")) throw new ProviderException(new(JobState.RequiresAuthorization,Detail:"Wybierz konto podłączone właściwą metodą Instagram Login.",SafeToRetry:true));
 var access=await Access(t.Account); return t.Platform==Platform.Instagram||t.AccessToken is null?access:vault.Open(t.AccessToken);
 }
}
