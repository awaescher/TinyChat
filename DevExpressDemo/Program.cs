using System;
using System.Windows.Forms;

namespace DevExpressDemo;

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

		var winFormsDemo = new WinFormsDemo.DemoForm();
		winFormsDemo.Show();

		Application.Run(new DemoForm());
	}
}
