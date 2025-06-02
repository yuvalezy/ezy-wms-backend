// using System.Web;
//
// namespace Service.StaticFiles; 
//
// public class FileHandler : IHttpHandler
// {
//     public void ProcessRequest(HttpContext context)
//     {
//         string filePath = context.Server.MapPath("~/wwwroot" + context.Request.Url.AbsolutePath);
//         context.Response.WriteFile(filePath);
//     }
//
//     public bool IsReusable => false;
// }
