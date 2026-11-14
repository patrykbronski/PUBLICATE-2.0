namespace Publicate;

// Only documented exclusive lists belong here. Container != codec.
// Sources and unknowns: docs/VIDEO_CODECS.md (2026-11-11).
public static class VideoCodecRules {
 public static string? Validate(Platform platform,string codec){
 if(string.IsNullOrWhiteSpace(codec)||codec=="unknown")return $"{platform}: nie rozpoznano kodeka filmu.";
 string[]? allowed=platform switch{
 Platform.Instagram=>["h264","hevc"],
 Platform.TikTok=>["h264","hevc","vp8","vp9"],
 _=>null
 };
 return allowed!=null&&!allowed.Contains(codec)?$"{platform}: wykryty kodek {codec}. Obsługiwane kodeki wideo: {string.Join(", ",allowed)} (hevc = H.265). Plik nie został zmieniony.":null;
 }
}
