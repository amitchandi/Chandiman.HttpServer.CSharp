using Microsoft.EntityFrameworkCore;

namespace Chandiman.HttpServer;

public class WebsiteContext : DbContext
{
    public DbSet<Website> Websites { get; set; }

    public string DbPath { get; }

    public WebsiteContext()
    {
        var folder = Environment.SpecialFolder.LocalApplicationData;
        var path = Environment.GetFolderPath(folder);
        DbPath = Path.Join(path, "website.db");
    }
}

public class Website
{
    public required string WebsiteName { get; set; }
    public required string WebsitePath { get; set; }
    public required string Path { get; set; }
    public int Port { get; set; }
}