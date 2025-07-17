using System.Reflection;

namespace TinyChat;

public class TableLayoutMessageHistoryControl : TableLayoutPanel, IChatMessageHistoryControl
{
	// see https://stackoverflow.com/a/58812783/704281
	private MethodInfo? _hScrollbarHideFunction = typeof(ScrollableControl).GetMethod("SetVisibleScrollbars", BindingFlags.Instance | BindingFlags.NonPublic);

	public TableLayoutMessageHistoryControl()
	{
		AutoScroll = true;

	}

	public void AppendMessage(IChatMessageControl messageControl)
	{
		var control = (Control)messageControl;
		Controls.Add(control);
		ScrollControlIntoView(control);
	}

	public void ClearMessageControls()
	{
		Controls.Clear();
	}

	protected override void OnClientSizeChanged(EventArgs e)
	{
		base.OnClientSizeChanged(e);

		foreach (Control control in Controls)
			control.MaximumSize = new Size(ClientRectangle.Width, 0);
	}

	protected override void OnResize(EventArgs eventargs)
	{
		base.OnResize(eventargs);
		_hScrollbarHideFunction?.Invoke(this, [false, VerticalScroll.Visible]);
	}

}
