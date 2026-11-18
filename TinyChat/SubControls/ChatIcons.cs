using System.Drawing.Text;

namespace TinyChat;

/// <summary>
/// Provides the WinForms icons of tool, skill, reasoning and result rows and of the send button.
/// The installed Windows icon font is preferred, like the DevExpress font icons: Segoe Fluent Icons (Windows 11)
/// before Segoe MDL2 Assets (Windows 10, Windows Server 2016 and later). The font is detected instead of the
/// Windows version, so systems without an icon font fall back to symbol characters.
/// </summary>
internal static class ChatIcons
{
	private static readonly string[] _iconFontNames = ["Segoe Fluent Icons", "Segoe MDL2 Assets"];
	private static readonly Lazy<string?> _iconFontName = new(FindIconFontName);

	/// <summary>Gets the name of the installed icon font, or <see langword="null"/> if none is installed.</summary>
	internal static string? IconFontName => _iconFontName.Value;

	internal static string Tool => IconFontName is null ? "\u26a1" : "\ue945";

	internal static string Skill => IconFontName is null ? "\U0001f4d6" : "\ue736";

	internal static string Reasoning => IconFontName is null ? "\U0001f4ad" : "\uea80";

	internal static string Result => IconFontName is null ? "\U0001f86a" : "\ue72a";

	internal static string Send => IconFontName is null ? "\u27A4" : "\ue725";

	internal static string Stop => IconFontName is null ? "\u25A0" : "\ue73b";

	internal static string Attach => IconFontName is null ? "\U0001f4ce" : "\ue723";

	internal static string Copy => IconFontName is null ? "\u29C9" : "\ue8c8";

	internal static string Revert => IconFontName is null ? "\u21B6" : "\ue7a7";

	internal static string ShowFullText => IconFontName is null ? "\u2197" : "\ue8a7";

	internal static string StatusSucceeded => "\ue73e";

	internal static string StatusFailed => "\ue814";

	internal static string StatusRejected => "\ue711";

	/// <summary>Gets the accent colors used for the icons, matching the DevExpress Fluent icons.</summary>
	internal static readonly Color ToolColor = Color.FromArgb(0xFF, 0xB1, 0x15);
	internal static readonly Color ReasoningColor = ToolColor;
	internal static readonly Color SkillColor = Color.FromArgb(0x11, 0x77, 0xD7);
	internal static readonly Color ResultColor = Color.FromArgb(0x03, 0x9C, 0x23);
	internal static readonly Color ErrorColor = Color.FromArgb(0xD1, 0x1C, 0x1C);

	/// <summary>
	/// Returns the accent color for an icon glyph when an icon font is used. The fallback symbols keep
	/// the text color, matching the previous rendering.
	/// </summary>
	internal static Color GetIconColor(Color accentColor, Color textColor) => IconFontName is null ? textColor : accentColor;

	/// <summary>
	/// Creates the font used to render the icon glyphs.
	/// </summary>
	internal static Font CreateFont(float size) => new(IconFontName ?? "Arial", size, FontStyle.Regular, GraphicsUnit.Point);

	/// <summary>
	/// Draws an icon glyph into a bitmap, for controls such as menu items that show images instead of text.
	/// </summary>
	internal static Bitmap CreateImage(string glyph, int pixelSize, Color color)
	{
		var bitmap = new Bitmap(pixelSize, pixelSize);
		using var graphics = Graphics.FromImage(bitmap);
		using var font = new Font(IconFontName ?? "Segoe UI Symbol", pixelSize * 0.75f, FontStyle.Regular, GraphicsUnit.Pixel);
		graphics.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;
		using var brush = new SolidBrush(color);
		using var format = new StringFormat(StringFormat.GenericTypographic) { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
		graphics.DrawString(glyph, font, brush, new RectangleF(0, 0, pixelSize, pixelSize), format);
		return bitmap;
	}

	private static string? FindIconFontName()
	{
		try
		{
			using var fonts = new InstalledFontCollection();
			var installedNames = fonts.Families.Select(family => family.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);
			return _iconFontNames.FirstOrDefault(installedNames.Contains);
		}
		catch (Exception)
		{
			return null;
		}
	}
}
