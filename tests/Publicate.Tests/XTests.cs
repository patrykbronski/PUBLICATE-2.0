using System.Text.Json;
using Publicate;
using Xunit;
namespace Publicate.Tests;
public class XTests {
 class FastX(ProviderHttp h,OAuthService o,IMediaStorage s,Microsoft.Extensions.Configuration.IConfiguration c):XPublisher(h,o,s,c){protected override Task Pause(TimeSpan d,CancellationToken ct)=>Task.CompletedTask;}
 static MediaAsset Image()=>new(){ContentType="image/jpeg",Bytes=3,Width=640,Height=480,Codec="mjpeg"};
 static MediaAsset Video()=>new(){ContentType="video/mp4",Bytes=3,Width=720,Height=1280,Duration=44,Codec="h264"};
 [Theory][InlineData("Zażółć gęślą jaźń",17)][InlineData("👨‍👩‍👧‍👦",2)][InlineData("https://example.com/very/long/path",23)][InlineData("漢字",4)][InlineData("e\u0301",1)]
 public void OfficialWeightedText(string text,int count)=>Assert.Equal(count,XText.Analyze(text).Length);
 [Fact] public void EmojiBoundary(){Assert.True(XText.Analyze(string.Concat(Enumerable.Repeat("😀",140))).Valid);Assert.False(XText.Analyze(string.Concat(Enumerable.Repeat("😀",141))).Valid);}
 [Fact] public void MediaLimits(){var a=Image();a.Bytes=5_000_001;Assert.NotEmpty(PostValidator.Validate(Platform.X,new("","",[a.Id],"{}"),[a]));Assert.NotEmpty(PostValidator.Validate(Platform.X,new("","",[],"{}"),[]));var images=Enumerable.Range(0,5).Select(_=>Image()).ToArray();Assert.NotEmpty(PostValidator.Validate(Platform.X,new("","",images.Select(x=>x.Id).ToArray(),"{}"),images));}
 [Theory][InlineData(false)][InlineData(true)]
 public async Task UploadBeforeCreateAndPreserveOrder(bool video){
 var s=AdapterTests.Setup(Platform.X,true);s.target.Account.Scopes+=" media.write";
 var media=video?new[]{Video()}:new[]{Image(),Image()};
 for(var i=0;i<media.Length;i++){s.handler.Add("{\"data\":{\"id\":\""+(100+i)+"\"}}");s.handler.Add("{}");s.handler.Add("{\"data\":{\"id\":\""+(100+i)+"\",\"processing_info\":{\"state\":\"succeeded\"}}}");}
 s.handler.Add("{\"data\":{\"id\":\"999\"}}",201);
 var result=await new FastX(s.http,s.oauth,s.storage,s.config).PublishPost(AdapterTests.Context(s.target,media),default);
 Assert.Equal(JobState.Published,result.State);Assert.Equal("https://x.com/i/status/999",result.Url);
 using var body=JsonDocument.Parse(s.handler.Bodies.Last());Assert.Equal(media.Length,body.RootElement.GetProperty("media").GetProperty("media_ids").GetArrayLength());Assert.Equal("100",body.RootElement.GetProperty("media").GetProperty("media_ids")[0].GetString());Assert.Contains(video?"tweet_video":"tweet_image",s.handler.Bodies[0]);Assert.Contains("segment_index",s.handler.Bodies[1]);
 }
 [Fact] public async Task MissingScopeStopsBeforeHttp(){var s=AdapterTests.Setup(Platform.X,true);var r=await new FastX(s.http,s.oauth,s.storage,s.config).PublishPost(AdapterTests.Context(s.target,Image()),default);Assert.Equal(JobState.RequiresAuthorization,r.State);Assert.Empty(s.handler.Requests);}
 [Fact] public async Task ProcessingFailureNeverCreatesPost(){var s=AdapterTests.Setup(Platform.X,true);s.target.Account.Scopes+=" media.write";s.handler.Add("{\"data\":{\"id\":\"100\"}}");s.handler.Add("{}");s.handler.Add("{\"data\":{\"processing_info\":{\"state\":\"pending\",\"check_after_secs\":1}}}");s.handler.Add("{\"data\":{\"processing_info\":{\"state\":\"failed\"}}}");var r=await new FastX(s.http,s.oauth,s.storage,s.config).PublishPost(AdapterTests.Context(s.target,Video()),default);Assert.Equal(JobState.Failed,r.State);Assert.True(r.SafeToRetry);Assert.DoesNotContain(s.handler.Requests,x=>x.Contains("/tweets"));}
 [Fact] public async Task UncertainPostCannotBeUnlockedByMediaStatus(){var s=AdapterTests.Setup(Platform.X,true);var r=await new FastX(s.http,s.oauth,s.storage,s.config).CheckPublishStatus(new(){State=JobState.Unknown,OperationId="x:posting"},s.target,default);Assert.Equal(JobState.Unknown,r.State);Assert.False(r.SafeToRetry);Assert.Empty(s.handler.Requests);}
 [Fact] public async Task CompletedMediaNeedsNewUserConsent(){var s=AdapterTests.Setup(Platform.X,true);s.handler.Add("{\"data\":{\"processing_info\":{\"state\":\"succeeded\"}}}");var r=await new FastX(s.http,s.oauth,s.storage,s.config).CheckPublishStatus(new(){State=JobState.Processing,OperationId="x:media:100"},s.target,default);Assert.Equal(JobState.Failed,r.State);Assert.True(r.SafeToRetry);Assert.Single(s.handler.Requests);Assert.StartsWith("GET",s.handler.Requests[0]);}
 [Fact] public async Task MissingPostIdIsNotSuccess(){var s=AdapterTests.Setup(Platform.X,true);s.handler.Add("{\"data\":{}}",201);var e=await Assert.ThrowsAsync<ProviderException>(()=>new FastX(s.http,s.oauth,s.storage,s.config).PublishPost(AdapterTests.Context(s.target),default));Assert.Equal(JobState.Unknown,e.Result.State);Assert.False(e.Result.SafeToRetry);}
 [Fact] public async Task BillingFailureDoesNotLeakProviderText(){var s=AdapterTests.Setup(Platform.X,true);s.handler.Add("{\"type\":\"https://api.x.com/2/problems/credits-depleted\",\"detail\":\"private-secret\"}",402);var e=await Assert.ThrowsAsync<ProviderException>(()=>s.http.Send(HttpMethod.Post,"https://api.x.com/2/tweets"));Assert.Equal(JobState.RequiresApproval,e.Result.State);Assert.Equal("credits-depleted",e.Result.ProviderCode);Assert.DoesNotContain("private-secret",e.Result.Detail);}
}
