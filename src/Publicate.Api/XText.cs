using Jint;
using System.Text.Json;
namespace Publicate;
public static class XText {
 static readonly object Gate=new();
 static readonly Lazy<Engine> Parser=new(()=>{
 using var stream=typeof(XText).Assembly.GetManifestResourceStream("Publicate.Api.Vendor.x-text.js")??throw new InvalidOperationException("Brak parsera X.");
 using var reader=new StreamReader(stream);
 return new Engine(options=>options.TimeoutInterval(TimeSpan.FromSeconds(2)).LimitMemory(128_000_000)).Execute(reader.ReadToEnd());
 });
 public static (int Length,bool Valid) Analyze(string text){
 if(text.Length>20_000)return(20_001,false);
 lock(Gate){var engine=Parser.Value;var json=engine.Invoke(engine.GetValue("PublicateXText").AsObject().Get("analyze"),text.Normalize()).AsString();using var d=JsonDocument.Parse(json);return(d.RootElement.GetProperty("length").GetInt32(),d.RootElement.GetProperty("valid").GetBoolean());}
 }
}
