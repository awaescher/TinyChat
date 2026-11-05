namespace TinyChat;

/// <summary>A borderless file icon or image preview with hover actions and a localizable tooltip.</summary>
internal class AttachmentCard : Panel
{
	private readonly ChatFileAttachment? _attachment;
	private readonly string _fileName;
	private readonly ChatNotice? _inputNotice;
	private readonly Func<ChatNotice, string>? _localizeNotice;
	private readonly Control _surface;
	private readonly Control _preview;
	private readonly Control? _remove;
	private readonly IAttachmentToolTip _toolTip;
	private readonly System.Windows.Forms.Timer _hoverTimer = new() { Interval = 100 };
	private readonly CancellationTokenSource _previewCancellation = new();
	private readonly Action? _removeAction;
	private Image? _sourceImage;
	private ChatNoticeSeverity? _noticeSeverity;
	private bool _previewDirty = true;
	private Size _renderedSize;
	private bool _isDisposing;
	private bool _isRemoving;
	private Task? _previewLoadTask;

	internal AttachmentCard(ChatFileAttachment attachment, Action? remove = null)
		: this(attachment, new Panel(), CreatePreview(attachment), CreateRemoveButton(remove), new AttachmentToolTip(), remove) { }

	internal AttachmentCard(ChatNotice notice, Func<ChatNotice, string> localize, Action? remove)
		: this(null, new Panel(), CreatePreview(null), CreateRemoveButton(remove), new AttachmentToolTip(), remove, notice, localize) { }

	protected AttachmentCard(ChatFileAttachment? attachment, Control surface, Control preview, Control? removeButton, IAttachmentToolTip toolTip, Action? remove, ChatNotice? inputNotice = null, Func<ChatNotice, string>? localize = null)
	{
		_attachment = attachment;
		_inputNotice = inputNotice;
		_localizeNotice = localize;
		_fileName = attachment?.Name ?? inputNotice?.FileName ?? string.Empty;
		_surface = surface;
		_preview = preview;
		_remove = removeButton;
		_toolTip = toolTip;
		_removeAction = remove;
		AccessibleName = _fileName;
		Margin = new Padding(0, 0, 6, 4);
		TabStop = true;
		Size = new Size(48, 48);
		_surface.Dock = DockStyle.Fill;
		Controls.Add(_surface);
		_surface.Controls.Add(_preview);
		if (_remove is not null)
		{
			_surface.Controls.Add(_remove);
			_remove.Visible = false;
			_remove.Click += (_, _) => RemoveAttachment();
			_remove.BringToFront();
		}
		foreach (var element in new[] { _surface, _preview, _remove }.OfType<Control>().Prepend(this))
		{
			WireHover(element);
			if (_attachment is not null && !ReferenceEquals(element, _remove))
			{
				element.Cursor = Cursors.Hand;
				element.Click += (_, _) => OpenAttachment();
			}
		}
		_hoverTimer.Tick += HoverTimerTick;
		ApplyPresentation(null);
	}

	protected Control PreviewControl => _preview;
	internal ChatNotice? InputNotice => _inputNotice;
	internal ChatNoticeSeverity? NoticeSeverity => _noticeSeverity;
	internal Task PreviewLoadCompletion => _previewLoadTask ?? Task.CompletedTask;
	private bool IsUnavailable => _isDisposing || _isRemoving || IsDisposed || Disposing;

	internal static string FormatSize(long bytes) => bytes < 1024 ? $"{bytes:N0} B" : bytes < 1024 * 1024 ? $"{bytes / 1024d:N0} KB" : $"{bytes / (1024d * 1024):N1} MB";

	private static Control? CreateRemoveButton(Action? remove)
	{
		if (remove is null)
			return null;
		var font = ChatIcons.CreateFont(9f);
		var button = new Button { FlatStyle = FlatStyle.Flat, Text = ChatIcons.IconFontName is null ? "×" : ChatIcons.StatusRejected, Font = font, Cursor = Cursors.Hand, TabStop = false };
		button.FlatAppearance.BorderSize = 0;
		button.Disposed += (_, _) => font.Dispose();
		return button;
	}

	private static Control CreatePreview(ChatFileAttachment? attachment)
	{
		var picture = new PictureBox
		{
			Image = attachment is { IsImage: true } ? FileAttachmentImageFactory.Create(attachment) : SystemIcons.Application.ToBitmap(),
			SizeMode = PictureBoxSizeMode.Zoom,
			TabStop = false
		};
		picture.Disposed += (_, _) => picture.Image?.Dispose();
		return picture;
	}

	protected virtual Image? GetPreviewImage() => ((PictureBox)_preview).Image;

	protected virtual void ReplacePreviewImage(Image image)
	{
		var picture = (PictureBox)_preview;
		var oldImage = picture.Image;
		picture.Image = image;
		oldImage?.Dispose();
	}

