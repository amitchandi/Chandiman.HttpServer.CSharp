using Chandiman.HttpServer;
using Chandiman.Extensions;
using System.Reflection;
using System.Text;
using System.Runtime.InteropServices;

namespace ConsoleHttpServer;

internal class Program
{
    private static Server? server;
    static void Main(string[] args)
    {
        string websitePath = GetWebsitePath();

        server = new();

        //server.AddWebsite("Default", websitePath, "", 3000);

        //server.AddWebsite("Test", GetTestWebsitePath(), "Test", 4000);

        foreach (var web in server.WebsiteContext.GetWebsites().Result)
        {
            Console.WriteLine(web);
        }

        server.OnError = ErrorHandler;

        server.OnRequest = (session, context) =>
        {
            session.Authenticated = true;
            session.UpdateLastConnectionTime();
        };

        // register a route handler:
        server.AddRoute(new Route()
        {
            Verb = Router.POST,
            Path = "/demo/redirect",
            Handler = new AuthenticatedRouteHandler(server, RedirectMe)
        });

        server.AddRoute(new Route()
        {
            Verb = Router.PUT,
            Path = "/demo/ajax",
            Handler = new AnonymousRouteHandler(server, AjaxResponder)
        });

        server.AddRoute(new Route()
        {
            Path = "/asd",
            Handler = new AnonymousRouteHandler(server, CustomHandler),

        });

        server.AddRoute(new()
        {
            Path = "/test",
            PostProcess = ReplacePostProcess.Process
        });

        server.Start(port: 3000);
        Console.ReadLine();
    }

    public static string GetWebsitePath()
    {
        // Path of our exe.
        string websitePath = Assembly.GetExecutingAssembly().Location;
        char PathSeperator;
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            PathSeperator = '/';
        }
        else
        {
            PathSeperator = '\\';
        }
        websitePath = websitePath
            .LeftOfRightmostOf(PathSeperator)
            .LeftOfRightmostOf(PathSeperator)
            .LeftOfRightmostOf(PathSeperator)
            .LeftOfRightmostOf(PathSeperator)
         + "/Website";

        return websitePath;
    }

    public static string GetTestWebsitePath()
    {
        // Path of our exe.
        string websitePath = Assembly.GetExecutingAssembly().Location;
        char PathSeperator;
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            PathSeperator = '/';
        }
        else
        {
            PathSeperator = '\\';
        }
        websitePath = websitePath
            .LeftOfRightmostOf(PathSeperator)
            .LeftOfRightmostOf(PathSeperator)
            .LeftOfRightmostOf(PathSeperator)
            .LeftOfRightmostOf(PathSeperator)
         + "/TestWebsite";

        return websitePath;
    }

    public static string ErrorHandler(Server.ServerError error)
    {
        string ret = error switch
        {
            Server.ServerError.ExpiredSession => "/ErrorPages/expiredSession.html",
            Server.ServerError.FileNotFound => "/ErrorPages/fileNotFound.html",
            Server.ServerError.NotAuthorized => "/ErrorPages/notAuthorized.html",
            Server.ServerError.PageNotFound => "/ErrorPages/pageNotFound.html",
            Server.ServerError.ServerError => "/ErrorPages/serverError.html",
            Server.ServerError.UnknownType => "/ErrorPages/unknownType.html",
            _ => "/ErrorPages/serverError.html",
        };
        return ret;
    }

    public static ResponsePacket RedirectMe(Session session, Dictionary<string, object?> parms)
        => server!.Redirect("/Demo/clicked");

    public static ResponsePacket AjaxResponder(Session session, Dictionary<string, object?> parms)
    {
        int number = int.Parse((string)parms["number"]!) + 10;
        string data = "You said " + number;
        ResponsePacket ret = new() { Data = Encoding.UTF8.GetBytes(data), ContentType = "text" };

        return ret;
    }

    public static ResponsePacket CustomHandler(Session session, Dictionary<string, object?> parms)
        => server!.CustomPath("Default", session, "/test", parms);
}
