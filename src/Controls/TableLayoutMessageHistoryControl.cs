namespace WinFormsChat.Controls;

public class TableLayoutMessageHistoryControl : TableLayoutPanel, IChatMessageHistoryControl
{
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
}
