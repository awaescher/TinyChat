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
	private readonly LabelControl _label;
	private string _welcomeText = string.Empty;
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
	/// Sets the plain welcome text. It is HTML-encoded, so it is shown as typed.
	/// </summary>
	/// <param name="text">The unformatted welcome text.</param>
	internal void SetWelcomeText(string text)
	{
		_welcomeText = text ?? string.Empty;
		_label.AccessibleDescription = _welcomeText;
		ApplyWelcomeText();
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

	private void ApplyWelcomeText()
	{
		if (_label is not null)
			_label.Text = WebUtility.HtmlEncode(_welcomeText);
	}
}
