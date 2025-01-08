using System.Net;

using Chandiman.Extensions;

namespace Chandiman.HttpServer;

/// <summary>
/// Sessions are associated with the client IP.
/// </summary>
public class Session
{
    public DateTime LastConnection { get; set; }
    public bool Authorized { get; set; }

    /// <summary>
    /// Can be used by controllers to add additional information that needs to persist in the session.
    /// </summary>
    public Dictionary<string, string> Objects { get; set; }

    public Session()
    {
        Objects = new Dictionary<string, string>();
        UpdateLastConnectionTime();
    }

    public void UpdateLastConnectionTime()
    {
        LastConnection = DateTime.Now;
    }


    /// <summary>
    /// Returns true if the last request exceeds the specified expiration time in seconds.
    /// </summary>
    public bool IsExpired(int expirationInSeconds)
    {
        return (DateTime.Now - LastConnection).TotalSeconds > expirationInSeconds;
    }
}

public class SessionManager
{
    /// <summary>
    /// Track all sessions.
    /// </summary>
    protected Dictionary<IPAddress, Session> sessionMap = new Dictionary<IPAddress, Session>();

    // TODO: We need a way to remove very old sessions so that the server doesn't accumulate thousands of stale endpoints.

    public SessionManager()
    {
        sessionMap = new Dictionary<IPAddress, Session>();
    }

    /// <summary>
    /// Creates or returns the existing session for this remote endpoint.
    /// </summary>
    public Session GetSession(IPEndPoint remoteEndPoint)
    {
        // The port is always changing on the remote endpoint, so we can only use IP portion.
        Session session = sessionMap.CreateOrGet(remoteEndPoint.Address);

        return session;
    }
}