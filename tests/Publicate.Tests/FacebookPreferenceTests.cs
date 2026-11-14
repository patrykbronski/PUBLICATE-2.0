using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Publicate;
using Xunit;
namespace Publicate.Tests;
public class FacebookPreferenceTests {
 [Theory]
 [InlineData("available",true)]
 [InlineData("foreign",false)]
 [InlineData("disconnected",false)]
 [InlineData("revoked",false)]
 [InlineData("instagram",false)]
 public async Task SelectionPersistsOnlyForAccessibleFacebookPage(string kind,bool allowed){
 await using var factory=new CallbackFactory();using var client=factory.CreateClient();
 client.DefaultRequestHeaders.Add("X-Publicate","1");
 var targetId=Guid.NewGuid();
 using(var scope=factory.Services.CreateScope()){
 var db=scope.ServiceProvider.GetRequiredService<AppDb>();await db.Database.EnsureCreatedAsync();
 var owner=kind=="foreign"?Guid.NewGuid():LocalOwner.Id;
 var account=new ConnectedAccount{OwnerId=owner,Platform=Platform.Facebook,Disconnected=kind=="disconnected"};
 db.Add(account);db.Add(new PublishingTarget{Id=targetId,Account=account,AccountId=account.Id,OwnerId=owner,Platform=kind=="instagram"?Platform.Instagram:Platform.Facebook,AccessToken=kind=="revoked"?null:"fixture"});await db.SaveChangesAsync();
 }
 var response=await client.PutAsJsonAsync("/api/preferences/facebook-target/"+targetId,new{});
 Assert.Equal(allowed?HttpStatusCode.OK:HttpStatusCode.NotFound,response.StatusCode);
 using(var scope=factory.Services.CreateScope()){
 var db=scope.ServiceProvider.GetRequiredService<AppDb>();var user=await db.Users.SingleAsync(u=>u.Id==LocalOwner.Id);
 Assert.Equal(allowed?(Guid?)targetId:null,user.PreferredFacebookTargetId);
 }
 }
}
