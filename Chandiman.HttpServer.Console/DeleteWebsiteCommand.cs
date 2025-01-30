using System.CommandLine;

namespace Chandiman.HttpServer.Console;

public partial class Program
{
    public static void Add_DeleteWebsiteCommand(Command websiteCommand)
    {
        var deleteWebsiteCommand = new Command(
            name: "delete",
            description: "delete the website with the given id"
        );

        var idArg = new Argument<string>(
            name: "id",
            description: "id of the website."
        );
        deleteWebsiteCommand.Add(idArg);

        deleteWebsiteCommand.SetHandler(async (id) =>
        {
            try
            {
                using WebsiteContext websiteContext = new();
                var website = await websiteContext.GetWebsiteById(id);
                if (website is null)
                {
                    System.Console.WriteLine($"Webstite with id:{id} does not exist.");
                    return;
                }
                await websiteContext.DeleteWebsite(website);
                await websiteContext.SaveChangesAsync();
                System.Console.WriteLine("Website was successfully deleted.");
            }
            catch (Exception ex)
            {
                System.Console.WriteLine("Something went wrong.");
                System.Console.WriteLine(ex);
            }
        }, idArg);

        websiteCommand.Add(deleteWebsiteCommand);
    }
}
