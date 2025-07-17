namespace WinFormsDemo;

public partial class DemoForm : Form
{
	public DemoForm()
	{
		InitializeComponent();
	}

	protected override void OnLoad(EventArgs e)
	{
		base.OnLoad(e);

		chatControl1.DataContext = DemoData.Create(Environment.UserName);
	}
}

