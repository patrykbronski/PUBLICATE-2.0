using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Publicate;
using Xunit;
namespace Publicate.Tests;
public class CallbackTestAuth(IOptionsMonitor<AuthenticationSchemeOptions> options,ILoggerFactory logger,UrlEncoder encoder):AuthenticationHandler<AuthenticationSchemeOptions>(options,logger,encoder){
 protected override Task<AuthenticateResult> HandleAuthenticateAsync()=>Task.FromResult(Request.Headers.ContainsKey("Test-Anonymous")?AuthenticateResult.NoResult():AuthenticateResult.Success(new AuthenticationTicket(new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier,Request.Headers.ContainsKey("Test-Other")?"11111111-1111-1111-1111-111111111111":LocalOwner.Id.ToString())],"test")),"test")));
}
public class CallbackFactory:WebApplicationFactory<Program>{
 public FakeHandler Handler {get;}=new();
 protected override void ConfigureWebHost(IWebHostBuilder builder)=>builder.ConfigureTestServices(services=>{
 services.AddDataProtection().UseEphemeralDataProtectionProvider();
 services.RemoveAll<AppDb>();services.RemoveAll<DbContextOptions<AppDb>>();services.RemoveAll<IDbContextOptionsConfiguration<AppDb>>();
 var database=Guid.NewGuid().ToString();services.AddDbContext<AppDb>(o=>o.UseInMemoryDatabase(database));
 services.AddAuthentication(o=>{o.DefaultAuthenticateScheme="test";o.DefaultChallengeScheme="test";}).AddScheme<AuthenticationSchemeOptions,CallbackTestAuth>("test",_=>{});
 services.RemoveAll<IHttpClientFactory>();services.AddSingleton<IHttpClientFactory>(Handler);
 var config=new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string,string?>{{"Providers:Instagram:ClientId","app"},{"Providers:Instagram:ClientSecret","fixture-secret"},{"Providers:Instagram:Version","v27.0"},{"Providers:Instagram:ConfigId","123"},{"Providers:Instagram:LoginMode","Unified"},{"OAuth:CallbackBase","https://publicate.example.test"},{"OAuth:DiagnosticsPath",Path.Combine(Path.GetTempPath(),"publicate-oauth-tests",database+".jsonl")}}).Build();
 services.RemoveAll<OAuthService>();services.AddScoped(s=>new OAuthService(config,s.GetRequiredService<ProviderHttp>(),s.GetRequiredService<TokenVault>(),s.GetRequiredService<AppDb>(),s.GetRequiredService<OAuthStates>()));
 services.RemoveAll<OAuthReports>();services.AddSingleton(s=>new OAuthReports(config,s.GetRequiredService<ILogger<OAuthReports>>()));
 });
}
public class OAuthCallbackTests {
 [Theory][InlineData(false)][InlineData(true)] public async Task CallbackAlwaysRedirectsAndReportIsOwnerBound(bool success){
 await using var f=new CallbackFactory();using var c=f.CreateClient(new(){AllowAutoRedirect=false,HandleCookies=false});c.DefaultRequestHeaders.Add("X-Publicate","1");
 var start=await c.PostAsJsonAsync("/api/oauth/Instagram/start",new{});start.EnsureSuccessStatusCode();var url=(await start.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>()).GetProperty("url").GetString()!;var state=QueryHelpers.ParseQuery(new Uri(url).Query)["state"].ToString();
 var cookie=start.Headers.GetValues("Set-Cookie").Single(x=>x.StartsWith("publicate.oauth.Instagram=")).Split(';')[0];c.DefaultRequestHeaders.Add("Cookie",cookie);
 if(success){f.Handler.Add("{\"access_token\":\"test-secret-token\"}");f.Handler.Add("""{"data":{"is_valid":true,"app_id":"app","user_id":"system","expires_at":0,"scopes":["instagram_basic"],"granular_scopes":[{"scope":"instagram_basic","target_ids":["ig"]}]}}""");f.Handler.Add("{\"id\":\"system\",\"client_business_id\":\"business\"}");f.Handler.Add("{\"data\":[{\"id\":\"ig\",\"username\":\"tester\"}]}");}
 else f.Handler.Add("{\"error\":{\"code\":1,\"fbtrace_id\":\"trace-id\",\"message\":\"private-code fixture-secret\"}}",400);
 var callback=await c.GetAsync("/api/oauth/Instagram/callback?state="+state+"&code=private-code");Assert.Equal(HttpStatusCode.Redirect,callback.StatusCode);var location=callback.Headers.Location!.ToString();Assert.DoesNotContain("private-code",location);Assert.DoesNotContain("fixture-secret",location);Assert.Contains("#oauth=",location);var id=location.Split("#oauth=")[1];
 var report=await c.GetStringAsync("/api/oauth/results/"+id);Assert.DoesNotContain("private-code",report);Assert.DoesNotContain("test-secret-token",report);Assert.Contains("instagram.unified.code_exchange",report);Assert.Contains(success?"\"success\":true":"\"success\":false",report);
 c.DefaultRequestHeaders.Add("Test-Other","1");Assert.Equal(HttpStatusCode.NotFound,(await c.GetAsync("/api/oauth/results/"+id)).StatusCode);
 }
 [Fact] public async Task MissingSessionRedirectsWithoutProviderRequest(){await using var f=new CallbackFactory();using var c=f.CreateClient(new(){AllowAutoRedirect=false});c.DefaultRequestHeaders.Add("Test-Anonymous","1");var r=await c.GetAsync("/api/oauth/Instagram/callback?state=bad&code=private-code");Assert.Equal(HttpStatusCode.Redirect,r.StatusCode);Assert.EndsWith("#oauth=session_required",r.Headers.Location!.ToString());Assert.Empty(f.Handler.Requests);}
 [Fact] public async Task InvalidStateReturnsToUiWithoutProviderRequest(){await using var f=new CallbackFactory();using var c=f.CreateClient(new(){AllowAutoRedirect=false});var r=await c.GetAsync("/api/oauth/Instagram/callback?state=bad&code=private-code");Assert.Equal(HttpStatusCode.Redirect,r.StatusCode);Assert.Contains("#oauth=",r.Headers.Location!.ToString());Assert.Empty(f.Handler.Requests);}
}
