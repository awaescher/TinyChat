namespace TinyChat;

/// <summary>
/// A text input control that allows users to type and send chat messages.
/// </summary>
public class ChatInputControl : Control, IChatInputControl
{
	const string SEND_CHAR = "\u27A4";
	const string STOP_CHAR = "\u25A0";

	/// <summary>
	/// Occurs before a message is sent from the text box.
	/// </summary>
	public event EventHandler<MessageSendingEventArgs>? MessageSending;

	/// <summary>
	/// The event that is raised when cancellation of a streaming message is requested.
	/// </summary>
	public event EventHandler? CancellationRequested;

	private readonly TextBox _textBox;
	private readonly Button _sendButton;
	private bool _isReceivingStream;

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
		_sendButton = new Button { Text = SEND_CHAR, MaximumSize = size, MinimumSize = size, Anchor = AnchorStyles.Bottom | AnchorStyles.Right };
		_sendButton.Left = ClientRectangle.Width - _sendButton.Width - panel.Padding.Right / 2 * 3;
		_sendButton.Top = ClientRectangle.Height - _sendButton.Height - panel.Padding.Bottom / 2 * 3;
		Controls.Add(_sendButton);
		_sendButton.BringToFront();

		_sendButton.Click += (s, e) => SendOrStop();
		_textBox.KeyPress += TextBox_KeyPress;
	}

	/// <inheritdoc />
	protected override void OnGotFocus(EventArgs e)
	{
		base.OnGotFocus(e);
		_textBox.Focus();
	}

	/// <summary>
	/// Handles the KeyPress event of the internal text box to send messages on Enter key.
	/// </summary>
	/// <param name="sender">The source of the event.</param>
	/// <param name="e">A <see cref="KeyPressEventArgs"/> that contains the event data.</param>
	private void TextBox_KeyPress(object? sender, KeyPressEventArgs e)
	{
		if (e.KeyChar == (char)Keys.Enter && !_isReceivingStream) // only send with Enter to prevent unwanted cancellation
		{
			e.Handled = true;
			Send();
		}
	}

	private void SendOrStop()
	{
		if (_isReceivingStream)
			Stop();
		else
			Send();
	}

	private void Send()
	{
		var sendArgs = new MessageSendingEventArgs(null! /* we dont know the sender but the ChatControl does */, new StringMessageContent(_textBox.Text));
		MessageSending?.Invoke(this, sendArgs);

		if (!sendArgs.Cancel)
			_textBox.Clear();
	}

	private void Stop()
	{
		if (_sendButton.Enabled)
			CancellationRequested?.Invoke(this, EventArgs.Empty);
	}

	/// <inheritdoc />
	void IChatInputControl.SetIsReceivingStream(bool isReceiving, bool allowCancellation)
	{
		_isReceivingStream = isReceiving;

		if (IsAvailable())
		{
			BeginInvoke(() =>
			{
				if (IsAvailable())
				{
					_sendButton.Text = isReceiving && allowCancellation ? STOP_CHAR : SEND_CHAR;
					_sendButton.Enabled = !isReceiving || allowCancellation;
				}
			});
		}
	}

	private bool IsAvailable() => !((this?.Disposing ?? false) || (this?.IsDisposed ?? false));
}
