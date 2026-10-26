namespace TinyChat;

/// <summary>
/// Displays selectable, read-only message text and grows vertically to fit its wrapped content.
/// </summary>
internal sealed class SelectableMessageTextControl : TextBox
{
	private const int CONTENT_PADDING = 2;

	private bool _adjustingHeight;
	private int _contentRevision;
	private int _lastMeasuredWidth = -1;
	private int _lastMeasuredRevision = -1;
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
		if (Visible && ClientSize.Width != _lastMeasuredWidth)
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
		if (!Visible || availableWidth <= 0 || availableWidth == _lastMeasuredWidth)
			return;

		_lastMeasuredWidth = availableWidth;
		_lastMeasuredRevision = _contentRevision;
		SetContentHeight(MeasureContentHeight(availableWidth));
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

		if (ClientSize.Width != _lastMeasuredWidth)
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

	private void UpdateContentHeight()
	{
		if (_adjustingHeight)
			return;

		var availableWidth = ClientSize.Width;
		if (availableWidth <= 0)
			availableWidth = MaximumSize.Width;
		if (availableWidth <= 0)
			return;
		if (_lastMeasuredWidth == availableWidth && _lastMeasuredRevision == _contentRevision)
		{
			if (Height != _preferredContentHeight)
				SetContentHeight(_preferredContentHeight);
			return;
		}

		_lastMeasuredWidth = availableWidth;
		_lastMeasuredRevision = _contentRevision;
		SetContentHeight(MeasureContentHeight(availableWidth));
	}

	private int MeasureContentHeight(int availableWidth)
	{
		var text = string.IsNullOrEmpty(Text) ? " " : Text;
		if (text.EndsWith('\n'))
			text += " ";

		var wrapWidth = Math.Max(1, availableWidth - CONTENT_PADDING);
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
		var lineHeight = Font.Height;
		var lineCount = Math.Max(1, (int)Math.Ceiling(measuredSize.Height / (double)lineHeight));
		return (lineCount * lineHeight) + CONTENT_PADDING;
	}

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
