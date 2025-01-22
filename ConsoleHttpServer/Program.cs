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

        server = new(websitePath);
        
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
            //FilePath = "test"
            Handler = new AnonymousRouteHandler(server, CustomHandler),
            
        });

        server.AddRoute(new(){
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
        char PathSeperator = '/';
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

    public static string ErrorHandler(Server.ServerError error)
    {
        string ret;

        switch (error)
        {
            case Server.ServerError.ExpiredSession:
                ret = "/ErrorPages/expiredSession.html";
                break;
            case Server.ServerError.FileNotFound:
                ret = "/ErrorPages/fileNotFound.html";
                break;
            case Server.ServerError.NotAuthorized:
                ret = "/ErrorPages/notAuthorized.html";
                break;
            case Server.ServerError.PageNotFound:
                ret = "/ErrorPages/pageNotFound.html";
                break;
            case Server.ServerError.ServerError:
                ret = "/ErrorPages/serverError.html";
                break;
            case Server.ServerError.UnknownType:
                ret = "/ErrorPages/unknownType.html";
                break;
            default:
                ret = "/ErrorPages/serverError.html";
                break;
        }

        return ret;
    }

    public static ResponsePacket RedirectMe(Session session, Dictionary<string, object?> parms)
    {
        return server!.Redirect("/demo/clicked");
    }

    public static ResponsePacket AjaxResponder(Session session, Dictionary<string, object?> parms)
    {
        int number = int.Parse((string)parms["number"]!) + 10;
        Console.WriteLine(number);
        string data = "You said " + number;
        ResponsePacket ret = new() { Data = Encoding.UTF8.GetBytes(data), ContentType = "text" };

        return ret;
    }

    public static ResponsePacket CustomHandler(Session session, Dictionary<string, object?> parms)
    {
        return server!.CustomPath(session, "/test", parms);
    }
}
