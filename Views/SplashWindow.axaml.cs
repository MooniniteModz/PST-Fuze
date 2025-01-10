using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace PSTFuze.Views;

public partial class SplashWindow : Window
{
    public SplashWindow()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public async Task ShowSplashScreen()
    {
        Show();
        await Task.Delay(3000);
        Hide();
        Close();
    }
}