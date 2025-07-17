using DevExpress.XtraEditors;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using TinyChat;

namespace DevExpressDemo;

public class DXChatMessageControl : PanelControl, IChatMessageControl
{
	private IChatMessage? _message;
	private Label _senderLabel;
	private Label _messageLabel;

	/// <summary>
	/// Initializes a new instance of the <see cref="ChatMessageControl"/> class.
	/// </summary>
	public DXChatMessageControl()
	{
		BorderStyle = DevExpress.XtraEditors.Controls.BorderStyles.NoBorder;

		// TODO use DevExpress LabelControl
		//_senderLabel = new LabelControl() { Dock = DockStyle.Top, AutoSizeMode = LabelAutoSizeMode.Vertical, Font = new Font(Font, FontStyle.Bold) };
		//_messageLabel = new LabelControl() { Dock = DockStyle.Fill, AutoSizeMode = LabelAutoSizeMode.Default};

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
