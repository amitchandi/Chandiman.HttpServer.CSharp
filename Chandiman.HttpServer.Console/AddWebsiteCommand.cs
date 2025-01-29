using System.CommandLine;

namespace Chandiman.HttpServer.Console;

public partial class Program
{
    public static void Add_AddWebsiteCommand(Command websiteCommand)
    {
        var addWebsiteCommand = new Command(
            name: "add",
            description: "Add a website"
        );

        websiteCommand.Add(addWebsiteCommand);
        var websiteIdArg = new Argument<string>(
            name: "id",
            description: "Unique identifier of the website. (Name)"
        );
        var websitePathArg = new Argument<string>(
            name: "wpath",
            description: "Physical path to website directory."
        );
        var pathArg = new Argument<string>(
            name: "path",
            description: "URL Path to website.",
            getDefaultValue: () => ""
        );
        var portArg = new Argument<int>(
            name: "port",
            description: "Port of the website.",
            getDefaultValue: () => 3000
        );
        addWebsiteCommand.Add(websiteIdArg);
        addWebsiteCommand.Add(websitePathArg);
        addWebsiteCommand.Add(pathArg);
        addWebsiteCommand.Add(portArg);

        addWebsiteCommand.SetHandler(async (id, wpath, path, port) =>
        {
            try
            {
                using WebsiteContext ctx = new();
                await ctx.AddAsync(new Website()
                {
                    WebsiteId = id,
                    WebsitePath = wpath,
                    Path = path,
                    Port = port
                });
                await ctx.SaveChangesAsync();
                System.Console.WriteLine("Website successfully inserted.");
            }
            catch (Exception ex)
            {
                System.Console.WriteLine("Something went wrong");
                System.Console.WriteLine(ex.ToString());
            }

        }, websiteIdArg, websitePathArg, pathArg, portArg);
    }
}
