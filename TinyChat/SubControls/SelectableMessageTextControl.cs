using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;

namespace TinyChat;

/// <summary>
/// Displays selectable, read-only message text and grows vertically to fit its wrapped content.
/// </summary>
internal sealed class SelectableMessageTextControl : TextBox
{
	private const int CONTENT_PADDING = 2;
	private const int WM_MOUSEWHEEL = 0x020A;
	private const int EM_GETLINECOUNT = 0x00BA;
	private const int EM_GETMARGINS = 0x00D4;

	private bool _adjustingHeight;
	private int _contentRevision;
	private int _lastMeasuredWidth = -1;
	private int _lastMeasuredRevision = -1;
	private bool _lastMeasurementWasRendered;
	private int _preferredContentHeight;

	/// <summary>
	/// Raised immediately before the content height changes.
	/// </summary>
	internal event EventHandler? ContentHeightChanging;

	/// <summary>
	/// Raised after the content height changed and requested a parent reflow.
	/// </summary>
	internal event EventHandler? ContentHeightChanged;

	/// <summary>
	/// Initializes a new instance of the <see cref="SelectableMessageTextControl"/> class.
	/// </summary>
	public SelectableMessageTextControl()
	{
		AutoSize = false;
		BorderStyle = BorderStyle.None;
		Cursor = Cursors.IBeam;
		HideSelection = true;
		Margin = Padding.Empty;
		Multiline = true;
		ReadOnly = true;
		ScrollBars = ScrollBars.None;
		ShortcutsEnabled = true;
		TabStop = true;
		WordWrap = true;
		_preferredContentHeight = Font.Height + CONTENT_PADDING;
		MinimumSize = new Size(0, _preferredContentHeight);
		Height = _preferredContentHeight;
	}

	/// <summary>
	/// Gets or sets the text. Line breaks are normalized to <c>\r\n</c>, because the native multiline
	/// edit control renders a bare <c>\n</c> without breaking the line while the height measurement
	/// counts it, which hid paragraphs and left empty space below the text.
	/// </summary>
	[AllowNull]
	public override string Text
	{
		get => base.Text;
		set
		{
			var normalizedText = (value ?? string.Empty).ReplaceLineEndings("\r\n");
			if (!string.Equals(base.Text, normalizedText, StringComparison.Ordinal))
				base.Text = normalizedText;
		}
	}

	/// <inheritdoc />
	public override Size GetPreferredSize(Size proposedSize)
	{
		var preferredSize = base.GetPreferredSize(proposedSize);

		// AutoSize parents (the message panels) drive their own height purely from the preferred size
		// of their docked children. If we returned a cached height here it could be stale for the
		// width the parent is currently laying out against, which leaves the panel too short and
		// clips/overlaps the following message. Measure against the width the parent proposes (falling
		// back to our current width) so the reported height is always correct for that width.
		var width = proposedSize.Width > 0 ? proposedSize.Width : ClientSize.Width;
		if (width <= 0)
			width = MaximumSize.Width;

		var height = width > 0
			? MeasureContentHeight(width)
			: _preferredContentHeight;

		return new Size(
			preferredSize.Width,
			Math.Max(Font.Height + CONTENT_PADDING, height));
	}

	/// <summary>
	/// Recalculates the content height after the parent layout has assigned the final width.
	/// </summary>
	/// <remarks>
	/// The cached measurement is invalidated first so the height is always re-measured against the
	/// width the layout pass just assigned. Nested layout passes (for example when the surrounding
	/// history panel is resized) can reuse a previously seen width and would otherwise keep a stale,
	/// wrongly sized height, leaving gaps or overflowing text after the control is resized.
	/// </remarks>
	internal void RefreshContentHeight()
	{
		_lastMeasuredWidth = -1;
		_lastMeasuredRevision = -1;
		UpdateContentHeight();
	}

