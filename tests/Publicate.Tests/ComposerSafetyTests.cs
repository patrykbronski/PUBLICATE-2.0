using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Publicate;
using Xunit;
namespace Publicate.Tests;
public class ComposerSafetyTests {
 [Theory][InlineData(0,1,true)][InlineData(1073741823,1,true)][InlineData(1073741824,1,false)][InlineData(long.MaxValue,1,false)]
 public void QuotaBoundaryCannotOverflow(long used,long incoming,bool expected)=>Assert.Equal(expected,PublicationRules.FitsQuota(used,incoming));
 [Fact] public async Task QuotaRejectsUploadBeforeInspectionOrStorage(){
 await using var f=new CallbackFactory();using var c=f.CreateClient();c.DefaultRequestHeaders.Add("X-Publicate","1");
 using(var scope=f.Services.CreateScope()){var db=scope.ServiceProvider.GetRequiredService<AppDb>();await db.Database.EnsureCreatedAsync();db.Add(new MediaAsset{OwnerId=LocalOwner.Id,Bytes=PublicationRules.UserQuotaBytes});await db.SaveChangesAsync();}
 using var form=new MultipartFormDataContent();form.Add(new ByteArrayContent(new byte[20]),"file","fixture.jpg");var result=await c.PostAsync("/api/media",form);Assert.Equal(HttpStatusCode.BadRequest,result.StatusCode);Assert.Contains("1 GB",await result.Content.ReadAsStringAsync());
 }
 [Theory][InlineData("invalid")][InlineData("target-changed")][InlineData("unconfirmed")][InlineData("stale")]
 public async Task WholeBatchRejectedBeforeAnyProviderCall(string reason){
 await using var f=new BatchFactory();using var c=f.CreateClient();c.DefaultRequestHeaders.Add("X-Publicate","1");var targetId=Guid.NewGuid();
 using(var scope=f.Services.CreateScope()){var db=scope.ServiceProvider.GetRequiredService<AppDb>();await db.Database.EnsureCreatedAsync();var account=new ConnectedAccount{OwnerId=LocalOwner.Id,Platform=Platform.Facebook,Scopes="pages_manage_posts pages_read_engagement"};db.Add(new PublishingTarget{Id=targetId,OwnerId=LocalOwner.Id,Platform=Platform.Facebook,Account=account,AccessToken="fixture"});(await db.Users.SingleAsync(u=>u.Id==LocalOwner.Id)).PreferredFacebookTargetId=targetId;await db.SaveChangesAsync();}
 var id=Guid.NewGuid();var state=new ComposerState(2,"Base","",false,[],[Platform.Facebook],[new(Guid.NewGuid(),Platform.Facebook,targetId,"fixture",reason=="invalid"?new string('x',5001):"text","",false,[])]);
 var save=await c.PutAsJsonAsync("/api/drafts",new SaveDraftInput(id,0,state),DraftService.Json);Assert.Equal(HttpStatusCode.OK,save.StatusCode);
 if(reason=="target-changed"){using var scope=f.Services.CreateScope();var db=scope.ServiceProvider.GetRequiredService<AppDb>();(await db.Users.SingleAsync(u=>u.Id==LocalOwner.Id)).PreferredFacebookTargetId=Guid.NewGuid();await db.SaveChangesAsync();}
 var result=await c.PostAsJsonAsync($"/api/drafts/{id}/publish",new BatchInput(reason=="stale"?0:1,Guid.NewGuid(),reason!="unconfirmed",false));Assert.Equal(HttpStatusCode.BadRequest,result.StatusCode);Assert.Empty(f.Facebook.Texts);Assert.Empty(f.LinkedIn.Texts);
 }
}
