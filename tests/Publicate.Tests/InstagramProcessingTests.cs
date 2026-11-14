using Publicate;
using Xunit;
namespace Publicate.Tests;
public class InstagramProcessingTests {
 sealed class FastInstagram(ProviderHttp h,OAuthService o,IMediaStorage s):InstagramPublisher(h,o,s){protected override Task PauseProcessing(CancellationToken ct)=>Task.CompletedTask;}
 [Fact] public async Task SlowVideoPublishesAfterMoreThanSixPolls(){
 var s=AdapterTests.Setup(Platform.Instagram);s.handler.Add("{\"id\":\"container\"}");for(var i=0;i<8;i++)s.handler.Add("{\"status_code\":\"IN_PROGRESS\"}");s.handler.Add("{\"status_code\":\"FINISHED\"}");s.handler.Add("{\"id\":\"post\"}");
 var m=new MediaAsset{ContentType="video/mp4",PublicUrl="https://media.example.test/video.mp4"};
 var r=await new FastInstagram(s.http,s.oauth,s.storage).PublishPost(new(s.target,new("","",[m.Id],"{}"),[m],_=>Task.CompletedTask),default);
 Assert.Equal(JobState.Published,r.State);Assert.Equal("post",r.ExternalId);Assert.Single(s.handler.Requests.Where(x=>x.Contains("/media_publish")));
 }
 [Fact] public async Task ExhaustedPollsDoNotPublish(){var s=AdapterTests.Setup(Platform.Instagram);s.handler.Add("{\"id\":\"container\"}");for(var i=0;i<36;i++)s.handler.Add("{\"status_code\":\"IN_PROGRESS\"}");var m=new MediaAsset{ContentType="video/mp4",PublicUrl="https://media.example.test/video.mp4"};var r=await new FastInstagram(s.http,s.oauth,s.storage).PublishPost(new(s.target,new("","",[m.Id],"{}"),[m],_=>Task.CompletedTask),default);Assert.Equal(JobState.Processing,r.State);Assert.DoesNotContain(s.handler.Requests,x=>x.Contains("/media_publish"));}
 [Fact] public async Task RepeatedStatusKeepsSafeRetryWithoutPublishing(){var s=AdapterTests.Setup(Platform.Instagram);var p=new FastInstagram(s.http,s.oauth,s.storage);var j=new PublicationJob{State=JobState.Processing,OperationId="container"};for(var i=0;i<3;i++){s.handler.Add("{\"status_code\":\"FINISHED\"}");var r=await p.CheckPublishStatus(j,s.target,default);Assert.True(r.SafeToRetry);Assert.Equal(JobState.Failed,r.State);PublicationService.Apply(j,r);}Assert.All(s.handler.Requests,x=>Assert.StartsWith("GET ",x));}
 [Theory][InlineData(JobState.Unknown)][InlineData(JobState.Publishing)][InlineData(JobState.Failed)]
 public async Task FinishedDoesNotUnlockUncertainPublication(JobState state){var s=AdapterTests.Setup(Platform.Instagram);s.handler.Add("{\"status_code\":\"FINISHED\"}");var r=await new FastInstagram(s.http,s.oauth,s.storage).CheckPublishStatus(new(){State=state,OperationId="container"},s.target,default);Assert.False(r.SafeToRetry);Assert.Null(r.ExternalId);}
 [Fact] public void HistoricalRepairRequiresEvidenceBeforeFinalPost(){var a=new PublicationAttempt{State=JobState.Processing,Detail="Kontener przetwarzany; post nie został wysłany. Kontrola statusu nie publikuje automatycznie."};Assert.True(PublicationService.InstagramNeverPublished([a,new(){Operation="Status",State=JobState.Failed}]));Assert.False(PublicationService.InstagramNeverPublished([a,new(){Operation="Status",State=JobState.Unknown}]));a.Detail="Uncertain";Assert.False(PublicationService.InstagramNeverPublished([a]));a.State=JobState.Unknown;Assert.False(PublicationService.InstagramNeverPublished([a]));}
}
