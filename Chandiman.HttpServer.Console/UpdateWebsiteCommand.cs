using System.CommandLine;

namespace Chandiman.HttpServer.Console;

public partial class Program
{
    public static void Add_UpdateWebsiteCommand(Command websiteCommand)
    {
        var updateWebsiteCommand = new Command(
            name: "update",
            description: "Update a website"
        );
        websiteCommand.Add(updateWebsiteCommand);

        var websiteIdArg = new Argument<string>(
            name: "websiteId",
            description: "unique identifier of the exisiting website."
        );
        updateWebsiteCommand.Add(websiteIdArg);

        var newWebsiteIdOption = new Option<string?>(
            name: "--id",
            description: "new unique identifier.",
            getDefaultValue: () => null
        );
        updateWebsiteCommand.Add(newWebsiteIdOption);

        var websitePathOption = new Option<string?>(
            name: "--wpath",
            description: "new physical to the website source directory.",
            getDefaultValue: () => null
        );
        updateWebsiteCommand.Add(websitePathOption);

        var pathOption = new Option<string?>(
            name: "--path",
            description: "new URL path.",
            getDefaultValue: () => null
        );
        updateWebsiteCommand.Add(pathOption);

        var portOption = new Option<int>(
            name: "--port",
            description: "new port.",
            getDefaultValue: () => 0
        );
        updateWebsiteCommand.Add(portOption);

        updateWebsiteCommand.SetHandler(async (websiteId, newId, websitePath, path, port) =>
        {
            using WebsiteContext websiteContext = new();
            var website = await websiteContext.GetWebsiteById(websiteId);

            if (website is null)
            {
                System.Console.WriteLine($"Webstite with id:{websiteId} does not exist.");
                return;
            }

            await websiteContext.DeleteWebsite(website);

            if (newId != null) website.WebsiteId = newId;
            if (websitePath != null) website.WebsitePath = websitePath;
            if (path != null) website.Path = path;
            if (port > 0) website.Port = port;

            await websiteContext.Websites.AddAsync(website);
            await websiteContext.SaveChangesAsync();
        },
        websiteIdArg,
        newWebsiteIdOption,
        websitePathOption,
        pathOption,
        portOption
        );
    }
}
