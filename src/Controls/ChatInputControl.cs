using WinFormsChat.Messages;

namespace WinFormsChat.Controls;

/// <summary>
/// A text input control that allows users to type and send chat messages.
/// </summary>
public class ChatInputControl : Control, IChatInputControl
{
	/// <summary>
	/// Occurs when a message is sent from the text box.
	/// </summary>
	public event EventHandler<IChatMessageContent> Send;

	private TextBox _textBox;

	/// <summary>
	/// Initializes a new instance of the <see cref="ChatInputControl"/> class.
	/// </summary>
	public ChatInputControl()
	{
		_textBox = new TextBox { Multiline = true, Visible = true, Dock = DockStyle.Fill };
		var panel = new Panel { Padding = new Padding(8), Dock = DockStyle.Fill };
		Controls.Add(panel);
		panel.Controls.Add(_textBox);

		MinimumSize = new Size(0, 100);

		_textBox.KeyPress += TextBox_KeyPress;
	}

	/// <summary>
	/// Handles the KeyPress event of the internal text box to send messages on Enter key.
	/// </summary>
	/// <param name="sender">The source of the event.</param>
	/// <param name="e">A <see cref="KeyPressEventArgs"/> that contains the event data.</param>
	private void TextBox_KeyPress(object? sender, KeyPressEventArgs e)
	{
		if (e.KeyChar == (char)Keys.Enter)
		{
			e.Handled = true;
			Send?.Invoke(this, new StringMessageContent(_textBox.Text));
			_textBox.Clear();
		}
	}
}
