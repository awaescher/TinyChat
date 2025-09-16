namespace TinyChat;

/// <summary>
/// A text input control that allows users to type and send chat messages.
/// </summary>
public class ChatInputControl : Control, IChatInputControl
{
	/// <summary>
	/// Occurs before a message is sent from the text box.
	/// </summary>
	public event EventHandler<MessageSendingEventArgs>? MessageSending;

	private readonly TextBox _textBox;

	/// <summary>
	/// Initializes a new instance of the <see cref="ChatInputControl"/> class.
	/// </summary>
	public ChatInputControl()
	{
		_textBox = new TextBox { Multiline = true, Visible = true, Dock = DockStyle.Fill };
		var panel = new Panel { Padding = new Padding(8), Dock = DockStyle.Fill };
		Controls.Add(panel);
		panel.Controls.Add(_textBox);

		var size = new Size(24, 24);
		var sendChar = "\u27A4";
		var sendButton = new Button { Text = sendChar, MaximumSize = size, MinimumSize = size, Anchor = AnchorStyles.Bottom | AnchorStyles.Right };
		sendButton.Left = ClientRectangle.Width - sendButton.Width - panel.Padding.Right / 2 * 3;
		sendButton.Top = ClientRectangle.Height - sendButton.Height - panel.Padding.Bottom / 2 * 3;
		Controls.Add(sendButton);
		sendButton.BringToFront();
		sendButton.Click += (s, e) => Send();

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
			Send();
		}
	}

	private void Send()
	{
		var sendArgs = new MessageSendingEventArgs(null! /* we dont know the sender but the ChatControl does */, new StringMessageContent(_textBox.Text));
		MessageSending?.Invoke(this, sendArgs);

		if (!sendArgs.Cancel)
			_textBox.Clear();
	}

	/// <inheritdoc />
	protected override void OnGotFocus(EventArgs e)
	{
		base.OnGotFocus(e);
		_textBox.Focus();
	}
}
