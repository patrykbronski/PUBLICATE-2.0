using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Publicate;
using Xunit;
namespace Publicate.Tests;
public class BatchPublisher(Platform platform):ISocialPublisher {
 public Platform Platform=>platform;
 public List<string> Texts=new();public bool Fail;
 public Capability GetCapabilities()=>Capabilities.Get(platform);
 public List<string> ValidatePost(ResolvedPost p,IReadOnlyList<MediaAsset> m)=>PostValidator.Validate(platform,p,m);
 public Task<PublishResult> PublishPost(PublishContext c,CancellationToken ct){lock(Texts)Texts.Add(c.Post.Text);return Task.FromResult(Fail?new PublishResult(JobState.Failed,Detail:"Fixture refusal",SafeToRetry:true):new PublishResult(JobState.Published,ExternalId:"fixture-confirmed"));}
 public Task<PublishResult> CheckPublishStatus(PublicationJob j,PublishingTarget t,CancellationToken ct)=>Task.FromResult(new PublishResult(JobState.Published,ExternalId:"fixture-confirmed"));
}
public class BatchFactory:CallbackFactory {
 public BatchPublisher Facebook=new(Platform.Facebook),LinkedIn=new(Platform.LinkedIn);
 protected override void ConfigureWebHost(IWebHostBuilder builder){base.ConfigureWebHost(builder);builder.ConfigureTestServices(s=>{s.RemoveAll<ISocialPublisher>();s.AddSingleton<ISocialPublisher>(Facebook);s.AddSingleton<ISocialPublisher>(LinkedIn);});}
}
public class BatchTests {
 [Theory][InlineData(false)][InlineData(true)] public async Task FourPlatformsIncludingYouTubeHaveIndependentResults(bool partial){
 await using var factory=new FourPlatformFactory();using var client=factory.CreateClient();client.DefaultRequestHeaders.Add("X-Publicate","1");factory.YouTube.Fail=partial;
 var platforms=new[]{Platform.Facebook,Platform.Instagram,Platform.LinkedIn,Platform.YouTube};var variants=new List<ComposerVariant>();Guid mediaId;
 using(var scope=factory.Services.CreateScope()){
 var db=scope.ServiceProvider.GetRequiredService<AppDb>();await db.Database.EnsureCreatedAsync();
 var media=new MediaAsset{OwnerId=LocalOwner.Id,ContentType="video/mp4",Codec="h264",Width=1080,Height=1920,Duration=10,Bytes=100000};db.Add(media);mediaId=media.Id;
 foreach(var platform in platforms){var a=new ConnectedAccount{OwnerId=LocalOwner.Id,Platform=platform,Scopes=string.Join(' ',Capabilities.Get(platform).RequiredScopes)};var t=new PublishingTarget{OwnerId=LocalOwner.Id,Platform=platform,Account=a,Name=platform.ToString(),AccessToken="fixture",Kind=platform==Platform.Instagram?"InstagramLogin":"Profile"};db.Add(t);
 variants.Add(new(Guid.NewGuid(),platform,t.Id,t.Name,platform+" independent","",false,[mediaId],platform==Platform.YouTube?"{\"title\":\"Short\",\"privacy\":\"private\",\"madeForKids\":false,\"containsSyntheticMedia\":false}":"{}"));
 if(platform==Platform.Facebook)(await db.Users.SingleAsync()).PreferredFacebookTargetId=t.Id;}
 await db.SaveChangesAsync();}
 var id=Guid.NewGuid();var saved=await client.PutAsJsonAsync("/api/drafts",new SaveDraftInput(id,0,new(2,"BASE","",false,[mediaId],platforms,variants.ToArray())),DraftService.Json);saved.EnsureSuccessStatusCode();
 var input=new BatchInput(1,Guid.NewGuid(),true,true);var response=await client.PostAsJsonAsync($"/api/drafts/{id}/publish",input);response.EnsureSuccessStatusCode();
 foreach(var publisher in new[]{factory.Facebook,factory.Instagram,factory.LinkedIn,factory.YouTube})Assert.Equal(publisher.Platform+" independent",Assert.Single(publisher.Texts));
 using(var scope=factory.Services.CreateScope()){var db=scope.ServiceProvider.GetRequiredService<AppDb>();var jobs=await db.PublicationJobs.ToListAsync();Assert.Equal(4,jobs.Count);Assert.Equal(partial?JobState.Failed:JobState.Published,jobs.Single(j=>j.Platform==Platform.YouTube).State);Assert.All(jobs.Where(j=>j.Platform!=Platform.YouTube),j=>Assert.Equal(JobState.Published,j.State));}
 (await client.PostAsJsonAsync($"/api/drafts/{id}/publish",input)).EnsureSuccessStatusCode();Assert.Single(factory.YouTube.Texts);Assert.Single(factory.Facebook.Texts);
 }
 [Theory][InlineData(false)][InlineData(true)] public async Task BatchUsesIndependentSnapshotsAndNeverRepeatsSuccess(bool partial){
 await using var factory=new BatchFactory();using var client=factory.CreateClient();client.DefaultRequestHeaders.Add("X-Publicate","1");factory.LinkedIn.Fail=partial;
 var variants=new List<ComposerVariant>();
 using(var scope=factory.Services.CreateScope()){var db=scope.ServiceProvider.GetRequiredService<AppDb>();await db.Database.EnsureCreatedAsync();foreach(var platform in new[]{Platform.Facebook,Platform.LinkedIn}){var a=new ConnectedAccount{OwnerId=LocalOwner.Id,Platform=platform,Scopes=string.Join(' ',Capabilities.Get(platform).RequiredScopes)};var t=new PublishingTarget{OwnerId=LocalOwner.Id,Platform=platform,Account=a,Name=platform.ToString(),AccessToken="fixture"};db.Add(t);variants.Add(new(Guid.NewGuid(),platform,t.Id,t.Name,platform+" text","CTA",platform==Platform.Facebook,[]));if(platform==Platform.Facebook)(await db.Users.SingleAsync(u=>u.Id==LocalOwner.Id)).PreferredFacebookTargetId=t.Id;}await db.SaveChangesAsync();}
 var id=Guid.NewGuid();var state=new ComposerState(2,"BASE MUST NOT BE SENT","",false,[],[Platform.Facebook,Platform.LinkedIn],variants.ToArray());
 var saved=await client.PutAsJsonAsync("/api/drafts",new SaveDraftInput(id,0,state),DraftService.Json);Assert.Equal(HttpStatusCode.OK,saved.StatusCode);
 var key=Guid.NewGuid();var input=new BatchInput(1,key,true,false);var result=await client.PostAsJsonAsync($"/api/drafts/{id}/publish",input);Assert.Equal(HttpStatusCode.OK,result.StatusCode);
 Assert.Equal("Facebook text\n\nCTA",Assert.Single(factory.Facebook.Texts));Assert.Equal("LinkedIn text",Assert.Single(factory.LinkedIn.Texts));
 var repeat=await client.PostAsJsonAsync($"/api/drafts/{id}/publish",input);Assert.Equal(HttpStatusCode.OK,repeat.StatusCode);Assert.Single(factory.Facebook.Texts);Assert.Single(factory.LinkedIn.Texts);
 using(var scope=factory.Services.CreateScope()){var db=scope.ServiceProvider.GetRequiredService<AppDb>();Assert.Equal(!partial,(await db.Posts.SingleAsync(p=>p.Id==id)).Archived);}
 if(partial){factory.LinkedIn.Fail=false;var retry=await client.PostAsJsonAsync($"/api/drafts/{id}/publish",input with{RequestKey=Guid.NewGuid()});Assert.Equal(HttpStatusCode.OK,retry.StatusCode);Assert.Single(factory.Facebook.Texts);Assert.Equal(2,factory.LinkedIn.Texts.Count);}
 }
}
public class FourPlatformFactory:BatchFactory {
 public BatchPublisher Instagram=new(Platform.Instagram),YouTube=new(Platform.YouTube);
 protected override void ConfigureWebHost(IWebHostBuilder builder){base.ConfigureWebHost(builder);builder.ConfigureTestServices(s=>{s.AddSingleton<ISocialPublisher>(Instagram);s.AddSingleton<ISocialPublisher>(YouTube);s.RemoveAll<IMediaStorage>();s.AddSingleton<IMediaStorage,MemoryStorage>();});builder.UseSetting("Storage:PublicBaseUrl","https://media.example.test");}

}
