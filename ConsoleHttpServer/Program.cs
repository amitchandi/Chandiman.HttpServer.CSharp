using Chandiman.HttpServer;
using Chandiman.Extensions;
using System.Reflection;
using System.Text;

namespace ConsoleHttpServer;

internal class Program
{
    private static Server server;
    static void Main(string[] args)
    {
        string websitePath = GetWebsitePath();

        server = new(websitePath);
        
        server.OnError = ErrorHandler;

        server.OnRequest = (session, context) =>
        {
            session.Authorized = true;
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

        server.Start();
        Console.ReadLine();
    }

    public static string GetWebsitePath()
    {
        // Path of our exe.
        string websitePath = Assembly.GetExecutingAssembly().Location;
        websitePath = websitePath.LeftOfRightmostOf("\\").LeftOfRightmostOf("\\").LeftOfRightmostOf("\\").LeftOfRightmostOf("\\") + "\\Website";

        return websitePath;
    }

    public static string? ErrorHandler(Server.ServerError error)
    {
        string? ret = null;

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
        }

        return ret;
    }

    public static ResponsePacket RedirectMe(Session session, Dictionary<string, string> parms)
    {
        return server.Redirect("/demo/clicked");
    }

    public static ResponsePacket AjaxResponder(Session session, Dictionary<string, string> parms)
    {
        int number = int.Parse(parms["number"]) + 10;
        string data = "You said " + number;
        ResponsePacket ret = new() { Data = Encoding.UTF8.GetBytes(data), ContentType = "text" };

        return ret;
    }
}
