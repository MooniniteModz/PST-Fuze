using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using PSTFuze.ViewModels;
using PSTFuze.Views;
using System.Threading.Tasks;
using Microsoft.Win32;
using System;


namespace PSTFuze;


public partial class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
        SetupRedemptionEula();
    }

    private void SetupRedemptionEula()
    {
        if (OperatingSystem.IsWindows())
        {
            try
            {
                // Try to write to HKEY_CURRENT_USER
                System.Diagnostics.Debug.AutoFlush = true;
                System.Diagnostics.Trace.AutoFlush = true;
                string regPath = @"Software\Dimastr\Redemption";
                using (var key = Registry.CurrentUser.CreateSubKey(regPath))
                {
                    if (key != null)
                    {
                        key.SetValue("AcceptEULA", "1");
                        key.SetValue("EULAAccepted", 1);
                        key.SetValue("EULAVersion", 1);
                        key.SetValue("DisableEULADialog", 1);
                    }
                }

                // Also try HKEY_LOCAL_MACHINE
                using (var key = Registry.LocalMachine.CreateSubKey(regPath))
                {
                    if (key != null)
                    {
                        key.SetValue("AcceptEULA", "1");
                        key.SetValue("EULAAccepted", 1);
                        key.SetValue("EULAVersion", 1);
                        key.SetValue("DisableEULADialog", 1);
                    }
                }
            }
            catch
            {
                // Ignore registry errors
            }
        }
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            // Start with splash screen only
            var splashScreen = new SplashWindow();
            desktop.MainWindow = splashScreen;

            async void StartupSequence()
            {
                await Task.Delay(3000);  // Show splash for 3 seconds

                // Create and show main window
                var mainWindow = new MainWindow
                {
                    DataContext = new MainWindowViewModel()
                };

                // Switch to main window
                desktop.MainWindow = mainWindow;
                mainWindow.Show();

                // Close splash screen
                splashScreen.Close();
            }

            splashScreen.Show();
            StartupSequence();
        }

        base.OnFrameworkInitializationCompleted();
    }
}