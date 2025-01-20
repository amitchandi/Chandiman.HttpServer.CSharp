using System.Net;
using System.Runtime.InteropServices;
using System.Text;
using Chandiman.Extensions;

namespace Chandiman.HttpServer;

public class Router
{
    public const string POST = "post";
    public const string GET = "get";
    public const string PUT = "put";
    public const string DELETE = "delete";

    private string WebsitePath { get; set; }
    private char PathSeperator { get; set; }

    private Dictionary<string, ExtensionInfo> extFolderMap;

    public List<Route> routes;
    public Server serverInstance;

    public Router(string websitePath, Server server)
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            PathSeperator = '/';
        }
        else
        {
            PathSeperator = '\\';
        }


        WebsitePath = websitePath;
        serverInstance = server;

        routes = [];
        extFolderMap = new Dictionary<string, ExtensionInfo>()
        {
            {"ico", new ExtensionInfo() {Loader=ImageLoader, ContentType="image/ico"}},
            {"png", new ExtensionInfo() {Loader=ImageLoader, ContentType="image/png"}},
            {"jpg", new ExtensionInfo() {Loader=ImageLoader, ContentType="image/jpg"}},
            {"gif", new ExtensionInfo() {Loader=ImageLoader, ContentType="image/gif"}},
            {"bmp", new ExtensionInfo() {Loader=ImageLoader, ContentType="image/bmp"}},
            {"html", new ExtensionInfo() {Loader=PageLoader, ContentType="text/html"}},
            {"css", new ExtensionInfo() {Loader=FileLoader, ContentType="text/css"}},
            {"js", new ExtensionInfo() {Loader=FileLoader, ContentType="text/javascript"}},
            {"", new ExtensionInfo() {Loader=PageLoader, ContentType="text/html"}},
        };
    }

    public void AddRoute(Route route) => routes.Add(route);

    /// <summary>
    /// Read in an image file and returns a ResponsePacket with the raw data.
    /// </summary>
    private ResponsePacket ImageLoader(Route? routeHandler, Session session, Dictionary<string, object?> kvParams, string fullPath, string ext, ExtensionInfo extInfo)
    {
        FileStream fStream = new FileStream(fullPath, FileMode.Open, FileAccess.Read);
        BinaryReader br = new BinaryReader(fStream);
        ResponsePacket ret = new ResponsePacket() { Data = br.ReadBytes((int)fStream.Length), ContentType = extInfo.ContentType };
        br.Close();
        fStream.Close();

        return ret;
    }

    /// <summary>
    /// Read in what is basically a text file and return a ResponsePacket with the text UTF8 encoded.
    /// </summary>
    private ResponsePacket FileLoader(Route? routeHandler, Session session, Dictionary<string, object?> kvParams, string fullPath, string ext, ExtensionInfo extInfo)
    {
        ResponsePacket ret;

        if (!File.Exists(fullPath))
        {
            ret = new ResponsePacket() { Error = Server.ServerError.FileNotFound };
            Console.WriteLine("File not found. " + fullPath);
        }
        else
        {
            string text = File.ReadAllText(fullPath);
            ret = new() { Data = Encoding.UTF8.GetBytes(text), ContentType = extInfo.ContentType, Encoding = Encoding.UTF8 };
        }

        return ret;
    }

    /// <summary>
    /// Load an HTML file, taking into account missing extensions and a file-less IP/domain, 
    /// which should default to index.html.
    /// </summary>
    private ResponsePacket PageLoader(Route? routeHandler, Session session, Dictionary<string, object?> kvParams, string fullPath, string ext, ExtensionInfo extInfo)
    {
        ResponsePacket ret;

        if (fullPath == WebsitePath) // If nothing follows the domain name or IP, then default to loading index.html.
        {
            ret = Route(session, GET, "/index.html", []);
        }
        else
        {
            if (string.IsNullOrEmpty(ext))
            {
                // No extension, so we make it ".html"
                fullPath += ".html";
            }
            
            // Inject the "Pages" folder into the path
            fullPath = WebsitePath + PathSeperator + "Pages" + fullPath.RightOf(WebsitePath);

            if (!File.Exists(fullPath))
            {
                ret = new() { Error = Server.ServerError.PageNotFound };
                Console.WriteLine("File not found. " + fullPath);
            }
            else
            {
                string text = File.ReadAllText(fullPath);

                // Do the application global post process replacement.
                text = serverInstance.PostProcess(session, kvParams, text);

                // If a custom post process callback exists, call it.
                routeHandler.IfNotNull((r) => r!.PostProcess.IfNotNull((p) => text = p!(session, kvParams, text)));

                // Do our default post process to catch any final CSRF stuff in the fully merged document.
                text = serverInstance.PostProcess(session, kvParams, text);


                ret = new ResponsePacket() { Data = Encoding.UTF8.GetBytes(text), ContentType = extInfo.ContentType, Encoding = Encoding.UTF8 };
            }
        }

        return ret;
    }

    public ResponsePacket Route(Session session, string verb, string path, Dictionary<string, object?> kvParams)
    {
        string ext = path.RightOfRightmostOf('.');
        ExtensionInfo? extInfo;
        ResponsePacket? ret = null;
        verb = verb.ToLower();

        if (extFolderMap.TryGetValue(ext, out extInfo))
        {
            string wpath = path.Substring(1).Replace('/', PathSeperator); // Strip off leading '/' and reformat as with windows path separator.
            string fullPath = Path.Combine(WebsitePath, wpath);

            Route? route = routes.SingleOrDefault(r => verb == r.Verb.ToLower() && path == r.Path);

            if (route != null)
            {
                // Application has a handler for this route.
                ResponsePacket? handlerResponse = route.Handler?.Handle(session, kvParams);

                if (handlerResponse == null)
                {
                    if (!route.FilePath.IsEmpty())
                    {
                        fullPath = Path.Combine(WebsitePath, route.FilePath);
                    }

                    // Respond with default content loader.
                    ret = extInfo.Loader!(route, session, kvParams, fullPath, ext, extInfo);
                }
                else
                {
                    // Respond with redirect.
                    ret = handlerResponse;
                }
            }
            else
            {
                // Attempt default behavior
                ret = extInfo.Loader!(route, session, kvParams, fullPath, ext, extInfo);
            }
        }
        else
        {
            ret = new ResponsePacket() { Error = Server.ServerError.UnknownType };
        }

        return ret;
    }

    internal class ExtensionInfo
    {
        public string? ContentType { get; set; }
        public Func<Route?, Session, Dictionary<string, object?>, string, string, ExtensionInfo, ResponsePacket>? Loader { get; set; }
    }
}

public class ResponsePacket
{
    public string? Redirect { get; set; }
    public byte[]? Data { get; set; }
    public string? ContentType { get; set; }
    public Encoding? Encoding { get; set; }
    public Server.ServerError Error { get; set; }
    public HttpStatusCode? StatusCode { get; set; }

    public ResponsePacket()
    {
        Error = Server.ServerError.OK;
        StatusCode = HttpStatusCode.OK;
    }
}

public class Route
{
    public string Verb { get; set; } = "get";
    public string Path { get; set; } = "";
    public RouteHandler? Handler { get; set; }
    public string FilePath { get; set; } = "";
    public Func<Session, Dictionary<string, object?>, string, string>? PostProcess { get; set; }
}