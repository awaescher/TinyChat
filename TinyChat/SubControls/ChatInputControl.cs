using System.ComponentModel;

namespace TinyChat;

/// <summary>
/// A text input control that allows users to compose messages with optional file attachments.
/// </summary>
public class ChatInputControl : Control, IChatInputControl, IChatAttachmentInputControl, IChatInputDraftControl, IChatInputDraftEditorControl, IChatInputLayoutControl, IChatInputAppearanceControl, IChatAttachmentNoticeInput, IChatInputMenuControl, IChatInputMinimumHeightControl
{
	private const int ATTACHMENT_DISPLAY_HEIGHT = 72;
	private const int DEFAULT_CONTENT_PADDING = 8;
	private const int SEND_BUTTON_INSET = 6;
	private const int MENU_ICON_SIZE = 16;
	private const int MENU_GLYPH_SIZE = 20;

	private readonly List<ChatFileAttachment> _attachments = [];
	private readonly FlowLayoutPanel _attachmentPanel;
	private readonly Panel _contentPanel;
	private readonly TableLayoutPanel _contentLayout;
	private readonly PasteAwareTextBox _textBox;
	private readonly Button _sendButton;
	private readonly InputMenuGlyph _menuGlyph;
	private readonly ToolTip _toolTip = new();
	private ContextMenuStrip? _menu;
	private readonly List<Image> _menuImages = [];
	private string _menuButtonText = string.Empty;
	private bool _showMenuButton;
	private bool _menuGlyphShown;
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
	public event EventHandler? MenuRequested;

