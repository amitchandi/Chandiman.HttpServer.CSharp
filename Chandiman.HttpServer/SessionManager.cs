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
    public Dictionary<string, object?> Objects { get; set; }

    public Session()
    {
        Objects = [];
        UpdateLastConnectionTime();
    }

    // Indexer for accessing session objects.  If an object isn't found, null is returned.
    public object? this[string objectKey]
    {
        get
        {
            Objects.TryGetValue(objectKey, out object? val);

            return val;
        }

        set { Objects[objectKey] = value; }
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

        if (!sessionMap.TryGetValue(remoteEndPoint.Address, out Session? session))
        {
            session = new Session();
            session.Objects[Server.ValidationTokenName] = Guid.NewGuid().ToString();
            sessionMap[remoteEndPoint.Address] = session;
        }

        return session;
    }
}