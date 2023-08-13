using System;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web.Http;

namespace Service.API;

public class LoadBalancingRouter {
    public static bool IsBalanced => Global.LoadBalancing && !Global.Port.HasValue;

    public static string SendRequest(HttpRequestMessage request, string content = null, bool isRetry = false) {
        var node = Global.Nodes.NextNode;
        node.CurrentTransactions++;
        try {
            int port = node.Port;
            Console.WriteLine("Send Command to Node: {0}", port);

            var    target = new UriBuilder("http", "localhost", port);
            string path   = request.RequestUri.ToString();
            string url    = $"{target}{path.Substring(path.IndexOf("/api") + 1)}";

            using var client = new HttpClient();
            using var req    = new HttpRequestMessage(request.Method, url);
            req.Headers.Add("Authorization", $"Bearer {request.Headers.Authorization.Parameter}");
            if (request.Method == HttpMethod.Post)
                req.Content = new StringContent(content, Encoding.UTF8, "application/json");

            using var response = client.SendAsync(req);
            response.Wait();

            using var result       = response.Result;
            using var taskResponse = result.Content.ReadAsStringAsync();
            taskResponse.Wait();
            if (result.IsSuccessStatusCode)
                return taskResponse.Result;
            if (result.StatusCode != HttpStatusCode.InternalServerError || isRetry)
                throw new HttpResponseException(new HttpResponseMessage(result.StatusCode) {
                    Content = new StringContent(taskResponse.Result)
                });
            
            Console.WriteLine("Internal error in port {0}", port);
            Console.WriteLine("Suspending node for port {0}", port);
            Task.Run(() => node.Restart());
            return SendRequest(request, content, true);

        }
        finally {
            node.CurrentTransactions--;
        }
    }
}