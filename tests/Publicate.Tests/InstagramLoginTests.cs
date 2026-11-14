using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using Publicate;
using Xunit;
namespace Publicate.Tests;
public class InstagramLoginTests {
 private static string Start(OAuthService oauth)=>QueryHelpers.ParseQuery(new Uri(oauth.Begin(Platform.Instagram,"browser")).Query)["state"].ToString();
 private static void Configure(Microsoft.Extensions.Configuration.IConfiguration c) {c["Providers:Instagram:ClientId"]="ig-client";c["Providers:Instagram:ClientSecret"]="ig-secret";}
 [Fact] public void IndependentConfigAndAuthorizationHost() {
 var s=AdapterTests.Setup(Platform.Instagram);Configure(s.config);
 var url=new Uri(s.oauth.Begin(Platform.Instagram,"browser"));var q=QueryHelpers.ParseQuery(url.Query);
 Assert.Equal("www.instagram.com",url.Host);Assert.Equal("ig-client",q["client_id"]);Assert.Contains("instagram_business_content_publish",q["scope"].ToString());Assert.DoesNotContain("pages_",url.ToString());Assert.DoesNotContain("secret",url.ToString());
 Assert.Throws<InvalidOperationException>(()=>s.oauth.CheckConfig(Platform.Facebook));
 }
 [Theory][InlineData(false)][InlineData(true)] public async Task ConnectCreatesOnlyInstagramAndUsesGrantedPermissions(bool wrapped) {
 var s=AdapterTests.Setup(Platform.Instagram);Configure(s.config);var state=Start(s.oauth);
 const string token="{\"access_token\":\"short\",\"user_id\":123,\"permissions\":[\"instagram_business_basic\",\"instagram_business_content_publish\"]}";
 s.handler.Add(wrapped?"{\"data\":["+token+"]}":token);
 s.handler.Add("{\"access_token\":\"long\",\"expires_in\":5184000}");s.handler.Add("{\"user_id\":\"123\",\"username\":\"test_creator\"}");
 await s.oauth.Complete(Platform.Instagram,state,"code","browser");
 var a=await s.db.ConnectedAccounts.SingleAsync();var t=await s.db.PublishingTargets.SingleAsync();
 Assert.Equal(Platform.Instagram,a.Platform);Assert.Equal(s.db.CurrentOwnerId,a.OwnerId);Assert.Equal("123",t.ExternalId);Assert.Equal("InstagramLogin",t.Kind);Assert.Null(t.AccessToken);Assert.Equal("long",await s.oauth.TargetToken(new(){Account=a,Platform=Platform.Instagram,Kind=t.Kind}));Assert.DoesNotContain("long",a.AccessToken);
 Assert.Equal("POST https://api.instagram.com/oauth/access_token",s.handler.Requests[0]);Assert.Contains("client_id=ig-client",s.handler.Bodies[0]);Assert.Contains("ig_exchange_token",s.handler.Requests[1]);Assert.Contains("graph.instagram.com/v25.0/me",s.handler.Requests[2]);Assert.DoesNotContain(s.handler.Requests,x=>x.Contains("facebook.com")||x.Contains("/accounts"));
 await Assert.ThrowsAsync<InvalidOperationException>(()=>s.oauth.Complete(Platform.Instagram,state,"code","browser"));Assert.Equal(3,s.handler.Requests.Count);
 }
 [Fact] public async Task WrongBrowserMakesNoExternalRequest() {var s=AdapterTests.Setup(Platform.Instagram);Configure(s.config);var state=Start(s.oauth);await Assert.ThrowsAsync<InvalidOperationException>(()=>s.oauth.Complete(Platform.Instagram,state,"code","other"));Assert.Empty(s.handler.Requests);}
 [Fact] public async Task MissingGrantsAreNeverAssumed() {
 var s=AdapterTests.Setup(Platform.Instagram);Configure(s.config);var state=Start(s.oauth);
 s.handler.Add("{\"access_token\":\"short\",\"user_id\":\"123\"}");s.handler.Add("{\"access_token\":\"long\",\"expires_in\":5184000}");s.handler.Add("{\"user_id\":\"123\",\"username\":\"tester\"}");
 await s.oauth.Complete(Platform.Instagram,state,"code","browser");Assert.StartsWith("UNVERIFIED:",(await s.db.ConnectedAccounts.SingleAsync()).Scopes);
 }
 [Fact] public async Task DeniedCodeDoesNotCreateAccount() {var s=AdapterTests.Setup(Platform.Instagram);Configure(s.config);var state=Start(s.oauth);s.handler.Add("{\"error\":{\"code\":190}}",400);await Assert.ThrowsAsync<ProviderException>(()=>s.oauth.Complete(Platform.Instagram,state,"bad","browser"));Assert.Empty(await s.db.ConnectedAccounts.ToListAsync());}
 [Fact] public async Task IdentityMismatchDoesNotPersistToken() {var s=AdapterTests.Setup(Platform.Instagram);Configure(s.config);var state=Start(s.oauth);s.handler.Add("{\"access_token\":\"short\",\"user_id\":\"123\"}");s.handler.Add("{\"access_token\":\"long\",\"expires_in\":5184000}");s.handler.Add("{\"user_id\":\"456\",\"username\":\"other\"}");await Assert.ThrowsAsync<ProviderException>(()=>s.oauth.Complete(Platform.Instagram,state,"code","browser"));Assert.Empty(await s.db.ConnectedAccounts.ToListAsync());}
 [Fact] public async Task LegacyPageTokenIsNeverSentToInstagram() {var s=AdapterTests.Setup(Platform.Instagram);s.target.Kind="Professional";s.target.Account.Scopes="instagram_basic instagram_content_publish pages_read_engagement";await Assert.ThrowsAsync<ProviderException>(()=>s.oauth.TargetToken(s.target));Assert.Empty(s.handler.Requests);}
 [Fact] public async Task RefreshUsesInstagramGrantWithoutRefreshToken() {var s=AdapterTests.Setup(Platform.Instagram);Configure(s.config);s.target.Account.ExpiresAt=DateTimeOffset.UtcNow.AddDays(2);s.handler.Add("{\"access_token\":\"rotated\",\"expires_in\":5184000}");await s.oauth.Refresh(s.target.Account);Assert.Equal("rotated",await s.oauth.Access(s.target.Account));Assert.Contains("graph.instagram.com/refresh_access_token?grant_type=ig_refresh_token",s.handler.Requests.Single());}
 [Fact] public async Task ExpiredInstagramTokenRequiresLogin() {var s=AdapterTests.Setup(Platform.Instagram);Configure(s.config);s.target.Account.ExpiresAt=DateTimeOffset.UtcNow.AddSeconds(-1);var e=await Assert.ThrowsAsync<ProviderException>(()=>s.oauth.Refresh(s.target.Account));Assert.Equal(JobState.RequiresAuthorization,e.Result.State);Assert.Empty(s.handler.Requests);}
 [Fact] public async Task FreshLongLivedTokenIsNotRefreshedBefore24Hours() {var s=AdapterTests.Setup(Platform.Instagram);Configure(s.config);s.target.Account.ExpiresAt=DateTimeOffset.UtcNow.AddDays(60);await s.oauth.Refresh(s.target.Account);Assert.Empty(s.handler.Requests);}
 [Fact] public async Task PublishingAndStatusUseInstagramHost() {
 var s=AdapterTests.Setup(Platform.Instagram);s.handler.Add("{\"id\":\"container\"}");s.handler.Add("{\"status_code\":\"FINISHED\"}");s.handler.Add("{\"id\":\"post\"}");s.handler.Add("{\"id\":\"post\",\"permalink\":\"https://www.instagram.com/p/test/\"}");
 var publisher=new InstagramPublisher(s.http,s.oauth,s.storage);var result=await publisher.PublishPost(AdapterTests.Context(s.target,new MediaAsset{ContentType="image/jpeg",PublicUrl="https://media.example.test/image.jpg"}),default);
 Assert.Equal(JobState.Published,result.State);var status=await publisher.CheckPublishStatus(new(){ExternalId=result.ExternalId,State=result.State},s.target,default);Assert.Equal(JobState.Published,status.State);Assert.All(s.handler.Requests,r=>Assert.Contains("https://graph.instagram.com/v25.0/",r));
 }
}
