using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Publicate;
using Xunit;
namespace Publicate.Tests;
public class StubPublisher:ISocialPublisher {
 public Platform Platform=>Platform.LinkedIn;
 public int Calls;
 public PublishResult Result=new(JobState.Published,ExternalId:"provider-confirmed");
 public Capability GetCapabilities()=>Capabilities.Get(Platform);
 public List<string> ValidatePost(ResolvedPost p,IReadOnlyList<MediaAsset> m)=>PostValidator.Validate(Platform,p,m);
 public Task<PublishResult> PublishPost(PublishContext c,CancellationToken ct){Calls++;return Task.FromResult(Result);}
 public Task<PublishResult> CheckPublishStatus(PublicationJob j,PublishingTarget t,CancellationToken ct)=>Task.FromResult(Result);
}
public class ServiceTests {
 static async Task<(PublicationService service,AppDb db,StubPublisher publisher,PostVariant variant)> Setup(){
 var s=AdapterTests.Setup(Platform.LinkedIn);var p=new Post{Description="Test"};var v=new PostVariant{Post=p,PostId=p.Id,Target=s.target,TargetId=s.target.Id};s.db.Add(v);await s.db.SaveChangesAsync();var publisher=new StubPublisher();return(new(s.db,[publisher],s.storage,s.config),s.db,publisher,v);
 }
 [Fact] public async Task IdempotencyKeyReturnsSameJobAndOnlyCallsProviderOnce(){var s=await Setup();var key=Guid.NewGuid();var a=await s.service.Publish(s.variant.Id,key,true,false);var b=await s.service.Publish(s.variant.Id,key,true,false);Assert.Equal(a.Id,b.Id);Assert.Equal(1,s.publisher.Calls);Assert.Single(await s.db.PublicationAttempts.ToListAsync());}
 [Fact] public async Task SecondKeyCannotDuplicatePublishedVariant(){var s=await Setup();await s.service.Publish(s.variant.Id,Guid.NewGuid(),true,false);await Assert.ThrowsAsync<InvalidOperationException>(()=>s.service.Publish(s.variant.Id,Guid.NewGuid(),true,false));Assert.Equal(1,s.publisher.Calls);}
 [Fact] public async Task UnknownNeverAutomaticallyRetries(){var s=await Setup();s.publisher.Result=new(JobState.Unknown);await s.service.Publish(s.variant.Id,Guid.NewGuid(),true,false);await Assert.ThrowsAsync<InvalidOperationException>(()=>s.service.Publish(s.variant.Id,Guid.NewGuid(),true,false));Assert.Equal(1,s.publisher.Calls);}
 [Fact] public async Task PermissionRejectionCanBeManuallyRetried(){var s=await Setup();s.publisher.Result=new(JobState.RequiresApproval,SafeToRetry:true);await s.service.Publish(s.variant.Id,Guid.NewGuid(),true,false);s.publisher.Result=new(JobState.Published,ExternalId:"new");var j=await s.service.Publish(s.variant.Id,Guid.NewGuid(),true,false);Assert.Equal(JobState.Published,j.State);Assert.Equal(2,s.publisher.Calls);}
 [Fact] public async Task ConfirmationRequiredBeforeCreatingJob(){var s=await Setup();await Assert.ThrowsAsync<InvalidOperationException>(()=>s.service.Publish(s.variant.Id,Guid.NewGuid(),false,false));Assert.Empty(await s.db.PublicationJobs.ToListAsync());Assert.Equal(0,s.publisher.Calls);}
 [Fact] public async Task ForeignOwnerVariantCannotBePublished(){var s=await Setup();s.variant.OwnerId=Guid.NewGuid();await s.db.SaveChangesAsync();await Assert.ThrowsAsync<KeyNotFoundException>(()=>s.service.Publish(s.variant.Id,Guid.NewGuid(),true,false));Assert.Equal(0,s.publisher.Calls);}
 [Fact] public async Task ForeignOwnerMediaCannotBeLoaded(){var s=await Setup();var media=new MediaAsset{OwnerId=Guid.NewGuid()};s.db.Add(media);await s.db.SaveChangesAsync();Assert.Empty(await s.service.Media([media.Id]));}
 [Fact] public async Task MissingScopePreventsHttp(){var s=await Setup();s.variant.Target.Account.Scopes="openid profile";await s.db.SaveChangesAsync();await Assert.ThrowsAsync<InvalidOperationException>(()=>s.service.Publish(s.variant.Id,Guid.NewGuid(),true,false));Assert.Equal(0,s.publisher.Calls);}
 [Fact] public async Task JobSnapshotDoesNotChangeWithBasePost(){var s=await Setup();var j=await s.service.Publish(s.variant.Id,Guid.NewGuid(),true,false);s.variant.Post.Description="Zmiana";await s.db.SaveChangesAsync();Assert.Contains("Test",j.SnapshotJson);Assert.DoesNotContain("Zmiana",j.SnapshotJson);}
 [Fact] public async Task CleanupDeletesFilesButPreservesMetadata(){
 var s=AdapterTests.Setup(Platform.LinkedIn);var now=DateTimeOffset.UtcNow;var old=new MediaAsset{Key="old.jpg",LastUsedAt=now.AddDays(-5),DeleteWarningAt=now.AddDays(-2)};var pending=new MediaAsset{Key="pending.jpg",LastUsedAt=now.AddDays(-60),DeleteWarningAt=now.AddDays(-2)};
 s.db.AddRange(old,pending);s.db.Add(new PublicationJob{MediaIds=[pending.Id],State=JobState.Unknown});await s.db.SaveChangesAsync();
 var services=new ServiceCollection().AddSingleton(s.db).AddSingleton<IMediaStorage>(s.storage).BuildServiceProvider();
 var worker=new CleanupWorker(services.GetRequiredService<IServiceScopeFactory>(),s.config,NullLogger<CleanupWorker>.Instance);await worker.Clean(services,default);
 Assert.Equal(new[]{"old.jpg"},s.storage.Deleted);Assert.NotNull(old.DeletedAt);Assert.Null(pending.DeletedAt);Assert.Equal(2,await s.db.MediaAssets.CountAsync());
 }
 [Fact] public async Task LinkedInUnknownAfterFinalPostCannotRetryFromVideoStatus(){var s=AdapterTests.Setup(Platform.LinkedIn);var r=await new LinkedInPublisher(s.http,s.oauth,s.storage).CheckPublishStatus(new(){State=JobState.Unknown,OperationId="urn:li:video:test"},s.target,default);Assert.Equal(JobState.Unknown,r.State);Assert.False(r.SafeToRetry);Assert.Empty(s.handler.Requests);}
}
