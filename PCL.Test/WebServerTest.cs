using System;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using PCL.Core.Service;

namespace PCL.Test;

[TestClass]
public class WebServerTest
{
    /// <summary>
    /// Please run the following command:
    /// <code>
    /// curl http://localhost:8080/test/foo
    /// curl http://localhost:8080/json
    /// curl -I http://localhost:8080/any/path
    /// curl -I http://localhost:8080
    /// </code>
    /// </summary>
    [TestMethod]
    public async Task TestRoutedWebServer()
    {
        Console.WriteLine("Starting web server with default listen (127.0.0.1:8080)...");
        var server = new RoutedWebServer();
        
        server.Route("/test", (path, _) => RoutedResponse.Text(path));
        Console.WriteLine("Test(/test): 200 OK (path relative to /test)");
        await server.StartResponseOnce();
        
        dynamic obj = new { a = 123, b = new { c = this, d = "text" } };
        server.Route("/json", () => RoutedResponse.Json(obj));
        Console.WriteLine("Test(/json): 200 OK (request JSON)");
        await server.StartResponseOnce();
        
        Console.WriteLine("Test(/any/path): 404 Not Found");
        await server.StartResponseOnce();
        
        server.Route("/", () => RoutedResponse.NoContent);
        Console.WriteLine("Test(/): 204 No Content");
        await server.StartResponseOnce();
        
        server.Dispose();
        Console.WriteLine("Test complete.");
    }
}
