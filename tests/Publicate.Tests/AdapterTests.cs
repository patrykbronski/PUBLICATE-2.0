using System.Net;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Publicate;
using Xunit;
namespace Publicate.Tests;
public class FakeHandler:HttpMessageHandler,IHttpClientFactory {
 public Queue<Func<HttpRequestMessage,HttpResponseMessage>> Replies {get;}=new();public List<string> Requests {get;}=[];public List<string> Bodies {get;}=[];
 protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage r,CancellationToken ct){Requests.Add(r.Method+" "+r.RequestUri);Bodies.Add(r.Content==null?"":await r.Content.ReadAsStringAsync(ct));if(Replies.Count==0)throw new InvalidOperationException("Unexpected HTTP request");return Replies.Dequeue()(r);}
 public HttpClient CreateClient(string name)=>new(this,false);
 public void Add(string json,int status=200,string? id=null)=>Replies.Enqueue(_=>{var r=new HttpResponseMessage((HttpStatusCode)status){Content=new StringContent(json,Encoding.UTF8,"application/json")};if(id!=null)r.Headers.Add("x-restli-id",id);return r;});
}
public class MemoryStorage:IMediaStorage {
 public List<string> Deleted {get;}=[];
 public Task Put(string k,Stream s,string t,CancellationToken ct)=>Task.CompletedTask;
 public Task<Stream> Read(string k,CancellationToken ct)=>Task.FromResult<Stream>(new MemoryStream([1,2,3]));
 public Task<string> Expose(MediaAsset a,CancellationToken ct)=>Task.FromResult("https://media.example.test/"+a.Key);
 public Task Delete(MediaAsset a,CancellationToken ct){Deleted.Add(a.Key);return Task.CompletedTask;}
}
public class AdapterTests {
 [Fact] public async Task LinkedInVideoAcceptsEmptyUploadTokenAndPartialLastChunk(){
 var s=Setup(Platform.LinkedIn);
 s.handler.Add("{\"value\":{\"video\":\"urn:li:video:1\",\"uploadToken\":\"\",\"uploadInstructions\":[{\"uploadUrl\":\"https://www.linkedin.com/upload\",\"firstByte\":0,\"lastByte\":4194303}]}}");
 s.handler.Replies.Enqueue(_=>{var response=new HttpResponseMessage(HttpStatusCode.OK){Content=new StringContent("")};response.Headers.TryAddWithoutValidation("ETag","/ambry-video/part-one");return response;});
 s.handler.Add("{}");s.handler.Add("{\"status\":\"AVAILABLE\"}");s.handler.Add("{}",201,"urn:li:share:video");
 var r=await new LinkedInPublisher(s.http,s.oauth,s.storage).PublishPost(Context(s.target,new MediaAsset{ContentType="video/mp4",Bytes=3}),default);
 Assert.Equal(JobState.Published,r.State);Assert.Contains("/ambry-video/part-one",s.handler.Bodies[2]);Assert.Contains("\"uploadToken\":\"\"",s.handler.Bodies[2]);
 }
 [Fact] public async Task TikTokUploadsAfterCreatorOptionsAndConsent(){
 var s=Setup(Platform.TikTok);s.handler.Add("{\"data\":{\"privacy_level_options\":[\"SELF_ONLY\"],\"max_video_post_duration_sec\":60},\"error\":{\"code\":\"ok\"}}");
 s.handler.Add("{\"data\":{\"publish_id\":\"operation\",\"upload_url\":\"https://open-upload.tiktokapis.com/upload\"},\"error\":{\"code\":\"ok\"}}");
 var m=new MediaAsset{ContentType="video/mp4",Bytes=3,Duration=3,PublicUrl="https://media.example.test/video.mp4"};var c=Context(s.target,m);c=c with{Post=c.Post with{SettingsJson="{\"privacy\":\"SELF_ONLY\",\"consent\":true}"}};
 var r=await new TikTokPublisher(s.http,s.oauth,s.storage,s.config).PublishPost(c,default);Assert.Equal(JobState.Processing,r.State);Assert.Null(r.ExternalId);Assert.Contains("SELF_ONLY",s.handler.Bodies[1]);Assert.Contains("PULL_FROM_URL",s.handler.Bodies[1]);Assert.Equal(2,s.handler.Requests.Count);
 }
 public static (FakeHandler handler,ProviderHttp http,OAuthService oauth,MemoryStorage storage,PublishingTarget target,IConfiguration config,AppDb db) Setup(Platform p,bool paid=false) {
 var handler=new FakeHandler();var http=new ProviderHttp(handler);var vault=new TokenVault(new EphemeralDataProtectionProvider());
 var config=new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string,string?>{{"Providers:LinkedIn:Version","202607"},{"Providers:Meta:Version","v25.0"},{"Providers:Instagram:Version","v25.0"},{"Providers:X:AllowPaidCalls",paid.ToString()},{"Providers:TikTok:PublishingApproved","true"},{"Providers:TikTok:VerifiedUrlPrefix","https://media.example.test/"}}).Build();
 var db=new AppDb(new DbContextOptionsBuilder<AppDb>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
 var account=new ConnectedAccount{Platform=p,AccessToken=vault.Seal("test-token"),ExpiresAt=DateTimeOffset.UtcNow.AddHours(1),Scopes=string.Join(" ",Capabilities.Get(p).RequiredScopes)};
 var target=new PublishingTarget{Account=account,AccountId=account.Id,Platform=p,ExternalId=p==Platform.LinkedIn?"urn:li:person:test":"target",Name="Test",Kind=p==Platform.Instagram?"InstagramLogin":"Profile"};
 return(handler,http,new(config,http,vault,db,new()),new(),target,config,db);
 }
 public static PublishContext Context(PublishingTarget t,params MediaAsset[] assets)=>new(t,new("Test","",assets.Select(x=>x.Id).ToArray(),"{}"),assets,_=>Task.CompletedTask);
 [Fact] public async Task LinkedInRequires201AndId(){var s=Setup(Platform.LinkedIn);s.handler.Add("{}",201,"urn:li:share:1");var r=await new LinkedInPublisher(s.http,s.oauth,s.storage).PublishPost(Context(s.target),default);Assert.Equal(JobState.Published,r.State);Assert.Equal("urn:li:share:1",r.ExternalId);Assert.Contains("/rest/posts",s.handler.Requests[0]);Assert.Contains("PUBLISHED",s.handler.Bodies[0]);}
 [Fact] public async Task LinkedInMissingIdIsUnknown(){var s=Setup(Platform.LinkedIn);s.handler.Add("{}",201);var r=await new LinkedInPublisher(s.http,s.oauth,s.storage).PublishPost(Context(s.target),default);Assert.Equal(JobState.Unknown,r.State);Assert.Null(r.ExternalId);}
 [Fact] public async Task LinkedInImageUploadsBeforePost(){var s=Setup(Platform.LinkedIn);s.handler.Add("{\"value\":{\"uploadUrl\":\"https://www.linkedin.com/upload\",\"image\":\"urn:li:image:1\"}}");s.handler.Add("{}");s.handler.Add("{}",201,"urn:li:share:2");var m=new MediaAsset{ContentType="image/jpeg"};var r=await new LinkedInPublisher(s.http,s.oauth,s.storage).PublishPost(Context(s.target,m),default);Assert.Equal(JobState.Published,r.State);Assert.StartsWith("PUT",s.handler.Requests[1]);Assert.Contains("urn:li:image:1",s.handler.Bodies[2]);}
 [Fact] public async Task FacebookTextConfirmsProviderId(){var s=Setup(Platform.Facebook);s.handler.Add("{\"id\":\"page_post\"}");var r=await new FacebookPublisher(s.http,s.oauth,s.storage).PublishPost(Context(s.target),default);Assert.Equal("page_post",r.ExternalId);Assert.Equal(JobState.Published,r.State);}
 [Fact] public async Task FacebookMultiPhotoAttachesUploadedIds(){var s=Setup(Platform.Facebook);s.handler.Add("{\"id\":\"photo1\"}");s.handler.Add("{\"id\":\"photo2\"}");s.handler.Add("{\"id\":\"post\"}");var r=await new FacebookPublisher(s.http,s.oauth,s.storage).PublishPost(Context(s.target,new MediaAsset(){ContentType="image/jpeg"},new(){ContentType="image/jpeg"}),default);Assert.Equal(JobState.Published,r.State);Assert.Contains("photo1",s.handler.Bodies[2]);Assert.Contains("photo2",s.handler.Bodies[2]);Assert.Contains("false",s.handler.Bodies[0]);}
 [Fact] public async Task FacebookVideoNotImmediatelyPublished(){var s=Setup(Platform.Facebook);s.handler.Add("{\"id\":\"video1\"}");var r=await new FacebookPublisher(s.http,s.oauth,s.storage).PublishPost(Context(s.target,new MediaAsset(){ContentType="video/mp4"}),default);Assert.Equal(JobState.Processing,r.State);Assert.Null(r.ExternalId);Assert.Equal("video1",r.OperationId);}
 [Fact] public async Task InstagramContainerIsNotPost(){var s=Setup(Platform.Instagram);s.handler.Add("{\"id\":\"container\"}");s.handler.Add("{\"status_code\":\"FINISHED\"}");s.handler.Add("{\"id\":\"actual_post\"}");var r=await new InstagramPublisher(s.http,s.oauth,s.storage).PublishPost(Context(s.target,new MediaAsset(){ContentType="image/jpeg",PublicUrl="https://media.example.test/image.jpg"}),default);Assert.Equal("actual_post",r.ExternalId);Assert.Equal("container",r.OperationId);Assert.Equal(JobState.Published,r.State);}
 [Fact] public async Task InstagramCarouselPreservesOrder(){var s=Setup(Platform.Instagram);foreach(var id in new[]{"first","second"}){s.handler.Add("{\"id\":\""+id+"\"}");s.handler.Add("{\"status_code\":\"FINISHED\"}");}s.handler.Add("{\"id\":\"parent\"}");s.handler.Add("{\"status_code\":\"FINISHED\"}");s.handler.Add("{\"id\":\"post\"}");var r=await new InstagramPublisher(s.http,s.oauth,s.storage).PublishPost(Context(s.target,new MediaAsset(){ContentType="image/jpeg",PublicUrl="https://media.example.test/a"},new(){ContentType="image/jpeg",PublicUrl="https://media.example.test/b"}),default);Assert.Equal(JobState.Published,r.State);Assert.Contains("children=first%2Csecond",s.handler.Bodies[4]);}
 [Fact] public async Task InstagramUnknownCannotBecomeRetryable(){var s=Setup(Platform.Instagram);s.handler.Add("{\"status_code\":\"FINISHED\"}");var r=await new InstagramPublisher(s.http,s.oauth,s.storage).CheckPublishStatus(new(){State=JobState.Unknown,OperationId="container"},s.target,default);Assert.False(r.SafeToRetry);Assert.Equal(JobState.Unknown,r.State);}
 [Fact] public async Task XDisabledMakesNoHttpCall(){var s=Setup(Platform.X);var r=await new XPublisher(s.http,s.oauth,s.storage,s.config).PublishPost(Context(s.target),default);Assert.Equal(JobState.RequiresApproval,r.State);Assert.Empty(s.handler.Requests);}
 [Fact] public async Task XOptInUsesRealResponse(){var s=Setup(Platform.X,true);s.handler.Add("{\"data\":{\"id\":\"123\"}}",201);var r=await new XPublisher(s.http,s.oauth,s.storage,s.config).PublishPost(Context(s.target),default);Assert.Equal("123",r.ExternalId);}
 [Fact] public async Task TikTokConsentRequiredBeforeAnyRequest(){var s=Setup(Platform.TikTok);var r=await new TikTokPublisher(s.http,s.oauth,s.storage,s.config).PublishPost(Context(s.target),default);Assert.Equal(JobState.RequiresApproval,r.State);Assert.Empty(s.handler.Requests);}
 [Fact] public async Task TikTokPrivateCompletionMayNotExposePostId(){var s=Setup(Platform.TikTok);s.handler.Add("{\"data\":{\"status\":\"PUBLISH_COMPLETE\"},\"error\":{\"code\":\"ok\"}}");var r=await new TikTokPublisher(s.http,s.oauth,s.storage,s.config).CheckPublishStatus(new(){OperationId="operation"},s.target,default);Assert.Equal(JobState.Published,r.State);Assert.Null(r.ExternalId);Assert.Null(r.Url);}
 [Theory][InlineData(401,JobState.RequiresAuthorization,true)][InlineData(403,JobState.RequiresApproval,true)][InlineData(429,JobState.Failed,true)][InlineData(503,JobState.Unknown,false)] public async Task ErrorsClassifiedWithoutLeakingSecrets(int status,JobState expected,bool retry){var s=Setup(Platform.LinkedIn);s.handler.Add("{\"error\":{\"code\":\"some_code\",\"message\":\"secret-token password\"}}",status);var error=await Assert.ThrowsAsync<ProviderException>(()=>s.http.Send(HttpMethod.Post,"https://api.linkedin.com/rest/posts",json:new{}));Assert.Equal(expected,error.Result.State);Assert.Equal(retry,error.Result.SafeToRetry);Assert.DoesNotContain("secret",error.Result.Detail);}
 [Fact] public async Task TikTokHttp200ErrorIsFailure(){var s=Setup(Platform.TikTok);s.handler.Add("{\"error\":{\"code\":\"scope_not_authorized\"}}");var error=await Assert.ThrowsAsync<ProviderException>(()=>s.http.Send(HttpMethod.Post,"https://open.tiktokapis.com/test"));Assert.Equal(JobState.RequiresApproval,error.Result.State);}
 [Fact] public async Task UnexpectedHtmlNeverBecomesSuccess(){var s=Setup(Platform.LinkedIn);s.handler.Add("<html>error</html>",200);var r=await new LinkedInPublisher(s.http,s.oauth,s.storage).PublishPost(Context(s.target),default);Assert.Equal(JobState.Unknown,r.State);}
}
