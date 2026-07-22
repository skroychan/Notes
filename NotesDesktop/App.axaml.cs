using System;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using MsBox.Avalonia;
using MsBox.Avalonia.Enums;
using skroy.NotesDesktop.Window;

namespace skroy.NotesDesktop;

public class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = new MainWindow();
        }

        AppDomain.CurrentDomain.UnhandledException += (_, e) => ShowErrorMessageBox(((Exception)e.ExceptionObject).Message);

        base.OnFrameworkInitializationCompleted();
    }

    private void ShowErrorMessageBox(string message)
    {
        var messageBox = MessageBoxManager.GetMessageBoxStandard("Fatal error", message, ButtonEnum.Ok, Icon.Error);
        messageBox.ShowAsync();
    }
}
