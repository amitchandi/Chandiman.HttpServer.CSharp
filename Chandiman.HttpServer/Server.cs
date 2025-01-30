using System.Net;
using System.Net.Sockets;
using System.Text.RegularExpressions;
using Chandiman.Extensions;
using Microsoft.EntityFrameworkCore;

namespace Chandiman.HttpServer;

public partial class Server
{
    private HttpListener? listener { get; set; }

    public int maxSimultaneousConnections { get; set; } = 20;
    private Semaphore sem { get; set; }

    private Router Router { get; set; }
    private SessionManager sessionManager { get; set; }

    public int expirationTimeSeconds { get; set; } = 60;

    public Action<Session, HttpListenerContext>? OnRequest { get; set; }

    public Func<ServerError, string>? OnError { get; set; }

    public string? PublicIP { get; set; }

    public Func<Session, Dictionary<string, object?>, string, string> PostProcess { get; set; }

    private List<Website> Websites { get; set; }

    public Server()
    {
        sem = new(maxSimultaneousConnections, maxSimultaneousConnections);
        sessionManager = new();
        PostProcess = DefaultPostProcess;
        Router = new(this);
        using WebsiteContext websiteContext = new();
        Websites = websiteContext.GetWebsites().Result;
    }

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

    private List<int> GetLocalHostPorts()
    {
        using WebsiteContext websiteContext = new();
        return websiteContext.Websites
            .Select(website => website.Port)
            .ToList();
    }

    [GeneratedRegex(@"\d{1,3}\.\d{1,3}\.\d{1,3}\.\d{1,3}")]
    private static partial Regex IPRegex();
    private static string GetExternalIP()
    {
        using HttpClient httpClient = new();
        using var resp = httpClient.GetAsync("http://checkip.dyndns.org/").Result;


        return IPRegex().Matches(resp.Content.ReadAsStringAsync().Result)[0].ToString();
    }

    /// <summary>
    /// Returns the url appended with a / for port 80, otherwise, the [url]:[port]/ if the port is not 80.
    /// </summary>
    private string UrlWithPort(string url, int port)
    {
        string ret = url + "/";

        if (port != 80)
        {
            ret = url + ":" + port.ToString() + "/";
        }

        return ret;
    }

    private HttpListener InitializeListener(List<IPAddress> localhostIPs, List<int> ports)
    {
        listener = new();

        foreach (var port in ports)
        {
            string url = UrlWithPort("http://localhost", port);

            try
            {
                listener.Prefixes.Add(url);
                Console.WriteLine("Listening on " + url);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
                // Ignore exception, which will occur on AWG servers
            }

            // Listen to IP address as well.
            localhostIPs.ForEach(ip =>
            {
                url = UrlWithPort("http://" + ip.ToString(), port);
                Console.WriteLine("Listening on " + url);
                listener.Prefixes.Add(url);
            });
        }
        // TODO: What's listening on this port that is preventing me from adding an HTTPS listener???  This started all of a sudden after a reboot.
        // https:
        //listener.Prefixes.Add("https://localhost:4443/");

        return listener;
    }

    /// <summary>
    /// Begin listening to connections on a separate worker thread.
    /// </summary>
    private void Start(HttpListener listener)
    {
        listener.Start();
        Task.Run(() => RunServer(listener));
    }

