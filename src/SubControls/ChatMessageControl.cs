using System.ComponentModel;

namespace TinyChat;

/// <summary>
/// A panel control that displays a chat message with sender name and content.
/// </summary>
public class ChatMessageControl : Panel, IChatMessageControl
{
	private IChatMessage? _message;
	private readonly Label _senderLabel;
	private readonly Label _messageLabel;

	/// <summary>
	/// Initializes a new instance of the <see cref="ChatMessageControl"/> class.
	/// </summary>
	public ChatMessageControl()
	{
		_senderLabel = new Label() { Dock = DockStyle.Top, AutoSize = true, Font = new Font(Font, FontStyle.Bold) };
		_messageLabel = new Label() { Dock = DockStyle.Fill, AutoSize = true };
		Controls.Add(_senderLabel);
		Controls.Add(_messageLabel);

		_messageLabel.BringToFront();

		AutoSize = true;
		Padding = new Padding(8);
	}

	/// <summary>
	/// Gets or sets the chat message displayed by this control.
	/// When set, the control updates to display the sender's name and rendered message content.
	/// If the message is null, both the sender and content labels will display empty strings.
	/// </summary>
	/// <value>
	/// The <see cref="IChatMessage"/> instance to display, or null to clear the display.
	/// </value>
	[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
	public IChatMessage? Message
	{
		get => _message;
		set
		{
			_message = value;
			_senderLabel.Text = Message?.Sender?.Name ?? string.Empty;

			_messageLabel.DataBindings.Clear();
			if (Message is not null)
				_messageLabel.DataBindings.Add(nameof(_messageLabel.Text), Message.Content, nameof(Message.Content.Content));
		}
	}

	/// <inheritdoc />
	[DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
	public override Size MaximumSize
	{
		get => base.MaximumSize;
		set
		{
			base.MaximumSize = value;
			_senderLabel.MaximumSize = new Size(value.Width - Padding.Horizontal, 0);
			_messageLabel.MaximumSize = new Size(value.Width - Padding.Horizontal, 0);
		}
	}
}