	/// <inheritdoc />
	[Browsable(false)]
	[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
	public bool ShowMenuButton
	{
		get => _showMenuButton;
		set
		{
			_showMenuButton = value;
			UpdateMenuGlyphVisibility();
			UpdateMenuToolTip();
		}
	}

	/// <inheritdoc />
	[Browsable(false)]
	[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
	public string MenuButtonText
	{
		get => _menuButtonText;
		set
		{
			_menuButtonText = value ?? string.Empty;
			UpdateMenuToolTip();
		}
	}

	/// <inheritdoc />
	public IReadOnlyList<ChatFileAttachment> PendingAttachments => _attachments.AsReadOnly();

	/// <inheritdoc />
	public int AttachmentDisplayHeight => ScaleLogicalPixels(ATTACHMENT_DISPLAY_HEIGHT);

	/// <inheritdoc />
	public event EventHandler? MinimumHeightChanged;

	/// <inheritdoc />
	[Browsable(false)]
	public int MinimumHeight => _contentPanel.Padding.Vertical + _textBox.Margin.Vertical + (_textBox.Height - _textBox.ClientSize.Height) + _sendButton.Height + 2 * ScaleLogicalPixels(SEND_BUTTON_INSET);

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
			MinimumHeightChanged?.Invoke(this, EventArgs.Empty);
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

		_menuGlyph = new InputMenuGlyph(() => _textBox.BackColor, () => _textBox.ForeColor) { Visible = false };
		_menuGlyph.HoverChanged += (_, _) =>
		{
			UpdateMenuToolTip();
			UpdateMenuGlyphVisibility();
		};
		Controls.Add(_menuGlyph);
		_menuGlyph.BringToFront();
		// With text, the symbol only appears while the pointer is over its own area.
		_textBox.TextChanged += (_, _) => UpdateMenuGlyphVisibility();
		_textBox.MouseMove += (_, _) => UpdateMenuGlyphVisibility();
		_textBox.MouseLeave += (_, _) => UpdateMenuGlyphVisibility();
		_menuGlyph.MouseLeave += (_, _) => UpdateMenuGlyphVisibility();
		PositionSendButton();

		_sendButton.Click += (_, _) => SendOrStop();
		_menuGlyph.Click += (_, _) => MenuRequested?.Invoke(this, EventArgs.Empty);
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
	public void SetDraft(string text, IEnumerable<ChatFileAttachment> attachments)
	{
		ClearDraft();
		_textBox.Text = (text ?? string.Empty).ReplaceLineEndings("\r\n");
		_textBox.SelectionStart = _textBox.TextLength;
		AddAttachments(attachments);
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
		UpdateAttachmentRowHeight();
		PositionSendButton();
	}

	/// <inheritdoc />
	protected override void OnDpiChangedAfterParent(EventArgs e)
	{
		base.OnDpiChangedAfterParent(e);
		PositionSendButton();
		MinimumHeightChanged?.Invoke(this, EventArgs.Empty);
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
		MinimumHeightChanged?.Invoke(this, EventArgs.Empty);
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

	bool IChatAttachmentNoticeInput.HasAttachmentNotices => _attachmentPanel.Controls.OfType<AttachmentCard>().Any(card => card.InputNotice is not null);

	void IChatAttachmentNoticeInput.AddAttachmentNotice(Control notice)
	{
		_attachmentPanel.Controls.Add(notice);
		UpdateAttachmentPanel();
	}

	void IChatAttachmentNoticeInput.ClearAttachmentNotices(string? fileName)
	{
		foreach (var notice in _attachmentPanel.Controls.OfType<AttachmentCard>().Where(card => card.InputNotice is { } notice && (fileName is null || string.Equals(notice.FileName, fileName, StringComparison.OrdinalIgnoreCase))).ToArray())
			notice.Dispose();
		UpdateAttachmentPanel();
	}

	private Control CreateAttachmentTile(ChatFileAttachment attachment)
	{
		AttachmentCard? card = null;
		card = new AttachmentCard(attachment, () => RemoveAttachment(attachment, card!));
		return card;
	}

	private void RemoveAttachment(ChatFileAttachment attachment, Control tile)
	{
		_attachments.Remove(attachment);
		tile.Dispose();
		UpdateAttachmentPanel();
	}

	private void UpdateAttachmentPanel()
	{
		var hasAttachments = _attachmentPanel.Controls.Count > 0;
		UpdateAttachmentRowHeight();
		_attachmentPanel.Visible = hasAttachments;
		AttachmentsChanged?.Invoke(this, EventArgs.Empty);
	}

	private void UpdateAttachmentRowHeight()
	{
		if (_contentLayout is null || _sendButton is null)
			return;
		var availableHeight = Math.Max(0, _contentLayout.ClientSize.Height - _sendButton.Height - ScaleLogicalPixels(12));
		var height = _attachmentPanel.Controls.Count > 0 ? Math.Min(AttachmentDisplayHeight, availableHeight) : 0;
		if (_contentLayout.RowStyles[0].Height != height)
			_contentLayout.RowStyles[0].Height = height;
	}

	private void PositionSendButton()
	{
		if (_sendButton is null || _textBox is null)
			return;

		var editorBounds = GetBoundsRelativeToInput(_textBox);
		var inset = ScaleLogicalPixels(SEND_BUTTON_INSET);
		_sendButton.Left = Math.Max(editorBounds.Left, editorBounds.Right - _sendButton.Width - inset);
		_sendButton.Top = editorBounds.Bottom - _sendButton.Height - GetVerticalInset(editorBounds, _sendButton, inset);

		if (_menuGlyph is null)
			return;

		// The symbol lies over the lower left corner of the editor, mirroring the send button, without changing the text layout.
		var glyphSize = ScaleLogicalPixels(MENU_GLYPH_SIZE);
		_menuGlyph.Size = new Size(glyphSize, glyphSize);
		_menuGlyph.Left = Math.Min(editorBounds.Left + inset, Math.Max(editorBounds.Left, _sendButton.Left - glyphSize));
		_menuGlyph.Top = _sendButton.Top + (_sendButton.Height - glyphSize) / 2;
	}

	/// <summary>
	/// Shows the symbol while the draft is empty. With text, it only appears while the pointer is over its own area,
	/// so it never covers the text otherwise.
	/// </summary>
	private void UpdateMenuGlyphVisibility()
	{
		if (_menuGlyph is null)
			return;

		var visible = _showMenuButton && (_textBox.TextLength == 0 || IsPointerOverMenuGlyph() || _menu?.Visible == true);
		if (_menuGlyphShown == visible)
			return;

		_menuGlyphShown = visible;
		_menuGlyph.Visible = visible;
		if (visible)
			_menuGlyph.BringToFront();
	}

	private bool IsPointerOverMenuGlyph() => IsHandleCreated && RectangleToScreen(_menuGlyph.Bounds).Contains(PointerPosition);

	/// <summary>Gets the pointer position in screen coordinates; replaceable in tests.</summary>
	internal Func<Point> _pointerPositionProvider = () => Cursor.Position;

	private Point PointerPosition => _pointerPositionProvider();

	/// <inheritdoc />
	protected override void OnMouseLeave(EventArgs e)
	{
		base.OnMouseLeave(e);
		UpdateMenuGlyphVisibility();
	}

	private void UpdateMenuToolTip()
	{
		_menuGlyph.AccessibleName = _menuButtonText;
		_toolTip.SetToolTip(_menuGlyph, _menuButtonText);
	}

	/// <summary>
	/// Keeps a button inside an editor that is too low for the regular inset by centering it vertically.
	/// </summary>
	private static int GetVerticalInset(Rectangle editorBounds, Control button, int inset) =>
		Math.Clamp((editorBounds.Height - button.Height) / 2, 0, inset);

	/// <inheritdoc />
	public void ShowMenu(IReadOnlyList<ChatInputMenuItem> items)
	{
		ArgumentNullException.ThrowIfNull(items);
		if (items.Count == 0 || !IsHandleCreated)
			return;

		_menu?.Dispose();
		DisposeMenuImages();
		_menu = new ContextMenuStrip { RightToLeft = RightToLeft };
		var iconPixels = ScaleLogicalPixels(MENU_ICON_SIZE);
		_menu.ImageScalingSize = new Size(iconPixels, iconPixels);
		foreach (var item in items)
		{
			if (item.BeginGroup && _menu.Items.Count > 0)
				_menu.Items.Add(new ToolStripSeparator());

			var image = item.Image;
			if (image is null && CreateMenuImage(item.Kind, iconPixels) is { } defaultImage)
			{
				_menuImages.Add(defaultImage);
				image = defaultImage;
			}

			var menuItem = new ToolStripMenuItem(item.Text, image, (_, _) => item.Execute())
			{
				AccessibleName = item.Text,
				Enabled = item.Enabled
			};
			_menu.Items.Add(menuItem);
		}

		_menu.Show(_menuGlyph, new Point(0, _menuGlyph.Height));
	}

	private void DisposeMenuImages()
	{
		foreach (var image in _menuImages)
			image.Dispose();
		_menuImages.Clear();
	}

	private static Image? CreateMenuImage(ChatInputMenuItemKind kind, int pixelSize) => kind switch
	{
		ChatInputMenuItemKind.AttachFiles => ChatIcons.CreateImage(ChatIcons.Attach, pixelSize, SystemColors.MenuText),
		_ => null
	};

	/// <inheritdoc />
	protected override void Dispose(bool disposing)
	{
		if (disposing)
		{
			_menu?.Dispose();
			DisposeMenuImages();
			_toolTip.Dispose();
		}

		base.Dispose(disposing);
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
		_menuGlyph?.Invalidate();
		_textBox.BorderStyle = EditorBorderStyle;
		_textBox.PlaceholderText = PlaceholderText;
		_textBox.CueText = PlaceholderText;
		_textBox.ReadOnly = false;
	}

	private static void ApplyAttachmentTileAppearance(Control tile)
	{
		if (tile is AttachmentCard card)
			card.ApplyPresentation(null);
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
			if (isPasteCommand && (TryRequestAttachmentPaste() || TryPasteNormalizedText()))
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
			if (m.Msg == WM_PASTE && (TryRequestAttachmentPaste() || TryPasteNormalizedText()))
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

		/// <summary>
		/// Pastes text from other applications that only separate lines with LF, which the text box would show on a single line.
		/// </summary>
		private bool TryPasteNormalizedText()
		{
			if (ReadOnly || ClipboardText.GetTextToNormalize() is not { } text)
				return false;

			SelectedText = text;
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
