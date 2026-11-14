using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
namespace Publicate;

public record OAuthStep(string Stage,int? HttpStatus,string? Code=null,string? Subcode=null,string? Type=null,string? TraceId=null,bool? IsTransient=null,string? Message=null,string? MessageHash=null);
public record OAuthReport(string Id,Guid OwnerId,string Platform,DateTimeOffset CreatedAt,bool Success,string Message,OAuthStep[] Steps);
public class OAuthReports(IConfiguration config,ILogger<OAuthReports> logger) {
 private readonly ConcurrentDictionary<string,OAuthReport> reports=new();
 private readonly object fileLock=new();
 public void Save(OAuthReport report) {
 foreach(var old in reports.Where(x=>x.Value.CreatedAt<DateTimeOffset.UtcNow.AddMinutes(-20)))reports.TryRemove(old.Key,out _);
 if(reports.Count>1000)foreach(var old in reports.OrderBy(x=>x.Value.CreatedAt).Take(100))reports.TryRemove(old.Key,out _);
 reports[report.Id]=report;
 // The report contains only an allowlist of diagnostic fields, never request URLs/bodies.
 var safe=new{report.Id,report.Platform,report.CreatedAt,report.Success,report.Message,report.Steps};
 try {lock(fileLock){var path=Path.GetFullPath(config["OAuth:DiagnosticsPath"]??"../../.data/oauth-diagnostics.jsonl");Directory.CreateDirectory(Path.GetDirectoryName(path)!);if(File.Exists(path)&&new FileInfo(path).Length>1_048_576)File.Move(path,path+".previous",true);File.AppendAllText(path,JsonSerializer.Serialize(safe)+Environment.NewLine);}}
 catch(Exception e) when(e is IOException or UnauthorizedAccessException){logger.LogWarning("Nie można zapisać bezpiecznej diagnostyki OAuth {ReportId}",report.Id);}
 if(!report.Success)logger.LogWarning("OAuth {Platform} {ReportId}: {Steps}",report.Platform,report.Id,JsonSerializer.Serialize(report.Steps));
 }
 public OAuthReport? Get(string id,Guid owner)=>reports.TryGetValue(id,out var report)&&report.OwnerId==owner&&report.CreatedAt>DateTimeOffset.UtcNow.AddMinutes(-20)?report:null;
}
public class OAuthTrace {
 public List<OAuthStep> Steps {get;}=[];
 public bool Active {get;set;}
 public static string? SafeField(JsonElement error,string key)=>error.TryGetProperty(key,out var value)&&Regex.IsMatch(value.ToString(),"^[a-zA-Z0-9_.-]{1,120}$")?value.ToString():null;
 public void Record(string stage,int status,JsonElement response) {
 if(!Active)return;
 if(response.TryGetProperty("error",out var error)&&error.ValueKind==JsonValueKind.Object){
 var message=ProviderHttp.Optional(error,"message");
 // Even provider messages can echo secrets. Only exact generic messages are retained.
 var safe=message is "Error validating client secret." or "An unknown error occurred." or "An unexpected error has occurred. Please retry your request later."?message:"Treść pominięta: może zawierać dane poufne.";
 Steps.Add(new(stage,status,SafeField(error,"code"),SafeField(error,"error_subcode"),SafeField(error,"type"),SafeField(error,"fbtrace_id"),error.TryGetProperty("is_transient",out var transient)&&transient.ValueKind is JsonValueKind.True or JsonValueKind.False?transient.GetBoolean():null,safe,message is null?null:Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(message)))));
 }else if(response.TryGetProperty("error",out var oauthError)&&oauthError.ValueKind==JsonValueKind.String){
 var description=ProviderHttp.Optional(response,"error_description");
 Steps.Add(new(stage,status,SafeField(response,"error"),Message:description is null?null:"Treść pominięta: może zawierać dane poufne.",MessageHash:description is null?null:Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(description)))));
 }else if(status>=400){
 var message=ProviderHttp.Optional(response,"message");
 Steps.Add(new(stage,status,SafeField(response,"serviceErrorCode"),Type:SafeField(response,"code"),Message:message is null?null:"Treść pominięta: może zawierać dane poufne.",MessageHash:message is null?null:Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(message)))));
 }else Steps.Add(new(stage,status));
 }
}
