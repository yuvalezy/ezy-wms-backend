using System.Net;
using System.Net.Http;

namespace Service.API.Models;

internal static class Response {
    internal static HttpResponseMessage ErrorMessage(string message, HttpStatusCode statusCode) {
        // message = message.Replace("\n", " ").Replace("\t", " ");
        var response = new HttpResponseMessage(statusCode) {
            Content = new MultipartContent {new StringContent(message)}
        };
        // response.Headers.Add("ErrorMessage", message);
        return response;
    }
}