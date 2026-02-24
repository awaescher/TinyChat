using System;
using System.Windows.Forms;

namespace DemoApp;

internal static class Program
{
	/// <summary>
	/// The main entry point for the application.
	/// </summary>
	[STAThread]
	static void Main()
	{
		Application.EnableVisualStyles();
		Application.SetCompatibleTextRenderingDefault(false);

		new NativeDemoForm().Show();
		new NativeOllamaDemoForm().Show();
		new DXDemoForm().Show();

		Application.Run(new DXOllamaDemoForm());
	}
}
