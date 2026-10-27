using System.Globalization;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;

namespace TinyChat;

/// <summary>
/// Converts the DevExpress HTML-inspired label markup into plain text and into standard HTML for the clipboard.
/// See: https://docs.devexpress.com/WindowsForms/4874/common-features/html-text-formatting
/// </summary>
internal static partial class DXFormattedText
{
	[GeneratedRegex("""<\s*(?<closing>/)?\s*(?<tag>backcolor|color|font|href|nbsp|size|sub|sup|br|b|i|s|u|r|p|a)\b(?<attributes>[^>]*)>""", RegexOptions.IgnoreCase)]
	private static partial Regex TagRegex();

	[GeneratedRegex("""(?<name>[a-z]+)\s*=\s*(?:"(?<double>[^"]*)"|'(?<single>[^']*)'|(?<bare>[^\s>]+))""", RegexOptions.IgnoreCase)]
	private static partial Regex AttributeRegex();

	[GeneratedRegex("^#(?:[0-9a-f]{3}|[0-9a-f]{6}|[0-9a-f]{8})$|^[a-z]+$", RegexOptions.IgnoreCase)]
	private static partial Regex SimpleColorRegex();

	/// <summary>
	/// Removes all markup and returns the text as the user reads it.
	/// </summary>
	internal static string ToPlainText(string formattedText)
	{
		if (string.IsNullOrEmpty(formattedText))
			return string.Empty;

		var text = TagRegex().Replace(formattedText, match => match.Groups["tag"].Value.ToLowerInvariant() switch
		{
			"br" when !match.Groups["closing"].Success => Environment.NewLine,
			"nbsp" when !match.Groups["closing"].Success => " ",
			"p" when match.Groups["closing"].Success => Environment.NewLine,
			_ => string.Empty
		});

		return WebUtility.HtmlDecode(text).Trim();
	}

	/// <summary>
	/// Converts the markup into an HTML fragment that keeps the formatting when pasted into other applications.
	/// </summary>
	internal static string ToHtml(string formattedText)
	{
		if (string.IsNullOrEmpty(formattedText))
			return string.Empty;

		var result = new StringBuilder(formattedText.Length + 64);
		var offset = 0;
		foreach (Match match in TagRegex().Matches(formattedText))
		{
			AppendText(result, formattedText[offset..match.Index]);
			result.Append(ConvertTag(match));
			offset = match.Index + match.Length;
		}

		AppendText(result, formattedText[offset..]);
		return result.ToString().Trim();
	}

	private static void AppendText(StringBuilder result, string text)
	{
		if (text.Length == 0)
			return;

		var encoded = WebUtility.HtmlEncode(WebUtility.HtmlDecode(text));
		result.Append(encoded.ReplaceLineEndings("<br>"));
	}

	private static string ConvertTag(Match match)
	{
		var closing = match.Groups["closing"].Success;
		var tag = match.Groups["tag"].Value.ToLowerInvariant();
		var attributes = match.Groups["attributes"].Value;

		return tag switch
		{
			"br" => closing ? string.Empty : "<br>",
			"nbsp" => closing ? string.Empty : "&nbsp;",
			"r" => string.Empty,
			"b" or "i" or "s" or "u" or "sub" or "sup" => closing ? $"</{tag}>" : $"<{tag}>",
			"p" => closing ? "</p>" : CreateParagraph(attributes),
			"a" or "href" => closing ? "</a>" : CreateLink(attributes),
			"color" => closing ? "</span>" : CreateSpan("color", NormalizeColor(GetValue(attributes, null))),
			"backcolor" => closing ? "</span>" : CreateSpan("background-color", NormalizeColor(GetValue(attributes, null))),
			"size" => closing ? "</span>" : CreateSpan("font-size", NormalizeSize(GetValue(attributes, null))),
			"font" => closing ? "</span>" : CreateSpan("font-family", NormalizeFontFamily(GetValue(attributes, "face"))),
			_ => string.Empty
		};
	}

	private static string CreateParagraph(string attributes)
	{
		var alignment = GetValue(attributes, "align")?.ToLowerInvariant();
		return alignment is "left" or "right" or "center" or "justify"
			? $"<p style=\"text-align:{alignment}\">"
			: "<p>";
	}

	private static string CreateLink(string attributes)
	{
		var href = GetValue(attributes, "href");
		return IsSafeLink(href)
			? $"<a href=\"{WebUtility.HtmlEncode(href)}\">"
			: "<a>";
	}

	private static string CreateSpan(string property, string? value)
	{
		return value is null
			? "<span>"
			: $"<span style=\"{property}:{WebUtility.HtmlEncode(value)}\">";
	}

	/// <summary>
	/// Reads a named attribute (<c>href="x"</c>) or the DevExpress short form (<c>&lt;color=red&gt;</c>).
	/// </summary>
	private static string? GetValue(string attributes, string? name)
	{
		var trimmed = attributes.Trim();
		if (trimmed.StartsWith('='))
			return WebUtility.HtmlDecode(trimmed[1..].Trim().Trim('"', '\''));

		if (name is null)
			return null;

		foreach (Match match in AttributeRegex().Matches(attributes))
		{
			if (!string.Equals(match.Groups["name"].Value, name, StringComparison.OrdinalIgnoreCase))
				continue;

			var value = match.Groups["double"].Success ? match.Groups["double"].Value
				: match.Groups["single"].Success ? match.Groups["single"].Value
				: match.Groups["bare"].Value;
			return WebUtility.HtmlDecode(value);
		}

		return null;
	}

	private static string? NormalizeColor(string? value)
	{
		if (string.IsNullOrWhiteSpace(value))
			return null;

		var color = value.Trim();
		if (SimpleColorRegex().IsMatch(color))
			return color;

		var components = color.Split(',', StringSplitOptions.TrimEntries);
		if (components.Length is not (3 or 4) ||
			components.Any(component => !int.TryParse(component, NumberStyles.Integer, CultureInfo.InvariantCulture, out var number) || number is < 0 or > 255))
		{
			return null;
		}

		if (components.Length == 3)
			return $"rgb({string.Join(",", components)})";

		var alpha = int.Parse(components[0], CultureInfo.InvariantCulture) / 255d;
		return string.Create(CultureInfo.InvariantCulture, $"rgba({components[1]},{components[2]},{components[3]},{alpha:0.###})");
	}

	private static string? NormalizeSize(string? value)
	{
		if (!int.TryParse(value, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out var size))
			return null;

		var isRelative = value!.TrimStart().StartsWith('+') || value.TrimStart().StartsWith('-');
		if (!isRelative)
			return size > 0 ? $"{size}pt" : null;

		// DevExpress adds points to the base font; the HTML fragment does not know it, so scale relatively.
		var percentage = Math.Clamp(100 + (size * 10), 50, 250);
		return $"{percentage}%";
	}

	private static string? NormalizeFontFamily(string? value)
	{
		if (string.IsNullOrWhiteSpace(value))
			return null;

		var safeName = new string(value.Where(character => char.IsLetterOrDigit(character) || character is ' ' or '-' or '_').ToArray()).Trim();
		return safeName.Length == 0 ? null : $"'{safeName}'";
	}

	private static bool IsSafeLink(string? href)
	{
		if (string.IsNullOrWhiteSpace(href) || !Uri.TryCreate(href, UriKind.Absolute, out var uri))
			return false;

		return uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps || uri.Scheme == Uri.UriSchemeMailto;
	}
}
