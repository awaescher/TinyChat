namespace TinyChat;

/// <summary>
/// Displays selectable, read-only message text and grows vertically to fit its wrapped content.
/// </summary>
internal sealed class SelectableMessageTextControl : TextBox
{
	private const int CONTENT_PADDING = 2;

	private bool _adjustingHeight;
	private int _lastMeasuredWidth = -1;
	private int _preferredContentHeight;

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
		return new Size(
			preferredSize.Width,
			Math.Max(Font.Height + CONTENT_PADDING, _preferredContentHeight));
	}

	/// <inheritdoc />
	protected override void OnFontChanged(EventArgs e)
	{
		base.OnFontChanged(e);
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

		if (ClientSize.Width != _lastMeasuredWidth)
			UpdateContentHeight();
		else if (Height != _preferredContentHeight)
			SetContentHeight(_preferredContentHeight);
	}

	/// <inheritdoc />
	protected override void OnTextChanged(EventArgs e)
	{
		base.OnTextChanged(e);
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

		_lastMeasuredWidth = availableWidth;
		SetContentHeight(MeasureContentHeight(availableWidth));
	}

	private int MeasureContentHeight(int availableWidth)
	{
		var text = string.IsNullOrEmpty(Text) ? " " : Text;
		if (text.EndsWith('\n'))
			text += " ";

		var measuredSize = TextRenderer.MeasureText(
			text,
			Font,
			new Size(Math.Max(1, availableWidth - CONTENT_PADDING), int.MaxValue),
			TextFormatFlags.NoPadding | TextFormatFlags.NoPrefix | TextFormatFlags.TextBoxControl | TextFormatFlags.WordBreak);
		return Math.Max(Font.Height, measuredSize.Height) + CONTENT_PADDING;
	}

	private void SetContentHeight(int height)
	{
		if (_adjustingHeight || (Height == height && _preferredContentHeight == height))
			return;

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
}
