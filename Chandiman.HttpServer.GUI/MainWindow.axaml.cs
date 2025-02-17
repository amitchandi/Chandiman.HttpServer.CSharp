using Avalonia.Controls;

namespace Chandiman.HttpServer.GUI;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    async void LoadWebsites()
    {
        using WebsiteContext ctx = new WebsiteContext();
        var websites = await ctx.GetWebsites();
        
    }
}