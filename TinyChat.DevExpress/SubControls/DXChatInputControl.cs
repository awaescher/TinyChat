using System.ComponentModel;
using DevExpress.Utils;
using DevExpress.Utils.Svg;
using DevExpress.XtraEditors;

namespace TinyChat;

/// <summary>
/// Provides a DevExpress-based chat input control for composing messages with optional file attachments.
/// </summary>
public class DXChatInputControl : Control, IChatInputControl, IChatAttachmentInputControl, IChatInputDraftControl, IChatInputDraftEditorControl, IChatInputLayoutControl, IChatInputAppearanceControl
{
	private const int ATTACHMENT_DISPLAY_HEIGHT = 60;
	private const int SEND_BUTTON_INSET = 6;

	private readonly List<ChatFileAttachment> _attachments = [];
	private readonly FlowLayoutPanel _attachmentPanel;
	private readonly PanelControl _contentPanel;
	private readonly TableLayoutPanel _contentLayout;
	private readonly PasteAwareMemoEdit _textBox;
	private readonly SimpleButton _sendButton;
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
			if (_contentPanel.Padding == value)
				return;

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

		_contentPanel = new PanelControl { Padding = new Padding(8), Dock = DockStyle.Fill, BorderStyle = DevExpress.XtraEditors.Controls.BorderStyles.NoBorder };
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
		PositionSendButton();
	}

	/// <inheritdoc />
	protected override void OnDpiChangedAfterParent(EventArgs e)
	{
		base.OnDpiChangedAfterParent(e);
		PositionSendButton();
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

	private Control CreateAttachmentTile(ChatFileAttachment attachment)
	{
		var tile = new PanelControl
		{
			AccessibleName = attachment.Name,
			Margin = new Padding(0, 0, 6, 0),
			Size = new Size(attachment.IsImage ? 64 : 180, 40)
		};
		var removeButton = new SimpleButton
		{
			Cursor = Cursors.Hand,
			Dock = DockStyle.Right,
			TabStop = false,
			Text = "×",
			Width = 24
		};
		removeButton.Click += (_, _) => RemoveAttachment(attachment, tile);

		tile.Controls.Add(removeButton);
		if (!attachment.IsImage)
		{
			var nameLabel = new LabelControl
			{
				AutoSizeMode = LabelAutoSizeMode.None,
				Dock = DockStyle.Fill,
				Padding = new Padding(4, 0, 0, 0),
				Text = attachment.Name
			};
			nameLabel.Appearance.TextOptions.VAlignment = DevExpress.Utils.VertAlignment.Center;
			nameLabel.Appearance.TextOptions.Trimming = DevExpress.Utils.Trimming.EllipsisCharacter;
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
		var preview = new PictureEdit
		{
			Dock = DockStyle.Left,
			EditValue = image,
			Width = 34
		};
		preview.Properties.BorderStyle = DevExpress.XtraEditors.Controls.BorderStyles.NoBorder;
		preview.Properties.SizeMode = DevExpress.XtraEditors.Controls.PictureSizeMode.Zoom;
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

		ApplyPanelAppearance(_contentPanel);
		_contentLayout.Font = Font;
		_contentLayout.ForeColor = ForeColor;
		_contentLayout.BackColor = BackColor;
		_attachmentPanel.Font = Font;
		_attachmentPanel.ForeColor = ForeColor;
		_attachmentPanel.BackColor = BackColor;

		ApplyEditorAppearance();

		foreach (var tile in _attachmentPanel.Controls.OfType<PanelControl>())
			ApplyAttachmentTileAppearance(tile);
	}

	private void ApplyAttachmentTileAppearance(PanelControl tile)
	{
		ApplyPanelAppearance(tile);

		foreach (var label in tile.Controls.OfType<LabelControl>())
		{
			label.Appearance.Font = Font;
			label.Appearance.ForeColor = ForeColor;
			label.Appearance.BackColor = BackColor;
			label.Appearance.Options.UseFont = true;
			label.Appearance.Options.UseForeColor = true;
			label.Appearance.Options.UseBackColor = true;
		}
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
