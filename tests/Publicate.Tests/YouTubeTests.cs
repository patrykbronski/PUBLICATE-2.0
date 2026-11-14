using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using Publicate;
using Xunit;
namespace Publicate.Tests;
public class YouTubeTests {
 const string Settings="{\"title\":\"Test\",\"privacy\":\"private\",\"madeForKids\":false,\"containsSyntheticMedia\":false}";
 static MediaAsset Video(double duration=179.99)=>new(){ContentType="video/mp4",Codec="h264",Width=1080,Height=1920,Duration=duration,Bytes=3};
 static ResolvedPost Post(MediaAsset m,string description="",string settings=Settings)=>new(description,"",[m.Id],settings);
 [Theory][InlineData(179.99,true)][InlineData(180,false)][InlineData(179.991,false)][InlineData(0,false)][InlineData(0.5,true)]
 public void ShortDuration(double duration,bool valid){var m=Video(duration);Assert.Equal(valid,PostValidator.Validate(Platform.YouTube,Post(m),[m]).Count==0);}
 [Fact] public void SquareAllowedLandscapeRejected(){var m=Video();m.Height=1080;Assert.Empty(PostValidator.Validate(Platform.YouTube,Post(m),[m]));m.Width=1920;Assert.NotEmpty(PostValidator.Validate(Platform.YouTube,Post(m),[m]));}
 [Fact] public void PhotosAndTextUnsupported(){var m=Video();m.ContentType="image/jpeg";Assert.NotEmpty(PostValidator.Validate(Platform.YouTube,Post(m),[m]));Assert.NotEmpty(PostValidator.Validate(Platform.YouTube,new("Text","",[],Settings),[]));}
 [Fact] public void Utf8ByteLimit(){var m=Video();Assert.Empty(PostValidator.Validate(Platform.YouTube,Post(m,new string('ą',2500)),[m]));Assert.NotEmpty(PostValidator.Validate(Platform.YouTube,Post(m,new string('ą',2501)),[m]));}
 [Theory][InlineData("{}")][InlineData("{\"title\":\"a\",\"privacy\":\"private\"}")][InlineData("{\"title\":\"a\",\"privacy\":\"bad\",\"madeForKids\":false}")][InlineData("{\"title\":\"<a>\",\"privacy\":\"private\",\"madeForKids\":false}")]
 public void ExplicitSettingsRequired(string settings){var m=Video();Assert.NotEmpty(PostValidator.Validate(Platform.YouTube,Post(m,settings:settings),[m]));}
 [Fact] public void OrderDoesNotChangePersistedPlatformNumbers(){Assert.Equal(4,(int)Platform.X);Assert.Equal(0,(int)Platform.LinkedIn);Assert.Equal(new[]{Platform.Facebook,Platform.Instagram,Platform.TikTok,Platform.YouTube,Platform.X,Platform.LinkedIn},PlatformOrder.All);}
 [Fact] public async Task OAuthExchangesCodeAndBindsChannel(){
 var s=AdapterTests.Setup(Platform.YouTube);s.config["Providers:YouTube:ClientId"]="fixture-client";s.config["Providers:YouTube:ClientSecret"]="fixture-secret";
 var uri=new Uri(s.oauth.Begin(Platform.YouTube,"browser"));var q=QueryHelpers.ParseQuery(uri.Query);
 Assert.Equal("accounts.google.com",uri.Host);Assert.Equal("offline",q["access_type"]);Assert.Equal("S256",q["code_challenge_method"]);Assert.DoesNotContain("fixture-secret",uri.ToString());
 s.handler.Add(JsonSerializer.Serialize(new{access_token="test-access",refresh_token="test-refresh",expires_in=3600,scope=string.Join(" ",Capabilities.Get(Platform.YouTube).RequiredScopes)}));
 s.handler.Add("{\"items\":[{\"id\":\"channel\",\"snippet\":{\"title\":\"Test channel\"}}]}");
 await s.oauth.Complete(Platform.YouTube,q["state"].ToString(),"test-code","browser");
 Assert.Equal("POST https://oauth2.googleapis.com/token",s.handler.Requests[0]);Assert.Contains("code_verifier=",s.handler.Bodies[0]);
 var a=await s.db.ConnectedAccounts.SingleAsync();Assert.Equal("channel",a.ExternalId);Assert.NotNull(a.RefreshToken);Assert.NotEqual("test-access",a.AccessToken);
 Assert.Equal("YouTubeChannel",(await s.db.PublishingTargets.SingleAsync()).Kind);
 s.handler.Add("{\"access_token\":\"renewed\",\"expires_in\":3600}");await s.oauth.Refresh(a);Assert.NotNull(a.RefreshToken);
 await Assert.ThrowsAsync<InvalidOperationException>(()=>s.oauth.Complete(Platform.YouTube,q["state"].ToString(),"test-code","browser"));
 var reconnect=QueryHelpers.ParseQuery(new Uri(s.oauth.Begin(Platform.YouTube,"browser")).Query);
 s.handler.Add(JsonSerializer.Serialize(new{access_token="reconnected",expires_in=3600,scope=string.Join(" ",Capabilities.Get(Platform.YouTube).RequiredScopes)}));
 s.handler.Add("{\"items\":[{\"id\":\"channel\",\"snippet\":{\"title\":\"Renamed channel\"}}]}");
 await s.oauth.Complete(Platform.YouTube,reconnect["state"].ToString(),"new-code","browser");
 Assert.Single(await s.db.ConnectedAccounts.ToListAsync());Assert.Single(await s.db.PublishingTargets.ToListAsync());Assert.NotNull(a.RefreshToken);Assert.Equal("Renamed channel",a.Name);
 await s.oauth.Disconnect(a);Assert.True(a.Disconnected);Assert.Null(a.RefreshToken);Assert.Empty(a.AccessToken);
 await Assert.ThrowsAsync<ProviderException>(()=>s.oauth.Access(a));
 }
 [Fact] public async Task UploadRequiresMatchingChannel(){var s=AdapterTests.Setup(Platform.YouTube);s.handler.Add("{\"items\":[{\"id\":\"different\"}]}");var m=Video();var r=await new YouTubePublisher(s.http,s.oauth,s.storage).PublishPost(new(s.target,Post(m),[m],_=>Task.CompletedTask),default);Assert.Equal(JobState.RequiresAuthorization,r.State);Assert.Single(s.handler.Requests);}
 [Fact] public async Task UploadIsProcessingUntilStatusConfirms(){
 var s=AdapterTests.Setup(Platform.YouTube);var m=Video();s.handler.Add("{\"items\":[{\"id\":\"target\"}]}");
 s.handler.Replies.Enqueue(req=>{Assert.Equal("3",req.Headers.GetValues("X-Upload-Content-Length").Single());var r=new HttpResponseMessage(HttpStatusCode.OK){Content=new StringContent("")};r.Headers.Location=new Uri("https://www.googleapis.com/upload/youtube/v3/videos?upload_id=secret");return r;});
 s.handler.Add("{\"id\":\"video-id\"}",201);string? checkpoint=null;
 var r=await new YouTubePublisher(s.http,s.oauth,s.storage).PublishPost(new(s.target,Post(m),[m],id=>{checkpoint=id;return Task.CompletedTask;}),default);
 Assert.Equal(JobState.Processing,r.State);Assert.Null(r.ExternalId);Assert.Null(r.Url);Assert.Equal("video-id",checkpoint);Assert.DoesNotContain("secret",JsonSerializer.Serialize(r));Assert.Contains("\"privacyStatus\":\"private\"",s.handler.Bodies[1]);Assert.Contains("\"selfDeclaredMadeForKids\":false",s.handler.Bodies[1]);
 }
 [Theory][InlineData("uploaded","private",JobState.Processing)][InlineData("processed","private",JobState.Published)][InlineData("processed","public",JobState.Unknown)][InlineData("rejected","private",JobState.Failed)]
 public async Task CheckDistinguishesProcessingPublicationAndMismatch(string upload,string privacy,JobState expected){var s=AdapterTests.Setup(Platform.YouTube);s.handler.Add(JsonSerializer.Serialize(new{items=new[]{new{id="video-id",snippet=new{channelId="target"},status=new{uploadStatus=upload,privacyStatus=privacy}}}}));
 var r=await new YouTubePublisher(s.http,s.oauth,s.storage).CheckPublishStatus(new(){OperationId="video-id",SnapshotJson=JsonSerializer.Serialize(Post(Video())),State=JobState.Processing},s.target,default);
 Assert.Equal(expected,r.State);Assert.False(r.SafeToRetry);if(expected!=JobState.Published){Assert.Null(r.ExternalId);Assert.Null(r.Url);}}
 [Fact] public async Task UnknownUploadHostNeverReceivesVideo(){var s=AdapterTests.Setup(Platform.YouTube);s.handler.Add("{\"items\":[{\"id\":\"target\"}]}");s.handler.Replies.Enqueue(_=>{var r=new HttpResponseMessage(HttpStatusCode.OK){Content=new StringContent("")};r.Headers.Location=new Uri("https://evil.example/upload");return r;});var m=Video();await Assert.ThrowsAsync<ProviderException>(()=>new YouTubePublisher(s.http,s.oauth,s.storage).PublishPost(new(s.target,Post(m),[m],_=>Task.CompletedTask),default));Assert.Equal(2,s.handler.Requests.Count);}
}
