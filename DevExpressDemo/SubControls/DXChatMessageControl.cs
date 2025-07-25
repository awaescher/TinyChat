using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using DevExpress.XtraEditors;
using TinyChat;

namespace DevExpressDemo;

/// <summary>
/// A DevExpress-based chat message control that displays a chat message with sender and content.
/// Implements the IChatMessageControl interface and inherits from PanelControl.
/// </summary>
public class DXChatMessageControl : PanelControl, IChatMessageControl
{
	/// <summary>
	/// The chat message displayed by this control.
	/// </summary>
	private IChatMessage? _message;

	/// <summary>
	/// Label control that displays the sender's name.
	/// </summary>
	private readonly LabelControl _senderLabel;

	/// <summary>
	/// Label control that displays the message content.
	/// </summary>
	private readonly LabelControl _messageLabel;

	/// <summary>
	/// Initializes a new instance of the <see cref="DXChatMessageControl"/> class.
	/// Sets up the layout with sender and message labels, configures styling and sizing behavior.
	/// </summary>
	public DXChatMessageControl()
	{
		BorderStyle = DevExpress.XtraEditors.Controls.BorderStyles.NoBorder;
		AutoSize = true;

		_senderLabel = new LabelControl() { Dock = DockStyle.Top, AutoSizeMode = LabelAutoSizeMode.Vertical, Font = new Font(Font, FontStyle.Bold) };
		_messageLabel = new LabelControl() { Dock = DockStyle.Top, AutoSizeMode = LabelAutoSizeMode.Vertical };

		Controls.Add(_senderLabel);
		Controls.Add(_messageLabel);

		_messageLabel.BringToFront();

		AutoSize = true;
		Padding = new Padding(8);
	}

	/// <summary>
	/// Gets or sets the chat message displayed by this control.
	/// When set, updates the sender and content labels with the message data.
	/// </summary>
	/// <value>
	/// The <see cref="IChatMessage"/> instance to display, or null if no message is set.
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

	/// <summary>
	/// Gets or sets the maximum size of the control.
	/// When set, also updates the maximum size of the internal labels to account for padding.
	/// </summary>
	/// <value>
	/// The maximum <see cref="Size"/> that this control can occupy.
	/// </value>
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
