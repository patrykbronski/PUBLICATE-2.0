using System.Text.Json;
using Publicate;
using Xunit;
namespace Publicate.Tests;
public class TikTokTests {
 [Fact] public async Task PrivateAccountErrorIsActionableAndDoesNotLeakMessage(){var s=AdapterTests.Setup(Platform.TikTok);s.handler.Add("{\"error\":{\"code\":\"unaudited_client_can_only_post_to_private_accounts\",\"message\":\"secret-value\"}}",403);var e=await Assert.ThrowsAsync<ProviderException>(()=>s.http.Send(HttpMethod.Post,"https://open.tiktokapis.com/v2/post/publish/video/init/"));Assert.Equal(JobState.RequiresApproval,e.Result.State);Assert.Contains("prywatne",e.Result.Detail);Assert.DoesNotContain("secret-value",e.Result.Detail);}
 [Theory]
 [InlineData("https://media.example.test/video.mp4",true)]
 [InlineData("https://media.example.test.evil.test/video.mp4",false)]
 [InlineData("http://media.example.test/video.mp4",false)]
 [InlineData("https://user@media.example.test/video.mp4",false)]
 [InlineData("https://media.example.test:444/video.mp4",false)]
 public void VerifiedPrefixChecksOrigin(string url,bool expected)=>Assert.Equal(expected,TikTokRules.VerifiedUrl(url,"https://media.example.test/"));
 [Fact] public void PrefixChecksPathBoundary(){Assert.False(TikTokRules.VerifiedUrl("https://media.example.test/mediabad/a.mp4","https://media.example.test/media"));Assert.True(TikTokRules.VerifiedUrl("https://media.example.test/media/a.mp4","https://media.example.test/media/"));}
 [Theory]
 [InlineData("{\"privacy\":\"SELF_ONLY\",\"consent\":true}",0)]
 [InlineData("{\"privacy\":\"PUBLIC_TO_EVERYONE\",\"consent\":true}",1)]
 [InlineData("{\"privacy\":\"SELF_ONLY\",\"consent\":true,\"brandedContent\":true}",1)]
 [InlineData("{\"privacy\":\"SELF_ONLY\",\"consent\":true,\"commercial\":true}",1)]
 public void SettingsRequireHonestPrivateConsent(string json,int errors)=>Assert.Equal(errors,TikTokRules.Settings(json).Count);
 [Fact] public void CreatorBlocksDurationAndDisabledInteractions(){using var doc=JsonDocument.Parse("{\"privacy_level_options\":[\"SELF_ONLY\"],\"max_video_post_duration_sec\":10,\"comment_disabled\":true}");var errors=TikTokRules.Creator(doc.RootElement,"{\"privacy\":\"SELF_ONLY\",\"consent\":true,\"comment\":true}",[new(){ContentType="video/mp4",Duration=20}]);Assert.Equal(2,errors.Count);}
 [Theory]
 [InlineData("UNRECOGNIZED",JobState.Unknown)]
 [InlineData("PROCESSING_DOWNLOAD",JobState.Processing)]
 [InlineData("FAILED",JobState.Failed)]
 public async Task StatusNeverInventsSuccess(string state,JobState expected){var s=AdapterTests.Setup(Platform.TikTok);s.handler.Add(JsonSerializer.Serialize(new{data=new{status=state,fail_reason="video_pull_failed"},error=new{code="ok"}}));var r=await new TikTokPublisher(s.http,s.oauth,s.storage,s.config).CheckPublishStatus(new(){OperationId="op"},s.target,default);Assert.Equal(expected,r.State);Assert.Null(r.ExternalId);Assert.Equal(state=="FAILED",r.SafeToRetry);}
 [Fact] public async Task UnverifiedMediaNeverStartsPublishing(){var s=AdapterTests.Setup(Platform.TikTok);s.handler.Add("{\"data\":{\"privacy_level_options\":[\"SELF_ONLY\"],\"max_video_post_duration_sec\":60},\"error\":{\"code\":\"ok\"}}");var c=AdapterTests.Context(s.target,new MediaAsset{ContentType="video/mp4",Duration=5,PublicUrl="https://other.example.test/a.mp4"});c=c with{Post=c.Post with{SettingsJson="{\"privacy\":\"SELF_ONLY\",\"consent\":true}"}};var r=await new TikTokPublisher(s.http,s.oauth,s.storage,s.config).PublishPost(c,default);Assert.Equal(JobState.RequiresApproval,r.State);Assert.Single(s.handler.Requests);}
}
