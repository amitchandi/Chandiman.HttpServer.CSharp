using System.CommandLine;

namespace Chandiman.HttpServer.ConsoleApp;

public partial class Program
{
    public static void Add_AddWebsiteCommand(Command websiteCommand)
    {
        var addWebsiteCommand = new Command(
            name: "add",
            description: "Add a website"
        );

        websiteCommand.Add(addWebsiteCommand);
        var websiteIdOption = new Option<string>(
            name: "--id",
            description: "Unique identifier of the website. (Name)"
        );
        var locationOption = new Option<string>(
            name: "--location",
            description: "Physical path to website directory."
        );
        locationOption.AddAlias("--l");
        var pathOption = new Option<string>(
            name: "--path",
            description: "URL Path to website. Set to '/' to access this website from the base path (ex. http://domain:port/)."
        );
        var portOption = new Option<int>(
            name: "--port",
            description: "Port of the website.",
            getDefaultValue: () => 3000
        );
        addWebsiteCommand.Add(websiteIdOption);
        addWebsiteCommand.Add(locationOption);
        addWebsiteCommand.Add(pathOption);
        addWebsiteCommand.Add(portOption);

        addWebsiteCommand.SetHandler(async (id, location, path, port) =>
        {
            if (path == "/") path = "";
            try
            {
                using WebsiteContext ctx = new();
                await ctx.AddAsync(new Website()
                {
                    Id = id,
                    Location = location,
                    Path = path,
                    Port = port
                });
                await ctx.SaveChangesAsync();
                Console.WriteLine("Website successfully inserted.");
            }
            catch (Exception ex)
            {
                Console.WriteLine("Something went wrong");
                Console.WriteLine(ex.ToString());
            }

        }, websiteIdOption, locationOption, pathOption, portOption);
    }
}
