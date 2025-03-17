using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Chandiman.HttpServer;
public class ServerException
{

}

public class InvalidWebsiteConfigException : Exception
{
    public InvalidWebsiteConfigException()
    {
    }

    public InvalidWebsiteConfigException(string message)
        : base(message)
    {
    }

    public InvalidWebsiteConfigException(string message, Exception inner)
        : base(message, inner)
    {
    }
}
