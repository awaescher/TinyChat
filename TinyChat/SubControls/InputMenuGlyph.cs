using System.Drawing.Drawing2D;

namespace TinyChat;

/// <summary>
/// The faint "+" of the input menu, laid over the lower left corner of the editor like the send button.
/// It paints with the editor's colors, which come from the active skin in DevExpress, and has no frame of its own.
/// </summary>
internal sealed class InputMenuGlyph : Control
{
	private readonly Func<Color> _backColor;
	private readonly Func<Color> _foreColor;
	private bool _hovered;
	private bool _pressed;

	public InputMenuGlyph(Func<Color> backColor, Func<Color> foreColor)
	{
		_backColor = backColor;
		_foreColor = foreColor;
		SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer | ControlStyles.ResizeRedraw | ControlStyles.UserPaint, true);
		SetStyle(ControlStyles.Selectable, false);
		AccessibleRole = AccessibleRole.PushButton;
		Cursor = Cursors.Hand;
		TabStop = false;
	}

	/// <summary>Occurs when the pointer starts or stops resting over the symbol.</summary>
	public event EventHandler? HoverChanged;

	/// <summary>Gets whether the pointer rests over the symbol.</summary>
	public bool Hovered => _hovered;

	protected override void OnMouseEnter(EventArgs e)
	{
		base.OnMouseEnter(e);
		SetHovered(true);
	}

	protected override void OnMouseLeave(EventArgs e)
	{
		_pressed = false;
		SetHovered(false);
		base.OnMouseLeave(e);
	}

	protected override void OnMouseDown(MouseEventArgs e)
	{
		base.OnMouseDown(e);
		if (e.Button != MouseButtons.Left)
			return;

		_pressed = true;
		Invalidate();
	}

	protected override void OnMouseUp(MouseEventArgs e)
	{
		base.OnMouseUp(e);
		_pressed = false;
		Invalidate();
	}

	protected override void OnPaint(PaintEventArgs e)
	{
		var backColor = _backColor();
		var foreColor = _foreColor();
		e.Graphics.Clear(backColor);
		e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

		var bounds = ClientRectangle;
		var active = _hovered || _pressed;
		if (active)
		{
			using var highlight = new SolidBrush(Blend(foreColor, backColor, _pressed ? 0.22f : 0.12f));
			e.Graphics.FillEllipse(highlight, bounds.Left, bounds.Top, bounds.Width - 1, bounds.Height - 1);
		}

		var length = bounds.Width * (active ? 0.56f : 0.42f);
		var center = new PointF(bounds.Left + (bounds.Width - 1) / 2f, bounds.Top + (bounds.Height - 1) / 2f);
		var color = active ? foreColor : Blend(foreColor, backColor, 0.4f);
		using var pen = new Pen(color, Math.Max(1f, bounds.Width / 13f)) { StartCap = LineCap.Round, EndCap = LineCap.Round };
		e.Graphics.DrawLine(pen, center.X - length / 2, center.Y, center.X + length / 2, center.Y);
		e.Graphics.DrawLine(pen, center.X, center.Y - length / 2, center.X, center.Y + length / 2);
	}

	private void SetHovered(bool hovered)
	{
		if (_hovered == hovered)
			return;

		_hovered = hovered;
		Invalidate();
		HoverChanged?.Invoke(this, EventArgs.Empty);
	}

	private static Color Blend(Color foreground, Color background, float amount) => Color.FromArgb(
		(int)Math.Round(background.R + (foreground.R - background.R) * amount),
		(int)Math.Round(background.G + (foreground.G - background.G) * amount),
		(int)Math.Round(background.B + (foreground.B - background.B) * amount));
}
