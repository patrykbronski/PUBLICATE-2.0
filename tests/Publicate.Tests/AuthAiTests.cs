using Microsoft.AspNetCore.DataProtection;
using Publicate;
using Xunit;
namespace Publicate.Tests;
public class TestClock:TimeProvider {public DateTimeOffset Time=DateTimeOffset.UtcNow;public override DateTimeOffset GetUtcNow()=>Time;}
public class AuthAiTests {
 [Fact] public void ExpiredStateRejected(){var clock=new TestClock();var states=new OAuthStates(clock);var state=states.Add(Platform.LinkedIn,"verifier","browser");clock.Time=clock.Time.AddMinutes(11);Assert.Throws<InvalidOperationException>(()=>states.Consume(state,Platform.LinkedIn,"browser"));}
 [Fact] public async Task RefreshRotatesAndEncryptsToken(){
 var s=AdapterTests.Setup(Platform.TikTok);var vault=new TokenVault(new EphemeralDataProtectionProvider());s.config["Providers:TikTok:ClientId"]="client";s.config["Providers:TikTok:ClientSecret"]="secret";
 var account=s.target.Account;account.RefreshToken=vault.Seal("old-refresh");s.db.Add(account);await s.db.SaveChangesAsync();s.handler.Add("{\"access_token\":\"new-access\",\"refresh_token\":\"rotated-refresh\",\"expires_in\":7200,\"scope\":\"video.publish,user.info.basic\"}");
 var oauth=new OAuthService(s.config,s.http,vault,s.db,new());await oauth.Refresh(account);
 Assert.Equal("rotated-refresh",vault.Open(account.RefreshToken!));Assert.Equal("new-access",vault.Open(account.AccessToken));Assert.DoesNotContain("new-access",account.AccessToken);Assert.Contains("video.publish",account.Scopes);Assert.True(account.ExpiresAt>DateTimeOffset.UtcNow.AddMinutes(110));
 }
 [Fact] public async Task ExpiredAccountWithoutRefreshRequiresAuthorization(){var s=AdapterTests.Setup(Platform.LinkedIn);s.config["Providers:LinkedIn:ClientId"]="client";s.config["Providers:LinkedIn:ClientSecret"]="secret";s.target.Account.ExpiresAt=DateTimeOffset.UtcNow.AddHours(-1);var e=await Assert.ThrowsAsync<ProviderException>(()=>s.oauth.Access(s.target.Account));Assert.Equal(JobState.RequiresAuthorization,e.Result.State);Assert.Empty(s.handler.Requests);}
 [Fact] public async Task LocalDisconnectRemovesAllStoredTokens(){var s=AdapterTests.Setup(Platform.LinkedIn);s.db.Add(s.target);await s.db.SaveChangesAsync();await s.oauth.Disconnect(s.target.Account);Assert.True(s.target.Account.Disconnected);Assert.Empty(s.target.Account.AccessToken);Assert.Null(s.target.AccessToken);Assert.Null(s.target.Account.RefreshToken);}
 [Fact] public async Task AiUnavailableNeverMakesRequest(){var s=AdapterTests.Setup(Platform.LinkedIn);var ai=new OpenAiDescriptionService(s.config,s.http,s.storage);Assert.False(ai.Available);await Assert.ThrowsAsync<InvalidOperationException>(()=>ai.Generate([],"LinkedIn",default));Assert.Empty(s.handler.Requests);}
 [Fact] public async Task AiUsesOrderedImagesAndRealResponse(){var s=AdapterTests.Setup(Platform.LinkedIn);s.config["Ai:ApiKey"]="test-key";s.config["Ai:Model"]="test-model";s.config["Ai:AllowPaidCalls"]="true";s.handler.Add("{\"output\":[{\"type\":\"message\",\"content\":[{\"type\":\"output_text\",\"text\":\"Odpowiedz testowego serwera\"}]}]}");var ai=new OpenAiDescriptionService(s.config,s.http,s.storage);var result=await ai.Generate([new(){ContentType="image/jpeg",Bytes=3},new(){ContentType="image/png",Bytes=3}],"LinkedIn",default);Assert.Equal("Odpowiedz testowego serwera",result);Assert.Contains("\"store\":false",s.handler.Bodies[0]);Assert.True(s.handler.Bodies[0].IndexOf("image/jpeg",StringComparison.Ordinal)<s.handler.Bodies[0].IndexOf("image/png",StringComparison.Ordinal));}
 [Fact] public async Task IncompleteUploadCannotBeSelected(){var s=AdapterTests.Setup(Platform.LinkedIn);var m=new MediaAsset{Uploaded=false};s.db.Add(m);await s.db.SaveChangesAsync();var service=new PublicationService(s.db,[],s.storage,s.config);Assert.Empty(await service.Media([m.Id]));}
}
