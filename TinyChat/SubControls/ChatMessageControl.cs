using System.ComponentModel;
using TinyChat.Messages.Formatting;

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
	/// Initializes a new instance of the <see cref="ChatMessageControl"/> class.
	/// </summary>
	public ChatMessageControl()
	{
		_senderLabel = new Label() { Dock = DockStyle.Top, AutoSize = true, Font = new Font(Font, FontStyle.Bold), UseMnemonic = false, Padding = new Padding(0, 0, 0, 3) };
		_messageLabel = new Label() { Dock = DockStyle.Top, AutoSize = true, UseMnemonic = false };
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
	/// When set, the control updates to display the sender's name and message content.
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
			UpdateMessageDisplay();
			UpdateAttachmentPreview();
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
			binding.Format += (_, e) => e.Value = MessageFormatter.Format(new StringMessageContent(e.Value?.ToString() ?? string.Empty));
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
