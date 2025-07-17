using System.ComponentModel;
using WinFormsChat.Messages;

namespace WinFormsChat.Controls;

/// <summary>
/// A panel control that displays a chat message with sender name and content.
/// </summary>
public class ChatMessageControl : Panel, IChatMessageControl
{
	private IChatMessage? _message;
	private Label _senderLabel;
	private Label _messageLabel;

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
	/// </summary>
	[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
	public IChatMessage? Message
	{
		get => _message;
		set
		{
			_message = value;
			_senderLabel.Text = Message?.Sender?.Name ?? string.Empty;
			_messageLabel.Text = Message?.Content?.Render() ?? string.Empty;
		}
	}
}
