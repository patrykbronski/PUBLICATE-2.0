using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Publicate;
using Xunit;
namespace Publicate.Tests;
public class DraftTests {
 static ComposerState Base(string text="Base")=>new(1,text,"CTA",true,[],[Platform.LinkedIn],[]);
 [Fact] public async Task EleventhDraftEvictsOldestAndAutosaveDoesNotExtendExpiry(){
 var s=AdapterTests.Setup(Platform.LinkedIn);var service=new DraftService(s.db,new(s.db,[],s.storage,s.config));
 for(var i=0;i<10;i++){var saved=await service.Save(new(Guid.NewGuid(),0,Base()));saved.Post.CreatedAt=DateTimeOffset.UtcNow.AddHours(-20+i);await s.db.SaveChangesAsync();}
 var oldest=await s.db.Posts.OrderBy(p=>p.CreatedAt).FirstAsync();var next=await service.Save(new(Guid.NewGuid(),0,Base()));Assert.Equal(oldest.Id,next.Evicted);Assert.True(oldest.Archived);
 var created=next.Post.CreatedAt;await service.Save(new(next.Post.Id,next.Post.Revision,Base("Updated")));Assert.Equal(created,next.Post.CreatedAt);Assert.Equal(10,await s.db.Posts.CountAsync(p=>!p.Archived));
 }
 [Fact] public async Task ExpiredAndStaleRevisionCannotSave(){var s=AdapterTests.Setup(Platform.LinkedIn);var service=new DraftService(s.db,new(s.db,[],s.storage,s.config));var p=(await service.Save(new(Guid.NewGuid(),0,Base()))).Post;
 await Assert.ThrowsAsync<InvalidOperationException>(()=>service.Save(new(p.Id,0,Base())));p.CreatedAt=DateTimeOffset.UtcNow.AddHours(-49);await s.db.SaveChangesAsync();await Assert.ThrowsAsync<InvalidOperationException>(()=>service.Save(new(p.Id,p.Revision,Base())));
 }
 [Fact] public async Task VariantIsSnapshotAndResetDoesNotChangeBase(){var s=AdapterTests.Setup(Platform.LinkedIn);s.db.Add(s.target);await s.db.SaveChangesAsync();var service=new DraftService(s.db,new(s.db,[],s.storage,s.config));var p=(await service.Save(new(Guid.NewGuid(),0,Base()))).Post;
 var v=new ComposerVariant(Guid.NewGuid(),Platform.LinkedIn,s.target.Id,"Target","Independent","CTA",false,[]);
 await service.Save(new(p.Id,p.Revision,Base() with{Step=2,Variants=[v]}));var stored=await s.db.PostVariants.SingleAsync();stored.Post=p;Assert.Equal("Independent",stored.Resolve().Text);Assert.Equal("Base",p.Description);
 await service.Save(new(p.Id,p.Revision,Base()));Assert.Empty(await s.db.PostVariants.ToListAsync());Assert.Equal("Base",p.Description);
 }
 [Fact] public async Task ForeignMediaCannotBeReferenced(){var s=AdapterTests.Setup(Platform.LinkedIn);var a=new MediaAsset{OwnerId=Guid.NewGuid()};s.db.Add(a);await s.db.SaveChangesAsync();var service=new DraftService(s.db,new(s.db,[],s.storage,s.config));await Assert.ThrowsAsync<InvalidOperationException>(()=>service.Save(new(Guid.NewGuid(),0,Base() with{MediaIds=[a.Id]})));}
 [Fact] public async Task ActiveDraftProtectsSharedFileButExpiredDraftDoesNot(){
 var s=AdapterTests.Setup(Platform.LinkedIn);var a=new MediaAsset{Key="shared.jpg",CreatedAt=DateTimeOffset.UtcNow.AddDays(-3)};var old=new Post{CreatedAt=DateTimeOffset.UtcNow.AddDays(-3),MediaIds=[a.Id]};var active=new Post{MediaIds=[a.Id]};s.db.AddRange(a,old,active);await s.db.SaveChangesAsync();
 var services=new ServiceCollection().AddSingleton(s.db).AddSingleton<IMediaStorage>(s.storage).BuildServiceProvider();var worker=new CleanupWorker(services.GetRequiredService<IServiceScopeFactory>(),s.config,NullLogger<CleanupWorker>.Instance);
 await worker.Clean(services,default);Assert.Null(a.DeletedAt);active.Archived=true;await s.db.SaveChangesAsync();await worker.Clean(services,default);Assert.NotNull(a.DeletedAt);
 }
 [Theory][InlineData(JobState.Processing)][InlineData(JobState.Unknown)][InlineData(JobState.Published)]
 public async Task PublishedAndUncertainVariantsCannotBeEditedOrReset(JobState status){var s=AdapterTests.Setup(Platform.LinkedIn);s.db.Add(s.target);await s.db.SaveChangesAsync();var service=new DraftService(s.db,new(s.db,[],s.storage,s.config));var p=(await service.Save(new(Guid.NewGuid(),0,Base()))).Post;var v=new ComposerVariant(Guid.NewGuid(),Platform.LinkedIn,s.target.Id,"Target","Original","",false,[]);var state=Base() with{Step=2,Variants=[v]};await service.Save(new(p.Id,p.Revision,state));s.db.Add(new PublicationJob{VariantId=v.Id,State=status});await s.db.SaveChangesAsync();await Assert.ThrowsAsync<InvalidOperationException>(()=>service.Save(new(p.Id,p.Revision,state with{Variants=[v with{Description="Changed"}]})));await Assert.ThrowsAsync<InvalidOperationException>(()=>service.Save(new(p.Id,p.Revision,Base())));}
 [Fact] public void BatchRequestIdsAreStableAndSeparate(){var batch=Guid.NewGuid();var variant=Guid.NewGuid();Assert.Equal(DraftService.RequestId(batch,variant),DraftService.RequestId(batch,variant));Assert.NotEqual(DraftService.RequestId(batch,variant),DraftService.RequestId(batch,Guid.NewGuid()));}
}
