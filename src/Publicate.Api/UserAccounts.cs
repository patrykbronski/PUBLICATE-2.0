using System.Net.Mail;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
namespace Publicate;

public record Credentials(string Email,string Password);
public record Registration(string Name,string Email,string Password);
public class UserAccounts(AppDb db,IPasswordHasher<User> hasher) {
 public static string NormalizeEmail(string? email) {
 var value=email?.Trim()??"";
 if(value.Length>254||!MailAddress.TryCreate(value,out var address)||address.Address!=value) throw new InvalidOperationException("Podaj prawidłowy adres e-mail.");
 return value.ToUpperInvariant();
 }
 public async Task<User> Register(Registration input) {
 var email=NormalizeEmail(input.Email);
 if(string.IsNullOrWhiteSpace(input.Name)||input.Name.Trim().Length>100)throw new InvalidOperationException("Nazwa musi mieć od 1 do 100 znaków.");
 if(input.Password is null||input.Password.Length is <12 or >128)throw new InvalidOperationException("Hasło musi mieć od 12 do 128 znaków.");
 if(await db.Users.AnyAsync(u=>u.NormalizedEmail==email))throw new InvalidOperationException("Nie można utworzyć konta z tym adresem. Jeśli masz już konto, zaloguj się.");
 var user=new User{Id=Guid.NewGuid(),Name=input.Name.Trim(),Email=input.Email.Trim(),NormalizedEmail=email};
 user.PasswordHash=hasher.HashPassword(user,input.Password);db.Users.Add(user);
 try {await db.SaveChangesAsync();} catch(DbUpdateException){throw new InvalidOperationException("Nie można utworzyć konta. Spróbuj zalogować się lub ponów później.");}
 return user;
 }
 public async Task<User?> Login(Credentials input) {
 if(input.Password is null||input.Password.Length>128)return null;
 string email;try{email=NormalizeEmail(input.Email);}catch(InvalidOperationException){return null;}
 var user=await db.Users.SingleOrDefaultAsync(u=>u.NormalizedEmail==email);
 if(user?.PasswordHash is null){hasher.HashPassword(new User(),input.Password);return null;}
 if(user.LockedUntil>DateTimeOffset.UtcNow)return null;
 var result=hasher.VerifyHashedPassword(user,user.PasswordHash,input.Password);
 if(result==PasswordVerificationResult.Failed){user.FailedLogins++;if(user.FailedLogins>=5){user.LockedUntil=DateTimeOffset.UtcNow.AddMinutes(15);user.FailedLogins=0;}await db.SaveChangesAsync();return null;}
 if(result==PasswordVerificationResult.SuccessRehashNeeded)user.PasswordHash=hasher.HashPassword(user,input.Password);
 user.FailedLogins=0;user.LockedUntil=null;await db.SaveChangesAsync();return user;
 }
 public static Task SignIn(HttpContext ctx,User user)=>ctx.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme,new ClaimsPrincipal(new ClaimsIdentity(new[]{new Claim(ClaimTypes.NameIdentifier,user.Id.ToString()),new Claim(ClaimTypes.Name,user.Name)},CookieAuthenticationDefaults.AuthenticationScheme)));
}
