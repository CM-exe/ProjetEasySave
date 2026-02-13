using EasyRemote;
using EasyRemote.Model;
using EasyRemote.Views;
using System.Data;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Windows;

namespace EasyRemote;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : System.Windows.Application
{
    private readonly IViewModel _ViewModel;

    public App()
    {
        this._ViewModel = new ViewModel();
    }
    private void ApplicationStartup(object sender, StartupEventArgs e)
    {
        HostWindow hostWindow = new HostWindow(this._ViewModel);
        hostWindow.Show();
    }
}
