using Microsoft.EntityFrameworkCore;
namespace Publicate;
public class AppDb(DbContextOptions<AppDb> options,IHttpContextAccessor? http=null):DbContext(options) {
 // Context-free construction is for isolated unit tests; HTTP requests never fall back to the legacy owner.
 public Guid CurrentOwnerId => http is null ? LocalOwner.Id : Guid.TryParse(http.HttpContext?.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value,out var id) ? id : throw new InvalidOperationException("Zaloguj się do PUBLICATE.");
 public override Task<int> SaveChangesAsync(CancellationToken ct=default) {
 foreach(var entry in ChangeTracker.Entries<Owned>().Where(e=>e.State==EntityState.Added&&e.Entity.OwnerId==Guid.Empty)) entry.Entity.OwnerId=CurrentOwnerId;
 return base.SaveChangesAsync(ct);
 }
 public DbSet<User> Users=>Set<User>();
 public DbSet<ConnectedAccount> ConnectedAccounts=>Set<ConnectedAccount>();
 public DbSet<PublishingTarget> PublishingTargets=>Set<PublishingTarget>();
 public DbSet<Post> Posts=>Set<Post>();
 public DbSet<MediaAsset> MediaAssets=>Set<MediaAsset>();
 public DbSet<PostVariant> PostVariants=>Set<PostVariant>();
 public DbSet<PublicationJob> PublicationJobs=>Set<PublicationJob>();
 public DbSet<PublicationAttempt> PublicationAttempts=>Set<PublicationAttempt>();
 protected override void OnModelCreating(ModelBuilder m) {
 m.Entity<User>().HasData(new User());
 m.Entity<User>().HasIndex(u=>u.NormalizedEmail).IsUnique().HasFilter("\"NormalizedEmail\" IS NOT NULL");
 m.Entity<User>().Property(u=>u.NormalizedEmail).HasMaxLength(254);
 m.Entity<User>().Property(u=>u.Email).HasMaxLength(254);
 m.Entity<User>().Property(u=>u.Name).HasMaxLength(100);
 foreach(var t in m.Model.GetEntityTypes().Where(t=>typeof(Owned).IsAssignableFrom(t.ClrType)).ToArray()) {
 m.Entity(t.ClrType).HasIndex("OwnerId");
 m.Entity(t.ClrType).HasOne(typeof(User)).WithMany().HasForeignKey("OwnerId").OnDelete(DeleteBehavior.Restrict);
 }
 m.Entity<PublicationJob>().HasIndex(x=>new{x.OwnerId,x.RequestKey}).IsUnique();
 m.Entity<PublicationJob>().HasIndex(x=>x.VariantId).IsUnique().HasFilter("\"State\" IN (3,4,5,6,11)");
 m.Entity<ConnectedAccount>().HasIndex(x=>new{x.OwnerId,x.Platform,x.ExternalId,x.AuthMethod}).IsUnique();
 m.Entity<PublishingTarget>().HasIndex(x=>new{x.AccountId,x.Platform,x.ExternalId}).IsUnique();
 m.Entity<PostVariant>().HasIndex(x=>new{x.PostId,x.TargetId}).IsUnique();
 m.Entity<PublishingTarget>().HasOne(x=>x.Account).WithMany().HasForeignKey(x=>x.AccountId).OnDelete(DeleteBehavior.Restrict);
 m.Entity<PostVariant>().HasOne(x=>x.Post).WithMany().HasForeignKey(x=>x.PostId).OnDelete(DeleteBehavior.Restrict);
 m.Entity<PostVariant>().HasOne(x=>x.Target).WithMany().HasForeignKey(x=>x.TargetId).OnDelete(DeleteBehavior.Restrict);
 m.Entity<PublicationJob>().HasOne(x=>x.Variant).WithMany().HasForeignKey(x=>x.VariantId).OnDelete(DeleteBehavior.Restrict);
 m.Entity<PublicationAttempt>().HasOne(x=>x.Job).WithMany().HasForeignKey(x=>x.JobId).OnDelete(DeleteBehavior.Restrict);
 }
}
