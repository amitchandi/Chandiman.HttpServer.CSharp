using System.Net;
using System.Net.Sockets;
using System.Text.RegularExpressions;
using Chandiman.Extensions;

namespace Chandiman.HttpServer;

public static class Server
{
    private static HttpListener? listener;

    public static int maxSimultaneousConnections = 20;
    private static Semaphore sem = new(maxSimultaneousConnections, maxSimultaneousConnections);

    private static Router router = new Router();
    private static SessionManager sessionManager;

    public static int expirationTimeSeconds = 60;

    public static Action<Session, HttpListenerContext> onRequest;

    public static Func<ServerError, string> OnError { get; set; }

    public static string? PublicIP;

    /// <summary>
    /// Returns list of IP addresses assigned to localhost network devices, such as hardwired ethernet, wireless, etc.
    /// </summary>
    private static List<IPAddress> GetLocalHostIPs()
    {
        IPHostEntry host;
        host = Dns.GetHostEntry(Dns.GetHostName());
        List<IPAddress> ret = host.AddressList.Where(ip => ip.AddressFamily == AddressFamily.InterNetwork).ToList();

        return ret;
    }

    public static string GetExternalIP()
    {
        string externalIP;
        externalIP = (new WebClient()).DownloadString("http://checkip.dyndns.org/");
        externalIP = (new Regex(@"\d{1,3}\.\d{1,3}\.\d{1,3}\.\d{1,3}")).Matches(externalIP)[0].ToString();

        return externalIP;
    }

    private static HttpListener InitializeListener(List<IPAddress> localhostIPs)
    {
        listener = new();
        listener.Prefixes.Add("http://localhost/");

        // Listen to IP address as well.
        localhostIPs.ForEach(ip =>
        {
            Console.WriteLine("Listening on IP " + "http://" + ip.ToString() + "/");
            listener.Prefixes.Add("http://" + ip.ToString() + "/");
        });

        return listener;
    }

    /// <summary>
    /// Begin listening to connections on a separate worker thread.
    /// </summary>
    private static void Start(HttpListener listener)
    {
        sessionManager = new SessionManager();
        listener.Start();
        Task.Run(() => RunServer(listener));
    }

    /// <summary>
    /// Start awaiting for connections, up to the "maxSimultaneousConnections" value.
    /// This code runs in a separate thread.
    /// </summary>
    private static void RunServer(HttpListener listener)
    {
        while (true)
        {
            sem.WaitOne();
            StartConnectionListener(listener);
        }
    }

    /// <summary>
    /// Await connections.
    /// </summary>
    private static async void StartConnectionListener(HttpListener listener)
    {
        ResponsePacket resp;

        // Wait for a connection. Return to caller while we wait.
        HttpListenerContext context = await listener.GetContextAsync();

        Session session = sessionManager.GetSession(context.Request.RemoteEndPoint);
        onRequest.IfNotNull(r => r(session, context));

        // Release the semaphore so that another listener can be immediately started up.
        sem.Release();

        Log(context.Request);

        HttpListenerRequest request = context.Request;

        try
        {
            string path = request.RawUrl!.LeftOf("?"); // Only the path, not any of the parameters
            string verb = request.HttpMethod; // get, post, delete, etc.
            string parms = request.RawUrl!.RightOf("?"); // Params on the URL itself follow the URL and are separated by a ?
            
            Dictionary<string, string> kvParams = GetKeyValues(parms); // Extract into key-value entries.
            string data = new StreamReader(context.Request.InputStream, context.Request.ContentEncoding).ReadToEnd();
            GetKeyValues(data, kvParams);
            Log(kvParams);

            

            resp = router.Route(session, verb, path, kvParams);

            // Update session last connection after getting the response,
            // as the router itself validates session expiration only on pages requiring authentication.
            session.UpdateLastConnectionTime();

            if (resp.Error != ServerError.OK)
            {
                resp.Redirect = OnError(resp.Error);
            }

            Respond(context.Request, context.Response, resp);
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
            Console.WriteLine(ex.StackTrace);
            resp = new ResponsePacket() { Redirect = OnError(ServerError.ServerError) };
            Respond(context.Request, context.Response, resp);
        }
    }

    /// <summary>
    /// Starts the web server.
    /// </summary>
    public static void Start(string websitePath)
    {
        router.WebsitePath = websitePath;

        List<IPAddress> localHostIPs = GetLocalHostIPs();
        HttpListener listener = InitializeListener(localHostIPs);
        Start(listener);
    }

    /// <summary>
    /// Log requests.
    /// </summary>
    public static void Log(HttpListenerRequest request)
    {
        Console.WriteLine(request.RemoteEndPoint + " " + request.HttpMethod + " /" + request.Url?.AbsoluteUri.RightOf('/', 3));
    }

    private static Dictionary<string, string> GetKeyValues(string data, Dictionary<string, string>? kv = null)
    {
        kv.IfNull(() => kv = new Dictionary<string, string>());
        data.If(d => d.Length > 0, (d) => d.Split('&').ForEach(keyValue => kv![keyValue.LeftOf('=')] = keyValue.RightOf('=')));

        return kv!;
    }

    private static void Log(Dictionary<string, string> kv)
    {
        kv.ForEach(kvp => Console.WriteLine(kvp.Key + " : " + kvp.Value));
    }

    private static void Respond(HttpListenerRequest request, HttpListenerResponse response, ResponsePacket resp)
    {
        if (string.IsNullOrEmpty(resp.Redirect))
        {
            response.ContentType = resp.ContentType;
            response.ContentLength64 = resp.Data!.Length;
            response.OutputStream.Write(resp.Data, 0, resp.Data.Length);
            response.ContentEncoding = resp.Encoding;
            response.StatusCode = (int)HttpStatusCode.OK;
        }
        else
        {
            response.StatusCode = (int)HttpStatusCode.Redirect;

            if (string.IsNullOrEmpty(PublicIP))
            {
                response.Redirect("http://" + request.UserHostAddress + resp.Redirect);
            }
            else
            {
                response.Redirect("http://" + PublicIP + resp.Redirect);
            }
        }
        
        response.OutputStream.Close();
    }

    public enum ServerError
    {
        OK,
        ExpiredSession,
        NotAuthorized,
        FileNotFound,
        PageNotFound,
        ServerError,
        UnknownType,
        ValidationError,
        AjaxError,
    }

    public static void AddRoute(Route route) => router.AddRoute(route);

    /// <summary>
    /// Return a ResponsePacket with the specified URL and an optional (singular) parameter.
    /// </summary>
    public static ResponsePacket Redirect(string url, string? parm = null)
    {
        ResponsePacket ret = new ResponsePacket() { Redirect = url };
        parm.IfNotNull((p) => ret.Redirect += "?" + p);

        return ret;
    }

    public static string ValidationTokenScript = "<%AntiForgeryToken%>";
    public static string ValidationTokenName = "__CSRFToken__";

    public static string PostProcess(Session session, string html)
    {
        string ret = html.Replace(ValidationTokenScript,
        "<input name='" +
        ValidationTokenName +
        "' type='hidden' value='" +
        session.Objects[ValidationTokenName]?.ToString() +
        " id='#__csrf__'" +
        "/>");

        return ret;
    }
}