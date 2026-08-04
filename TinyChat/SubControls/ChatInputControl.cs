using System.ComponentModel;

namespace TinyChat;

/// <summary>
/// A text input control that allows users to compose messages with optional file attachments.
/// </summary>
public class ChatInputControl : Control, IChatInputControl, IChatAttachmentInputControl, IChatInputDraftControl, IChatInputLayoutControl
{
	private const int ATTACHMENT_DISPLAY_HEIGHT = 60;
	private const int DEFAULT_CONTENT_PADDING = 8;
	private const string SEND_CHAR = "\u27A4";
	private const string STOP_CHAR = "\u25A0";

	private readonly List<ChatFileAttachment> _attachments = [];
	private readonly FlowLayoutPanel _attachmentPanel;
	private readonly Panel _contentPanel;
	private readonly TableLayoutPanel _contentLayout;
	private readonly PasteAwareTextBox _textBox;
	private readonly Button _sendButton;
	private bool _isReceivingStream;
	private bool _allowCancellation;

	/// <summary>
	/// Occurs before a message is sent from the text box.
	/// </summary>
	public event EventHandler<MessageSendingEventArgs>? MessageSending;

	/// <summary>
	/// The event that is raised when cancellation of a streaming message is requested.
	/// </summary>
	public event EventHandler? CancellationRequested;

	/// <inheritdoc />
	public event EventHandler<AttachmentPasteRequestedEventArgs>? AttachmentPasteRequested;

	/// <inheritdoc />
	public event EventHandler? AttachmentsChanged;

	/// <inheritdoc />
	public IReadOnlyList<ChatFileAttachment> PendingAttachments => _attachments.AsReadOnly();

	/// <inheritdoc />
	public int AttachmentDisplayHeight => ATTACHMENT_DISPLAY_HEIGHT;

	/// <inheritdoc />
	[Browsable(false)]
	[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
	public Padding ContentPadding
	{
		get => _contentPanel.Padding;
		set
		{
			_contentPanel.Padding = value;
			PositionSendButton();
			PerformLayout();
		}
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="ChatInputControl"/> class.
	/// </summary>
	public ChatInputControl()
	{
		_textBox = new PasteAwareTextBox { Multiline = true, Visible = true, Dock = DockStyle.Fill };
		_textBox.AttachmentPasteRequested += (_, e) => AttachmentPasteRequested?.Invoke(this, e);
		_attachmentPanel = new FlowLayoutPanel
		{
			AutoScroll = true,
			Dock = DockStyle.Fill,
			FlowDirection = FlowDirection.LeftToRight,
			Margin = Padding.Empty,
			Visible = false,
			WrapContents = false
		};
		_contentLayout = new TableLayoutPanel
		{
			ColumnCount = 1,
			Dock = DockStyle.Fill,
			GrowStyle = TableLayoutPanelGrowStyle.FixedSize,
			Margin = Padding.Empty,
			Padding = Padding.Empty,
			RowCount = 2
		};
		_contentLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
		_contentLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 0));
		_contentLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
		_contentLayout.Controls.Add(_attachmentPanel, 0, 0);
		_contentLayout.Controls.Add(_textBox, 0, 1);

		_contentPanel = new Panel { Padding = new Padding(DEFAULT_CONTENT_PADDING), Dock = DockStyle.Fill };
		Controls.Add(_contentPanel);
		_contentPanel.Controls.Add(_contentLayout);

		var size = new Size(24, 24);
		_sendButton = new Button { Text = SEND_CHAR, MaximumSize = size, MinimumSize = size, Anchor = AnchorStyles.Bottom | AnchorStyles.Right };
		Controls.Add(_sendButton);
		_sendButton.BringToFront();
		PositionSendButton();

		_sendButton.Click += (_, _) => SendOrStop();
		_textBox.KeyPress += TextBox_KeyPress;
		ApplyAppearance();
	}

