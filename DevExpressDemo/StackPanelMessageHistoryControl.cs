using DevExpress.Utils.Layout;
using DevExpress.XtraEditors;
using System;
using System.Drawing;
using System.Windows.Forms;
using TinyChat;

namespace DevExpressDemo;

public class StackPanelMessageHistoryControl : XtraScrollableControl, IChatMessageHistoryControl
{
	private StackPanel _stackPanel = new();

	public StackPanelMessageHistoryControl()
	{
		AutoScroll = true;

		_stackPanel.LayoutDirection = DevExpress.Utils.Layout.StackPanelLayoutDirection.TopDown;
		_stackPanel.AutoSize = true;
		_stackPanel.Visible = true;
		_stackPanel.Dock = DockStyle.Top;
		Controls.Add(_stackPanel);
	}

	public void AppendMessage(IChatMessageControl messageControl)
	{
		var control = (Control)messageControl;
		_stackPanel.Controls.Add(control);
		SetSizeContraints(control);
		ScrollControlIntoView(control);
	}

	public void ClearMessageControls()
	{
		_stackPanel.Controls.Clear();
	}

	protected override void OnClientSizeChanged(EventArgs e)
	{
		base.OnClientSizeChanged(e);

		SuspendLayout();

		foreach (Control control in _stackPanel.Controls)
		{
			SetSizeContraints(control);
		}

		ResumeLayout();
	}

	private void SetSizeContraints(Control control)
	{
		control.MinimumSize = new Size(ClientRectangle.Width, 0);
		control.MaximumSize = new Size(ClientRectangle.Width, 0);
	}
}