    /// <summary>
    /// Start awaiting for connections, up to the "maxSimultaneousConnections" value.
    /// This code runs in a separate thread.
    /// </summary>
    private void RunServer(HttpListener listener)
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
    private async void StartConnectionListener(HttpListener listener)
    {
        ResponsePacket resp;

        // Wait for a connection. Return to caller while we wait.
        HttpListenerContext context = await listener.GetContextAsync();
        HttpListenerRequest request = context.Request;

        string path = request.RawUrl!.LeftOf("?"); // Only the path, not any of the parameters
        string verb = request.HttpMethod; // get, post, delete, etc.
        string parms = request.RawUrl!.RightOf("?"); // Params on the URL itself follow the URL and are separated by a ?

        var website_path = path.RightOf("/").LeftOf("/");

        // TODO: the empty path here is the temp default. this should be configurable in some way
        var default_website = Websites
            .Where(website => website.Path == "")
            .First();

        var website = Websites
            .Where(website => website.Path == website_path)
            .DefaultIfEmpty(default_website)
            .First();

        Session session = sessionManager.GetSession(request.RemoteEndPoint);
        OnRequest.IfNotNull(r => r!(session, context));

        // Release the semaphore so that another listener can be immediately started up.
        sem.Release();

        Log(request);

        try
        {
            Dictionary<string, object?> kvParams = GetKeyValues(parms); // Extract into key-value entries.
            string data = new StreamReader(context.Request.InputStream, context.Request.ContentEncoding).ReadToEnd();
            Console.WriteLine(data);
            GetKeyValues(data, kvParams);
            Log(kvParams);

            if (!VerifyCSRF(session, verb, kvParams))
            {
                Console.WriteLine("CSRF did not match.  Terminating connection.");
                context.Response.OutputStream.Close();
            }
            else
            {
                if (request.Url?.Port != website.Port)
                {
                    resp = new ResponsePacket()
                    {
                        Error = ServerError.PageNotFound
                    };
                }
                else
                    resp = Router.Route(website, session, verb, path, kvParams);

                // Update session last connection after getting the response,
                // as the router itself validates session expiration only on pages requiring authentication.
                session.UpdateLastConnectionTime();

                if (resp.Error != ServerError.OK)
                {
                    resp.Redirect = OnError.IfNotNullReturn((OnError) => OnError!(resp.Error));
                }

                try
                {
                    Respond(context.Request, context.Response, resp);
                }
                catch (Exception ex)
                {
                    // The response failed!
                    // TODO: We need to put in some decent logging!
                    Console.WriteLine(ex.Message);
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.Message);
            Console.WriteLine(ex.StackTrace);
            resp = new ResponsePacket()
            {
                Redirect = OnError.IfNotNullReturn((OnError) => OnError!(ServerError.ServerError))
            };
            Respond(context.Request, context.Response, resp);
        }
    }

    /// <summary>
    /// If a CSRF validation token exists, verify it matches our session value.
    /// If one doesn't exist, issue a warning to the console.
    /// </summary>
    private bool VerifyCSRF(Session session, string verb, Dictionary<string, object?> kvParams)
    {
        bool ret = true;

        if (verb.ToLower() != "get")
        {
            if (kvParams.TryGetValue(ValidationTokenName, out object? token))
            {
                ret = session.Objects[ValidationTokenName]?.ToString() == token?.ToString();
            }
            else
            {
                Console.WriteLine("Warning - CSRF token is missing. Consider adding it to the request.");
            }
        }

        return ret;
    }

    /// <summary>
    /// Starts the web server.
    /// </summary>
    public void Start(int port = 80, bool acquirePublicIP = false)
    {
        using WebsiteContext websiteContext = new();
        if (!websiteContext.Websites.Any())
            throw new Exception("Websites must not be empty. You can add a website by running Server.AddWebsite()");

        OnError.IfNull(() => Console.WriteLine("Warning - the onError callback has not been initialized by the application."));

        if (acquirePublicIP)
        {
            PublicIP = GetExternalIP();
            Console.WriteLine("public IP: " + PublicIP);
        }

        List<IPAddress> localHostIPs = GetLocalHostIPs();
        List<int> ports = GetLocalHostPorts();
        HttpListener listener = InitializeListener(localHostIPs, ports);
        try
        {
            Start(listener);
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.ToString());
        }
    }

    /// <summary>
    /// Tranform URL or Request Body parameters into key-value pairs in a Dictionary
    /// </summary>
    /// <param name="data">URL or Request Body parameters as text</param>
    /// <param name="kv">optional exisitng key-value parameters</param>
    /// <returns></returns>
    private Dictionary<string, object?> GetKeyValues(string data, Dictionary<string, object?>? kv = null)
    {
        kv.IfNull(() => kv = new Dictionary<string, object?>());
        data.If(d => d.Length > 0, (d) => d.Split('&').ForEach(keyValue => kv![keyValue.LeftOf('=')] = keyValue.RightOf('=')));

        return kv!;
    }

    /// <summary>
    /// Log requests.
    /// </summary>
    public void Log(HttpListenerRequest request)
        => Console.WriteLine(request.RemoteEndPoint + " " + request.HttpMethod + " /" + request.Url?.AbsoluteUri);

