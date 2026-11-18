using System.ComponentModel;
using DevExpress.Utils;
using DevExpress.Utils.Layout;
using DevExpress.Utils.Menu;
using DevExpress.Utils.Svg;
using DevExpress.XtraBars;
using DevExpress.XtraEditors;

namespace TinyChat;

/// <summary>
/// Provides a DevExpress-based chat input control for composing messages with optional file attachments.
/// </summary>
public class DXChatInputControl : Control, IChatInputControl, IChatAttachmentInputControl, IChatInputDraftControl, IChatInputDraftEditorControl, IChatInputLayoutControl, IChatInputAppearanceControl, IChatAttachmentNoticeInput, IChatInputMenuControl, IChatInputMinimumHeightControl
{
	private const int ATTACHMENT_DISPLAY_HEIGHT = 72;
	private const int SEND_BUTTON_INSET = 6;
	private const int MENU_GLYPH_SIZE = 20;

	private readonly List<ChatFileAttachment> _attachments = [];
	private readonly StackPanel _attachmentPanel;
	private readonly PanelControl _contentPanel;
	private readonly TableLayoutPanel _contentLayout;
	private readonly PasteAwareMemoEdit _textBox;
	private readonly SimpleButton _sendButton;
	private readonly InputMenuGlyph _menuGlyph;
	private readonly ToolTipController _toolTipController = new();
	private DXPopupMenu? _menu;
	private readonly BarManager _menuManager = new();
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
			_menuGlyph.AccessibleName = _menuButtonText;
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
			if (_contentPanel.Padding == value)
				return;

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

	private SvgImage SendImage { get; } = FluentIcons.Send;

	private SvgImage StopImage { get; } = FluentIcons.Stop;

