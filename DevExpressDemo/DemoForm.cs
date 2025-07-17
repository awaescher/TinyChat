using System;
using System.Windows.Forms;
using DevExpress.LookAndFeel;
using DevExpress.XtraBars.ToolbarForm;
using WinFormsDemo;

namespace DevExpressDemo;

public partial class DemoForm : ToolbarForm, IMessageFilter
{
	private const int WM_LBUTTONUP = 0x0202;

	public DemoForm()
	{
		InitializeComponent();
		KeyPreview = true;
	}

	protected override void OnLoad(EventArgs e)
	{
		base.OnLoad(e);

		Application.AddMessageFilter(this);

		UserLookAndFeel.Default.SetSkinStyle(SkinStyle.Office2010Blue);

		dxChatControl.DataContext = DemoData.Create(Environment.UserName);
		SelectControl(dxChatControl);
	}

	public bool PreFilterMessage(ref Message m)
	{
		if (m.Msg == WM_LBUTTONUP)
		{
			try
			{
				var control = Control.FromHandle(m.HWnd);
				var parent = control?.Parent;

				// only select controls that belong to the chat in the left side of the splitter
				var isOnLeftSplitter = false;
				while (parent != null)
				{
					if (parent == splitMain.Panel1)
						isOnLeftSplitter = true;

					parent = parent?.Parent;
				}

				if (isOnLeftSplitter)
					SelectControl(control);
			}
			catch
			{
			}
		}

		return false;
	}

	private void SelectControl(Control control)
	{
		propertyGridControl1.SelectedObject = control;
		labelControl1.Text = control?.GetType().Name ?? "";
	}
}
