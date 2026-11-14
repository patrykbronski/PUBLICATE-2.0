using System.Text.Json;
using Publicate;
using Xunit;
namespace Publicate.Tests;
public class VideoCodecTests {
 static JsonElement Probe(string codec,string format="mov,mp4,m4a,3gp,3g2,mj2",string duration="25")=>JsonSerializer.SerializeToElement(new{streams=new[]{new{codec_type="video",codec_name=codec,width=1216,height=2160}},format=new{format_name=format,duration}});
 [Theory][InlineData("hevc")][InlineData("h264")][InlineData("av1")][InlineData("vp9")]
 public void BaseUploadRecordsCodecWithoutPlatformRestriction(string codec){var m=new MediaAsset{ContentType="video/mp4"};MediaInspector.ApplyProbe(Probe(codec),m);Assert.Equal(codec,m.Codec);Assert.Equal(25,m.Duration);Assert.Equal(1216,m.Width);}
 [Theory][InlineData("matroska","25")][InlineData("mp4","0")][InlineData("mp4","NaN")]
 public void InvalidContainerOrDurationStillRejected(string format,string duration){Assert.Throws<InvalidOperationException>(()=>MediaInspector.ApplyProbe(Probe("hevc",format,duration),new(){ContentType="video/mp4"}));}
 [Theory][InlineData(Platform.Instagram,"hevc",true)][InlineData(Platform.Instagram,"vp9",false)][InlineData(Platform.TikTok,"vp9",true)][InlineData(Platform.TikTok,"vp8",true)][InlineData(Platform.TikTok,"av1",false)][InlineData(Platform.YouTube,"hevc",true)][InlineData(Platform.Facebook,"hevc",true)][InlineData(Platform.LinkedIn,"hevc",true)]
 public void CodecValidationDependsOnDestination(Platform platform,string codec,bool allowed){var error=VideoCodecRules.Validate(platform,codec);Assert.Equal(allowed,error==null);if(error!=null){Assert.Contains(codec,error);Assert.Contains(platform.ToString(),error);}}
 [Fact] public void HevcPassesPostValidatorForInstagram(){var m=new MediaAsset{ContentType="video/mp4",Codec="hevc",Width=1216,Height=2160,Duration=25,Bytes=5500000};Assert.Empty(PostValidator.Validate(Platform.Instagram,new("Test","",[m.Id],"{}"),[m]));}
 [Fact] public void ImageDisguisedAsOtherImageRejected(){Assert.Throws<InvalidOperationException>(()=>MediaInspector.ApplyProbe(Probe("png"),new(){ContentType="image/jpeg"}));}
 [Fact] public void AttachedCoverIsNotTheVideoTrack(){var probe=JsonDocument.Parse("""{"streams":[{"codec_type":"video","codec_name":"mjpeg","width":100,"height":100,"disposition":{"attached_pic":1}},{"codec_type":"video","codec_name":"hevc","width":1080,"height":1920}],"format":{"format_name":"mp4","duration":"25"}}""");var m=new MediaAsset{ContentType="video/mp4"};MediaInspector.ApplyProbe(probe.RootElement,m);Assert.Equal("hevc",m.Codec);}
}
