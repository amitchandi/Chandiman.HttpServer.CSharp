using Chandiman.Extensions;

namespace Chandiman.HttpServer;
/// <summary>
/// The base class for route handlers.
/// </summary>
public abstract class RouteHandler
{
    protected Server server;
    protected Func<Session, Dictionary<string, string>, ResponsePacket> handler;

    public RouteHandler(Server server, Func<Session, Dictionary<string, string>, ResponsePacket> handler)
    {
        this.handler = handler;
        this.server = server;
    }

    public abstract ResponsePacket Handle(Session session, Dictionary<string, string> parms);
}

/// <summary>
/// Page is always visible.
/// </summary>
public class AnonymousRouteHandler : RouteHandler
{
    public AnonymousRouteHandler(Server server, Func<Session, Dictionary<string, string>, ResponsePacket> handler)
        : base(server, handler)
    {
    }

    public override ResponsePacket Handle(Session session, Dictionary<string, string> parms)
    {
        return handler(session, parms);
    }
}

/// <summary>
/// Page is visible only to authorized users.
/// </summary>
public class AuthenticatedRouteHandler : RouteHandler
{
    public AuthenticatedRouteHandler(Server server, Func<Session, Dictionary<string, string>, ResponsePacket> handler)
        : base(server, handler)
    {
    }

    public override ResponsePacket Handle(Session session, Dictionary<string, string> parms)
    {
        ResponsePacket ret;

        if (session.Authenticated)
        {
            ret = handler(session, parms);
        }
        else
        {
            ret = server.OnError.IfNotNullReturn((OnError)
                => server.Redirect(OnError!(Server.ServerError.NotAuthorized)));
        }

        return ret;
    }
}

/// <summary>
/// Page is visible only to authorized users whose session has not expired.
/// </summary>
public class AuthenticatedExpirableRouteHandler : AuthenticatedRouteHandler
{
    public AuthenticatedExpirableRouteHandler(Server server, Func<Session, Dictionary<string, string>, ResponsePacket> handler)
        : base(server, handler)
    {
    }

    public override ResponsePacket Handle(Session session, Dictionary<string, string> parms)
    {
        ResponsePacket ret;

        if (session.IsExpired(server.expirationTimeSeconds))
        {
            session.Authenticated = false;
            
            ret = server.OnError.IfNotNullReturn((OnError)
                => server.Redirect(OnError!(Server.ServerError.ExpiredSession)));
        }
        else
        {
            ret = base.Handle(session, parms);
        }

        return ret;
    }
}
