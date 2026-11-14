using System.Text.Json;
using Publicate;
using Xunit;
namespace Publicate.Tests;
public class LinkedInDiagnosticsTests {
 [Fact] public void OAuthStringErrorIsRecordedWithoutSensitiveDescription(){var trace=new OAuthTrace{Active=true};using var doc=JsonDocument.Parse("{\"error\":\"invalid_client\",\"error_description\":\"secret test-secret https://example.test/?code=private\"}");trace.Record("linkedin.code_exchange",401,doc.RootElement);var step=Assert.Single(trace.Steps);Assert.Equal("invalid_client",step.Code);Assert.NotNull(step.MessageHash);Assert.DoesNotContain("test-secret",JsonSerializer.Serialize(step));Assert.DoesNotContain("code=private",JsonSerializer.Serialize(step));}
 [Fact] public async Task InvalidClientDoesNotSuggestSimplyLoggingInAgain(){var handler=new FakeHandler();handler.Add("{\"error\":\"invalid_client\",\"error_description\":\"hidden\"}",401);var ex=await Assert.ThrowsAsync<ProviderException>(()=>new ProviderHttp(handler).Send(HttpMethod.Post,"https://www.linkedin.com/oauth/v2/accessToken",operation:"linkedin.code_exchange"));Assert.Contains("Client Secret",ex.Result.Detail);Assert.False(ex.Result.SafeToRetry);Assert.Equal("invalid_client",ex.Result.ProviderCode);}
 [Fact] public void LinkedInRestErrorKeepsOnlySafeCode(){var trace=new OAuthTrace{Active=true};using var doc=JsonDocument.Parse("{\"serviceErrorCode\":65600,\"message\":\"token SECRET\"}");trace.Record("linkedin.profile",401,doc.RootElement);Assert.Equal("65600",trace.Steps[0].Code);Assert.DoesNotContain("SECRET",JsonSerializer.Serialize(trace.Steps));}
}
