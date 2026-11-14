using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Publicate;
using Xunit;
namespace Publicate.Tests;
public class UserAccountTests {
 static AppDb Database(IHttpContextAccessor? http=null)=>new(new DbContextOptionsBuilder<AppDb>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options,http);
 static IHttpContextAccessor Principal(Guid id)=>new HttpContextAccessor{HttpContext=new DefaultHttpContext{User=new ClaimsPrincipal(new ClaimsIdentity([new Claim(ClaimTypes.NameIdentifier,id.ToString())],"test"))}};
 [Fact] public async Task RegistrationHashesPasswordAndCreatesDistinctUser(){using var db=Database();var service=new UserAccounts(db,new PasswordHasher<User>());var a=await service.Register(new("A","a@example.test","a long password 123"));var b=await service.Register(new("B","b@example.test","a long password 123"));Assert.NotEqual(a.Id,b.Id);Assert.NotEqual(LocalOwner.Id,a.Id);Assert.NotEqual(a.PasswordHash,b.PasswordHash);Assert.DoesNotContain("PasswordHash",JsonSerializer.Serialize(a));Assert.Equal(a.Id,(await service.Login(new(" A@EXAMPLE.TEST ","a long password 123")))?.Id);}
 [Fact] public async Task DuplicateEmailIsCaseInsensitive(){using var db=Database();var service=new UserAccounts(db,new PasswordHasher<User>());await service.Register(new("A","a@example.test","a long password 123"));await Assert.ThrowsAsync<InvalidOperationException>(()=>service.Register(new("B","A@EXAMPLE.TEST","a long password 123")));}
 [Theory][InlineData("bad","long enough password")][InlineData("a@example.test","short")]
 public async Task RegistrationValidatesCredentials(string email,string password){using var db=Database();await Assert.ThrowsAsync<InvalidOperationException>(()=>new UserAccounts(db,new PasswordHasher<User>()).Register(new("A",email,password)));Assert.Empty(db.Users);}
 [Fact] public async Task FailedLoginsLockAccount(){using var db=Database();var service=new UserAccounts(db,new PasswordHasher<User>());var user=await service.Register(new("A","a@example.test","a long password 123"));for(var i=0;i<5;i++)Assert.Null(await service.Login(new("a@example.test","incorrect password")));Assert.True(user.LockedUntil>DateTimeOffset.UtcNow);Assert.Null(await service.Login(new("a@example.test","a long password 123")));}
 [Fact] public void MissingHttpIdentityNeverUsesLegacyOwner(){using var db=Database(new HttpContextAccessor{HttpContext=new DefaultHttpContext()});Assert.Throws<InvalidOperationException>(()=>db.CurrentOwnerId);}
 [Fact] public async Task SavingAssignsAuthenticatedOwner(){var id=Guid.NewGuid();using var db=Database(Principal(id));var p=new Post();db.Add(p);await db.SaveChangesAsync();Assert.Equal(id,p.OwnerId);}
 [Fact] public async Task DifferentUsersCannotLoadVariantsOrMedia(){var owner=Guid.NewGuid();var http=Principal(owner);using var db=Database(http);var target=new PublishingTarget{Account=new ConnectedAccount()};var v=new PostVariant{Post=new Post(),Target=target};var media=new MediaAsset();db.AddRange(v,media);await db.SaveChangesAsync();var setup=AdapterTests.Setup(Platform.LinkedIn);var service=new PublicationService(db,[],setup.storage,setup.config);Assert.NotNull(await service.Variant(v.Id));http.HttpContext!.User=Principal(Guid.NewGuid()).HttpContext!.User;await Assert.ThrowsAsync<KeyNotFoundException>(()=>service.Variant(v.Id));Assert.Empty(await service.Media([media.Id]));}
 [Fact] public async Task OAuthCallbackCannotBeClaimedByAnotherUser(){var owner=Guid.NewGuid();var states=new OAuthStates();var state=states.Add(Platform.LinkedIn,"verifier","browser",owner);using var db=Database(Principal(Guid.NewGuid()));var s=AdapterTests.Setup(Platform.LinkedIn);var oauth=new OAuthService(s.config,s.http,new TokenVault(new Microsoft.AspNetCore.DataProtection.EphemeralDataProtectionProvider()),db,states);await Assert.ThrowsAsync<InvalidOperationException>(()=>oauth.Complete(Platform.LinkedIn,state,"code","browser"));Assert.Empty(s.handler.Requests);Assert.Empty(db.ConnectedAccounts);}
}
