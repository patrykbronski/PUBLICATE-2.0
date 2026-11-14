using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Publicate;

var builder=WebApplication.CreateBuilder(args);
builder.Configuration.AddJsonFile("appsettings.Local.json",optional:true,reloadOnChange:false).AddEnvironmentVariables();
builder.WebHost.ConfigureKestrel(o=>o.Limits.MaxRequestBodySize=68L*1024*1024);
builder.Services.ConfigureHttpJsonOptions(o=>o.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.Configure<Microsoft.AspNetCore.Http.Features.FormOptions>(o=>o.MultipartBodyLengthLimit=68L*1024*1024);
builder.Services.AddDbContext<AppDb>(o=>o.UseNpgsql(builder.Configuration.GetConnectionString("Database")));
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<IPasswordHasher<User>,PasswordHasher<User>>();
builder.Services.Configure<PasswordHasherOptions>(o=>o.IterationCount=210000);
builder.Services.AddScoped<UserAccounts>();
var dp=builder.Services.AddDataProtection().PersistKeysToFileSystem(new DirectoryInfo(Path.GetFullPath("../../.data/keys"))).SetApplicationName(builder.Configuration["Instance:ProtectionName"]??"PUBLICATE");
if(OperatingSystem.IsWindows()) dp.ProtectKeysWithDpapi();
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme).AddCookie(o=>{
 o.Cookie.Name=builder.Configuration["Instance:CookieName"]??"publicate.session";o.Cookie.HttpOnly=true;o.Cookie.SameSite=SameSiteMode.Lax;o.Cookie.SecurePolicy=builder.Configuration["Local:Origin"]?.StartsWith("https://")==true?CookieSecurePolicy.Always:CookieSecurePolicy.SameAsRequest;o.ExpireTimeSpan=TimeSpan.FromHours(8);
 o.Events.OnRedirectToLogin=c=>{c.Response.StatusCode=401;return Task.CompletedTask;};o.Events.OnRedirectToAccessDenied=c=>{c.Response.StatusCode=403;return Task.CompletedTask;};
 o.Events.OnValidatePrincipal=async c=>{var db=c.HttpContext.RequestServices.GetRequiredService<AppDb>();if(!Guid.TryParse(c.Principal?.FindFirst(ClaimTypes.NameIdentifier)?.Value,out var id)||!await db.Users.AnyAsync(u=>u.Id==id&&u.PasswordHash!=null)){c.RejectPrincipal();await c.HttpContext.SignOutAsync();}};
});
builder.Services.AddAuthorization();
builder.Services.AddCors(o=>o.AddDefaultPolicy(p=>p.WithOrigins(builder.Configuration["Local:Origin"]??"http://localhost:5173").AllowAnyHeader().AllowAnyMethod().AllowCredentials()));
builder.Services.AddRateLimiter(o=>{o.RejectionStatusCode=429;o.GlobalLimiter=PartitionedRateLimiter.Create<HttpContext,string>(ctx=>RateLimitPartition.GetFixedWindowLimiter(ctx.Connection.RemoteIpAddress?.ToString()??"local",_=>new(){PermitLimit=120,Window=TimeSpan.FromMinutes(1),QueueLimit=0}));o.AddFixedWindowLimiter("login",o=>{o.PermitLimit=10;o.Window=TimeSpan.FromMinutes(1);o.QueueLimit=0;});});
builder.Services.AddHttpClient("provider",c=>c.Timeout=TimeSpan.FromMinutes(3)).ConfigurePrimaryHttpMessageHandler(()=>new SocketsHttpHandler{AllowAutoRedirect=false});
builder.Services.AddSingleton<OAuthStates>();builder.Services.AddSingleton<TokenVault>();builder.Services.AddSingleton<OperationGate>();
builder.Services.AddScoped<OAuthTrace>();builder.Services.AddSingleton<OAuthReports>();
builder.Services.AddScoped<ProviderHttp>();builder.Services.AddScoped<OAuthService>();builder.Services.AddScoped<PublicationService>();builder.Services.AddScoped<DraftService>();builder.Services.AddScoped<MediaInspector>();
builder.Services.AddSingleton<IMediaStorage>(s=>builder.Configuration["Storage:Provider"]=="R2"?new R2MediaStorage(builder.Configuration):new LocalMediaStorage(builder.Configuration));
builder.Services.AddScoped<ISocialPublisher,LinkedInPublisher>();builder.Services.AddScoped<ISocialPublisher,FacebookPublisher>();builder.Services.AddScoped<ISocialPublisher,InstagramPublisher>();builder.Services.AddScoped<ISocialPublisher,TikTokPublisher>();builder.Services.AddScoped<ISocialPublisher,XPublisher>();builder.Services.AddScoped<ISocialPublisher,YouTubePublisher>();
builder.Services.AddScoped<IAiDescriptionService,OpenAiDescriptionService>();builder.Services.AddHostedService<CleanupWorker>();
var app=builder.Build();
if(args.Contains("--migrate")) {using var scope=app.Services.CreateScope();await scope.ServiceProvider.GetRequiredService<AppDb>().Database.MigrateAsync();return;}
app.Use(async(ctx,next)=>{
 ctx.Response.Headers["X-Content-Type-Options"]="nosniff";ctx.Response.Headers["Referrer-Policy"]="no-referrer";ctx.Response.Headers["Cache-Control"]="no-store";
 try {await next();}
 catch(KeyNotFoundException){ctx.Response.StatusCode=404;await ctx.Response.WriteAsJsonAsync(new{error="Nie znaleziono zasobu."});}
 catch(ProviderException e){ctx.Response.StatusCode=502;await ctx.Response.WriteAsJsonAsync(new{error=e.Result.Detail,diagnostic=e.Result});}
 catch(InvalidOperationException e){ctx.Response.StatusCode=400;await ctx.Response.WriteAsJsonAsync(new{error=e.Message});}
 catch(Exception){ctx.Response.StatusCode=500;await ctx.Response.WriteAsJsonAsync(new{error="Błąd aplikacji lub bazy danych. Sprawdź konfigurację i migracje; szczegóły sekretów nie są zwracane."});}
});
app.UseCors();app.UseRateLimiter();app.UseAuthentication();app.UseAuthorization();
app.Use(async(ctx,next)=>{
 if(ctx.Request.Path.StartsWithSegments("/api")&&ctx.Request.Method is not ("GET" or "HEAD" or "OPTIONS")) {
 var origin=ctx.Request.Headers.Origin.ToString();
 var expected=app.Configuration["Local:Origin"]??"http://localhost:5173";
 if(ctx.Request.Headers["X-Publicate"]!="1"||(origin!=""&&origin!=expected)) {ctx.Response.StatusCode=403;return;}
 }
 // One process PoC: mutations and cleanup share a gate to prevent retention/publish races.
 if(ctx.Request.Method is not ("GET" or "HEAD" or "OPTIONS")) {
 var gate=ctx.RequestServices.GetRequiredService<OperationGate>().Mutex;await gate.WaitAsync(ctx.RequestAborted);try{await next();}finally{gate.Release();}
 } else await next();
});
app.MapGet("/api/health",()=>new{status="running",database="Use /api/readiness after login",realPublicationsVerified=false});
app.MapPost("/api/session",async(Credentials r,HttpContext ctx,UserAccounts accounts)=>{
 var user=await accounts.Login(r);if(user==null)return Results.Json(new{error="Nieprawidłowy e-mail lub hasło albo czasowa blokada logowania."},statusCode:401);
 await UserAccounts.SignIn(ctx,user);return Results.Ok(new{user.Id,user.Name,user.Email});
}).RequireRateLimiting("login");
app.MapPost("/api/register",async(Registration r,HttpContext ctx,UserAccounts accounts)=>{
 if(!app.Configuration.GetValue("Local:AllowRegistration",true)) return Results.Json(new{error="Rejestracja jest wyłączona na czas publicznych testów. Zaloguj się na istniejące konto."},statusCode:403);
 var user=await accounts.Register(r);await UserAccounts.SignIn(ctx,user);return Results.Ok(new{user.Id,user.Name,user.Email});
}).RequireRateLimiting("login");
var api=app.MapGroup("/api").RequireAuthorization();
api.MapDrafts();
api.MapGet("/session",async(AppDb db)=>{var user=await db.Users.SingleAsync(u=>u.Id==db.CurrentOwnerId);return Results.Ok(new{user.Id,user.Name,user.Email});});
api.MapDelete("/session",async(HttpContext c)=>{await c.SignOutAsync();return Results.Ok();});
api.MapGet("/readiness",async(AppDb db)=>Results.Ok(new{database=await db.Database.CanConnectAsync(),pendingMigrations=await db.Database.GetPendingMigrationsAsync()}));
api.MapGet("/platforms",(OAuthService o,IAiDescriptionService ai)=>new{platforms=PlatformOrder.All.Select(p=>{string? problem=null;try{o.CheckConfig(p);}catch(InvalidOperationException e){problem=e.Message;}return new{platform=p,capabilities=p==Platform.Instagram&&o.InstagramUnified?Capabilities.Get(p) with {RequiredScopes=["instagram_basic","instagram_content_publish"],Note="Instagram Unified Login · wybierasz konto IG niezależnie od publikacji na Facebooku"}:Capabilities.Get(p),configured=problem==null,problem};}),aiAvailable=ai.Available});
api.MapGet("/accounts",async(AppDb db)=>await db.ConnectedAccounts.Where(x=>x.OwnerId==db.CurrentOwnerId).ToListAsync());
api.MapGet("/preferences/facebook-target",async(AppDb db)=>Results.Ok(new {targetId=(await db.Users.SingleAsync(u=>u.Id==db.CurrentOwnerId)).PreferredFacebookTargetId}));
api.MapPut("/preferences/facebook-target/{id:guid}",async(Guid id,AppDb db)=>{
 var target=await db.PublishingTargets.Include(t=>t.Account).SingleOrDefaultAsync(t=>t.Id==id&&t.OwnerId==db.CurrentOwnerId&&t.Platform==Platform.Facebook&&!t.Account.Disconnected&&t.AccessToken!=null);
 if(target is null)return Results.NotFound(new{error="Strona nie jest dostępna dla tego konta PUBLICATE."});
 var user=await db.Users.SingleAsync(u=>u.Id==db.CurrentOwnerId);user.PreferredFacebookTargetId=id;await db.SaveChangesAsync();return Results.Ok(new{targetId=id});
});
api.MapGet("/targets",async(AppDb db)=>await db.PublishingTargets.Include(t=>t.Account).Where(t=>t.OwnerId==db.CurrentOwnerId&&!t.Account.Disconnected&&(t.Platform!=Platform.Facebook||t.AccessToken!=null)&&(t.Platform!=Platform.Instagram||t.Kind=="InstagramLogin"||t.Kind=="InstagramUnified")).ToListAsync());
api.MapPost("/oauth/{platform}/start",(Platform platform,OAuthService oauth,HttpContext ctx)=>{
 var browser=Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
 ctx.Response.Cookies.Append("publicate.oauth."+platform,browser,new CookieOptions{HttpOnly=true,SameSite=SameSiteMode.Lax,Secure=oauth.Callback(platform).StartsWith("https://"),MaxAge=TimeSpan.FromMinutes(10),Path="/api/oauth"});
 return Results.Ok(new{url=oauth.Begin(platform,browser)});
});
api.MapGet("/oauth/{platform}/callback",async(Platform platform,HttpContext ctx,OAuthService oauth,OAuthTrace trace,OAuthReports reports,AppDb db)=>{
 var origin=app.Configuration["Local:Origin"]??"http://localhost:5173";
 var cookie="publicate.oauth."+platform;
 var browser=ctx.Request.Cookies[cookie]??"";
 ctx.Response.Cookies.Delete(cookie,new CookieOptions{Path="/api/oauth",HttpOnly=true,Secure=origin.StartsWith("https://"),SameSite=SameSiteMode.Lax});
 // Anonymous requests may only return to the UI; no token exchange without the owner session.
 if(ctx.User.Identity?.IsAuthenticated!=true)return Results.Redirect(origin+"/#oauth=session_required");
 var id=Guid.NewGuid().ToString("N");trace.Active=true;var success=false;string message;
 try {
 await oauth.Complete(platform,ctx.Request.Query["state"].ToString(),ctx.Request.Query["code"].ToString(),browser,ctx.Request.Query.ContainsKey("error"));
 success=true;message="Połączono konto "+platform+".";
 }catch(ProviderException e){message=e.Result.Detail??"Dostawca odrzucił autoryzację.";if(trace.Steps.Count==0)trace.Steps.Add(new("authorization",e.Result.HttpStatus,e.Result.ProviderCode));}
 catch(InvalidOperationException){message="Nie można zakończyć OAuth: stan wygasł, został użyty albo konfiguracja lub sesja uległa zmianie. Rozpocznij nowe połączenie.";trace.Steps.Add(new("callback_validation",null));}
 catch(Exception){message="Nie udało się zakończyć połączenia. Sprawdź identyfikator diagnostyczny; nie odświeżaj starego callbacku.";trace.Steps.Add(new("callback_failure",null));}
 reports.Save(new(id,db.CurrentOwnerId,platform.ToString(),DateTimeOffset.UtcNow,success,message,trace.Steps.ToArray()));
 return Results.Redirect(origin+"/#oauth="+id);
}).AllowAnonymous();
api.MapGet("/oauth/results/{id}",(string id,AppDb db,OAuthReports reports)=>reports.Get(id,db.CurrentOwnerId) is {} report?Results.Ok(report):Results.NotFound());
async Task<ConnectedAccount> Account(Guid id,AppDb db)=>await db.ConnectedAccounts.SingleOrDefaultAsync(x=>x.Id==id&&x.OwnerId==db.CurrentOwnerId)??throw new KeyNotFoundException();
api.MapPost("/accounts/{id:guid}/targets",async(Guid id,AppDb db,OAuthService o)=>{await o.Discover(await Account(id,db));return Results.Ok();});
api.MapPost("/accounts/{id:guid}/refresh",async(Guid id,AppDb db,OAuthService o)=>{await o.Refresh(await Account(id,db));return Results.Ok();});
api.MapDelete("/accounts/{id:guid}",async(Guid id,AppDb db,OAuthService o)=>{await o.Disconnect(await Account(id,db));return Results.Ok(new{message="Tokeny lokalne usunięte. Cofnij także zgodę w ustawieniach platformy."});});
api.MapGet("/media",async(AppDb db)=>await db.MediaAssets.Where(x=>x.OwnerId==db.CurrentOwnerId&&x.DeletedAt==null&&x.Uploaded).OrderByDescending(x=>x.CreatedAt).ToListAsync());
api.MapPost("/media",async(HttpRequest request,AppDb db,IMediaStorage storage,MediaInspector inspector)=>{
 var form=await request.ReadFormAsync();var file=form.Files.GetFile("file");
 if(file==null||file.Length is <=0 or >67108864) return Results.BadRequest(new{error="Wybierz plik do 64 MiB."});
 var used=await db.MediaAssets.Where(m=>m.OwnerId==db.CurrentOwnerId&&m.DeletedAt==null).SumAsync(m=>m.Bytes);if(!PublicationRules.FitsQuota(used,file.Length,app.Configuration.GetValue<long>("Storage:UserQuotaBytes",PublicationRules.UserQuotaBytes)))return Results.BadRequest(new{error="Przekroczono limit magazynu 1 GB. Usuń niepotrzebne szkice lub poczekaj na wygaśnięcie plików."});
 var ext=Path.GetExtension(file.FileName).ToLowerInvariant();
 var type=ext switch{".jpg" or ".jpeg"=>"image/jpeg",".png"=>"image/png",".mp4"=>"video/mp4",_=>""};
 if(type=="") return Results.BadRequest(new{error="Dozwolone JPEG, PNG, MP4."});
 var asset=new MediaAsset{FileName=Path.GetFileName(file.FileName),Key=Guid.NewGuid().ToString("N")+(ext==".jpeg"?".jpg":ext),ContentType=type,Bytes=file.Length};
 var staging=Path.GetFullPath(app.Configuration["Storage:StagingRoot"]??"../../.data/staging");Directory.CreateDirectory(staging);
 var temp=Path.Combine(staging,Guid.NewGuid().ToString("N")+ext);
 try {await using(var f=File.Create(temp)) await file.CopyToAsync(f);await inspector.Inspect(temp,asset,request.HttpContext.RequestAborted);asset.Uploaded=false;db.Add(asset);await db.SaveChangesAsync();await using var input=File.OpenRead(temp);await storage.Put(asset.Key,input,type,request.HttpContext.RequestAborted);asset.Uploaded=true;await db.SaveChangesAsync();return Results.Ok(asset);}
 finally {File.Delete(temp);}
});
api.MapGet("/media/{id:guid}/preview",async(Guid id,AppDb db,IMediaStorage storage)=>{var a=await db.MediaAssets.SingleOrDefaultAsync(x=>x.Id==id&&x.OwnerId==db.CurrentOwnerId&&x.DeletedAt==null&&x.Uploaded)??throw new KeyNotFoundException();return Results.Stream(await storage.Read(a.Key,CancellationToken.None),a.ContentType,enableRangeProcessing:true);});
app.MapGet("/media/{key}",async(string key,AppDb db,IMediaStorage storage,IConfiguration c)=>{
 if(c["Storage:Provider"]=="R2") return Results.NotFound();
 var a=await db.MediaAssets.SingleOrDefaultAsync(x=>x.Key==key&&x.PublicUrl!=null&&x.DeletedAt==null&&x.Uploaded);if(a==null)return Results.NotFound();
 return Results.Stream(await storage.Read(a.Key,CancellationToken.None),a.ContentType,enableRangeProcessing:true);
});
async Task TouchMedia(Guid[] ids,AppDb db) {
 if(ids.Distinct().Count()!=ids.Length)throw new InvalidOperationException("Pliki nie mogą się powtarzać.");
 var assets=await db.MediaAssets.Where(x=>x.OwnerId==db.CurrentOwnerId&&ids.Contains(x.Id)&&x.DeletedAt==null&&x.Uploaded).ToListAsync();
 if(assets.Count!=ids.Length)throw new InvalidOperationException("Nieprawidłowe referencje multimediów.");
 foreach(var a in assets){a.LastUsedAt=DateTimeOffset.UtcNow;a.DeleteWarningAt=null;}
}
api.MapGet("/posts",async(AppDb db)=>await db.Posts.Where(x=>x.OwnerId==db.CurrentOwnerId).OrderByDescending(x=>x.UpdatedAt).ToListAsync());
api.MapPost("/posts",async(PostInput r,DraftService drafts,AppDb db)=>{var saved=await drafts.Save(new(Guid.NewGuid(),0,new(1,r.Description,r.Cta,!string.IsNullOrEmpty(r.Cta),r.MediaIds,[],[])));saved.Post.ComposerJson=null;await db.SaveChangesAsync();return Results.Ok(saved.Post);});
api.MapPut("/posts/{id:guid}",async(Guid id,PostInput r,AppDb db)=>{var p=await db.Posts.SingleOrDefaultAsync(x=>x.Id==id&&x.OwnerId==db.CurrentOwnerId)??throw new KeyNotFoundException();if(p.ComposerJson!=null)throw new InvalidOperationException("Użyj zapisu kreatora.");await TouchMedia(r.MediaIds,db);if(!DraftService.Active(p,DateTimeOffset.UtcNow))throw new InvalidOperationException("Szkic wygasł.");p.Description=r.Description;p.Cta=r.Cta;p.MediaIds=r.MediaIds;p.UpdatedAt=DateTimeOffset.UtcNow;await db.SaveChangesAsync();return Results.Ok(p);});
api.MapGet("/posts/{id:guid}/variants",async(Guid id,AppDb db)=>await db.PostVariants.Where(v=>v.OwnerId==db.CurrentOwnerId&&v.PostId==id).ToListAsync());
api.MapPost("/posts/{id:guid}/variants",async(Guid id,TargetInput r,AppDb db)=>{
 if(await db.Posts.AnyAsync(p=>p.Id==id&&p.OwnerId==db.CurrentOwnerId&&p.ComposerJson!=null))throw new InvalidOperationException("Użyj zapisu kreatora.");
 if(!await db.Posts.AnyAsync(x=>x.Id==id&&x.OwnerId==db.CurrentOwnerId)||!await db.PublishingTargets.AnyAsync(x=>x.Id==r.TargetId&&x.OwnerId==db.CurrentOwnerId))throw new KeyNotFoundException();
 var v=await db.PostVariants.SingleOrDefaultAsync(x=>x.PostId==id&&x.TargetId==r.TargetId&&x.OwnerId==db.CurrentOwnerId);
 if(v==null){v=new PostVariant{PostId=id,TargetId=r.TargetId};db.Add(v);await db.SaveChangesAsync();}return Results.Ok(v);
});
api.MapPut("/variants/{id:guid}",async(Guid id,VariantInput r,PublicationService service,AppDb db)=>{var v=await service.Variant(id);if(v.Post.ComposerJson!=null)throw new InvalidOperationException("Użyj zapisu kreatora.");if(r.MediaOverride!=null)await TouchMedia(r.MediaOverride,db);using var parsed=JsonDocument.Parse(r.SettingsJson);if(parsed.RootElement.ValueKind!=JsonValueKind.Object)throw new InvalidOperationException("SettingsJson musi być obiektem.");v.DescriptionOverride=r.DescriptionOverride;v.CtaOverride=r.CtaOverride;v.MediaOverride=r.MediaOverride;v.SettingsJson=r.SettingsJson;await db.SaveChangesAsync();return Results.Ok(v);});
api.MapPost("/variants/{id:guid}/validate",async(Guid id,PublicationService s)=>new{issues=await s.Validate(await s.Variant(id))});
api.MapPost("/variants/{id:guid}/publish",async(Guid id,PublishInput r,PublicationService s)=>{if((await s.Variant(id)).Post.ComposerJson!=null)throw new InvalidOperationException("Publikuj zestaw z kreatora.");return await s.Publish(id,r.RequestKey,r.Confirmed,r.ExposeConfirmed);});
api.MapGet("/jobs",async(AppDb db)=>await db.PublicationJobs.Where(x=>x.OwnerId==db.CurrentOwnerId).OrderByDescending(x=>x.CreatedAt).Take(200).ToListAsync());
api.MapGet("/jobs/{id:guid}/attempts",async(Guid id,AppDb db)=>await db.PublicationAttempts.Where(x=>x.OwnerId==db.CurrentOwnerId&&x.JobId==id).OrderBy(x=>x.CreatedAt).ToListAsync());
api.MapPost("/jobs/{id:guid}/status",async(Guid id,PublicationService s)=>await s.Check(id));
api.MapPost("/targets/{id:guid}/creator",async(Guid id,AppDb db,IEnumerable<ISocialPublisher> publishers)=>{var t=await db.PublishingTargets.Include(x=>x.Account).SingleOrDefaultAsync(x=>x.Id==id&&x.OwnerId==db.CurrentOwnerId&&x.Platform==Platform.TikTok)??throw new KeyNotFoundException();return await ((TikTokPublisher)publishers.Single(x=>x.Platform==Platform.TikTok)).Creator(t,CancellationToken.None);});
api.MapPost("/ai/description",async(AiInput r,PublicationService s,IAiDescriptionService ai)=>{if(!r.ConfirmCost)throw new InvalidOperationException("Wymagana zgoda na koszt i przesłanie zdjęć do AI.");var media=await s.Media(r.MediaIds);if(media.Count!=r.MediaIds.Length)throw new InvalidOperationException("Brak multimediów.");return new{text=await ai.Generate(media,r.Platform,CancellationToken.None)};});
if(!string.IsNullOrWhiteSpace(app.Configuration.GetConnectionString("Database"))) {
 try {
 using var scope=app.Services.CreateScope();var db=scope.ServiceProvider.GetRequiredService<AppDb>();
 // A previous process may have sent the final request before crashing. Never auto-replay.
 await db.PublicationJobs.Where(j=>j.State==JobState.Uploading||j.State==JobState.Publishing).ExecuteUpdateAsync(s=>s.SetProperty(j=>j.State,JobState.Unknown).SetProperty(j=>j.SafeToRetry,false).SetProperty(j=>j.Diagnostic,"Proces przerwany. Wynik nieznany — sprawdź konto dostawcy; bez automatycznego retry."));
 } catch(Exception){app.Logger.LogWarning("Nie wykonano odzyskiwania stanu. Sprawdź readiness i migracje przed publikowaniem.");}
}
app.Run();
public record PostInput(string Description,string Cta,Guid[] MediaIds);
public record TargetInput(Guid TargetId);
public record VariantInput(string? DescriptionOverride,string? CtaOverride,Guid[]? MediaOverride,string SettingsJson);
public record PublishInput(Guid RequestKey,bool Confirmed,bool ExposeConfirmed);
public record AiInput(Guid[] MediaIds,string Platform,bool ConfirmCost);
public partial class Program { }
