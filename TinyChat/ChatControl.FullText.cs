using System.ComponentModel;

namespace TinyChat;

public partial class ChatControl
{
	private int _maximumDetailTextLength = DetailTextTruncation.DEFAULT_MAXIMUM_LENGTH;
	private int _maximumDetailTextLines = DetailTextTruncation.DEFAULT_MAXIMUM_LINES;

	/// <summary>
	/// Gets or sets the maximum number of characters of reasoning, tool arguments and tool results shown in the conversation.
	/// Longer texts are shortened and offer a button that shows the complete text. Zero disables the limit.
	/// </summary>
	[Category("Chat Appearance")]
	[Description("Gets or sets the maximum number of characters of reasoning and tool results shown in the conversation. Zero disables the limit.")]
	[DefaultValue(DetailTextTruncation.DEFAULT_MAXIMUM_LENGTH)]
	public int MaximumDetailTextLength
	{
		get => _maximumDetailTextLength;
		set
		{
			ArgumentOutOfRangeException.ThrowIfNegative(value);
			_maximumDetailTextLength = value;
			foreach (var control in _messageControls.OfType<IChatMessageTruncationControl>())
				control.MaximumDetailTextLength = value;
		}
	}

	/// <summary>
	/// Gets or sets the maximum number of lines of reasoning, tool arguments and tool results shown in the conversation.
	/// Longer texts are shortened and offer a button that shows the complete text. Zero disables the limit.
	/// </summary>
	[Category("Chat Appearance")]
	[Description("Gets or sets the maximum number of lines of reasoning and tool results shown in the conversation. Zero disables the limit.")]
	[DefaultValue(DetailTextTruncation.DEFAULT_MAXIMUM_LINES)]
	public int MaximumDetailTextLines
	{
		get => _maximumDetailTextLines;
		set
		{
			ArgumentOutOfRangeException.ThrowIfNegative(value);
			_maximumDetailTextLines = value;
			foreach (var control in _messageControls.OfType<IChatMessageTruncationControl>())
				control.MaximumDetailTextLines = value;
		}
	}

	/// <summary>
	/// Creates the dialog that shows the complete, unshortened text of reasoning or a tool call.
	/// Override to use another control suite.
	/// </summary>
	/// <param name="title">The dialog title.</param>
	/// <param name="text">The complete text.</param>
	/// <returns>The dialog to show modally.</returns>
	protected virtual Form CreateFullTextDialog(string title, string text) => new FullTextDialog(title, text, MessageFont);

	/// <summary>
	/// Shows the complete, unshortened text of reasoning or a tool call in a dialog.
	/// </summary>
	/// <param name="title">The dialog title.</param>
	/// <param name="text">The complete text.</param>
	protected virtual void ShowFullText(string title, string text)
	{
		using var dialog = CreateFullTextDialog(title, text);
		dialog.ShowDialog(FindForm());
	}

	internal void OpenFullText(string title, string text)
	{
		try
		{
			ShowFullText(title, text);
		}
		catch (Exception ex)
		{
			try
			{
				ErrorOccurred?.Invoke(this, new Helper.ChatErrorEventArgs(ex));
			}
			catch
			{
				// A failing error handler must not escape the button click.
			}
		}
	}

	internal static ChatControl? FindOwner(Control control)
	{
		for (var parent = control.Parent; parent is not null; parent = parent.Parent)
		{
			if (parent is ChatControl owner)
				return owner;
		}

		return null;
	}

	private void ApplyDetailTextLimits(IChatMessageControl messageControl)
	{
		if (messageControl is not IChatMessageTruncationControl truncationControl)
			return;

		truncationControl.MaximumDetailTextLength = MaximumDetailTextLength;
		truncationControl.MaximumDetailTextLines = MaximumDetailTextLines;
	}
}
