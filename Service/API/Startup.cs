using System;
using System.Web.Http;
using Microsoft.Owin;
using Microsoft.Owin.Cors;
using Microsoft.Owin.FileSystems;
using Microsoft.Owin.Security.OAuth;
using Microsoft.Owin.StaticFiles;
using Newtonsoft.Json.Converters;
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

        var jsonFormatter = config.Formatters.JsonFormatter;
        var item          = new StringEnumConverter();
        jsonFormatter.SerializerSettings.Converters.Add(item);
        jsonFormatter.SerializerSettings.ContractResolver = new Newtonsoft.Json.Serialization.CamelCasePropertyNamesContractResolver();
        
        var options = new OAuthAuthorizationServerOptions {
            TokenEndpointPath         = new PathString("/token"),
            Provider                  = new ApplicationAuthProvider(),
            AllowInsecureHttp         = true
        };
        if (Global.LoadBalancing && Global.RestAPISettings.EnableRedisServer)
            options.RefreshTokenProvider = new RefreshTokenProvider(Global.RestAPISettings.RedisServer);
        app.UseOAuthAuthorizationServer(options);
        app.UseOAuthBearerAuthentication(new OAuthBearerAuthenticationOptions());

        app.UseWebApi(config);

        // Configure static file serving
        var physicalFileSystem = new PhysicalFileSystem("./wwwroot");
        var fileOptions = new FileServerOptions {
            EnableDefaultFiles = true,
            FileSystem         = physicalFileSystem,
            StaticFileOptions = {
                FileSystem            = physicalFileSystem,
                ServeUnknownFileTypes = true
            }
        };
        app.UseFileServer(fileOptions);
    }
    
}