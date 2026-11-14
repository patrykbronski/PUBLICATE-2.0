using Publicate;
using Xunit;
namespace Publicate.Tests;
public class ApiLimitsTests {
 static MediaAsset Image()=>new(){ContentType="image/jpeg",Width=1080,Height=1080,Bytes=100_000};
 static MediaAsset Video(double seconds)=>new(){ContentType="video/mp4",Width=1920,Height=1080,Bytes=100_000,Codec="h264",Duration=seconds};
 static List<string> Validate(Platform p,params MediaAsset[] media)=>PostValidator.Validate(p,new("test","",media.Select(m=>m.Id).ToArray(),"{}"),media);
 [Theory][InlineData(Platform.Instagram)][InlineData(Platform.LinkedIn)]
 public void TwentyImagesAcceptedTwentyOneRejected(Platform p){Assert.Empty(Validate(p,Enumerable.Range(0,20).Select(_=>Image()).ToArray()));Assert.NotEmpty(Validate(p,Enumerable.Range(0,21).Select(_=>Image()).ToArray()));}
 [Theory][InlineData(Platform.Instagram,900)][InlineData(Platform.LinkedIn,1800)]
 public void VideoDurationBoundary(Platform p,int max){Assert.Empty(Validate(p,Video(max)));Assert.NotEmpty(Validate(p,Video(max+0.01)));Assert.NotEmpty(Validate(p,Video(2.99)));}
 [Fact] public void InstagramLargeImageIsScaledByProvider(){var m=Image();m.Width=4272;m.Height=2848;Assert.Empty(Validate(Platform.Instagram,m));}
 [Fact] public void InstagramLandscapeReelAllowed(){Assert.Empty(Validate(Platform.Instagram,Video(120)));var m=Video(120);m.Width=1921;Assert.NotEmpty(Validate(Platform.Instagram,m));}
 [Fact] public void InstagramImageByteBoundary(){var m=Image();m.Bytes=8_000_000;Assert.Empty(Validate(Platform.Instagram,m));m.Bytes++;Assert.NotEmpty(Validate(Platform.Instagram,m));}
 [Fact] public void LinkedInDoesNotInheritInstagramEightMbLimit(){var m=Image();m.Bytes=9_000_000;Assert.Empty(Validate(Platform.LinkedIn,m));}
 [Fact] public void LinkedInMinimumVideoSize(){var m=Video(10);m.Bytes=74_999;Assert.NotEmpty(Validate(Platform.LinkedIn,m));m.Bytes++;Assert.Empty(Validate(Platform.LinkedIn,m));}
 [Fact] public void TikTokDurationIsDynamicNotSixtySeconds(){Assert.Null(PublicationRules.Get(Platform.TikTok).VideoMaxSeconds);Assert.Empty(Validate(Platform.TikTok,Video(120)));}
 [Fact] public void TikTokVideoDimensions(){var m=Video(120);m.Width=359;Assert.NotEmpty(Validate(Platform.TikTok,m));m.Width=360;Assert.Empty(Validate(Platform.TikTok,m));m.Height=4097;Assert.NotEmpty(Validate(Platform.TikTok,m));}
 [Fact] public void MixedCarouselStillExplicitlyUnsupported(){Assert.NotEmpty(Validate(Platform.Instagram,Image(),Video(20)));}
 [Fact] public void ProductUploadLimitRemains(){var m=Video(120);m.Bytes=PublicationRules.MaxFileBytes+1;Assert.NotEmpty(Validate(Platform.Instagram,m));}
 [Fact] public void InvalidDurationRejected(){Assert.NotEmpty(Validate(Platform.Instagram,Video(double.NaN)));}
 [Fact] public void UnimplementedPhotoCapabilitiesAreNotAdvertised(){Assert.Equal(0,Capabilities.Get(Platform.TikTok).Images);Assert.Equal(4,Capabilities.Get(Platform.X).Images);}
 [Fact] public async Task InstagramSendsAllTwentyContainersInOrder(){
 var s=AdapterTests.Setup(Platform.Instagram);var media=Enumerable.Range(0,20).Select(i=>new MediaAsset{ContentType="image/jpeg",PublicUrl=$"https://media.example.test/{i}.jpg"}).ToArray();
 for(var i=0;i<20;i++){s.handler.Add($"{{\"id\":\"child{i}\"}}");s.handler.Add("{\"status_code\":\"FINISHED\"}");}
 s.handler.Add("{\"id\":\"parent\"}");s.handler.Add("{\"status_code\":\"FINISHED\"}");s.handler.Add("{\"id\":\"post\"}");
 var result=await new InstagramPublisher(s.http,s.oauth,s.storage).PublishPost(AdapterTests.Context(s.target,media),default);
 Assert.Equal(JobState.Published,result.State);Assert.Equal("post",result.ExternalId);
 Assert.Contains("children="+string.Join("%2C",Enumerable.Range(0,20).Select(i=>"child"+i)),s.handler.Bodies[40]);
 }
 [Fact] public async Task LinkedInSendsTwentyImageReferences(){
 var s=AdapterTests.Setup(Platform.LinkedIn);var media=Enumerable.Range(0,20).Select(_=>Image()).ToArray();
 for(var i=0;i<20;i++){s.handler.Add($"{{\"value\":{{\"uploadUrl\":\"https://www.linkedin.com/upload\",\"image\":\"urn:li:image:{i}\"}}}}");s.handler.Add("{}");}
 s.handler.Add("{}",201,"urn:li:share:post");
 var result=await new LinkedInPublisher(s.http,s.oauth,s.storage).PublishPost(AdapterTests.Context(s.target,media),default);
 Assert.Equal(JobState.Published,result.State);
 using var body=System.Text.Json.JsonDocument.Parse(s.handler.Bodies[40]);
 Assert.Equal(20,body.RootElement.GetProperty("content").GetProperty("multiImage").GetProperty("images").GetArrayLength());
 }
 [Fact] public async Task TikTokRejectsAccountDurationBeforeUpload(){
 var s=AdapterTests.Setup(Platform.TikTok);s.handler.Add("{\"data\":{\"privacy_level_options\":[\"SELF_ONLY\"],\"max_video_post_duration_sec\":90},\"error\":{\"code\":\"ok\"}}");
 var c=AdapterTests.Context(s.target,Video(120));c=c with{Post=c.Post with{SettingsJson="{\"privacy\":\"SELF_ONLY\",\"consent\":true}"}};
 var result=await new TikTokPublisher(s.http,s.oauth,s.storage,s.config).PublishPost(c,default);
 Assert.Equal(JobState.Failed,result.State);Assert.Single(s.handler.Requests);Assert.DoesNotContain("video/init",s.handler.Requests[0]);
 }
}
