using System;
using System.Windows;

namespace skroy.NotesDesktop;

public partial class App : Application
{
	protected override void OnStartup(StartupEventArgs e)
	{
		base.OnStartup(e);

		AppDomain.CurrentDomain.UnhandledException += (_, e) => MessageBox.Show(((Exception)e.ExceptionObject).Message);
	}
}
