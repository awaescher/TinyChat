using System.ComponentModel;
using TinyChat.Messages.Formatting;

namespace TinyChat;

/// <summary>
/// A panel control that displays a chat message with sender name and content.
/// </summary>
public class ChatMessageControl : Panel, IChatMessageControl, IChatMessageLayoutControl
{
	private IChatMessage? _message;
	private bool _isReceivingStream;
	private readonly Label _senderLabel;
	private readonly SelectableMessageTextControl _messageTextControl;
	private FileAttachmentPreviewPanel? _attachmentPreviewPanel;
	private IMessageFormatter _messageFormatter = new PlainTextMessageFormatter();
	private Font? _senderFont;

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
		_senderLabel = new Label() { Dock = DockStyle.Top, AutoSize = true, UseMnemonic = false, Padding = new Padding(0, 0, 0, 3) };
		_messageTextControl = new SelectableMessageTextControl { Dock = DockStyle.Top };
		Controls.Add(_senderLabel);
		Controls.Add(_messageTextControl);

		_messageTextControl.BringToFront();

		AutoSize = true;
		Padding = new Padding(8);

		ApplyAppearance();
		WireMouseDown(_senderLabel);
	}

	/// <inheritdoc />
	[Browsable(false)]
	[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
	public Padding ContentPadding
	{
		get => Padding;
		set => Padding = value;
	}

	private void WireMouseDown(params Control[] controls)
	{
		foreach (var c in controls)
			c.MouseDown += (_, e) => OnMouseDown(e);
	}

	/// <summary>
	/// Gets or sets the chat message displayed by this control.
	/// When set, the control updates to display the sender's name and message content.
	/// If the message is null, the sender header and selectable content editor are cleared.
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
			if (_message?.Content is { } previousContent)
				previousContent.PropertyChanged -= MessageContent_PropertyChanged;

			_message = value;
			if (_message?.Content is { } currentContent)
				currentContent.PropertyChanged += MessageContent_PropertyChanged;

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
			UpdateContentWidthConstraints();
		}
	}

	/// <inheritdoc />
	protected override void OnBackColorChanged(EventArgs e)
	{
		base.OnBackColorChanged(e);
		ApplyColors();
	}

	/// <inheritdoc />
	protected override void OnFontChanged(EventArgs e)
	{
		base.OnFontChanged(e);
		ApplyFont();
	}

	/// <inheritdoc />
	protected override void OnForeColorChanged(EventArgs e)
	{
		base.OnForeColorChanged(e);
		ApplyColors();
	}

	/// <inheritdoc />
	protected override void OnParentBackColorChanged(EventArgs e)
	{
		base.OnParentBackColorChanged(e);
		ApplyColors();
	}

	/// <inheritdoc />
	protected override void OnPaddingChanged(EventArgs e)
	{
		BeforeLayoutChange?.Invoke(this, EventArgs.Empty);
		try
		{
			base.OnPaddingChanged(e);
			UpdateContentWidthConstraints();
			PerformLayout();
		}
		finally
		{
			AfterLayoutChange?.Invoke(this, EventArgs.Empty);
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
	public override string ToString() => _messageTextControl.Text;

	/// <inheritdoc />
	protected override void Dispose(bool disposing)
	{
		if (disposing && _message?.Content is { } content)
			content.PropertyChanged -= MessageContent_PropertyChanged;

		var senderFont = disposing ? _senderFont : null;
		_senderFont = null;
		base.Dispose(disposing);
		senderFont?.Dispose();
	}

	private void UpdateMessageDisplay()
	{
		BeforeLayoutChange?.Invoke(this, EventArgs.Empty);
		try
		{
			_senderLabel.Text = Message?.Sender?.Name ?? string.Empty;

			if (Message is null)
			{
				UpdateMessageText(string.Empty, visible: false);
				return;
			}

			if (Message.Content is FileAttachmentMessageContent attachmentContent)
			{
				var text = MessageFormatter.Format(new StringMessageContent(attachmentContent.Text));
				UpdateMessageText(text, !string.IsNullOrWhiteSpace(text));
			}
			else
			{
				var text = Message.Content.Content?.ToString() ?? string.Empty;
				UpdateMessageText(MessageFormatter.Format(new StringMessageContent(text)), visible: true);
			}
		}
		finally
		{
			AfterLayoutChange?.Invoke(this, EventArgs.Empty);
		}
	}

	private void UpdateMessageText(string text, bool visible)
	{
		if (!string.Equals(_messageTextControl.Text, text, StringComparison.Ordinal))
		{
			var preserveSelection = _messageTextControl.IsHandleCreated;
			var selectionStart = preserveSelection ? _messageTextControl.SelectionStart : 0;
			var selectionLength = preserveSelection ? _messageTextControl.SelectionLength : 0;

			_messageTextControl.Text = text;

			if (preserveSelection)
			{
				selectionStart = Math.Min(selectionStart, text.Length);
				selectionLength = Math.Min(selectionLength, text.Length - selectionStart);
				_messageTextControl.Select(selectionStart, selectionLength);
			}
		}

		_messageTextControl.Visible = visible;
	}

	private void MessageContent_PropertyChanged(object? sender, PropertyChangedEventArgs e)
	{
		if (IsDisposed || Disposing)
			return;

		if (InvokeRequired)
		{
			if (IsHandleCreated)
			{
				try
				{
					BeginInvoke(UpdateMessageDisplay);
				}
				catch (InvalidOperationException) when (IsDisposed || Disposing || !IsHandleCreated)
				{
					// The message was removed while the cross-thread update was being queued.
				}
			}
			return;
		}

		UpdateMessageDisplay();
	}

	private void ApplyAppearance()
	{
		if (_senderLabel is null || _messageTextControl is null)
			return;

		ApplyFont();
		ApplyColors();
	}

	private void ApplyColors()
	{
		if (_senderLabel is null || _messageTextControl is null)
			return;

		_senderLabel.ForeColor = ForeColor;
		_messageTextControl.BackColor = GetOpaqueBackColor();
		_messageTextControl.ForeColor = ForeColor;
		ApplyAttachmentPreviewColors();
	}

	private void ApplyFont()
	{
		if (_senderLabel is null || _messageTextControl is null)
			return;

		var oldSenderFont = _senderFont;
		_senderFont = new Font(Font, FontStyle.Bold);
		_senderLabel.Font = _senderFont;
		_messageTextControl.Font = Font;
		ApplyAttachmentPreviewFont();
		oldSenderFont?.Dispose();
	}

	private Color GetOpaqueBackColor()
	{
		for (Control? current = this; current is not null; current = current.Parent)
		{
			if (current.BackColor.A == byte.MaxValue)
				return current.BackColor;
		}

		return SystemColors.Control;
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

		_attachmentPreviewPanel = previewPanel;
		Controls.Add(previewPanel);
		previewPanel.BringToFront();
		UpdateContentWidthConstraints();
		ApplyAttachmentPreviewFont();
		ApplyAttachmentPreviewColors();
	}

	private void UpdateContentWidthConstraints()
	{
		if (_senderLabel is null || _messageTextControl is null)
			return;

		var contentWidth = MaximumSize.Width == 0 ? 0 : Math.Max(0, MaximumSize.Width - ContentPadding.Horizontal);
		_senderLabel.MaximumSize = new Size(contentWidth, 0);
		_messageTextControl.MaximumSize = new Size(contentWidth, 0);
		if (_attachmentPreviewPanel is not null)
			_attachmentPreviewPanel.MaximumSize = new Size(contentWidth, 0);
	}

	private void ApplyAttachmentPreviewFont()
	{
		if (_attachmentPreviewPanel is null)
			return;

		foreach (var label in GetDescendantLabels(_attachmentPreviewPanel))
			label.Font = Font;
	}

	private void ApplyAttachmentPreviewColors()
	{
		if (_attachmentPreviewPanel is null)
			return;

		var backColor = GetOpaqueBackColor();
		_attachmentPreviewPanel.BackColor = backColor;
		foreach (Control preview in _attachmentPreviewPanel.Controls)
		{
			if (preview is Panel)
				preview.BackColor = backColor;

			foreach (var label in GetDescendantLabels(preview))
			{
				label.BackColor = backColor;
				label.ForeColor = ForeColor;
			}
		}
	}

	private static IEnumerable<Label> GetDescendantLabels(Control parent)
	{
		foreach (Control child in parent.Controls)
		{
			if (child is Label label)
				yield return label;

			foreach (var descendant in GetDescendantLabels(child))
				yield return descendant;
		}
	}
}
