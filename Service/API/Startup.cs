using System;
using System.Web.Http;
using Microsoft.Owin;
using Microsoft.Owin.Cors;
using Microsoft.Owin.Security.OAuth;
using Owin;

namespace Service.API; 

public class Startup {
    // ReSharper disable once UnusedMember.Global
    public void Configuration(IAppBuilder app) {
        app.Use<LoggingMiddleware>();
        app.UseCors(CorsOptions.AllowAll);
        var config = new HttpConfiguration();
        config.MapHttpAttributeRoutes();
        config.Routes.MapHttpRoute("DefaultApi", "api/{controller}");
        config.Routes.MapHttpRoute("DataApi", "api/{controller}/{action}");
        config.Formatters.XmlFormatter.UseXmlSerializer = true;

        var options = new OAuthAuthorizationServerOptions {
            TokenEndpointPath         = new PathString("/token"),
            Provider                  = new ApplicationAuthProvider(),
            AccessTokenExpireTimeSpan = TimeSpan.FromMinutes(60),
            AllowInsecureHttp         = true
        };
        if (Global.LoadBalancing && Global.RestAPISettings.EnableRedisServer)
            options.RefreshTokenProvider = new RefreshTokenProvider(Global.RestAPISettings.RedisServer);
        app.UseOAuthAuthorizationServer(options);
        app.UseOAuthBearerAuthentication(new OAuthBearerAuthenticationOptions());

        app.UseWebApi(config);
    }
}