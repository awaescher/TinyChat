using System.ComponentModel;
using System.Net;
using DevExpress.Utils;
using DevExpress.XtraEditors;

namespace TinyChat;

/// <summary>
/// DevExpress implementation of the empty-chat welcome surface.
/// </summary>
internal sealed class DXWelcomeControl : PanelControl
{
	private const string ROBOT_ART = "●\n┌─┴─┐\n◉‿◉\n└───┘";
	private const string ROBOT_FONT_NAME = "Segoe UI Symbol";

	private readonly LabelControl _label;
	private ContentAlignment _textAlign = ContentAlignment.MiddleCenter;

	/// <summary>
	/// Gets or sets the horizontal and vertical alignment of the welcome text.
	/// </summary>
	[Browsable(false)]
	[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
	public ContentAlignment TextAlign
	{
		get => _textAlign;
		set
		{
			if (!Enum.IsDefined(value))
				throw new InvalidEnumArgumentException(nameof(value), (int)value, typeof(ContentAlignment));

			_textAlign = value;
			ApplyTextAlignment();
		}
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="DXWelcomeControl"/> class.
	/// </summary>
	public DXWelcomeControl()
	{
		BorderStyle = DevExpress.XtraEditors.Controls.BorderStyles.NoBorder;
		_label = new LabelControl
		{
			AllowHtmlString = true,
			AutoSizeMode = LabelAutoSizeMode.None,
			Dock = DockStyle.Fill,
			UseMnemonic = false
		};
		_label.Appearance.TextOptions.WordWrap = WordWrap.Wrap;
		_label.Appearance.Options.UseTextOptions = true;
		Controls.Add(_label);
		ApplyTextAlignment();
		ApplyAppearance();
	}

	/// <summary>
	/// Sets plain welcome text and uses a symbol font only for the built-in robot artwork.
	/// </summary>
	/// <param name="text">The unformatted welcome text.</param>
	internal void SetWelcomeText(string text)
	{
		var plainText = text ?? string.Empty;
		_label.AccessibleDescription = plainText;
		_label.Text = FormatWelcomeText(plainText);
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

	private void ApplyAppearance()
	{
		if (_label is null)
			return;

		_label.Appearance.Font = Font;
		_label.Appearance.ForeColor = ForeColor;
		_label.Appearance.BackColor = BackColor;
		_label.Appearance.Options.UseFont = true;
		_label.Appearance.Options.UseForeColor = true;
		_label.Appearance.Options.UseBackColor = true;
	}

	private void ApplyTextAlignment()
	{
		if (_label is null)
			return;

		_label.Appearance.TextOptions.HAlignment = TextAlign switch
		{
			ContentAlignment.TopLeft or ContentAlignment.MiddleLeft or ContentAlignment.BottomLeft => HorzAlignment.Near,
			ContentAlignment.TopCenter or ContentAlignment.MiddleCenter or ContentAlignment.BottomCenter => HorzAlignment.Center,
			ContentAlignment.TopRight or ContentAlignment.MiddleRight or ContentAlignment.BottomRight => HorzAlignment.Far,
			_ => throw new InvalidEnumArgumentException(nameof(TextAlign), (int)TextAlign, typeof(ContentAlignment))
		};
		_label.Appearance.TextOptions.VAlignment = TextAlign switch
		{
			ContentAlignment.TopLeft or ContentAlignment.TopCenter or ContentAlignment.TopRight => VertAlignment.Top,
			ContentAlignment.MiddleLeft or ContentAlignment.MiddleCenter or ContentAlignment.MiddleRight => VertAlignment.Center,
			ContentAlignment.BottomLeft or ContentAlignment.BottomCenter or ContentAlignment.BottomRight => VertAlignment.Bottom,
			_ => throw new InvalidEnumArgumentException(nameof(TextAlign), (int)TextAlign, typeof(ContentAlignment))
		};
		_label.Appearance.Options.UseTextOptions = true;
	}

	private static string FormatWelcomeText(string text)
	{
		if (!text.StartsWith(ROBOT_ART, StringComparison.Ordinal))
			return WebUtility.HtmlEncode(text);

		var encodedRobot = WebUtility.HtmlEncode(ROBOT_ART);
		var encodedMessage = WebUtility.HtmlEncode(text[ROBOT_ART.Length..]);
		return $"<font=\"{ROBOT_FONT_NAME}\">{encodedRobot}</font>{encodedMessage}";
	}
}
