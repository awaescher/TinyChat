using System.ComponentModel;
using DevExpress.XtraEditors;
using TinyChat.Messages.Formatting;

namespace TinyChat;

/// <summary>
/// A DevExpress-based chat message control that displays a chat message with sender and content.
/// Implements the IChatMessageControl interface and inherits from PanelControl.
/// </summary>
public class DXChatMessageControl : PanelControl, IChatMessageControl, IChatMessageLayoutControl
{
	private IChatMessage? _message;
	private bool _isReceivingStream;
	private readonly LabelControl _senderLabel;
	private readonly DXSelectableMessageTextControl _messageEditor;
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

	/// <inheritdoc />
	[Browsable(false)]
	[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
	public Padding ContentPadding
	{
		get => Padding;
		set => Padding = value;
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="DXChatMessageControl"/> class.
	/// Sets up the sender and permanently visible selectable HTML content control.
	/// </summary>
	public DXChatMessageControl()
	{
		BorderStyle = DevExpress.XtraEditors.Controls.BorderStyles.NoBorder;
		AutoSize = true;

		_senderLabel = new LabelControl() { AllowHtmlString = true, Dock = DockStyle.Top, AutoSizeMode = LabelAutoSizeMode.Vertical, Font = new Font(Font, FontStyle.Bold), UseMnemonic = false, Padding = new Padding(0, 0, 0, 3) };
		_messageEditor = new DXSelectableMessageTextControl
		{
			Dock = DockStyle.Top,
			Visible = false
		};
		_messageEditor.ContentHeightChanging += (_, _) => BeforeLayoutChange?.Invoke(this, EventArgs.Empty);
		_messageEditor.ContentHeightChanged += (_, _) => AfterLayoutChange?.Invoke(this, EventArgs.Empty);

		Controls.Add(_senderLabel);
		Controls.Add(_messageEditor);

		_messageEditor.BringToFront();

		AutoSize = true;
		ContentPadding = new Padding(8);

		WireMouseDown(_senderLabel);
		ApplyAppearance();
	}

	private void WireMouseDown(params Control[] controls)
	{
		foreach (var c in controls)
			c.MouseDown += (_, e) => OnMouseDown(e);
	}

	/// <summary>
	/// Gets or sets the chat message displayed by this control.
	/// When set, updates the sender header and selectable content editor.
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
			if (_message?.Content is { } previousContent)
				previousContent.PropertyChanged -= MessageContent_PropertyChanged;

			_message = value;
			if (_message?.Content is { } currentContent)
				currentContent.PropertyChanged += MessageContent_PropertyChanged;

			UpdateMessageDisplay();
			UpdateAttachmentPreview();
		}
	}

	/// <summary>
	/// Gets or sets the maximum size of the control.
	/// When set, also updates the maximum size of the internal header and editor to account for padding.
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
			UpdateContentWidthConstraints();
		}
	}

	/// <inheritdoc />
	protected override void OnPaddingChanged(EventArgs e)
	{
		base.OnPaddingChanged(e);
		UpdateContentWidthConstraints();
		PerformLayout();
	}

	/// <inheritdoc />
	protected override void OnFontChanged(EventArgs e)
	{
		base.OnFontChanged(e);
		ApplyAppearance();
	}

	/// <inheritdoc />
	protected override void OnForeColorChanged(EventArgs e)
	{
		base.OnForeColorChanged(e);
		ApplyAppearance();
	}

	/// <inheritdoc />
	protected override void OnBackColorChanged(EventArgs e)
	{
		base.OnBackColorChanged(e);
		ApplyAppearance();
	}

	/// <inheritdoc />
	protected override void OnSizeChanged(EventArgs e)
	{
		base.OnSizeChanged(e);
		_messageEditor?.UpdateContentHeight();

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
	public override string ToString() => _messageEditor.Text;

	/// <inheritdoc />
	protected override void Dispose(bool disposing)
	{
		if (disposing && _message?.Content is { } content)
			content.PropertyChanged -= MessageContent_PropertyChanged;

		base.Dispose(disposing);
	}

	private void UpdateMessageDisplay()
	{
		BeforeLayoutChange?.Invoke(this, EventArgs.Empty);
		try
		{
			_senderLabel.Text = Message?.Sender?.Name ?? string.Empty;
			if (Message is null)
			{
				_messageEditor.SetTextPreservingSelection(string.Empty);
				_messageEditor.Visible = false;
				return;
			}

			string formattedText;
			var hideEmptyFormattedText = false;
			if (Message.Content is FileAttachmentMessageContent attachmentContent)
			{
				formattedText = MessageFormatter.Format(new StringMessageContent(attachmentContent.Text));
				hideEmptyFormattedText = true;
			}
			else
			{
				var sourceText = Message.Content.Content?.ToString() ?? string.Empty;
				formattedText = MessageFormatter.Format(sourceText);
			}

			_messageEditor.SetFormattedTextPreservingSelection(formattedText);
			_messageEditor.Visible = !hideEmptyFormattedText || !string.IsNullOrWhiteSpace(_messageEditor.Text);
			_messageEditor.UpdateContentHeight();
		}
		finally
		{
			AfterLayoutChange?.Invoke(this, EventArgs.Empty);
		}
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
		ApplyAppearance();
	}

	private void ApplyAppearance()
	{
		if (_senderLabel is null || _messageEditor is null)
			return;

		_senderLabel.Appearance.Font = Font;
		_senderLabel.Appearance.FontStyleDelta = FontStyle.Bold;
		_senderLabel.Appearance.ForeColor = ForeColor;
		_senderLabel.Appearance.BackColor = BackColor;
		_senderLabel.Appearance.Options.UseFont = true;
		_senderLabel.Appearance.Options.UseForeColor = true;
		_senderLabel.Appearance.Options.UseBackColor = true;

		_messageEditor.ApplyTextAppearance(Font, ForeColor, BackColor);

		if (_attachmentPreviewPanel is not null)
		{
			_attachmentPreviewPanel.Font = Font;
			_attachmentPreviewPanel.ForeColor = ForeColor;
			_attachmentPreviewPanel.BackColor = BackColor;
		}

	}

	private void UpdateContentWidthConstraints()
	{
		if (_senderLabel is null || _messageEditor is null)
			return;

		var contentWidth = MaximumSize.Width == 0
			? 0
			: Math.Max(0, MaximumSize.Width - ContentPadding.Horizontal);
		_senderLabel.MaximumSize = new Size(contentWidth, 0);
		_messageEditor.MaximumSize = new Size(contentWidth, 0);
		if (contentWidth > 0 && _messageEditor.Width != contentWidth)
			_messageEditor.Width = contentWidth;
		if (_attachmentPreviewPanel is not null)
			_attachmentPreviewPanel.MaximumSize = new Size(contentWidth, 0);

		_messageEditor.UpdateContentHeight();
	}
}
