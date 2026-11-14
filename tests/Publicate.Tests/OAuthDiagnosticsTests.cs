using System.Text.Json;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.WebUtilities;
using Publicate;
using Xunit;
namespace Publicate.Tests;
public class OAuthDiagnosticsTests {
 [Fact] public async Task InvalidAppSecretHasActionableSafeDiagnostic(){var trace=new OAuthTrace{Active=true};var h=new FakeHandler();h.Add("{\"error\":{\"code\":1,\"message\":\"Error validating client secret.\"}}",400);var error=await Assert.ThrowsAsync<ProviderException>(()=>new ProviderHttp(h,trace).Send(HttpMethod.Post,"https://graph.facebook.com/v27.0/oauth/access_token",operation:"instagram.unified.code_exchange"));Assert.Contains("App Secret",error.Result.Detail);Assert.False(error.Result.SafeToRetry);Assert.Equal("Error validating client secret.",Assert.Single(trace.Steps).Message);}
 [Fact] public async Task ProviderErrorRecordsStageAndAllowlistedMetaFieldsWithoutSecrets(){
 var trace=new OAuthTrace{Active=true};var handler=new FakeHandler();var http=new ProviderHttp(handler,trace);
 handler.Add("""{"error":{"message":"code=private-code token=private-token client_secret=private-secret","code":1,"error_subcode":123,"type":"OAuthException","fbtrace_id":"safe-trace-id","is_transient":true}}""",400);
 await Assert.ThrowsAsync<ProviderException>(()=>http.Send(HttpMethod.Post,"https://graph.facebook.com/v27.0/oauth/access_token",form:new(){{"code","private-code"},{"client_secret","private-secret"}},operation:"instagram.unified.code_exchange"));
 var entry=Assert.Single(trace.Steps);Assert.Equal("instagram.unified.code_exchange",entry.Stage);Assert.Equal(400,entry.HttpStatus);Assert.Equal("1",entry.Code);Assert.Equal("123",entry.Subcode);Assert.True(entry.IsTransient);Assert.Equal("safe-trace-id",entry.TraceId);
 var json=JsonSerializer.Serialize(entry);Assert.DoesNotContain("private-code",json);Assert.DoesNotContain("private-token",json);Assert.DoesNotContain("private-secret",json);Assert.DoesNotContain("oauth/access_token",json);
 }
 [Fact] public async Task SuccessfulExchangeNeverLogsResponseToken(){var trace=new OAuthTrace{Active=true};var h=new FakeHandler();h.Add("{\"access_token\":\"keep-secret\"}");await new ProviderHttp(h,trace).Send(HttpMethod.Post,"https://graph.facebook.com/test",operation:"code_exchange");Assert.DoesNotContain("keep-secret",JsonSerializer.Serialize(trace.Steps));Assert.Equal(200,Assert.Single(trace.Steps).HttpStatus);}
 [Fact] public async Task FailedInspectionIdentifiedAfterSuccessfulExchange(){
 var s=AdapterTests.Setup(Platform.Instagram);s.config["Providers:Instagram:LoginMode"]="Unified";s.config["Providers:Instagram:ClientId"]="app";s.config["Providers:Instagram:ClientSecret"]="secret";s.config["Providers:Instagram:ConfigId"]="123";
 var trace=new OAuthTrace{Active=true};var oauth=new OAuthService(s.config,new(s.handler,trace),new TokenVault(new EphemeralDataProtectionProvider()),s.db,new());
 var state=QueryHelpers.ParseQuery(new Uri(oauth.Begin(Platform.Instagram,"browser")).Query)["state"].ToString();s.handler.Add("{\"access_token\":\"private-token\"}");s.handler.Add("{\"error\":{\"code\":1,\"message\":\"An unknown error occurred.\"}}",400);
 await Assert.ThrowsAsync<ProviderException>(()=>oauth.Complete(Platform.Instagram,state,"code","browser"));Assert.Equal(2,trace.Steps.Count);Assert.Equal("instagram.unified.token_inspection",trace.Steps[1].Stage);Assert.Equal(400,trace.Steps[1].HttpStatus);
 }
 [Fact] public async Task DeclinedConsentConsumesStateWithoutHttp(){var s=AdapterTests.Setup(Platform.Instagram);s.config["Providers:Instagram:ClientId"]="app";s.config["Providers:Instagram:ClientSecret"]="secret";var state=QueryHelpers.ParseQuery(new Uri(s.oauth.Begin(Platform.Instagram,"browser")).Query)["state"].ToString();await Assert.ThrowsAsync<ProviderException>(()=>s.oauth.Complete(Platform.Instagram,state,"","browser",true));await Assert.ThrowsAsync<InvalidOperationException>(()=>s.oauth.Complete(Platform.Instagram,state,"code","browser"));Assert.Empty(s.handler.Requests);}
}
