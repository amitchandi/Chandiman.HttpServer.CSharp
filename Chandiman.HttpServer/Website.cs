using Microsoft.EntityFrameworkCore;

namespace Chandiman.HttpServer;

public class WebsiteContext : DbContext
{
    public DbSet<Website> Websites { get; set; }

    private string DbPath { get; }

    //TODO: figure out way to create db file in the location of the binaries
    public WebsiteContext()
    {
        var folder = Environment.SpecialFolder.LocalApplicationData;
        var path = Environment.GetFolderPath(folder);
        DbPath = Path.Join(path, "website.db");
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        => optionsBuilder.UseSqlite($"Data Source={DbPath}");
}

public class Website
{
    public required string WebsiteId { get; set; }
    public required string WebsitePath { get; set; }
    public required string Path { get; set; }
    public int Port { get; set; }
}