    /// <summary>
    /// Log URL parameters
    /// </summary>
    /// <param name="kv"></param>
    private void Log(Dictionary<string, object?> kv)
        => kv.ForEach(kvp => Console.WriteLine(kvp.Key + " : " + kvp.Value));

    /// <summary>
    /// Handle HttpListener Response
    /// </summary>
    /// <param name="request">HttpListener Request</param>
    /// <param name="response">HttpListener Response</param>
    /// <param name="resp">ResponsePacket</param>
    private void Respond(HttpListenerRequest request, HttpListenerResponse response, ResponsePacket resp)
    {
        if (string.IsNullOrEmpty(resp.Redirect))
        {
            if (resp.Data != null)
            {
                response.ContentType = resp.ContentType;
                response.ContentLength64 = resp.Data.Length;
                response.OutputStream.Write(resp.Data, 0, resp.Data.Length);
                response.ContentEncoding = resp.Encoding;
            }

            response.StatusCode = (int)HttpStatusCode.OK;
        }
        else
        {
            response.StatusCode = (int)HttpStatusCode.Redirect;

            if (string.IsNullOrEmpty(PublicIP))
            {
                string redirectUrl = request!.Url!.Scheme + "://" + request!.Url!.Host + ":" + request.Url.Port + resp.Redirect;
                response.Redirect(redirectUrl);
            }
            else
            {
                string redirectUrl = request!.Url!.Scheme + "://" + request!.Url!.Host + ":" + request.Url.Port + resp.Redirect;
                response.Redirect(redirectUrl);
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

    /// <summary>
    /// Add <paramref name="route"/> to routes
    /// </summary>
    /// <param name="route"></param>
    public void AddRoute(Route route) => Router.AddRoute(route);

    /// <summary>
    /// Return a ResponsePacket with the specified URL and an optional (singular) parameter.
    /// </summary>
    public ResponsePacket Redirect(string url, string? parm = null)
    {
        ResponsePacket ret = new ResponsePacket() { Redirect = url };
        parm.IfNotNull((p) => ret.Redirect += "?" + p);

        return ret;
    }

    /// <summary>
    /// Return a ResponsePacket with the specified filePath as the target file to load
    /// </summary>
    /// <param name="session">Session</param>
    /// <param name="filePath">path to file</param>
    /// <param name="parms">URL paramaters</param>
    /// <returns>ResponsePacket</returns>
    public ResponsePacket CustomPath(string websitepath, Session session, string filePath, Dictionary<string, object?> parms)
    {
        var website = Websites
            .Where(website => website.Path == websitepath)
            .First();
        return Router.Route(website, session, Router.GET, filePath, parms);
    }

    public const string ValidationTokenScript = "<%AntiForgeryToken%>";
    public const string ValidationTokenName = "__CSRFToken__";

    /// <summary>
    /// Callable by the application for default handling, therefore must be public.
    /// </summary>
    public string DefaultPostProcess(Session session, Dictionary<string, object?> kvParms, string html)
    {
        string ret = html.Replace(ValidationTokenScript, "<input name=" + ValidationTokenName.SingleQuote() +
            " type='hidden' value=" + session[ValidationTokenName]?.ToString()?.SingleQuote() +
            " id='__csrf__'/>");

        // For when the CSRF is in a knockout model or other JSON that is being posted back to the server.
        ret = ret.Replace("@CSRF@", session[ValidationTokenName]?.ToString()?.SingleQuote());

        ret = ret.Replace("@CSRFValue@", session[ValidationTokenName]?.ToString());

        return ret;
    }

    //TODO: might remove
    public async void AddWebsite(string websiteName, string websitePath, string path, int Port)
    {
        try
        {
            using WebsiteContext websiteContext = new();
            await websiteContext.Websites.AddAsync(new Website
            {
                WebsiteId = websiteName,
                WebsitePath = websitePath,
                Path = path,
                Port = Port
            });
            await websiteContext.SaveChangesAsync();
            Websites.Clear();
            Websites = await websiteContext.GetWebsites();
        }
        catch (DbUpdateException ex)
        {
            Console.WriteLine(ex.ToString());
        }
    }
}