	private async Task LoadFileIconAsync()
	{
		var cancellationToken = _previewCancellation.Token;
		Image? image = null;
		try
		{
			image = await WindowsFileIconFactory.CreateAsync(_fileName).ConfigureAwait(false);
			await this.InvokeAsync(() =>
			{
				if (IsUnavailable)
					return;
				_sourceImage?.Dispose();
				_sourceImage = image;
				image = null;
				_previewDirty = true;
				RenderPreview();
			}, cancellationToken).ConfigureAwait(false);
		}
		catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { }
		catch (InvalidOperationException) when (IsUnavailable || !IsHandleCreated) { }
		finally
		{
			image?.Dispose();
		}
	}

	internal void ApplyPresentation(ChatControl? owner)
	{
		if (IsUnavailable)
			return;
		for (Control? parent = Parent; owner is null && parent is not null; parent = parent.Parent)
			owner = parent as ChatControl;
		var appearance = owner?.AttachmentAppearance;
		Width = Scale(appearance?.CardWidth ?? 48);
		MinimumSize = new Size(0, Scale(48));
		Height = MinimumSize.Height;
		_surface.BackColor = appearance is { BackColor.IsEmpty: false } ? appearance.BackColor : BackColor;
		_surface.ForeColor = appearance is { ForeColor.IsEmpty: false } ? appearance.ForeColor : ForeColor;
		_preview.BackColor = _surface.BackColor;
		var extension = Path.GetExtension(_fileName).TrimStart('.').ToUpperInvariant();
		var text = string.Join(" · ", new[] { extension, _attachment is null ? string.Empty : FormatSize(_attachment.Data.Length) }.Where(value => value.Length > 0));
		if (_attachment is not null)
			text += Environment.NewLine + (owner?.DisplayTexts.OpenAttachment ?? ChatLocalization.GetString("OpenAttachment"));
		_preview.AccessibleName = _fileName;
		IReadOnlyList<ChatNotice> notices = _inputNotice is not null ? [_inputNotice] : _attachment is not null ? owner?.GetAttachmentNotices(_attachment) ?? [] : [];
		ChatNoticeSeverity? severity = notices.Count == 0 ? null : notices.Any(notice => notice.Severity == ChatNoticeSeverity.Error) ? ChatNoticeSeverity.Error : ChatNoticeSeverity.Warning;
		if (_noticeSeverity != severity)
		{
			_noticeSeverity = severity;
			_previewDirty = true;
		}
		if (notices.Count > 0)
			text += Environment.NewLine + string.Join(Environment.NewLine, notices.Select(notice => owner?.NoticeTextProvider(notice) ?? _localizeNotice?.Invoke(notice) ?? ChatLocalization.Format(notice)));
		AccessibleDescription = text;
		_preview.AccessibleDescription = text;
		foreach (var control in new[] { this, _surface, _preview })
			_toolTip.Set(control, _fileName, text);
		if (_remove is not null)
		{
			_remove.AccessibleName = owner?.DisplayTexts.RemoveAttachment ?? ChatLocalization.GetString("RemoveAttachment");
			_toolTip.Set(_remove, string.Empty, _remove.AccessibleName);
		}
		ArrangeElements();
	}

	private int Scale(int pixels) => Math.Max(1, (int)Math.Round(pixels * DeviceDpi / 96f));
	private void ArrangeElements()
	{
		if (_surface is null || IsUnavailable)
			return;
		var inset = Scale(4);
		_preview.Bounds = Rectangle.FromLTRB(inset, inset, Math.Max(inset, Width - inset), Height - inset);

		if (_remove is not null)
			_remove.Bounds = new Rectangle(Width - Scale(22), 0, Scale(22), Scale(22));
		RenderPreview();
	}

	private void RenderPreview()
	{
		if (IsUnavailable || _preview.ClientSize.Width == 0 || _preview.ClientSize.Height == 0 || (!_previewDirty && _renderedSize == _preview.ClientSize))
			return;
		_sourceImage ??= GetPreviewImage() is { } initial ? new Bitmap(initial) : SystemIcons.Application.ToBitmap();
		var size = _preview.ClientSize;
		var canvas = new Bitmap(size.Width, size.Height, System.Drawing.Imaging.PixelFormat.Format32bppPArgb);
		using (var graphics = Graphics.FromImage(canvas))
		{
			graphics.Clear(Color.Transparent);
			graphics.CompositingMode = System.Drawing.Drawing2D.CompositingMode.SourceOver;
			graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
			var scale = Math.Min((float)size.Width / _sourceImage.Width, (float)size.Height / _sourceImage.Height);
			var width = Math.Max(1, (int)Math.Round(_sourceImage.Width * scale));
			var height = Math.Max(1, (int)Math.Round(_sourceImage.Height * scale));
			graphics.DrawImage(_sourceImage, new Rectangle((size.Width - width) / 2, (size.Height - height) / 2, width, height));
			if (_noticeSeverity is { } severity)
			{
				var badgeSize = new Size(Scale(16), Scale(16));
				using var badge = CreateNoticeBadge(badgeSize, severity);
				graphics.DrawImageUnscaled(badge, size.Width - badge.Width, size.Height - badge.Height);
			}
		}
		_previewDirty = false;
		_renderedSize = size;
		ReplacePreviewImage(canvas);
	}

