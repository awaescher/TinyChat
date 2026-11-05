using System.ComponentModel;

namespace TinyChat;

/// <summary>Designer settings shared by draft, history and tool-result attachment cards.</summary>
[TypeConverter(typeof(ExpandableObjectConverter))]
public sealed class AttachmentAppearance
{
	private int _cardWidth = 48;
	private Color _backColor = Color.Empty;
	private Color _foreColor = Color.Empty;
	internal event EventHandler? Changed;

	/// <summary>Gets or sets the preview width in logical pixels, before DPI scaling.</summary>
	[DefaultValue(48)]
	public int CardWidth
	{
		get => _cardWidth;
		set
		{
			ArgumentOutOfRangeException.ThrowIfLessThan(value, 40);
			_cardWidth = value;
			Changed?.Invoke(this, EventArgs.Empty);
		}
	}

	/// <summary>Gets or sets the card background. Empty inherits the surrounding surface or DevExpress skin.</summary>
	[DefaultValue(typeof(Color), "")]
	public Color BackColor { get => _backColor; set { _backColor = value; Changed?.Invoke(this, EventArgs.Empty); } }

	/// <summary>Gets or sets the card text color. Empty inherits the surrounding text color.</summary>
	[DefaultValue(typeof(Color), "")]
	public Color ForeColor { get => _foreColor; set { _foreColor = value; Changed?.Invoke(this, EventArgs.Empty); } }

	/// <inheritdoc />
	public override string ToString() => string.Empty;
}
