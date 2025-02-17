using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Chandiman.HttpServer.GUI.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    public string Greeting { get; } = "Welcome to Avalonia!";

    public ObservableCollection<Website> Websites { get; }
    
    public ObservableCollection<Guy> dsa { get; }
    
    public MainWindowViewModel()
    {
        using WebsiteContext ctx = new WebsiteContext();
        var asd = ctx.GetWebsites().Result;
        Websites = new ObservableCollection<Website>(asd);

        List<Guy> guys = new List<Guy>()
        {
            new Guy(){ Name = "" }
        };

        dsa = new ObservableCollection<Guy>(guys);
    }

    public class Guy
    {
        public string Name { get; set; }
    }
}