using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace Service.Middlewares;

public class RequestLoggingMiddleware(RequestDelegate next) {
    public async Task InvokeAsync(HttpContext context) {
        // Log request details
        Console.WriteLine($"Request: {context.Request.Method} {context.Request.Path}");

        // Log query string
        if (context.Request.QueryString.HasValue) {
            Console.WriteLine($"Query String: {context.Request.QueryString.Value}");
        }

        // Log request body if it exists and is of a type that can have a body
        if (context.Request.ContentLength > 0 &&
            (context.Request.Method == HttpMethods.Post || context.Request.Method == HttpMethods.Put || context.Request.Method == HttpMethods.Patch)) {
            context.Request.EnableBuffering(); // Allows us to read the request body without consuming it

            using var reader = new StreamReader(context.Request.Body, leaveOpen: true);
            string    body   = await reader.ReadToEndAsync();
            Console.WriteLine($"Body: {body}");

            // Reset the request body stream position so it can be read by the next middleware or controller
            context.Request.Body.Position = 0;
        }

        // Call the next middleware in the pipeline
        await next(context);
    }
}