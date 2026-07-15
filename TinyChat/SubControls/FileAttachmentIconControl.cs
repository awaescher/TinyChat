namespace TinyChat;

/// <summary>
/// Draws a small font-independent document outline for non-image attachments.
/// </summary>
internal sealed class FileAttachmentIconControl : Control
{
	public FileAttachmentIconControl()
	{
		SetStyle(
			ControlStyles.AllPaintingInWmPaint |
			ControlStyles.OptimizedDoubleBuffer |
			ControlStyles.ResizeRedraw |
			ControlStyles.SupportsTransparentBackColor |
			ControlStyles.UserPaint,
			true);
		BackColor = Color.Transparent;
		TabStop = false;
	}

	/// <inheritdoc />
	protected override void OnPaint(PaintEventArgs e)
	{
		base.OnPaint(e);

		var iconWidth = Math.Min(16, Math.Max(8, ClientSize.Width - 10));
		var iconHeight = Math.Min(20, Math.Max(10, ClientSize.Height - 8));
		var left = (ClientSize.Width - iconWidth) / 2;
		var top = (ClientSize.Height - iconHeight) / 2;
		var right = left + iconWidth - 1;
		var bottom = top + iconHeight - 1;
		var fold = Math.Min(5, iconWidth / 3);

		e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
		using var pen = new Pen(SystemColors.GrayText, 1.2f);
		e.Graphics.DrawLines(
			pen,
			[
				new Point(left, top),
				new Point(right - fold, top),
				new Point(right, top + fold),
				new Point(right, bottom),
				new Point(left, bottom),
				new Point(left, top)
			]);
		e.Graphics.DrawLine(pen, right - fold, top, right - fold, top + fold);
		e.Graphics.DrawLine(pen, right - fold, top + fold, right, top + fold);

		var textLeft = left + 3;
		var textRight = right - 3;
		var firstLineY = top + fold + 4;
		e.Graphics.DrawLine(pen, textLeft, firstLineY, textRight, firstLineY);
		if (firstLineY + 4 < bottom)
			e.Graphics.DrawLine(pen, textLeft, firstLineY + 4, textRight, firstLineY + 4);
	}
}