	protected virtual Image CreateNoticeBadge(Size size, ChatNoticeSeverity severity)
	{
		var bitmap = new Bitmap(size.Width, size.Height, System.Drawing.Imaging.PixelFormat.Format32bppPArgb);
		using var graphics = Graphics.FromImage(bitmap);
		graphics.Clear(Color.Transparent);
		graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAliasGridFit;
		using var font = ChatIcons.CreateFont(size.Height * 0.6f);
		using var brush = new SolidBrush(severity == ChatNoticeSeverity.Error ? ChatIcons.ErrorColor : ChatIcons.ToolColor);
		using var format = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
		graphics.DrawString(ChatIcons.IconFontName is null ? "\u26a0" : ChatIcons.StatusFailed, font, brush, new RectangleF(PointF.Empty, size), format);
		return bitmap;
	}

	private void WireHover(Control control)
	{
		control.MouseEnter += (_, _) => UpdateHover(true);
		control.MouseLeave += (_, _) => QueueHideAction();
	}

	private void QueueHideAction()
	{
		if (!IsUnavailable && IsHandleCreated && _remove is not null)
			_hoverTimer.Start();
	}

	private void HoverTimerTick(object? sender, EventArgs e)
	{
		if (IsUnavailable)
			return;
		if (!IsHandleCreated)
		{
			_hoverTimer.Stop();
			return;
		}
		if (!RectangleToScreen(ClientRectangle).Contains(System.Windows.Forms.Cursor.Position) && !ContainsFocus)
			UpdateHover(false);
	}

	private void UpdateHover(bool hovered)
	{
		if (IsUnavailable)
			return;
		_hoverTimer.Stop();
		if (_remove is not null && !_remove.IsDisposed)
			_remove.Visible = hovered;
	}

	private void RemoveAttachment()
	{
		if (IsUnavailable || _removeAction is null)
			return;
		_isRemoving = true;
		_hoverTimer.Stop();
		try
		{
			_removeAction();
		}
		finally
		{
			_isRemoving = false;
		}
	}

	private void OpenAttachment()
	{
		if (IsUnavailable || _attachment is null)
			return;
		try
		{
			_attachment.OpenWithDefaultApplication();
		}
		catch (System.ComponentModel.Win32Exception) { }
		catch (IOException) { }
		catch (UnauthorizedAccessException) { }
	}

	protected override void OnHandleCreated(EventArgs e)
	{
		base.OnHandleCreated(e);
		ApplyPresentation(null);
		if (!IsUnavailable && _attachment is not { IsImage: true })
			_previewLoadTask ??= LoadFileIconAsync();
	}

	protected override void OnHandleDestroyed(EventArgs e)
	{
		if (!IsUnavailable)
			_hoverTimer.Stop();
		_previewLoadTask = null;
		base.OnHandleDestroyed(e);
	}

	protected override void OnDpiChangedAfterParent(EventArgs e)
	{
		base.OnDpiChangedAfterParent(e);
		ApplyPresentation(null);
	}

	protected override void OnParentChanged(EventArgs e)
	{
		base.OnParentChanged(e);
		if (_surface is not null)
			ApplyPresentation(null);
	}

	protected override void OnFontChanged(EventArgs e)
	{
		base.OnFontChanged(e);
		if (_surface is not null)
			ApplyPresentation(null);
	}

	protected override void OnBackColorChanged(EventArgs e)
	{
		base.OnBackColorChanged(e);
		if (_surface is not null)
			ApplyPresentation(null);
	}

	protected override void OnForeColorChanged(EventArgs e)
	{
		base.OnForeColorChanged(e);
		if (_surface is not null)
			ApplyPresentation(null);
	}

	protected override void OnLayout(LayoutEventArgs e)
	{
		base.OnLayout(e);
		ArrangeElements();
	}

	protected override void OnGotFocus(EventArgs e)
	{
		base.OnGotFocus(e);
		UpdateHover(true);
	}

	protected override void OnLostFocus(EventArgs e)
	{
		base.OnLostFocus(e);
		QueueHideAction();
	}


	protected override void OnKeyDown(KeyEventArgs e)
	{
		base.OnKeyDown(e);
		if (e.KeyCode == Keys.Delete && _removeAction is not null)
		{
			e.Handled = true;
			RemoveAttachment();
		}
		else if (e.KeyCode == Keys.Enter)
		{
			e.Handled = true;
			OpenAttachment();
		}
	}

	protected override void Dispose(bool disposing)
	{
		if (disposing && !_isDisposing)
		{
			_isDisposing = true;
			_hoverTimer.Stop();
			_hoverTimer.Tick -= HoverTimerTick;
			_hoverTimer.Dispose();
			_previewCancellation.Cancel();
			_previewCancellation.Dispose();
			_toolTip.Dispose();
			_sourceImage?.Dispose();
		}
		base.Dispose(disposing);
	}
}
