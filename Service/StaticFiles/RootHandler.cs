// using System.Web;
//
// namespace Service.StaticFiles;
//
// public class RootHandler : IHttpHandler {
//     public void ProcessRequest(HttpContext context) {
//         string filePath = context.Server.MapPath("~/wwwroot/index.html");
//         context.Response.WriteFile(filePath);
//     }
//
//     public bool IsReusable => false;
// }