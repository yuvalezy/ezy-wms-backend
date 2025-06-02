// using System;
// using System.Threading.Tasks;
// using Microsoft.Owin;
//
// namespace Service.API;
//
// public class LoggingMiddleware : OwinMiddleware {
//     public LoggingMiddleware(OwinMiddleware next) : base(next) {
//     }
//
//     public override async Task Invoke(IOwinContext context) {
//         var request  = context.Request;
//         var response = context.Response;
//         try {
//             // Log the incoming request
//             LogRequest(request);
//
//             // Allow the request to continue through the pipeline
//             await Next.Invoke(context);
//
//             // Optionally, you can log the response as well
//             LogResponse(response);
//         }
//         catch (Exception ex) {
//             // Log the unhandled exception
//             LogException(ex);
//
//             // Re-throw the exception to propagate it up the middleware pipeline
//             throw;
//         }
//     }
//
//     private void LogRequest(IOwinRequest request) {
//         // Here, you can extract various information from the request to log
//         var    headers    = request.Headers;
//         string method     = request.Method;
//         var    uri        = request.Uri;
//         // var    bodyStream = request.Body; // Be careful with this, as reading it might mean you have to rewind it
//
//         // TODO: Add your logging logic here. This is just a basic example.
//         Console.WriteLine($"Request - Method: {method}, URI: {uri}");
//     }
//
//     private void LogResponse(IOwinResponse response) {
//         // Extract required information from the response
//         var statusCode   = response.StatusCode;
//         var reasonPhrase = response.ReasonPhrase;
//
//         // TODO: Add your logging logic here.
//         Console.WriteLine($"Response - Status Code: {statusCode}, Reason: {reasonPhrase}");
//     }
//
//     private void LogException(Exception ex) {
//         // Log the exception details
//         // You can use a logging library or write to a log file here.
//         Console.WriteLine($"Exception: {ex.Message}");
//         Console.WriteLine($"StackTrace: {ex.StackTrace}");
//     }
// }