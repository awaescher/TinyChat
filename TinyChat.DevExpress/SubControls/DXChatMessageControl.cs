using System.ComponentModel;
using DevExpress.XtraEditors;
using TinyChat.Messages.Formatting;

namespace TinyChat;

/// <summary>
/// A DevExpress-based chat message control that displays a chat message with sender and content.
/// Implements the IChatMessageControl interface and inherits from PanelControl.
/// </summary>
public class DXChatMessageControl : PanelControl, IChatMessageControl
{
	private IChatMessage? _message;
	private bool _isReceivingStream;
	private readonly LabelControl _senderLabel;
	private readonly LabelControl _messageLabel;
	private FileAttachmentPreviewPanel? _attachmentPreviewPanel;
	private IMessageFormatter _messageFormatter = new PlainTextMessageFormatter();

	/// <inheritdoc/>
	public event EventHandler? SizeUpdatedWhileStreaming;

	/// <inheritdoc/>
	public event EventHandler? BeforeLayoutChange;

	/// <inheritdoc/>
	public event EventHandler? AfterLayoutChange;

	/// <summary>
	/// Gets or sets the formatter that converts message content into displayable strings.
	/// </summary>
	[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
	public required IMessageFormatter MessageFormatter
	{
		get => _messageFormatter;
		set
		{
			_messageFormatter = value ?? throw new ArgumentNullException(nameof(value));
			UpdateMessageDisplay();
		}
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="DXChatMessageControl"/> class.
	/// Sets up the layout with sender and message labels, configures styling and sizing behavior.
	/// </summary>
	public DXChatMessageControl()
	{
		BorderStyle = DevExpress.XtraEditors.Controls.BorderStyles.NoBorder;
		AutoSize = true;

		_senderLabel = new LabelControl() { AllowHtmlString = true, Dock = DockStyle.Top, AutoSizeMode = LabelAutoSizeMode.Vertical, Font = new Font(Font, FontStyle.Bold), UseMnemonic = false, Padding = new Padding(0, 0, 0, 3) };
		_messageLabel = new LabelControl() { AllowHtmlString = true, Dock = DockStyle.Top, AutoSizeMode = LabelAutoSizeMode.Vertical, UseMnemonic = false };

		Controls.Add(_senderLabel);
		Controls.Add(_messageLabel);

		_messageLabel.BringToFront();

		AutoSize = true;
		Padding = new Padding(8);

		WireMouseDown(_senderLabel, _messageLabel);
	}

	private void WireMouseDown(params Control[] controls)
	{
		foreach (var c in controls)
			c.MouseDown += (_, e) => OnMouseDown(e);
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
			UpdateMessageDisplay();
			UpdateAttachmentPreview();
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
			var contentWidth = value.Width == 0 ? 0 : Math.Max(0, value.Width - Padding.Horizontal);
			_senderLabel.MaximumSize = new Size(contentWidth, 0);
			_messageLabel.MaximumSize = new Size(contentWidth, 0);
			if (_attachmentPreviewPanel is not null)
				_attachmentPreviewPanel.MaximumSize = new Size(contentWidth, 0);
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

	/// <inheritdoc />
	void IChatMessageControl.ShowSenderHeader(bool show)
	{
		_senderLabel.Visible = show;
	}

	/// <inheritdoc/>
	public override string ToString() => _messageLabel.Text;

	private void UpdateMessageDisplay()
	{
		_senderLabel.Text = Message?.Sender?.Name ?? string.Empty;

		_messageLabel.DataBindings.Clear();
		_messageLabel.Text = string.Empty;
		_messageLabel.Visible = Message is not null;
		if (Message is null)
			return;

		if (Message.Content is FileAttachmentMessageContent attachmentContent)
		{
			_messageLabel.Text = MessageFormatter.Format(new StringMessageContent(attachmentContent.Text));
			_messageLabel.Visible = !string.IsNullOrWhiteSpace(_messageLabel.Text);
		}
		else
		{
			var binding = _messageLabel.DataBindings.Add(nameof(_messageLabel.Text), Message.Content, nameof(Message.Content.Content));
			binding.Format += (_, e) => e.Value = MessageFormatter.Format(e.Value?.ToString() ?? string.Empty);
		}
	}

	private void UpdateAttachmentPreview()
	{
		_attachmentPreviewPanel?.Dispose();
		_attachmentPreviewPanel = null;

		if (Message?.Content is not FileAttachmentMessageContent attachmentContent)
			return;

		var previewPanel = new FileAttachmentPreviewPanel(attachmentContent) { Dock = DockStyle.Top };
		if (!previewPanel.HasPreviews)
		{
			previewPanel.Dispose();
			return;
		}

		var contentWidth = MaximumSize.Width == 0 ? 0 : Math.Max(0, MaximumSize.Width - Padding.Horizontal);
		previewPanel.MaximumSize = new Size(contentWidth, 0);
		_attachmentPreviewPanel = previewPanel;
		Controls.Add(previewPanel);
		previewPanel.BringToFront();
	}
}
