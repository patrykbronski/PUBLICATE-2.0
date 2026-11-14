using Microsoft.AspNetCore.WebUtilities;
using Publicate;
using Xunit;
namespace Publicate.Tests;
public class FacebookLoginTests {
 [Fact] public void BusinessLoginUsesConfigurationWithoutScopeAndFreshState() {
 var s=AdapterTests.Setup(Platform.Facebook);s.config["Providers:Meta:ClientId"]="fixture-app";s.config["Providers:Meta:ClientSecret"]="fixture-secret";
 s.config["Providers:Meta:ConfigId"]="29698534673097784";
 var uri=new Uri(s.oauth.Begin(Platform.Facebook,"browser"));
 var q=QueryHelpers.ParseQuery(uri.Query);
 Assert.Equal("www.facebook.com",uri.Host);
 Assert.Equal("29698534673097784",q["config_id"].ToString());
 Assert.Equal("code",q["response_type"].ToString());
 Assert.Equal("true",q["override_default_response_type"].ToString());
 Assert.EndsWith("/api/oauth/Facebook/callback",q["redirect_uri"].ToString());
 Assert.False(q.ContainsKey("scope"));
 Assert.DoesNotContain("fixture-secret",uri.ToString());
 var next=QueryHelpers.ParseQuery(new Uri(s.oauth.Begin(Platform.Facebook,"browser")).Query);
 Assert.NotEqual(q["state"].ToString(),next["state"].ToString());
 }
 [Fact] public void StandardLoginRetainsScopes() {
 var s=AdapterTests.Setup(Platform.Facebook);s.config["Providers:Meta:ClientId"]="fixture-app";s.config["Providers:Meta:ClientSecret"]="fixture-secret";
 var q=QueryHelpers.ParseQuery(new Uri(s.oauth.Begin(Platform.Facebook,"browser")).Query);
 Assert.False(q.ContainsKey("config_id"));
 Assert.Contains("pages_manage_posts",q["scope"].ToString());
 }
 [Fact] public void InvalidConfigurationRejected() {
 var s=AdapterTests.Setup(Platform.Facebook);s.config["Providers:Meta:ClientId"]="fixture-app";s.config["Providers:Meta:ClientSecret"]="fixture-secret";s.config["Providers:Meta:ConfigId"]="invalid";
 Assert.Throws<InvalidOperationException>(()=>s.oauth.Begin(Platform.Facebook,"browser"));
 }
}