	/// <summary>
	/// Initializes a new instance of the <see cref="DXChatInputControl"/> class.
	/// </summary>
	public DXChatInputControl()
	{
		_textBox = new PasteAwareMemoEdit { Visible = true, Dock = DockStyle.Fill };
		_textBox.Properties.BorderStyle = DevExpress.XtraEditors.Controls.BorderStyles.Default;
		_textBox.Properties.ReadOnly = false;
		_textBox.Properties.ScrollBars = ScrollBars.None;
		_textBox.AttachmentPasteRequested += (_, e) => AttachmentPasteRequested?.Invoke(this, e);
		_textBox.LocationChanged += (_, _) => PositionSendButton();
		_textBox.SizeChanged += (_, _) => PositionSendButton();
		_attachmentPanel = new StackPanel
		{
			AutoScroll = true,
			Dock = DockStyle.Fill,
			LayoutDirection = StackPanelLayoutDirection.LeftToRight,
			BorderStyle = DevExpress.XtraEditors.Controls.BorderStyles.NoBorder,
			UseSkinIndents = false,
			Margin = Padding.Empty,
			Visible = false
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

		_contentPanel = new PanelControl { Padding = new Padding(0), Dock = DockStyle.Fill, BorderStyle = DevExpress.XtraEditors.Controls.BorderStyles.NoBorder };
		Controls.Add(_contentPanel);
		_contentPanel.Controls.Add(_contentLayout);

		var size = new Size(24, 24);
		_sendButton = new SimpleButton { MaximumSize = size, MinimumSize = size, Anchor = AnchorStyles.Bottom | AnchorStyles.Right, AllowFocus = false, ShowFocusRectangle = DevExpress.Utils.DefaultBoolean.False };
		_sendButton.ImageOptions.SvgImage = SendImage;
		_sendButton.ImageOptions.SvgImageSize = new Size(16, 16);

		_sendButton.ImageOptions.Location = ImageLocation.MiddleCenter;
		_sendButton.Padding = Padding.Empty;
		Controls.Add(_sendButton);
		PositionSendButton();
		_sendButton.BringToFront();

		_menuGlyph = new InputMenuGlyph(GetEditorBackColor, GetEditorForeColor) { Visible = false };
		_menuGlyph.HoverChanged += (_, _) =>
		{
			UpdateMenuToolTip();
			UpdateMenuGlyphVisibility();
		};
		Controls.Add(_menuGlyph);
		_menuGlyph.BringToFront();
		// With text, the symbol only appears while the pointer is over its own area.
		_textBox.EditValueChanged += (_, _) => UpdateMenuGlyphVisibility();
		_textBox.MouseMove += (_, _) => UpdateMenuGlyphVisibility();
		_textBox.MouseLeave += (_, _) => UpdateMenuGlyphVisibility();
		_textBox.MaskBox.MouseMove += (_, _) => UpdateMenuGlyphVisibility();
		_textBox.MaskBox.MouseLeave += (_, _) => UpdateMenuGlyphVisibility();
		_menuGlyph.MouseLeave += (_, _) => UpdateMenuGlyphVisibility();
		_textBox.Paint += (_, _) => _menuGlyph.Invalidate();
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
		_textBox.SelectionStart = _textBox.Text.Length;
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
	protected override void OnSizeChanged(EventArgs e)
	{
		base.OnSizeChanged(e);
		PositionSendButton();
	}

	/// <inheritdoc />
	protected override void OnFontChanged(EventArgs e)
	{
		base.OnFontChanged(e);
		ApplyAppearance();
		MinimumHeightChanged?.Invoke(this, EventArgs.Empty);
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

		_sendButton.ImageOptions.SvgImage = _isReceivingStream && _allowCancellation ? StopImage : SendImage;
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
		DXAttachmentCard? card = null;
		card = new DXAttachmentCard(attachment, () => RemoveAttachment(attachment, card!));
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

	private Color GetEditorBackColor()
	{
		var color = _textBox.PaintBackColor;
		return color.IsEmpty ? _textBox.MaskBox.BackColor : color;
	}

	private Color GetEditorForeColor()
	{
		var color = _textBox.PaintForeColor;
		return color.IsEmpty ? _textBox.MaskBox.ForeColor : color;
	}

	/// <summary>
	/// Shows the symbol while the draft is empty. With text, it only appears while the pointer is over its own area,
	/// so it never covers the text otherwise.
	/// </summary>
	private void UpdateMenuGlyphVisibility()
	{
		if (_menuGlyph is null)
			return;

		var visible = _showMenuButton && (string.IsNullOrEmpty(_textBox.Text) || IsPointerOverMenuGlyph());
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
		if (!_menuGlyph.Hovered)
		{
			_toolTipController.HideHint();
			return;
		}

		var glyph = _menuGlyph.RectangleToScreen(_menuGlyph.ClientRectangle);
		_toolTipController.ShowHint(_menuButtonText, ToolTipLocation.TopRight, new Point(glyph.Left, glyph.Top));
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
		_menu = new DXPopupMenu { IsRightToLeft = RightToLeft == RightToLeft.Yes };
		foreach (var item in items)
		{
			var menuItem = new DXMenuItem(item.Text, (_, _) => item.Execute())
			{
				BeginGroup = item.BeginGroup,
				Enabled = item.Enabled
			};
			if (item.Image is not null)
				menuItem.ImageOptions.Image = item.Image;
			else if (CreateMenuImage(item.Kind) is { } svgImage)
				menuItem.ImageOptions.SvgImage = svgImage;
			_menu.Items.Add(menuItem);
		}

		// A BarManager draws the menu with the active skin; without one, DevExpress falls back to an unskinned
		// WinForms context menu. It also closes the menu on clicks outside of it, but only within its form.
		var owner = _menuGlyph;
		GetMenuManager().ShowPopupMenu(_menu, owner, new Point(0, owner.Height));
	}

	/// <summary>
	/// Gets the skinned menu manager of the host form, or the input's own manager bound to that form.
	/// </summary>
	private IDXMenuManager GetMenuManager()
	{
		if (MenuManagerHelper.FindMenuManager(this) is { } hostManager && !MenuManagerHelper.IsStandardContextMenuManager(hostManager))
			return hostManager;

		var form = FindForm();
		if (form is not null && _menuManager.Form != form)
			_menuManager.Form = form;

		return _menuManager;
	}

	private static SvgImage? CreateMenuImage(ChatInputMenuItemKind kind) => kind switch
	{
		ChatInputMenuItemKind.AttachFiles => FluentIcons.Attach,
		_ => null
	};

	/// <inheritdoc />
	protected override void Dispose(bool disposing)
	{
		if (disposing)
		{
			_menu?.Dispose();
			_toolTipController.Dispose();
			_menuManager.Dispose();
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

		ApplyPanelAppearance(_contentPanel);
		_contentLayout.Font = Font;
		_contentLayout.ForeColor = ForeColor;
		_contentLayout.BackColor = BackColor;
		_attachmentPanel.Font = Font;
		_attachmentPanel.ForeColor = ForeColor;
		_attachmentPanel.BackColor = BackColor;

		ApplyEditorAppearance();

		foreach (var tile in _attachmentPanel.Controls.OfType<AttachmentCard>())
			tile.ApplyPresentation(null);
	}

	private void ApplyPanelAppearance(PanelControl panel)
	{
		panel.Appearance.Font = Font;
		panel.Appearance.ForeColor = ForeColor;
		panel.Appearance.BackColor = BackColor;
		panel.Appearance.Options.UseFont = true;
		panel.Appearance.Options.UseForeColor = true;
		panel.Appearance.Options.UseBackColor = true;
	}

	private void ApplyEditorAppearance()
	{
		if (_textBox is null)
			return;

		_textBox.Properties.ReadOnly = false;
		_textBox.Properties.BorderStyle = EditorBorderStyle switch
		{
			BorderStyle.None => DevExpress.XtraEditors.Controls.BorderStyles.NoBorder,
			BorderStyle.FixedSingle => DevExpress.XtraEditors.Controls.BorderStyles.Simple,
			_ => DevExpress.XtraEditors.Controls.BorderStyles.Default
		};
		_textBox.Properties.NullValuePrompt = PlaceholderText;
		_textBox.Properties.NullValuePromptShowForEmptyValue = true;
		ApplyEditorAppearance(_textBox.Properties.Appearance);
		ApplyEditorAppearance(_textBox.Properties.AppearanceFocused);
	}

	private void ApplyEditorAppearance(AppearanceObject appearance)
	{
		appearance.Font = Font;
		appearance.Options.UseFont = true;

		appearance.ForeColor = EditorForeColor;
		appearance.Options.UseForeColor = !EditorForeColor.IsEmpty;
		appearance.BackColor = EditorBackColor;
		appearance.Options.UseBackColor = !EditorBackColor.IsEmpty;
	}

	private bool IsAvailable() => !(Disposing || IsDisposed);

	private sealed class PasteAwareMemoEdit : MemoEdit
	{
		private const int WM_PASTE = 0x0302;

		public event EventHandler<AttachmentPasteRequestedEventArgs>? AttachmentPasteRequested;

		/// <summary>Gets the skinned background color the editor paints with.</summary>
		internal Color PaintBackColor => ViewInfo.PaintAppearance.BackColor;

		/// <summary>Gets the skinned text color the editor paints with.</summary>
		internal Color PaintForeColor => ViewInfo.PaintAppearance.ForeColor;

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

		protected override void WndProc(ref Message m)
		{
			if (m.Msg == WM_PASTE && (TryRequestAttachmentPaste() || TryPasteNormalizedText()))
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

		/// <summary>
		/// Pastes text from other applications that only separate lines with LF, which the editor would show on a single line.
		/// </summary>
		private bool TryPasteNormalizedText()
		{
			if (Properties.ReadOnly || ClipboardText.GetTextToNormalize() is not { } text)
				return false;

			var current = Text ?? string.Empty;
			var start = Math.Clamp(SelectionStart, 0, current.Length);
			var length = Math.Clamp(SelectionLength, 0, current.Length - start);
			Text = current.Remove(start, length).Insert(start, text);
			SelectionStart = start + text.Length;
			SelectionLength = 0;
			ScrollToCaret();
			return true;
		}
	}
}
