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

        // not sure what to do about Websites being possibly null
        if (Websites == null)
            throw new Exception();
    }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        => optionsBuilder.UseSqlite($"Data Source={DbPath}");

    public async Task<List<Website>> GetWebsites()
        => await Websites.ToListAsync();

    public async Task<Website> GetWebsiteByPath(string path)
        => await Websites
            .Where(website => website.Path == path)
            .FirstAsync();

    public async Task<Website> GetWebsiteById(string id)
        => await Websites
            .Where(website => website.WebsiteId == id)
            .FirstAsync();

    public async Task AddWebsite(Website website)
    {
        await Websites.AddAsync(website);
        await SaveChangesAsync();
    }

    public async Task DeleteWebsite(Website website)
    {
        Websites.Remove(website);
        await SaveChangesAsync();
    }
}

public class Website
{
    public required string WebsiteId { get; set; }
    public required string WebsitePath { get; set; }
    public required string Path { get; set; }
    public int Port { get; set; }

    public override string ToString()
    {
        return "WebsiteId: " + WebsiteId + "\nWebsitePath: " + WebsitePath
        + "\nPath: " + Path + "\nPort: " + Port;
    }
}