	/// <summary>
	/// Re-measures the content height only when the assigned width no longer matches the last
	/// measurement, keeping layout passes cheap while still reacting to resizes.
	/// </summary>
	internal void EnsureContentHeightForCurrentWidth()
	{
		if (Visible && !IsMeasurementCurrent(ClientSize.Width))
			UpdateContentHeight();
	}

	/// <summary>
	/// Re-measures the content height against an explicitly supplied width. This is used by the
	/// hosting panel during its layout pass, before the docked control's own <see cref="Control.ClientSize"/>
	/// has been updated, so the parent can position the docked siblings with the correct height in a
	/// single pass instead of leaving them overlapping.
	/// </summary>
	/// <param name="availableWidth">The width the control will occupy after layout.</param>
	internal void EnsureContentHeightForWidth(int availableWidth)
	{
		if (!Visible || availableWidth <= 0 || IsMeasurementCurrent(availableWidth))
			return;

		SetContentHeight(Measure(availableWidth));
	}

	/// <inheritdoc />
	protected override void OnFontChanged(EventArgs e)
	{
		base.OnFontChanged(e);
		_contentRevision++;
		if (Visible)
			UpdateContentHeight();
	}

	/// <inheritdoc />
	protected override void OnHandleCreated(EventArgs e)
	{
		base.OnHandleCreated(e);
		UpdateContentHeight();
	}

	/// <inheritdoc />
	protected override void OnSizeChanged(EventArgs e)
	{
		base.OnSizeChanged(e);

		if (_adjustingHeight)
			return;
		if (!Visible)
		{
			_lastMeasuredWidth = -1;
			return;
		}

		if (!IsMeasurementCurrent(ClientSize.Width))
			UpdateContentHeight();
		else if (Height != _preferredContentHeight)
			SetContentHeight(_preferredContentHeight);
	}

	/// <inheritdoc />
	protected override void OnTextChanged(EventArgs e)
	{
		base.OnTextChanged(e);
		_contentRevision++;
		if (Visible)
			UpdateContentHeight();
		else
			_lastMeasuredRevision = -1;
	}

	/// <inheritdoc />
	protected override void OnVisibleChanged(EventArgs e)
	{
		base.OnVisibleChanged(e);
		if (Visible)
			UpdateContentHeight();
	}

	/// <summary>
	/// Forwards mouse wheel input to the scrollable history. The native edit control consumes the
	/// wheel even without scroll bars, so the history could not be scrolled while hovering text.
	/// </summary>
	protected override void WndProc(ref Message m)
	{
		if (m.Msg == WM_MOUSEWHEEL && FindScrollableAncestor() is { } scrollableAncestor)
		{
			SendMessage(scrollableAncestor.Handle, m.Msg, m.WParam, m.LParam);
			return;
		}

		base.WndProc(ref m);
	}

	private Control? FindScrollableAncestor()
	{
		for (var parent = Parent; parent is not null; parent = parent.Parent)
		{
			if (parent is ScrollableControl { AutoScroll: true } && parent.IsHandleCreated)
				return parent;
		}

		return null;
	}

	private void UpdateContentHeight()
	{
		if (_adjustingHeight)
			return;

		var availableWidth = ClientSize.Width;
		if (availableWidth <= 0)
			availableWidth = MaximumSize.Width;
		if (availableWidth <= 0)
			return;
		if (IsMeasurementCurrent(availableWidth))
		{
			if (Height != _preferredContentHeight)
				SetContentHeight(_preferredContentHeight);
			return;
		}

		SetContentHeight(Measure(availableWidth));
	}

	/// <summary>
	/// A measurement stays valid for the same width and text, unless it was only predicted and the
	/// rendered line count of the native control can be read for that width now.
	/// </summary>
	private bool IsMeasurementCurrent(int availableWidth)
	{
		return _lastMeasuredWidth == availableWidth &&
			_lastMeasuredRevision == _contentRevision &&
			(_lastMeasurementWasRendered || !CanReadRenderedLines(availableWidth));
	}

