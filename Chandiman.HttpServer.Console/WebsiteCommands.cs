using System.CommandLine;

namespace Chandiman.HttpServer.Console;

public partial class Program
{
    public static void AddCommands(Command websiteCommand)
    {
        Add_AddWebsiteCommand(websiteCommand);
        Add_ListWebsiteCommand(websiteCommand);
        Add_DeleteWebsiteCommand(websiteCommand);
    }
}
