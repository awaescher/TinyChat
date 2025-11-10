using System.ComponentModel;
using TinyChat.Messages.Rendering;

namespace TinyChat;

/// <summary>
/// A panel control that displays a chat message with sender name and content.
/// </summary>
public class ChatMessageControl : Panel, IChatMessageControl
{
	private IChatMessage? _message;
	private bool _isReceivingStream;
	private readonly Label _senderLabel;
	private readonly Label _messageLabel;

	/// <summary>
	/// The event that is raised when the size of the control is updated while streaming a message.
	/// </summary>
	public event EventHandler? SizeUpdatedWhileStreaming;

	/// <summary>
	/// Gets or sets the renderer that converts message content into displayable strings.
	/// </summary>
	[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
	public required IMessageRenderer MessageRenderer { get; set; }

	/// <summary>
	/// Initializes a new instance of the <see cref="ChatMessageControl"/> class.
	/// </summary>
	public ChatMessageControl()
	{
		_senderLabel = new Label() { Dock = DockStyle.Top, AutoSize = true, Font = new Font(Font, FontStyle.Bold), UseMnemonic = false };
		_messageLabel = new Label() { Dock = DockStyle.Fill, AutoSize = true, UseMnemonic = false };
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
			{
				var binding = _messageLabel.DataBindings.Add(nameof(_messageLabel.Text), Message.Content, nameof(Message.Content.Content));
				binding.Format += (_, e) => e.Value = MessageRenderer.Render(new StringMessageContent(e.Value?.ToString() ?? string.Empty));
			}
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

	/// <inheritdoc />
	protected override void OnSizeChanged(EventArgs e)
	{
		base.OnSizeChanged(e);

		if (_isReceivingStream)
			SizeUpdatedWhileStreaming?.Invoke(this, EventArgs.Empty);
	}

	/// <inheritdoc />
	void IChatMessageControl.SetIsReceivingStream(bool isReceiving)
	{
		_isReceivingStream = isReceiving;
	}
}
