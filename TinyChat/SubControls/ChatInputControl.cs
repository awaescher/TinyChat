using System.ComponentModel;

namespace TinyChat;

/// <summary>
/// A text input control that allows users to compose messages with optional file attachments.
/// </summary>
public class ChatInputControl : Control, IChatInputControl, IChatAttachmentInputControl, IChatInputDraftControl, IChatInputLayoutControl, IChatInputAppearanceControl
{
	private const int ATTACHMENT_DISPLAY_HEIGHT = 60;
	private const int DEFAULT_CONTENT_PADDING = 8;
	private const int SEND_BUTTON_INSET = 6;

	private readonly List<ChatFileAttachment> _attachments = [];
	private readonly FlowLayoutPanel _attachmentPanel;
	private readonly Panel _contentPanel;
	private readonly TableLayoutPanel _contentLayout;
	private readonly PasteAwareTextBox _textBox;
	private readonly Button _sendButton;
	private Color _editorBackColor = Color.Empty;
	private Color _editorForeColor = Color.Empty;
	private BorderStyle _editorBorderStyle = BorderStyle.Fixed3D;
	private string _placeholderText = string.Empty;
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
			PerformLayout();
			PositionSendButton();
		}
	}

	/// <inheritdoc />
	[Browsable(false)]
	[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
	public Color EditorBackColor
	{
		get => _editorBackColor;
		set
		{
			_editorBackColor = value;
			ApplyEditorAppearance();
		}
	}

	/// <inheritdoc />
	[Browsable(false)]
	[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
	public Color EditorForeColor
	{
		get => _editorForeColor;
		set
		{
			_editorForeColor = value;
			ApplyEditorAppearance();
		}
	}

	/// <inheritdoc />
	[Browsable(false)]
	[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
	public BorderStyle EditorBorderStyle
	{
		get => _editorBorderStyle;
		set
		{
			if (!Enum.IsDefined(value))
				throw new InvalidEnumArgumentException(nameof(value), (int)value, typeof(BorderStyle));

			_editorBorderStyle = value;
			ApplyEditorAppearance();
		}
	}

	/// <inheritdoc />
	[Browsable(false)]
	[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
	public string PlaceholderText
	{
		get => _placeholderText;
		set
		{
			_placeholderText = value ?? string.Empty;
			ApplyEditorAppearance();
		}
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="ChatInputControl"/> class.
	/// </summary>
	public ChatInputControl()
	{
		_textBox = new PasteAwareTextBox
		{
			BorderStyle = BorderStyle.Fixed3D,
			Cursor = Cursors.IBeam,
			Dock = DockStyle.Fill,
			Multiline = true,
			ReadOnly = false,
			TabStop = true,
			Visible = true
		};
		_textBox.AttachmentPasteRequested += (_, e) => AttachmentPasteRequested?.Invoke(this, e);
		_textBox.LocationChanged += (_, _) => PositionSendButton();
		_textBox.SizeChanged += (_, _) => PositionSendButton();
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
		_sendButton = new NoFocusCueButton { Text = ChatIcons.Send, MaximumSize = size, MinimumSize = size, Anchor = AnchorStyles.Bottom | AnchorStyles.Right, TabStop = false };
		if (ChatIcons.IconFontName is not null)
			_sendButton.Font = ChatIcons.CreateFont(9f);
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
	protected override void OnLayout(LayoutEventArgs levent)
	{
		base.OnLayout(levent);
		PositionSendButton();
	}

	/// <inheritdoc />
	protected override void OnDpiChangedAfterParent(EventArgs e)
	{
		base.OnDpiChangedAfterParent(e);
		PositionSendButton();
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
	protected override void OnSystemColorsChanged(EventArgs e)
	{
		base.OnSystemColorsChanged(e);
		ApplyEditorAppearance();
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

		_sendButton.Text = _isReceivingStream && _allowCancellation ? ChatIcons.Stop : ChatIcons.Send;
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
		if (_sendButton is null || _textBox is null)
			return;

		var editorBounds = GetBoundsRelativeToInput(_textBox);
		var inset = ScaleLogicalPixels(SEND_BUTTON_INSET);
		_sendButton.Left = Math.Max(editorBounds.Left, editorBounds.Right - _sendButton.Width - inset);
		_sendButton.Top = Math.Max(editorBounds.Top, editorBounds.Bottom - _sendButton.Height - inset);
	}

	private Rectangle GetBoundsRelativeToInput(Control descendant)
	{
		var location = descendant.Location;
		for (var parent = descendant.Parent; parent is not null && parent != this; parent = parent.Parent)
			location.Offset(parent.Location);

		return new Rectangle(location, descendant.Size);
	}

	private int ScaleLogicalPixels(int logicalPixels)
	{
		return Math.Max(1, (int)Math.Round(logicalPixels * DeviceDpi / 96f));
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
		ApplyEditorAppearance();

		foreach (Control tile in _attachmentPanel.Controls)
			ApplyAttachmentTileAppearance(tile);
	}

	private void ApplyEditorAppearance()
	{
		if (_textBox is null)
			return;

		_textBox.BackColor = EditorBackColor.IsEmpty ? SystemColors.Window : EditorBackColor;
		_textBox.ForeColor = EditorForeColor.IsEmpty ? SystemColors.WindowText : EditorForeColor;
		_textBox.BorderStyle = EditorBorderStyle;
		_textBox.PlaceholderText = PlaceholderText;
		_textBox.CueText = PlaceholderText;
		_textBox.ReadOnly = false;
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
		private const int WM_PAINT = 0x000F;

		private string _cueText = string.Empty;

		public event EventHandler<AttachmentPasteRequestedEventArgs>? AttachmentPasteRequested;

		[Browsable(false)]
		[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		internal string CueText
		{
			get => _cueText;
			set
			{
				_cueText = value ?? string.Empty;
				Invalidate();
			}
		}

		protected override void OnBackColorChanged(EventArgs e)
		{
			base.OnBackColorChanged(e);
			Invalidate();
		}

		protected override void OnEnabledChanged(EventArgs e)
		{
			base.OnEnabledChanged(e);
			Invalidate();
		}

		protected override void OnFontChanged(EventArgs e)
		{
			base.OnFontChanged(e);
			Invalidate();
		}

		protected override void OnGotFocus(EventArgs e)
		{
			base.OnGotFocus(e);
			Invalidate();
		}

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

		protected override void OnLostFocus(EventArgs e)
		{
			base.OnLostFocus(e);
			Invalidate();
		}

		protected override void OnTextChanged(EventArgs e)
		{
			base.OnTextChanged(e);
			Invalidate();
		}

		protected override void WndProc(ref Message m)
		{
			if (m.Msg == WM_PASTE && TryRequestAttachmentPaste())
				return;

			base.WndProc(ref m);

			if (m.Msg == WM_PAINT)
				DrawCueText();
		}

		private void DrawCueText()
		{
			if (Focused || !Enabled || !string.IsNullOrEmpty(Text) || string.IsNullOrEmpty(CueText))
				return;

			var textBounds = ClientRectangle;
			textBounds.Inflate(-2, -1);
			if (textBounds.Width <= 0 || textBounds.Height <= 0)
				return;

			var flags = TextFormatFlags.NoPadding |
				TextFormatFlags.NoPrefix |
				TextFormatFlags.TextBoxControl |
				TextFormatFlags.Top |
				TextFormatFlags.WordBreak;
			if (RightToLeft == RightToLeft.Yes)
				flags |= TextFormatFlags.Right | TextFormatFlags.RightToLeft;
			else
				flags |= TextFormatFlags.Left;

			using var graphics = CreateGraphics();
			TextRenderer.DrawText(graphics, CueText, Font, textBounds, SystemColors.GrayText, BackColor, flags);
		}

		private bool TryRequestAttachmentPaste()
		{
			if (AttachmentPasteRequested is null || ClipboardAttachmentHelper.GetClipboardAttachmentData() is not { } data)
				return false;

			AttachmentPasteRequested.Invoke(this, new AttachmentPasteRequestedEventArgs(data));
			return true;
		}
	}

	private sealed class NoFocusCueButton : Button
	{
		public NoFocusCueButton()
		{
			SetStyle(ControlStyles.Selectable, false);
		}

		protected override bool ShowFocusCues => false;
	}
}
