using System.CommandLine;

namespace Chandiman.HttpServer.ConsoleApp;

public partial class Program
{
    public static void Add_ListWebsiteCommand(Command websiteCommand)
    {
        var listWebsiteCommand = new Command(
            name: "list",
            description: "List the configured websites"
        );
        websiteCommand.Add(listWebsiteCommand);

        listWebsiteCommand.SetHandler(async () =>
        {
            using WebsiteContext websiteContext = new();
            var list = await websiteContext.GetWebsites();
            foreach (var website in list)
                Console.WriteLine(website + "\n");
        });
    }
}
