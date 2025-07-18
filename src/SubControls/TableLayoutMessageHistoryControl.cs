using System.Reflection;

namespace TinyChat;

/// <summary>
/// A table layout panel control that displays a scrollable history of chat messages.
/// Implements <see cref="IChatMessageHistoryControl"/> to provide message management functionality.
/// </summary>
public class TableLayoutMessageHistoryControl : TableLayoutPanel, IChatMessageHistoryControl
{
	/// <summary>
	/// Reflection method info used to hide the horizontal scrollbar while keeping the vertical scrollbar visible.
	/// See https://stackoverflow.com/a/58812783/704281 for implementation details.
	/// </summary>
	private readonly MethodInfo? _hScrollbarHideFunction = typeof(ScrollableControl).GetMethod("SetVisibleScrollbars", BindingFlags.Instance | BindingFlags.NonPublic);

	/// <summary>
	/// Initializes a new instance of the <see cref="TableLayoutMessageHistoryControl"/> class.
	/// Enables auto-scrolling to allow users to scroll through message history.
	/// </summary>
	public TableLayoutMessageHistoryControl()
	{
		AutoScroll = true;

	}

	/// <summary>
	/// Appends a new message control to the message history and scrolls it into view.
	/// </summary>
	/// <param name="messageControl">The chat message control to add to the history.</param>
	public void AppendMessageControl(IChatMessageControl messageControl)
	{
		var control = (Control)messageControl;
		Controls.Add(control);
		ScrollControlIntoView(control);
	}

	/// <summary>
	/// Removes all message controls from the history, clearing the display.
	/// </summary>
	public void ClearMessageControls()
	{
		Controls.Clear();
	}

	/// <summary>
	/// Handles client size changes by updating the maximum width of all child controls
	/// to match the new client area width, ensuring proper text wrapping.
	/// </summary>
	/// <param name="e">Event arguments containing size change information.</param>
	protected override void OnClientSizeChanged(EventArgs e)
	{
		base.OnClientSizeChanged(e);

		foreach (Control control in Controls)
			control.MaximumSize = new Size(ClientRectangle.Width, 0);
	}

	/// <summary>
	/// Handles resize events by hiding the horizontal scrollbar while maintaining
	/// the visibility state of the vertical scrollbar.
	/// </summary>
	/// <param name="eventargs">Event arguments containing resize information.</param>
	protected override void OnResize(EventArgs eventargs)
	{
		base.OnResize(eventargs);
		_hScrollbarHideFunction?.Invoke(this, [false, VerticalScroll.Visible]);
	}

}
