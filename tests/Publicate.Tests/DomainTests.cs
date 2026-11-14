using System.Text.Json;
using Publicate;
using Xunit;
namespace Publicate.Tests;
public class DomainTests {
 static MediaAsset Photo(string type="image/jpeg")=>new(){ContentType=type,Bytes=10000,Width=1080,Height=1080,Codec=type=="image/png"?"png":"mjpeg"};
 static ResolvedPost Post(params MediaAsset[] assets)=>new("Opis","CTA",assets.Select(x=>x.Id).ToArray(),"{}");
 [Fact] public void VariantInheritsThenPreservesIntentionalEdits(){
 var p=new Post{Description="Baza",Cta="CTA",MediaIds=[Guid.NewGuid()]};var v=new PostVariant{Post=p};
 Assert.Equal("Baza",v.Resolve().Description);v.DescriptionOverride="Własny";v.CtaOverride="";v.MediaOverride=[];
 p.Description="Nowa baza";p.Cta="Nowe CTA";p.MediaIds=[Guid.NewGuid()];
 Assert.Equal("Własny",v.Resolve().Description);Assert.Equal("",v.Resolve().Cta);Assert.Empty(v.Resolve().MediaIds);
 v.DescriptionOverride=null;Assert.Equal("Nowa baza",v.Resolve().Description);
 }
 [Fact] public void TwoVariantsEditIndependently(){var p=new Post{Description="Baza"};var a=new PostVariant{Post=p};var b=new PostVariant{Post=p};a.DescriptionOverride="A";Assert.Equal("Baza",b.Resolve().Description);}
 [Fact] public void MediaOrderUsesReferences(){var a=Guid.NewGuid();var b=Guid.NewGuid();var p=new Post{MediaIds=[a,b]};var v=new PostVariant{Post=p,MediaOverride=[b,a]};Assert.Equal(new[]{b,a},v.Resolve().MediaIds);Assert.Equal(new[]{a,b},p.MediaIds);}
 [Theory][InlineData(Platform.LinkedIn)][InlineData(Platform.Facebook)][InlineData(Platform.X)] public void TextSupported(Platform p)=>Assert.Empty(PostValidator.Validate(p,new("Hello","",[],"{}"),[]));
 [Theory][InlineData(Platform.Instagram)][InlineData(Platform.TikTok)] public void TextUnsupported(Platform p)=>Assert.NotEmpty(PostValidator.Validate(p,new("Hello","",[],"{}"),[]));
 [Theory][InlineData(Platform.LinkedIn)][InlineData(Platform.Facebook)][InlineData(Platform.Instagram)] public void ValidImage(Platform p){var a=Photo();Assert.Empty(PostValidator.Validate(p,Post(a),[a]));}
 [Fact] public void InstagramRejectsPng(){var a=Photo("image/png");Assert.Contains(PostValidator.Validate(Platform.Instagram,Post(a),[a]),s=>s.Contains("JPEG"));}
 [Fact] public void InstagramRejectsAspectRatio(){var a=Photo();a.Height=2000;Assert.Contains(PostValidator.Validate(Platform.Instagram,Post(a),[a]),s=>s.Contains("4:5"));}
 [Fact] public void ImagePixelLimit(){var a=Photo();a.Width=a.Height=7000;Assert.NotEmpty(PostValidator.Validate(Platform.LinkedIn,Post(a),[a]));}
 [Fact] public void RejectDeletedAndMissingMedia(){var a=Photo();a.DeletedAt=DateTimeOffset.UtcNow;Assert.NotEmpty(PostValidator.Validate(Platform.LinkedIn,Post(a),[a]));Assert.NotEmpty(PostValidator.Validate(Platform.LinkedIn,Post(a),[]));}
 [Fact] public void RejectMixedMedia(){var a=Photo();var b=new MediaAsset{ContentType="video/mp4",Bytes=10000,Width=1080,Height=1920,Duration=20,Codec="h264"};Assert.NotEmpty(PostValidator.Validate(Platform.LinkedIn,Post(a,b),[a,b]));}
 [Fact] public void RejectOversizedImage(){var a=Photo();a.Bytes=9*1024*1024;Assert.NotEmpty(PostValidator.Validate(Platform.Facebook,Post(a),[a]));}
 [Fact] public void CaptionIncludesCta(){var p=new ResolvedPost(new string('a',2999),"CTA",[],"{}");Assert.NotEmpty(PostValidator.Validate(Platform.LinkedIn,p,[]));}
 [Fact] public void NoSyntheticIdOnUnknown(){var j=new PublicationJob();PublicationService.Apply(j,new(JobState.Unknown,Detail:"timeout"));Assert.Null(j.ExternalId);Assert.Null(j.Url);Assert.Null(j.CompletedAt);Assert.False(j.SafeToRetry);}
 [Fact] public void ProcessingIsNotPublished(){var j=new PublicationJob();PublicationService.Apply(j,new(JobState.Processing,OperationId:"container"));Assert.Equal(JobState.Processing,j.State);Assert.Null(j.ExternalId);Assert.Null(j.CompletedAt);}
 [Fact] public void PublishedSetsCompletion(){var j=new PublicationJob();PublicationService.Apply(j,new(JobState.Published,ExternalId:"provider-id"));Assert.NotNull(j.CompletedAt);Assert.Equal("provider-id",j.ExternalId);}
 [Theory][InlineData(JobState.Unknown)][InlineData(JobState.Processing)][InlineData(JobState.Publishing)][InlineData(JobState.Uploading)] public void PendingBlocksRetention(JobState state){var now=DateTimeOffset.UtcNow;var a=new MediaAsset{LastUsedAt=now.AddDays(-60),DeleteWarningAt=now.AddDays(-3)};var j=new PublicationJob{State=state,MediaIds=[a.Id]};Assert.False(RetentionPolicy.CanDelete(a,[j],false,now,TimeSpan.FromHours(48),TimeSpan.FromDays(30)));}
 [Fact] public void SharedAssetWaitsForAllJobs(){var now=DateTimeOffset.UtcNow;var a=new MediaAsset{LastUsedAt=now.AddDays(-10),DeleteWarningAt=now.AddDays(-3)};var done=new PublicationJob{State=JobState.Published,MediaIds=[a.Id],CompletedAt=now.AddDays(-5)};var pending=new PublicationJob{State=JobState.Processing,MediaIds=[a.Id]};Assert.False(RetentionPolicy.CanDelete(a,[done,pending],false,now,TimeSpan.FromHours(48),TimeSpan.FromDays(30)));}
 [Fact] public void RetentionRequiresWarningAndSafetyPeriod(){var now=DateTimeOffset.UtcNow;var a=new MediaAsset{LastUsedAt=now.AddDays(-4)};Assert.False(RetentionPolicy.CanDelete(a,[],false,now,TimeSpan.FromHours(48),TimeSpan.FromDays(30)));a.DeleteWarningAt=now.AddDays(-2);Assert.True(RetentionPolicy.CanDelete(a,[],false,now,TimeSpan.FromHours(48),TimeSpan.FromDays(30)));Assert.False(RetentionPolicy.CanDelete(a,[],true,now,TimeSpan.FromHours(48),TimeSpan.FromDays(30)));}
 [Fact] public void RetentionUsesLatestCompletion(){var now=DateTimeOffset.UtcNow;var a=new MediaAsset{LastUsedAt=now.AddDays(-8),DeleteWarningAt=now.AddDays(-3)};var j=new PublicationJob{State=JobState.Published,MediaIds=[a.Id],CompletedAt=now.AddHours(-12)};Assert.False(RetentionPolicy.CanDelete(a,[j],false,now,TimeSpan.FromHours(48),TimeSpan.FromDays(30)));}
 [Fact] public void OAuthStateOneUseAndBrowserBound(){var s=new OAuthStates();var state=s.Add(Platform.LinkedIn,"verifier","browser");Assert.Throws<InvalidOperationException>(()=>s.Consume(state,Platform.LinkedIn,"other"));Assert.Throws<InvalidOperationException>(()=>s.Consume(state,Platform.LinkedIn,"browser"));state=s.Add(Platform.X,"v","b");Assert.Equal("v",s.Consume(state,Platform.X,"b").Verifier);Assert.Throws<InvalidOperationException>(()=>s.Consume(state,Platform.X,"b"));}
 [Fact] public void OAuthStateCannotSwitchProvider(){var s=new OAuthStates();var state=s.Add(Platform.X,"v","b");Assert.Throws<InvalidOperationException>(()=>s.Consume(state,Platform.LinkedIn,"b"));}
 [Fact] public void TokensNeverSerialize(){var a=new ConnectedAccount{AccessToken="VERY_SECRET",RefreshToken="REFRESH_SECRET"};var t=new PublishingTarget{AccessToken="PAGE_SECRET",Account=a};var json=JsonSerializer.Serialize(new{a,t});Assert.DoesNotContain("SECRET",json);}
 [Theory][InlineData("http://127.0.0.1/a")][InlineData("https://evil.com/a")][InlineData("https://linkedin.com.evil.com/a")][InlineData("https://linkedin.com:8443/a")] public void UploadSsrfBlocked(string url)=>Assert.Throws<ProviderException>(()=>ProviderHttp.ValidateUploadUrl(url,"linkedin.com"));
 [Fact] public void UploadProviderHostAccepted()=>ProviderHttp.ValidateUploadUrl("https://www.linkedin.com/upload","linkedin.com");
}