	/// <inheritdoc />
	public void AddAttachments(IEnumerable<ChatFileAttachment> attachments)
	{
		ArgumentNullException.ThrowIfNull(attachments);

		foreach (var attachment in attachments)
		{
			ArgumentNullException.ThrowIfNull(attachment);
			_attachments.Add(attachment);
			_attachmentPanel.Controls.Add(CreateAttachmentTile(attachment));
		}

		UpdateAttachmentPanel();
	}

	/// <inheritdoc />
	public void ClearAttachments()
	{
		_attachments.Clear();
		while (_attachmentPanel.Controls.Count > 0)
			_attachmentPanel.Controls[0].Dispose();

		UpdateAttachmentPanel();
	}

	/// <inheritdoc />
	public void ClearDraft()
	{
		_textBox.Clear();
		ClearAttachments();
	}

	/// <inheritdoc />
	protected override void OnGotFocus(EventArgs e)
	{
		base.OnGotFocus(e);
		_textBox.Focus();
	}

	/// <inheritdoc />
	protected override void OnHandleCreated(EventArgs e)
	{
		base.OnHandleCreated(e);
		ApplyReceivingState();
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
	protected override void OnSizeChanged(EventArgs e)
	{
		base.OnSizeChanged(e);
		PositionSendButton();
	}

	private void TextBox_KeyPress(object? sender, KeyPressEventArgs e)
	{
		if (!_isReceivingStream)
		{
			var lineBreakEnter = ModifierKeys.HasFlag(Keys.Control) || ModifierKeys.HasFlag(Keys.Shift);
			if (e.KeyChar == (char)Keys.Enter && !lineBreakEnter)
			{
				e.Handled = true;
				Send();
			}
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
		if (_attachments.Count == 0 && string.IsNullOrWhiteSpace(_textBox.Text))
			return;

		IChatMessageContent content = _attachments.Count > 0
			? new FileAttachmentMessageContent(_attachments.ToArray(), _textBox.Text)
			: new StringMessageContent(_textBox.Text);
		var sendArgs = new MessageSendingEventArgs(null!, content);
		MessageSending?.Invoke(this, sendArgs);

		if (!sendArgs.Cancel)
			ClearDraft();
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
		_allowCancellation = allowCancellation;

		if (!IsAvailable() || !IsHandleCreated)
			return;

		if (InvokeRequired)
			BeginInvoke(ApplyReceivingState);
		else
			ApplyReceivingState();
	}

	private void ApplyReceivingState()
	{
		if (!IsAvailable())
			return;

		_sendButton.Text = _isReceivingStream && _allowCancellation ? STOP_CHAR : SEND_CHAR;
		_sendButton.Enabled = !_isReceivingStream || _allowCancellation;
	}

	private Control CreateAttachmentTile(ChatFileAttachment attachment)
	{
		var tile = new Panel
		{
			AccessibleName = attachment.Name,
			BorderStyle = BorderStyle.FixedSingle,
			Margin = new Padding(0, 0, 6, 0),
			Size = new Size(attachment.IsImage ? 64 : 180, 40)
		};
		var removeButton = new Button
		{
			Cursor = Cursors.Hand,
			Dock = DockStyle.Right,
			FlatStyle = FlatStyle.Flat,
			TabStop = false,
			Text = "×",
			Width = 24
		};
		removeButton.FlatAppearance.BorderSize = 0;
		removeButton.Click += (_, _) => RemoveAttachment(attachment, tile);

		tile.Controls.Add(removeButton);
		if (!attachment.IsImage)
		{
			var nameLabel = new Label
			{
				AutoEllipsis = true,
				Dock = DockStyle.Fill,
				Padding = new Padding(4, 0, 0, 0),
				Text = attachment.Name,
				TextAlign = ContentAlignment.MiddleLeft,
				UseMnemonic = false
			};
			tile.Controls.Add(nameLabel);
			nameLabel.BringToFront();
		}

		tile.Controls.Add(CreateAttachmentIcon(attachment));
		ApplyAttachmentTileAppearance(tile);
		return tile;
	}

	private static Control CreateAttachmentIcon(ChatFileAttachment attachment)
	{
		if (!attachment.IsImage)
		{
			return new FileAttachmentIconControl
			{
				Dock = DockStyle.Left,
				Width = 34
			};
		}

		var image = FileAttachmentImageFactory.Create(attachment);
		var preview = new PictureBox
		{
			Dock = DockStyle.Left,
			Image = image,
			Padding = new Padding(2),
			SizeMode = PictureBoxSizeMode.Zoom,
			Width = 34
		};
		preview.Disposed += (_, _) => image.Dispose();
		return preview;
	}

	private void RemoveAttachment(ChatFileAttachment attachment, Control tile)
	{
		_attachments.Remove(attachment);
		tile.Dispose();
		UpdateAttachmentPanel();
	}

	private void UpdateAttachmentPanel()
	{
		var hasAttachments = _attachments.Count > 0;
		_contentLayout.RowStyles[0].Height = hasAttachments ? ATTACHMENT_DISPLAY_HEIGHT : 0;
		_attachmentPanel.Visible = hasAttachments;
		AttachmentsChanged?.Invoke(this, EventArgs.Empty);
	}

	private void PositionSendButton()
	{
		if (_sendButton is null || _contentPanel is null)
			return;

		_sendButton.Left = Math.Max(0, ClientSize.Width - _sendButton.Width - ContentPadding.Right);
		_sendButton.Top = Math.Max(0, ClientSize.Height - _sendButton.Height - ContentPadding.Bottom);
	}

	private void ApplyAppearance()
	{
		if (_textBox is null || _contentPanel is null || _contentLayout is null || _attachmentPanel is null)
			return;

		ApplyFont();
		ApplyColors();
	}

	private void ApplyFont()
	{
		if (_textBox is null || _attachmentPanel is null)
			return;

		_textBox.Font = Font;
		foreach (Control tile in _attachmentPanel.Controls)
		{
			foreach (var label in tile.Controls.OfType<Label>())
				label.Font = Font;
		}
	}

	private void ApplyColors()
	{
		if (_textBox is null || _contentPanel is null || _contentLayout is null || _attachmentPanel is null)
			return;

		var backColor = GetOpaqueBackColor();
		_contentPanel.BackColor = backColor;
		_contentLayout.BackColor = backColor;
		_attachmentPanel.BackColor = backColor;
		_textBox.BackColor = backColor;
		_textBox.ForeColor = ForeColor;

		foreach (Control tile in _attachmentPanel.Controls)
			ApplyAttachmentTileAppearance(tile);
	}

	private void ApplyAttachmentTileAppearance(Control tile)
	{
		var backColor = GetOpaqueBackColor();
		tile.BackColor = backColor;
		foreach (var label in tile.Controls.OfType<Label>())
		{
			label.BackColor = backColor;
			label.Font = Font;
			label.ForeColor = ForeColor;
		}
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

	private bool IsAvailable() => !(Disposing || IsDisposed);

	private sealed class PasteAwareTextBox : TextBox
	{
		private const int WM_PASTE = 0x0302;

		public event EventHandler<AttachmentPasteRequestedEventArgs>? AttachmentPasteRequested;

		protected override void OnKeyDown(KeyEventArgs e)
		{
			var isPasteCommand = (e.Control && e.KeyCode == Keys.V) || (e.Shift && e.KeyCode == Keys.Insert);
			if (isPasteCommand && TryRequestAttachmentPaste())
			{
				e.Handled = true;
				e.SuppressKeyPress = true;
				return;
			}

			base.OnKeyDown(e);
		}

		protected override void WndProc(ref Message m)
		{
			if (m.Msg == WM_PASTE && TryRequestAttachmentPaste())
				return;

			base.WndProc(ref m);
		}

		private bool TryRequestAttachmentPaste()
		{
			if (AttachmentPasteRequested is null || ClipboardAttachmentHelper.GetClipboardAttachmentData() is not { } data)
				return false;

			AttachmentPasteRequested.Invoke(this, new AttachmentPasteRequestedEventArgs(data));
			return true;
		}
	}
}
