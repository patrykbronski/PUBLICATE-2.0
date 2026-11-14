using System.Text.Json;
namespace Publicate;

public static class TikTokRules {
 public static bool Flag(JsonElement settings,string key)=>settings.TryGetProperty(key,out var value)&&value.ValueKind==JsonValueKind.True;
 public static bool VerifiedUrl(string? url,string? prefix) {
  if(!Uri.TryCreate(url,UriKind.Absolute,out var u)||!Uri.TryCreate(prefix,UriKind.Absolute,out var p))return false;
  return u.Scheme=="https"&&p.Scheme=="https"&&!u.IsLoopback&&!p.IsLoopback&&u.UserInfo==""&&p.UserInfo==""&&p.Query==""&&p.Fragment==""&&u.Fragment==""&&u.IdnHost==p.IdnHost&&u.Port==p.Port&&u.AbsolutePath.StartsWith(p.AbsolutePath.TrimEnd('/')+"/",StringComparison.Ordinal);
 }
 public static List<string> Settings(string json) {
  using var doc=JsonDocument.Parse(json);var s=doc.RootElement;var errors=new List<string>();
  if(ProviderHttp.Optional(s,"privacy")!="SELF_ONLY"||!Flag(s,"consent"))errors.Add("TikTok: wybierz SELF_ONLY i zaakceptuj Music Usage Confirmation.");
  if(Flag(s,"brandedContent"))errors.Add("TikTok: płatna współpraca nie może być publikowana jako SELF_ONLY.");
  if(Flag(s,"commercial")&&!Flag(s,"ownBrand"))errors.Add("TikTok: wskaż własną markę albo wyłącz oznaczenie treści komercyjnej.");
  if(!Flag(s,"commercial")&&Flag(s,"ownBrand"))errors.Add("TikTok: niespójne oznaczenie własnej marki.");
  return errors;
 }
 public static List<string> Creator(JsonElement creator,string json,IReadOnlyList<MediaAsset> media) {
  var errors=Settings(json);using var doc=JsonDocument.Parse(json);var s=doc.RootElement;
  if(!creator.TryGetProperty("privacy_level_options",out var options)||options.ValueKind!=JsonValueKind.Array||!options.EnumerateArray().Any(x=>x.GetString()=="SELF_ONLY"))errors.Add("TikTok: konto nie udostępnia SELF_ONLY; publikacja zablokowana.");
  if(!creator.TryGetProperty("max_video_post_duration_sec",out var max)||!max.TryGetDouble(out var seconds)||seconds<=0||media.Count!=1||media[0].ContentType!="video/mp4"||!double.IsFinite(media[0].Duration)||media[0].Duration<=0||media[0].Duration>seconds)errors.Add("TikTok: brak poprawnego filmu lub przekroczony aktualny limit długości twórcy.");
  foreach(var key in new[]{"comment","duet","stitch"})if(Flag(s,key)&&(!creator.TryGetProperty(key+"_disabled",out var disabled)||disabled.ValueKind!=JsonValueKind.False))errors.Add("TikTok: interakcja "+key+" niedostępna dla tego konta.");
  return errors;
 }
}
