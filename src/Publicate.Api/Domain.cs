using System.Text.Json.Serialization;
namespace Publicate;
// Append values only: persisted enum numbers must never change.
public enum Platform { LinkedIn, Facebook, Instagram, TikTok, X, YouTube }
public static class PlatformOrder {
 public static readonly Platform[] All=[Platform.Facebook,Platform.Instagram,Platform.TikTok,Platform.YouTube,Platform.X,Platform.LinkedIn];
}
public enum JobState { Draft, Validating, Ready, Uploading, Processing, Publishing, Published, Failed, RequiresAuthorization, RequiresApproval, Unsupported, Unknown }
public static class LocalOwner { public static readonly Guid Id = Guid.Parse("68b82a6a-d7d8-41bd-a672-b302c88d0486"); }
public abstract class Owned { public Guid Id { get; set; } = Guid.NewGuid(); public Guid OwnerId { get; set; } }
public class User {
 public Guid Id { get; set; } = LocalOwner.Id;
 public string Name { get; set; } = "Lokalny tester";
 public string? Email { get; set; }
 public Guid? PreferredFacebookTargetId { get; set; }
 public string PreferredTargetsJson { get; set; } = "{}";
 [JsonIgnore] public string? NormalizedEmail { get; set; }
 [JsonIgnore] public string? PasswordHash { get; set; }
 [JsonIgnore] public int FailedLogins { get; set; }
 [JsonIgnore] public DateTimeOffset? LockedUntil { get; set; }
}
public class ConnectedAccount : Owned {
 public string AuthMethod { get; set; } = "";
 public string? ClientBusinessId { get; set; }
 public Platform Platform { get; set; }
 public string ExternalId { get; set; } = "";
 public string Name { get; set; } = "";
 [JsonIgnore] public string AccessToken { get; set; } = "";
 [JsonIgnore] public string? RefreshToken { get; set; }
 public DateTimeOffset? ExpiresAt { get; set; }
 public string Scopes { get; set; } = "";
 public bool Disconnected { get; set; }
}
public class PublishingTarget : Owned {
 public Guid AccountId { get; set; }
 [JsonIgnore] public ConnectedAccount Account { get; set; } = null!;
 public Platform Platform { get; set; }
 public string ExternalId { get; set; } = "";
 public string Name { get; set; } = "";
 public string Kind { get; set; } = "Profile";
 [JsonIgnore] public string? AccessToken { get; set; }
}
public class Post : Owned {
 public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
 public bool Archived { get; set; }
 public int Revision { get; set; }
 public string? ComposerJson { get; set; }
 public string Description { get; set; } = "";
 public string Cta { get; set; } = "";
 public Guid[] MediaIds { get; set; } = [];
 public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
}
public class PostVariant : Owned {
 public Guid PostId { get; set; }
 [JsonIgnore] public Post Post { get; set; } = null!;
 public Guid TargetId { get; set; }
 [JsonIgnore] public PublishingTarget Target { get; set; } = null!;
 // null inherits; empty is an intentional override. Files are shared by reference.
 public string? DescriptionOverride { get; set; }
 public string? CtaOverride { get; set; }
 public Guid[]? MediaOverride { get; set; }
 public string SettingsJson { get; set; } = "{}";
 public ResolvedPost Resolve() => new(DescriptionOverride ?? Post.Description,CtaOverride ?? Post.Cta,MediaOverride ?? Post.MediaIds,SettingsJson);
}
public record ResolvedPost(string Description,string Cta,Guid[] MediaIds,string SettingsJson) {
 public string Text => string.Join("\n\n",new[]{Description,Cta}.Where(s=>!string.IsNullOrWhiteSpace(s)));
}
public class MediaAsset : Owned {
 public bool Uploaded { get; set; } = true;
 public string FileName { get; set; } = "";
 public string Key { get; set; } = "";
 public string ContentType { get; set; } = "";
 public long Bytes { get; set; }
 public int Width { get; set; }
 public int Height { get; set; }
 public double Duration { get; set; }
 public string Codec { get; set; } = "";
 public string? PublicUrl { get; set; }
 public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
 public DateTimeOffset LastUsedAt { get; set; } = DateTimeOffset.UtcNow;
 public DateTimeOffset? DeleteWarningAt { get; set; }
 public DateTimeOffset? DeletedAt { get; set; }
}
public class PublicationJob : Owned {
 public Guid VariantId { get; set; }
 [JsonIgnore] public PostVariant Variant { get; set; } = null!;
 public Platform Platform { get; set; }
 public string TargetName { get; set; } = "";
 public Guid RequestKey { get; set; }
 public string SnapshotJson { get; set; } = "";
 public Guid[] MediaIds { get; set; } = [];
 public JobState State { get; set; }
 public string? ExternalId { get; set; }
 public string? OperationId { get; set; }
 public string? Url { get; set; }
 public string? Diagnostic { get; set; }
 public bool SafeToRetry { get; set; }
 public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
 public DateTimeOffset? CompletedAt { get; set; }
}
public class PublicationAttempt : Owned {
 public Guid JobId { get; set; }
 [JsonIgnore] public PublicationJob Job { get; set; } = null!;
 public string Operation { get; set; } = "Publish";
 public int? HttpStatus { get; set; }
 public string? ProviderCode { get; set; }
 public JobState State { get; set; }
 public long DurationMs { get; set; }
 public string? ExternalId { get; set; }
 public string? Detail { get; set; }
 public bool SafeToRetry { get; set; }
 public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
public record PublishResult(JobState State,string? ExternalId=null,string? OperationId=null,string? Url=null,string? Detail=null,int? HttpStatus=null,string? ProviderCode=null,bool SafeToRetry=false);
public record Capability(Platform Platform,bool Text,int Images,bool Video,int TextLimit,string[] RequiredScopes,string Note);
public static class Capabilities {
 // Conservative PoC limits, not the complete platform limits.
 public static Capability Get(Platform p) => p switch {
 Platform.YouTube=>new(p,false,0,true,5000,["https://www.googleapis.com/auth/youtube.upload","https://www.googleapis.com/auth/youtube.readonly"],"YouTube Shorts · jeden film pionowy lub kwadratowy do 2:59,99; bez postów ze zdjęciem lub samym tekstem. Opis do 5000 bajtów UTF-8."),
 Platform.LinkedIn=>new(p,true,20,true,3000,["w_member_social"],"Posts API; do 20 zdjęć; odczyt statusu może wymagać dodatkowych uprawnień"),
 Platform.Facebook=>new(p,true,10,true,5000,["pages_manage_posts","pages_read_engagement"],"Tylko Pages; wersję Graph ustaw w konfiguracji"),
 Platform.Instagram=>new(p,false,20,true,2200,["instagram_business_basic","instagram_business_content_publish"],"Konto profesjonalne; do 20 zdjęć JPEG lub jeden Reel MP4; publiczny HTTPS"),
 Platform.TikTok=>new(p,false,0,true,2200,["video.publish"],"Zgoda na scope, prywatne konto, SELF_ONLY"),
 _=>new(p,true,4,true,280,["tweet.write"],"Tekst Unicode, do 4 zdjęć lub 1 film; płatne API")};
}
