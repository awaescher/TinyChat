namespace WinFormsDemo;

internal static class Program
{
	/// <summary>
	///  The main entry point for the application.
	/// </summary>
	[STAThread]
	static void Main(string[] args)
	{
		// To customize application configuration such as set high DPI settings or default font,
		// see https://aka.ms/applicationconfiguration.
		ApplicationConfiguration.Initialize();
		
		// Check if user wants to run the IChatClient demo
		if (args.Length > 0 && args[0] == "--ichatclient")
		{
			Application.Run(new IChatClientDemoForm());
		}
		else
		{
			Application.Run(new DemoForm());
		}
	}
}