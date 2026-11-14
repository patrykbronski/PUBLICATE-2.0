using System.Net;
using System.Net.Http.Headers;
using Publicate;
using Xunit;
namespace Publicate.Tests;

public class YouTubeRecoveryTests {
 const string Session="https://www.googleapis.com/upload/youtube/v3/videos?upload_id=fixture-secret";
 static MediaAsset Media()=>new(){ContentType="video/mp4",Bytes=3};
 static HttpResponseMessage Incomplete(string? range){var r=new HttpResponseMessage((HttpStatusCode)308){Content=new StringContent("")};if(range!=null)r.Headers.TryAddWithoutValidation("Range",range);r.Headers.RetryAfter=new RetryConditionHeaderValue(TimeSpan.Zero);return r;}
 [Fact] public async Task LostResponseChecksSessionInsteadOfCreatingDuplicate(){
 var s=AdapterTests.Setup(Platform.YouTube);s.handler.Replies.Enqueue(_=>throw new HttpRequestException("fixture"));
 s.handler.Replies.Enqueue(req=>{Assert.Equal("bytes */3",req.Content!.Headers.ContentRange!.ToString());Assert.Equal(0,req.Content.Headers.ContentLength);return new(HttpStatusCode.Created){Content=new StringContent("{\"id\":\"existing-video\"}")};});
 var r=await s.http.UploadYouTube(Session,"fixture",Media(),s.storage,default);
 Assert.Equal("existing-video",ProviderHttp.Required(r.Json,"id"));Assert.Equal(2,s.handler.Requests.Count);Assert.All(s.handler.Requests,x=>Assert.Equal("PUT "+Session,x));
 }
 [Fact] public async Task ResumeSendsOnlyUnacknowledgedBytes(){
 var s=AdapterTests.Setup(Platform.YouTube);s.handler.Replies.Enqueue(_=>Incomplete("bytes=0-0"));
 s.handler.Replies.Enqueue(req=>{Assert.Equal("bytes 1-2/3",req.Content!.Headers.ContentRange!.ToString());Assert.Equal(2,req.Content.Headers.ContentLength);return new(HttpStatusCode.Created){Content=new StringContent("{\"id\":\"video\"}")};});
 await s.http.UploadYouTube(Session,"fixture",Media(),s.storage,default);Assert.Equal(new string(new[]{(char)2,(char)3}),s.handler.Bodies[1]);
 }
 [Theory][InlineData("bytes=1-2")][InlineData("bytes=0-3")][InlineData("invalid")]
 public async Task InvalidRangeStopsWithoutUnsafeReplay(string range){var s=AdapterTests.Setup(Platform.YouTube);s.handler.Replies.Enqueue(_=>Incomplete(range));var e=await Assert.ThrowsAsync<ProviderException>(()=>s.http.UploadYouTube(Session,"fixture",Media(),s.storage,default));Assert.Equal(JobState.Unknown,e.Result.State);Assert.False(e.Result.SafeToRetry);Assert.Single(s.handler.Requests);}
 [Fact] public async Task ExpiredSessionDoesNotStartANewUpload(){var s=AdapterTests.Setup(Platform.YouTube);s.handler.Add("{}",404);var e=await Assert.ThrowsAsync<ProviderException>(()=>s.http.UploadYouTube(Session,"fixture",Media(),s.storage,default));Assert.False(e.Result.SafeToRetry);Assert.Single(s.handler.Requests);Assert.DoesNotContain("fixture-secret",e.Message);}
 [Fact] public async Task ServerErrorProbesBeforeResuming(){var s=AdapterTests.Setup(Platform.YouTube);s.handler.Add("{}",503);s.handler.Replies.Enqueue(req=>{Assert.Equal("bytes */3",req.Content!.Headers.ContentRange!.ToString());return Incomplete("bytes=0-1");});s.handler.Add("{\"id\":\"video\"}",201);await s.http.UploadYouTube(Session,"fixture",Media(),s.storage,default);Assert.Equal(((char)3).ToString(),s.handler.Bodies[2]);}
 [Theory][InlineData("invalid_grant",JobState.RequiresAuthorization)][InlineData("invalid_client",JobState.RequiresAuthorization)]
 public async Task OAuthErrorDoesNotLeakDescription(string code,JobState state){var s=AdapterTests.Setup(Platform.YouTube);s.handler.Add("{\"error\":\""+code+"\",\"error_description\":\"secret-data\"}",400);var e=await Assert.ThrowsAsync<ProviderException>(()=>s.http.Send(HttpMethod.Post,"https://oauth2.googleapis.com/token",operation:"youtube.token_refresh"));Assert.Equal(state,e.Result.State);Assert.Equal(code,e.Result.ProviderCode);Assert.DoesNotContain("secret-data",e.Message);}
 [Fact] public async Task QuotaIsNotMisreportedAsMissingScope(){var s=AdapterTests.Setup(Platform.YouTube);s.handler.Add("{\"error\":{\"code\":403,\"errors\":[{\"reason\":\"quotaExceeded\"}],\"message\":\"secret\"}}",403);var e=await Assert.ThrowsAsync<ProviderException>(()=>s.http.Send(HttpMethod.Get,"https://www.googleapis.com/youtube/v3/channels",operation:"youtube.channels"));Assert.Equal("quotaExceeded",e.Result.ProviderCode);Assert.Contains("limit",e.Message);Assert.DoesNotContain("secret",e.Message);}
 [Fact] public void SyntheticDisclosureMustBeExplicit(){var m=Media();m.Width=1080;m.Height=1920;m.Duration=1;var p=new ResolvedPost("","",[m.Id],"{\"title\":\"test\",\"privacy\":\"private\",\"madeForKids\":false}");Assert.Contains(YouTubeRules.Validate(p,[m]),x=>x.Contains("syntetyczne"));}
}
