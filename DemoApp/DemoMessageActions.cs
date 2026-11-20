using System;
using System.Drawing;
using System.Drawing.Text;
using System.Linq;
using DemoApp.Properties;
using TinyChat;

namespace DemoApp;

internal static class DemoMessageActions
{
	private const string HEART_COLOR = "#C42B1C";

	// Fluent UI System Icons (MIT License, Copyright (c) 2020 Microsoft Corporation).
	// https://github.com/microsoft/fluentui-system-icons/blob/main/assets/Heart/SVG/ic_fluent_heart_16_regular.svg
	private const string HEART_SVG = $"""
		<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 16 16">
			<path d="M7.54112 3.94779C6.26943 2.6761 4.21207 2.66992 2.94588 3.93611C1.67969 5.20231 1.68587 7.25966 2.95756 8.53136L7.66505 13.2389C7.86031 13.4341 8.1769 13.4341 8.37216 13.2389L13.0552 8.55858C14.3184 7.28827 14.3144 5.23668 13.0425 3.96476C11.7686 2.69079 9.71024 2.68461 8.44178 3.95306L7.99452 4.40119L7.54112 3.94779ZM12.3461 7.85345L8.01861 12.1782L3.66467 7.82425C2.78255 6.94213 2.77827 5.51794 3.65299 4.64322C4.5277 3.76851 5.9519 3.77278 6.83402 4.6549L7.64337 5.46426C7.84197 5.66285 8.16514 5.65896 8.35889 5.45564L9.14889 4.66017C10.0259 3.78319 11.451 3.78747 12.3354 4.67187C13.2178 5.5542 13.2205 6.97415 12.3461 7.85345Z" fill="{HEART_COLOR}" />
		</svg>
		""";

	internal static string LikeMessageConfirmation =>
		Resources.ResourceManager.GetString(nameof(LikeMessageConfirmation), Resources.Culture)!;

	internal static ChatMessageAction CreateHeart(Action showConfirmation, Image? image = null)
	{
		return new ChatMessageAction(ChatMessageActionKind.Custom, showConfirmation)
		{
			SegoeFluentIcon = SegoeFluentIcons.Heart,
			SvgIcon = HEART_SVG,
			Image = image,
			ToolTipText = Resources.ResourceManager.GetString("LikeMessageToolTip", Resources.Culture)
		};
	}

	internal static Bitmap CreateHeartImage()
	{
		using var installedFonts = new InstalledFontCollection();
		var fontFamily = installedFonts.Families.FirstOrDefault(family => family.Name == "Segoe Fluent Icons")
			?? installedFonts.Families.FirstOrDefault(family => family.Name == "Segoe MDL2 Assets");
		var glyph = fontFamily is null ? "\u2661" : SegoeFluentIcons.Heart;

		var image = new Bitmap(16, 16);
		using var graphics = Graphics.FromImage(image);
		using var font = new Font(fontFamily?.Name ?? "Segoe UI Symbol", 13f, GraphicsUnit.Pixel);
		using var brush = new SolidBrush(ColorTranslator.FromHtml(HEART_COLOR));
		using var format = new StringFormat(StringFormat.GenericTypographic)
		{
			Alignment = StringAlignment.Center,
			LineAlignment = StringAlignment.Center
		};
		graphics.TextRenderingHint = TextRenderingHint.AntiAliasGridFit;
		graphics.DrawString(glyph, font, brush, new RectangleF(0, 0, image.Width, image.Height), format);
		return image;
	}
}
