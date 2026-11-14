using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.RegularExpressions;
namespace Publicate;
public sealed class ProviderException(PublishResult result):Exception(result.Detail) { public PublishResult Result { get; }=result; }
public record ApiReply(JsonElement Json,int Status,string? Id,string? Location=null,string? Range=null,TimeSpan? RetryAfter=null);
public partial class ProviderHttp(IHttpClientFactory factory,OAuthTrace? trace=null) {
 public async Task<string> UploadPart(string url,byte[] bytes,CancellationToken ct) {
 using var req=new HttpRequestMessage(HttpMethod.Put,url){Content=new ByteArrayContent(bytes)};
 req.Content.Headers.ContentType=new("application/octet-stream");
 using var r=await factory.CreateClient("provider").SendAsync(req,ct);
 if(!r.IsSuccessStatusCode||!r.Headers.TryGetValues("ETag",out var etags)||string.IsNullOrWhiteSpace(etags.FirstOrDefault())) throw new ProviderException(new(JobState.Unknown,Detail:"Upload części filmu bez potwierdzonego ETag.",HttpStatus:(int)r.StatusCode));
 return etags.First().Trim('"');
 }
 public async Task<ApiReply> Send(HttpMethod method,string url,string? token=null,object? json=null,Dictionary<string,string>? form=null,Dictionary<string,string>? headers=null,HttpContent? content=null,CancellationToken ct=default,string operation="provider_request") {
 using var req=new HttpRequestMessage(method,url);
 if(token is not null) req.Headers.Authorization=new AuthenticationHeaderValue("Bearer",token);
 if(headers is not null) foreach(var h in headers) req.Headers.TryAddWithoutValidation(h.Key,h.Value);
 req.Content=content ?? (form is not null ? new FormUrlEncodedContent(form) : json is not null ? JsonContent.Create(json) : null);
 HttpResponseMessage received;
 try{received=await factory.CreateClient("provider").SendAsync(req,ct);}catch(HttpRequestException){if(trace?.Active==true)trace.Steps.Add(new(operation,null,Type:"NetworkError"));throw;}catch(OperationCanceledException){if(trace?.Active==true)trace.Steps.Add(new(operation,null,Type:"Timeout"));throw;}
 using var response=received;
 var body=await response.Content.ReadAsStringAsync(ct);
 JsonElement root;
 try { root=JsonDocument.Parse(string.IsNullOrWhiteSpace(body)?"{}":body).RootElement.Clone(); }
 catch(JsonException) { root=JsonDocument.Parse("{}").RootElement.Clone(); }
 trace?.Record(operation,(int)response.StatusCode,root);
 if((!response.IsSuccessStatusCode && !(operation=="youtube.upload"&&(int)response.StatusCode==308)) || (root.TryGetProperty("error",out var err) && err.ValueKind==JsonValueKind.Object && err.TryGetProperty("code",out var ec) && ec.ToString()!="ok")) {
 var status=(int)response.StatusCode;
 string? code=null;
 if(root.TryGetProperty("error",out var e)) code=e.ValueKind==JsonValueKind.Object ? e.TryGetProperty("code",out var c)?c.ToString():null : e.ToString();
 // Allowlist code only: provider message may contain credentials, URLs or user content.
 if(code is not null && !Regex.IsMatch(code,"^[a-zA-Z0-9_.-]{1,100}$")) code="provider_error";
 if(new Uri(url).Host=="api.x.com"){
 var problem=Optional(root,"type");var name=problem?.Split('/').LastOrDefault();
 if(code==null&&name!=null&&Regex.IsMatch(name,"^[a-zA-Z0-9_-]{1,80}$"))code=name;
 var message=status switch{
 400=>"X odrzucił parametry. Sprawdź tekst, format materiału i zakresy OAuth.",
 401=>"X odrzucił autoryzację. Połącz konto ponownie.",
 402=>"X: brak kredytów lub osiągnięty limit wydatków. Sprawdź konsolę rozliczeń; nie doładowujemy automatycznie.",
 403=>"X odmówił dostępu. Sprawdź scopes (media.write dla plików), uprawnienia aplikacji i ograniczenia konta/materiału.",
 429=>"X: osiągnięto limit żądań. Odczekaj przed ręczną próbą; brak automatycznego ponawiania.",
 _=>"X nie potwierdził operacji. Sprawdź historię i konto przed ponowieniem."};
 throw new ProviderException(new(status==401?JobState.RequiresAuthorization:status is 402 or 403 or 429?JobState.RequiresApproval:status>=500?JobState.Unknown:JobState.Failed,Detail:message,HttpStatus:status,ProviderCode:code,SafeToRetry:status is 400 or 401 or 402 or 403 or 422 or 429));
 }
 if(new Uri(url).Host=="open.tiktokapis.com"){
 var detail=code switch{
 "invalid_client" or "client_key_invalid"=>"TikTok odrzucił dane aplikacji. Sprawdź Client key i Client secret z tego samego Sandboxa.",
 "invalid_grant"=>"TikTok odrzucił kod lub refresh token. Rozpocznij nowe połączenie konta.",
 "url_ownership_unverified"=>"TikTok nie potwierdził własności adresu materiału. Sprawdź dokładny URL prefix w tej aplikacji TikTok.",
 "unaudited_client_can_only_post_to_private_accounts"=>"Nieaudytowana aplikacja może publikować tylko na prywatnym koncie TikTok. Ustaw konto jako prywatne.",
 "privacy_level_option_mismatch"=>"Opcja prywatności zmieniła się. Odśwież dane twórcy i wybierz widoczność ponownie.",
 "spam_risk_too_many_posts" or "spam_risk_too_many_pending_share" or "reached_active_user_cap"=>"TikTok ograniczył liczbę publikacji lub aktywnych twórców. Spróbuj później; nie ponawiamy automatycznie.",
 _=>null};
 if(detail!=null)throw new ProviderException(new(code is "invalid_client" or "client_key_invalid" or "invalid_grant"?JobState.RequiresAuthorization:JobState.RequiresApproval,Detail:detail,HttpStatus:status,ProviderCode:code,SafeToRetry:status<500));
 }
 if(operation.StartsWith("youtube.",StringComparison.Ordinal)){
 if(e.ValueKind==JsonValueKind.Object&&e.TryGetProperty("errors",out var reasons)&&reasons.ValueKind==JsonValueKind.Array&&reasons.GetArrayLength()>0){var reason=Optional(reasons[0],"reason");if(reason!=null&&Regex.IsMatch(reason,"^[a-zA-Z0-9_.-]{1,100}$"))code=reason;}
 if(code=="invalid_grant")throw new ProviderException(new(JobState.RequiresAuthorization,Detail:"Google odrzucił kod lub token odświeżania. Połącz YouTube ponownie. W trybie Testing refresh token może wygasać po 7 dniach.",HttpStatus:status,ProviderCode:code));
 if(code is "invalid_client" or "unauthorized_client")throw new ProviderException(new(JobState.RequiresAuthorization,Detail:"Google odrzucił dane aplikacji. Sprawdź Client ID i Client Secret klienta Web application w konfiguracji backendu.",HttpStatus:status,ProviderCode:code));
 if(code is "quotaExceeded" or "dailyLimitExceeded" or "uploadLimitExceeded")throw new ProviderException(new(JobState.RequiresApproval,Detail:"Wyczerpano limit projektu Google lub kanału YouTube. Sprawdź limity w Google Cloud i YouTube Studio; nie ponawiaj automatycznie.",HttpStatus:status,ProviderCode:code));
 }
 var auth=status==401 || code is "190" or "access_token_invalid";
 var denied=status==403 || code is "10" or "200" or "scope_not_authorized";
 var state=auth?JobState.RequiresAuthorization:denied?JobState.RequiresApproval:status>=500?JobState.Unknown:JobState.Failed;
 if(operation is "linkedin.code_exchange" or "linkedin.token_refresh"){
 if(code is "invalid_client" or "invalid_client_id" or "invalid_client_secret" or "unauthorized_client")throw new ProviderException(new(JobState.RequiresAuthorization,Detail:"LinkedIn odrzucił dane aplikacji. Sprawdź Client ID i aktualny Client Secret z tej samej aplikacji w zakładce Auth; ponowne logowanie bez poprawy konfiguracji nie rozwiąże problemu.",HttpStatus:status,ProviderCode:code,SafeToRetry:false));
 if(code=="invalid_grant")throw new ProviderException(new(JobState.RequiresAuthorization,Detail:"LinkedIn odrzucił kod lub grant OAuth. Rozpocznij nowe połączenie; sprawdź zgodność redirect URI. Nie odświeżaj starego callbacku.",HttpStatus:status,ProviderCode:code,SafeToRetry:false));
 if(status==401)throw new ProviderException(new(JobState.RequiresAuthorization,Detail:"LinkedIn zwrócił HTTP 401 podczas uzyskiwania tokenu. Nie potwierdzono danych aplikacji; sprawdź kod diagnostyczny. Sam status nie wskazuje jednoznacznej przyczyny.",HttpStatus:status,ProviderCode:code,SafeToRetry:false));
 }
 if(root.TryGetProperty("error",out var metaError)&&metaError.ValueKind==JsonValueKind.Object&&Optional(metaError,"message")=="Error validating client secret.")
 throw new ProviderException(new(JobState.Failed,Detail:"Meta odrzuciła App Secret. Administrator musi sprawdzić sekret właściwej aplikacji w konfiguracji backendu.",HttpStatus:status,ProviderCode:code,SafeToRetry:false));
 throw new ProviderException(new(state,Detail:auth?"Token odrzucony; połącz konto ponownie.":denied?"Brak uprawnienia, roli lub zgody produktu.":status==429?"Limit API. Sprawdź panel dostawcy przed ponowieniem.":"Dostawca odrzucił żądanie albo nie potwierdził wyniku. Sprawdź kod i panel dostawcy.",HttpStatus:status,ProviderCode:code,SafeToRetry:auth||denied||status==429));
 }
 return new(root,(int)response.StatusCode,response.Headers.TryGetValues("x-restli-id",out var ids)?ids.FirstOrDefault():null,response.Headers.Location?.ToString(),response.Headers.TryGetValues("Range",out var ranges)?ranges.FirstOrDefault():null,response.Headers.RetryAfter?.Delta ?? (response.Headers.RetryAfter?.Date is {} date ? date-DateTimeOffset.UtcNow : null));
 }
 public static string Required(JsonElement e,string key) => e.TryGetProperty(key,out var v) && !string.IsNullOrWhiteSpace(v.ToString()) ? v.ToString() : throw new ProviderException(new(JobState.Unknown,Detail:$"Brak oczekiwanego pola {key} w odpowiedzi API."));
 public static string? Optional(JsonElement e,string key) => e.TryGetProperty(key,out var v)?v.ToString():null;
 public static string Escape(string s)=>Uri.EscapeDataString(s);
 public static void ValidateUploadUrl(string url,params string[] suffixes) {
 if(!Uri.TryCreate(url,UriKind.Absolute,out var u)||u.Scheme!="https"||!u.IsDefaultPort||!string.IsNullOrEmpty(u.UserInfo)||!suffixes.Any(s=>u.Host==s||u.Host.EndsWith("."+s,StringComparison.OrdinalIgnoreCase)))
 throw new ProviderException(new(JobState.Failed,Detail:"API zwróciło niedozwolony host uploadu; wymagany przegląd allowlisty."));
 }
}