	private int Measure(int availableWidth)
	{
		_lastMeasuredWidth = availableWidth;
		_lastMeasuredRevision = _contentRevision;
		_lastMeasurementWasRendered = CanReadRenderedLines(availableWidth);
		return MeasureContentHeight(availableWidth);
	}

	private bool CanReadRenderedLines(int availableWidth) => IsHandleCreated && availableWidth == ClientSize.Width;

	private int MeasureContentHeight(int availableWidth)
	{
		// Once the control has its final width the native edit control's own wrapping is authoritative;
		// a prediction can only approximate where it breaks words such as "research-summary.md".
		var lineCount = CanReadRenderedLines(availableWidth)
			? Math.Max(1, (int)SendMessage(Handle, EM_GETLINECOUNT, IntPtr.Zero, IntPtr.Zero))
			: PredictLineCount(availableWidth);

		// The first line keeps the full font height so descenders are never clipped; every further line
		// only adds the edit control's own line step.
		return Font.Height + ((lineCount - 1) * GetRenderedLineHeight()) + CONTENT_PADDING;
	}

	/// <summary>
	/// Gets the distance between two rendered lines. The edit control steps its lines by the font's
	/// cell height, which is smaller than <see cref="Control.Font"/>'s <c>Height</c>; using the latter
	/// left a growing gap below every multi-line text.
	/// </summary>
	private int GetRenderedLineHeight()
	{
		if (IsHandleCreated && TextLength > 0 && GetLineFromCharIndex(TextLength) > 0)
		{
			var firstLine = GetPositionFromCharIndex(0).Y;
			var lastLineIndex = GetLineFromCharIndex(TextLength);
			var lastLine = GetPositionFromCharIndex(GetFirstCharIndexFromLine(lastLineIndex)).Y;
			if (lastLine > firstLine)
				return (int)Math.Round((lastLine - firstLine) / (double)lastLineIndex);
		}

		return TextRenderer.MeasureText("Wg", Font, Size.Empty, TextFormatFlags.NoPadding).Height;
	}

	private int PredictLineCount(int availableWidth)
	{
		var text = string.IsNullOrEmpty(Text) ? " " : Text;
		if (text.EndsWith('\n'))
			text += " ";

		var wrapWidth = Math.Max(1, availableWidth - GetHorizontalTextMargin());
		var measuredSize = TextRenderer.MeasureText(
			text,
			Font,
			new Size(wrapWidth, int.MaxValue),
			TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix | TextFormatFlags.TextBoxControl | TextFormatFlags.WordBreak);

		// GDI's MeasureText (used above) rounds the wrapped block height down and does not account for
		// the external leading a multiline TextBox actually reserves per rendered line. Relying on that
		// raw value leaves the last line's descender clipped. We therefore derive the number of wrapped
		// lines from the measured height and size the control as an exact whole number of TextBox line
		// heights, which matches what the control paints.
		return Math.Max(1, (int)Math.Ceiling(measuredSize.Height / (double)GetRenderedLineHeight()));
	}

	private int GetHorizontalTextMargin()
	{
		if (!IsHandleCreated)
			return CONTENT_PADDING;

		var margins = (long)SendMessage(Handle, EM_GETMARGINS, IntPtr.Zero, IntPtr.Zero);
		return (int)(margins & 0xFFFF) + (int)((margins >> 16) & 0xFFFF);
	}

	[DllImport("user32.dll")]
	private static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);

	private void SetContentHeight(int height)
	{
		if (_adjustingHeight || (Height == height && _preferredContentHeight == height))
			return;

		ContentHeightChanging?.Invoke(this, EventArgs.Empty);
		try
		{
			_adjustingHeight = true;
			try
			{
				_preferredContentHeight = height;
				MinimumSize = new Size(MinimumSize.Width, height);
				Height = height;
			}
			finally
			{
				_adjustingHeight = false;
			}

			Parent?.PerformLayout(this, nameof(PreferredSize));
		}
		finally
		{
			ContentHeightChanged?.Invoke(this, EventArgs.Empty);
		}
	}
}
