using System.CommandLine;

namespace Chandiman.HttpServer.Console;

partial class Program
{
    static async Task<int> Main(string[] args)
    {
        var rootCommand = new RootCommand("Chandiman.HttpServer CommandLine app for configurations");

        var websiteCommand = new Command("website", "Configure websites.");
        rootCommand.Add(websiteCommand);

        AddCommands(websiteCommand);

        rootCommand.SetHandler((listWebsite) =>
        {
            System.Console.WriteLine("nothing");
        });

        return await rootCommand.InvokeAsync(args);
    }
}
