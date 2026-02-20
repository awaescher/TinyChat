using System.Text;
using System.Text.RegularExpressions;
using System.Web;

namespace TinyChat.Messages.Formatting;

/// <summary>
/// Renders message content as simplified HTML, supporting only specified HTML tags.
/// Markdown is converted to HTML where supported, otherwise stripped to plain text.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="SimplifiedHtmlMessageFormatter"/> class.
/// </remarks>
/// <param name="supportedTags">Array of supported HTML tag names (e.g., "b", "i", "a", "ul"). Case-insensitive.</param>
public partial class SimplifiedHtmlMessageFormatter(params string[] supportedTags) : IMessageFormatter
{
	[GeneratedRegex(@"<([a-z][a-z0-9]*)\b[^>]*>(.*?)</\1>", RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.Singleline)]
	private static partial Regex HtmlTagsRegex();

	[GeneratedRegex(@"<([a-z][a-z0-9]*)\b[^>]*/>", RegexOptions.Compiled | RegexOptions.IgnoreCase)]
	private static partial Regex SelfClosingHtmlTagsRegex();

	[GeneratedRegex(@"<!--[\s\S]*?-->", RegexOptions.Compiled)]
	private static partial Regex HtmlCommentsRegex();

	[GeneratedRegex(@"!\[([^\]]*)\]\(([^\)]+)\)", RegexOptions.Compiled)]
	private static partial Regex MarkdownImagesRegex();

	[GeneratedRegex(@"\[([^\]]+)\]\(([^\)]+)\)", RegexOptions.Compiled)]
	private static partial Regex MarkdownLinksRegex();

	[GeneratedRegex(@"^(#{1,6})\s+(.+)$", RegexOptions.Compiled | RegexOptions.Multiline)]
	private static partial Regex MarkdownHeadersRegex();

	[GeneratedRegex(@"(\*\*\*|___)(.*?)\1", RegexOptions.Compiled)]
	private static partial Regex MarkdownBoldItalicRegex();

	[GeneratedRegex(@"(\*\*|__)(.*?)\1", RegexOptions.Compiled)]
	private static partial Regex MarkdownBoldRegex();

	[GeneratedRegex(@"(\*|_)(.*?)\1", RegexOptions.Compiled)]
	private static partial Regex MarkdownItalicRegex();

	[GeneratedRegex(@"~~(.*?)~~", RegexOptions.Compiled)]
	private static partial Regex MarkdownStrikethroughRegex();

	[GeneratedRegex(@"`([^`]*)`", RegexOptions.Compiled)]
	private static partial Regex MarkdownInlineCodeRegex();

	[GeneratedRegex(@"```(?:\w+)?(?:\s*\n)?([\s\S]*?)```", RegexOptions.Compiled)]
	private static partial Regex MarkdownCodeBlockRegex();

	[GeneratedRegex(@"^[-*+]\s+(.+)$", RegexOptions.Compiled | RegexOptions.Multiline)]
	private static partial Regex MarkdownUnorderedListRegex();

	[GeneratedRegex(@"^\d+\.\s+(.+)$", RegexOptions.Compiled | RegexOptions.Multiline)]
	private static partial Regex MarkdownOrderedListRegex();

	[GeneratedRegex(@"^>\s+(.+)$", RegexOptions.Compiled | RegexOptions.Multiline)]
	private static partial Regex MarkdownBlockquoteRegex();

	[GeneratedRegex(@"<ul[^>]*>(.*?)</ul>", RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.Singleline)]
	private static partial Regex HtmlUnorderedListRegex();

	[GeneratedRegex(@"<ol[^>]*>(.*?)</ol>", RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.Singleline)]
	private static partial Regex HtmlOrderedListRegex();

	[GeneratedRegex(@"<li[^>]*>(.*?)</li>", RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.Singleline)]
	private static partial Regex HtmlListItemRegex();

	[GeneratedRegex(@"<span\s+style=['""]color:\s*([^'""]+)['""]>([^<]+)</span>", RegexOptions.Compiled | RegexOptions.IgnoreCase)]
	private static partial Regex HtmlColorSpanRegex();

	[GeneratedRegex(@"<span\s+style=['""]background-color:\s*([^'""]+)['""]>([^<]+)</span>", RegexOptions.Compiled | RegexOptions.IgnoreCase)]
	private static partial Regex HtmlBackColorSpanRegex();

	[GeneratedRegex(@"href=['""]([^'""]+)['""]", RegexOptions.Compiled | RegexOptions.IgnoreCase)]
	private static partial Regex HrefAttributeRegex();

	[GeneratedRegex(@"src=['""]([^'""]+)['""]", RegexOptions.Compiled | RegexOptions.IgnoreCase)]
	private static partial Regex SrcAttributeRegex();

	[GeneratedRegex(@"alt=['""]([^'""]*)['""]", RegexOptions.Compiled | RegexOptions.IgnoreCase)]
	private static partial Regex AltAttributeRegex();

	[GeneratedRegex(@"<font=""([^""]+)"">", RegexOptions.Compiled | RegexOptions.IgnoreCase)]
	private static partial Regex FontDevExpressFormatRegex();

	[GeneratedRegex(@"face=['""]([^'""]+)['""]", RegexOptions.Compiled | RegexOptions.IgnoreCase)]
	private static partial Regex FontFaceAttributeRegex();

	[GeneratedRegex(@"<size=([+-]?\d+)>", RegexOptions.Compiled | RegexOptions.IgnoreCase)]
	private static partial Regex SizeAttributeRegex();

	[GeneratedRegex(@"<color=([^>]+)>", RegexOptions.Compiled | RegexOptions.IgnoreCase)]
	private static partial Regex ColorAttributeRegex();

	[GeneratedRegex(@"<backcolor=([^>]+)>", RegexOptions.Compiled | RegexOptions.IgnoreCase)]
	private static partial Regex BackColorAttributeRegex();

	[GeneratedRegex(@"rgba?\s*\(\s*(\d+)\s*,\s*(\d+)\s*,\s*(\d+)(?:\s*,\s*(\d+(?:\.\d+)?))?\s*\)", RegexOptions.Compiled | RegexOptions.IgnoreCase)]
	private static partial Regex RgbColorRegex();

	private readonly HashSet<string> _supportedTags = new(supportedTags.Select(NormalizeTag), StringComparer.OrdinalIgnoreCase);

	// Tag mappings from markdown/HTML to canonical HTML tags
	private static readonly Dictionary<string, string> _tagAliases = new(StringComparer.OrdinalIgnoreCase)
	{
		{ "strong", "b" },
		{ "em", "i" },
		{ "del", "s" },
		{ "strike", "s" }
	};

	/// <summary>
	/// Default monospace font family for code blocks and inline code.
	/// </summary>
	public string DefaultCodeFontName { get; set; } = "Consolas";


	/// <summary>
	/// Renders the message content as simplified HTML.
	/// </summary>
	/// <param name="content">The message content to format.</param>
	/// <returns>HTML representation of the content with only supported tags.</returns>
	public string Format(IChatMessageContent content)
	{
		if (content is StringMessageContent stringContent)
			return Format(stringContent.ToString());

		if (content is FunctionCallMessageContent or FunctionResultMessageContent)
			return Format(content.ToString() ?? string.Empty);

		throw new NotSupportedException($"Only {nameof(StringMessageContent)}, {nameof(FunctionCallMessageContent)}, and {nameof(FunctionResultMessageContent)} are supported by {nameof(SimplifiedHtmlMessageFormatter)}.");
	}

	/// <summary>
	/// Renders the message content as simplified HTML.
	/// </summary>
	/// <param name="content">The message content to format.</param>
	/// <returns>HTML representation of the content with only supported tags.</returns>
	public string Format(string content)
	{
		var text = content ?? string.Empty;

		// Step 1: Remove HTML comments
		text = HtmlCommentsRegex().Replace(text, string.Empty);

		// Step 2: Convert color/backcolor span tags to DevExpress format (BEFORE processing HTML tags)
		text = ConvertColorSpansToDevExpressFormat(text);

		// Step 3: Convert supported Markdown to HTML
		text = ConvertMarkdownToHtml(text);

		// Step 4: Process HTML tags (keep supported, strip unsupported)
		text = ProcessHtmlTags(text);

		return text.Trim();
	}

	private string ConvertMarkdownToHtml(string text)
	{
		// Process code blocks first (before other conversions)
		// Code blocks are formatted with font tags and line breaks if font is supported
		text = MarkdownCodeBlockRegex().Replace(text, match =>
		{
			var codeContent = match.Groups[1].Value;
			if (codeContent.EndsWith('\n'))
				codeContent = codeContent[..^1];

			var escapedCode = HttpUtility.HtmlEncode(codeContent);
			// DevExpress doesn't decode &quot; in AllowHtmlString mode, so we need to convert it back
			escapedCode = escapedCode.Replace("&quot;", "\"");

			// Render with font tag if supported, otherwise just plain text
			if (IsSupported("font") && !string.IsNullOrWhiteSpace(DefaultCodeFontName))
			{
				// Don't use HtmlAttributeEncode for font name - DevExpress uses special syntax
				var fontFace = DefaultCodeFontName.Replace("\"", "&quot;");
				// Add line breaks before and after code block
				return $"\n<font=\"{fontFace}\">{escapedCode}</font>\n";
			}
			else
			{
				// Fallback to plain text with line breaks
				return $"\n{escapedCode}\n";
			}
		});

		// Process inline code
		// Inline code is formatted with font tags if font is supported
		text = MarkdownInlineCodeRegex().Replace(text, match =>
		{
			var codeContent = match.Groups[1].Value;
			var escapedCode = HttpUtility.HtmlEncode(codeContent);
			// DevExpress doesn't decode &quot; in AllowHtmlString mode, so we need to convert it back
			escapedCode = escapedCode.Replace("&quot;", "\"");

			// Render with font tag if supported, otherwise just plain text
			if (IsSupported("font") && !string.IsNullOrWhiteSpace(DefaultCodeFontName))
			{
				// Don't use HtmlAttributeEncode for font name - DevExpress uses special syntax
				var fontFace = DefaultCodeFontName.Replace("\"", "&quot;");
				return $"<font=\"{fontFace}\">{escapedCode}</font>";
			}
			else
			{
				// Fallback to plain text
				return escapedCode;
			}
		});
		// Process images
		if (IsSupported("img"))
		{
			text = MarkdownImagesRegex().Replace(text, match =>
			{
				var alt = HttpUtility.HtmlAttributeEncode(match.Groups[1].Value);
				var src = HttpUtility.HtmlAttributeEncode(match.Groups[2].Value);
				return $"<img src=\"{src}\" alt=\"{alt}\">";
			});
		}
		else
		{
			text = MarkdownImagesRegex().Replace(text, "$1");
		}

		// Process links
		if (IsSupported("a"))
		{
			text = MarkdownLinksRegex().Replace(text, match =>
			{
				var linkText = match.Groups[1].Value;
				// Recursively process formatting in link text
				linkText = ProcessInlineFormatting(linkText);
				var url = HttpUtility.HtmlAttributeEncode(match.Groups[2].Value);
				return $"<a href=\"{url}\">{linkText}</a>";
			});
		}
		else
		{
			text = MarkdownLinksRegex().Replace(text, match =>
			{
				var linkText = match.Groups[1].Value;
				return ProcessInlineFormatting(linkText);
			});
		}

		// Process headers - convert to size+bold instead of h1-h6 tags
		text = MarkdownHeadersRegex().Replace(text, match =>
		{
			var level = match.Groups[1].Value.Length;
			var headerText = match.Groups[2].Value;
			var tag = $"h{level}";
			var processedText = ProcessInlineFormatting(headerText);

			// First check if the h1-h6 tag is supported
			if (IsSupported(tag))
			{
				return $"<{tag}>{processedText}</{tag}>";
			}

			// If h-tag not supported, use size+bold format (DevExpress style)
			var hasBold = IsSupported("b");
			var hasSize = IsSupported("size");

			if (!hasBold && !hasSize)
				return processedText;

			var result = processedText;

			if (hasBold)
				result = $"<b>{result}</b>";

			if (hasSize && level < 6)
			{
				var sizeIncrease = 6 - level; // h1=+5, h2=+4, h3=+3, h4=+2, h5=+1
				result = $"<size=+{sizeIncrease}>{result}</size>";
			}

			return result;
		});

		// Process blockquotes
		if (IsSupported("blockquote"))
		{
			text = MarkdownBlockquoteRegex().Replace(text, match =>
			{
				var quoteText = ProcessInlineFormatting(match.Groups[1].Value);
				return $"<blockquote>{quoteText}</blockquote>";
			});
		}

		// Process lists BEFORE inline formatting to preserve markers
		if (IsSupported("ul") && IsSupported("li"))
		{
			text = ConvertUnorderedLists(text);
		}

		if (IsSupported("ol") && IsSupported("li"))
		{
			text = ConvertOrderedLists(text);
		}

		// Process inline formatting (bold, italic, strikethrough) AFTER lists
		text = ProcessInlineFormatting(text);

		return text;
	}

	private string ProcessInlineFormatting(string text)
	{
		// Process bold+italic (***) first to handle triple asterisks correctly
		if (IsSupported("b") && IsSupported("i"))
		{
			text = MarkdownBoldItalicRegex().Replace(text, "<b><i>$2</i></b>");
		}
		else if (IsSupported("b"))
		{
			// If only bold is supported, treat *** as bold
			text = MarkdownBoldItalicRegex().Replace(text, "<b>$2</b>");
		}
		else if (IsSupported("i"))
		{
			// If only italic is supported, treat *** as italic
			text = MarkdownBoldItalicRegex().Replace(text, "<i>$2</i>");
		}
		else
		{
			// Neither supported, strip
			text = MarkdownBoldItalicRegex().Replace(text, "$2");
		}

		// Process bold (before italic to handle *** correctly)
		if (IsSupported("b"))
		{
			text = MarkdownBoldRegex().Replace(text, match =>
			{
				var content = match.Groups[2].Value;
				// Recursively process inner content
				content = ProcessInlineFormattingWithoutBold(content);
				return $"<b>{content}</b>";
			});
		}
		else
		{
			text = MarkdownBoldRegex().Replace(text, match =>
			{
				var content = match.Groups[2].Value;
				return ProcessInlineFormattingWithoutBold(content);
			});
		}

		// Process italic
		if (IsSupported("i"))
		{
			text = MarkdownItalicRegex().Replace(text, "<i>$2</i>");
		}
		else
		{
			text = MarkdownItalicRegex().Replace(text, "$2");
		}

		// Process strikethrough
		if (IsSupported("s"))
		{
			text = MarkdownStrikethroughRegex().Replace(text, "<s>$1</s>");
		}
		else
		{
			text = MarkdownStrikethroughRegex().Replace(text, "$1");
		}

		return text;
	}

	private string ProcessInlineFormattingWithoutBold(string text)
	{
		// Process italic
		if (IsSupported("i"))
		{
			text = MarkdownItalicRegex().Replace(text, "<i>$2</i>");
		}
		else
		{
			text = MarkdownItalicRegex().Replace(text, "$2");
		}

		// Process strikethrough
		if (IsSupported("s"))
		{
			text = MarkdownStrikethroughRegex().Replace(text, "<s>$1</s>");
		}
		else
		{
			text = MarkdownStrikethroughRegex().Replace(text, "$1");
		}

		return text;
	}

	private string ConvertUnorderedLists(string text)
	{
		var lines = text.Split('\n');
		var result = new StringBuilder();
		var inList = false;

		for (var i = 0; i < lines.Length; i++)
		{
			var line = lines[i];
			var match = MarkdownUnorderedListRegex().Match(line);

			if (match.Success)
			{
				if (!inList)
				{
					result.Append("<ul>");
					inList = true;
				}
				var itemText = match.Groups[1].Value;
				// Process inline formatting in list items
				itemText = ProcessInlineFormatting(itemText);
				result.Append($"<li>{itemText}</li>");
			}
			else
			{
				if (inList)
				{
					result.Append("</ul>\n");
					inList = false;
				}
				result.Append(line);

				// Add newline if not last line
				if (i < lines.Length - 1)
					result.Append('\n');
			}
		}

		if (inList)
			result.Append("</ul>");

		return result.ToString();
	}

	private string ConvertOrderedLists(string text)
	{
		var lines = text.Split('\n');
		var result = new StringBuilder();
		var inList = false;

		for (var i = 0; i < lines.Length; i++)
		{
			var line = lines[i];
			var match = MarkdownOrderedListRegex().Match(line);

			if (match.Success)
			{
				if (!inList)
				{
					result.Append("<ol>");
					inList = true;
				}
				var itemText = match.Groups[1].Value;
				// Process inline formatting in list items
				itemText = ProcessInlineFormatting(itemText);
				result.Append($"<li>{itemText}</li>");
			}
			else
			{
				if (inList)
				{
					result.Append("</ol>\n");
					inList = false;
				}
				result.Append(line);

				// Add newline if not last line
				if (i < lines.Length - 1)
					result.Append('\n');
			}
		}

		if (inList)
			result.Append("</ol>");

		return result.ToString();
	}

	private string ProcessHtmlTags(string text)
	{
		// Convert HTML lists to plain text BEFORE processing other tags
		text = ConvertHtmlListsIfNotSupported(text);

		bool changed;
		do
		{
			changed = false;
			var originalText = text;

			// Process regular paired tags
			text = HtmlTagsRegex().Replace(text, match =>
			{
				var tagName = match.Groups[1].Value;
				var content = match.Groups[2].Value;
				var normalizedTag = NormalizeTag(tagName);

				// Always recursively process content first to handle nested tags
				var processedContent = ProcessHtmlTagsInner(content);

				// Handle special conversions for HTML tags

				// Convert <code> and <pre> to <font> if font is supported but code/pre are not
				if ((normalizedTag == "code" || normalizedTag == "pre") && !IsSupported(normalizedTag) && IsSupported("font") && !string.IsNullOrWhiteSpace(DefaultCodeFontName))
				{
					// Don't use HtmlAttributeEncode for font name - DevExpress uses special syntax
					var fontFace = DefaultCodeFontName.Replace("\"", "&quot;");
					// Don't double-encode - content is already processed
					return $"<font=\"{fontFace}\">{processedContent}</font>";
				}

				// Convert h1-h6 tags to size+bold if h-tags not supported but size/b are
				if (normalizedTag.Length == 2 && normalizedTag[0] == 'h' && char.IsDigit(normalizedTag[1]))
				{
					var level = int.Parse(normalizedTag[1].ToString());

					if (!IsSupported(normalizedTag))
					{
						// If h-tag not supported, use size+bold format (DevExpress style)
						var hasBold = IsSupported("b");
						var hasSize = IsSupported("size");

						if (!hasBold && !hasSize)
							return processedContent;

						var result = processedContent;

						if (hasBold)
							result = $"<b>{result}</b>";

						if (hasSize && level < 6)
						{
							var sizeIncrease = 6 - level; // h1=+5, h2=+4, h3=+3, h4=+2, h5=+1
							result = $"<size=+{sizeIncrease}>{result}</size>";
						}

						return result;
					}
				}

				if (IsSupported(normalizedTag))
				{
					// For specific tags, keep only essential attributes
					var attributes = "";

					if (normalizedTag == "a")
					{
						// Extract href attribute only
						var hrefMatch = HrefAttributeRegex().Match(match.Value);
						if (hrefMatch.Success)
							attributes = $" href=\"{hrefMatch.Groups[1].Value}\"";
					}
					else if (normalizedTag == "img")
					{
						// Extract src and alt attributes only
						var srcMatch = SrcAttributeRegex().Match(match.Value);
						var altMatch = AltAttributeRegex().Match(match.Value);

						if (srcMatch.Success)
							attributes += $" src=\"{srcMatch.Groups[1].Value}\"";
						if (altMatch.Success)
							attributes += $" alt=\"{altMatch.Groups[1].Value}\"";

						attributes = attributes.Trim();
						if (attributes.Length > 0)
							attributes = " " + attributes;
					}
					else if (normalizedTag == "font")
					{
						// DevExpress uses <font="FontName"> syntax instead of <font face="FontName">
						// Extract both face attribute and the simplified DevExpress format
						var devExpressMatch = FontDevExpressFormatRegex().Match(match.Value);
						var faceMatch = FontFaceAttributeRegex().Match(match.Value);

						if (devExpressMatch.Success)
							attributes = $"=\"{devExpressMatch.Groups[1].Value}\"";
						else if (faceMatch.Success)
							attributes = $"=\"{faceMatch.Groups[1].Value}\"";
					}
					else if (normalizedTag == "size")
					{
						// Extract size value (e.g., <size=+3>)
						var sizeMatch = SizeAttributeRegex().Match(match.Value);
						if (sizeMatch.Success)
							attributes = $"={sizeMatch.Groups[1].Value}";
					}
					else if (normalizedTag == "color")
					{
						// Extract color value (e.g., <color=red> or <color=#FF0000>)
						var colorMatch = ColorAttributeRegex().Match(match.Value);
						if (colorMatch.Success)
							attributes = $"={colorMatch.Groups[1].Value}";
					}
					else if (normalizedTag == "backcolor")
					{
						// Extract backcolor value
						var backcolorMatch = BackColorAttributeRegex().Match(match.Value);
						if (backcolorMatch.Success)
							attributes = $"={backcolorMatch.Groups[1].Value}";
					}

					if (normalizedTag == "size" || normalizedTag == "color" || normalizedTag == "backcolor")
					{
						return $"<{normalizedTag}{attributes}>{processedContent}</{normalizedTag}>";
					}

					return $"<{normalizedTag}{attributes}>{processedContent}</{normalizedTag}>";
				}
				else
				{
					// Strip tag but keep processed content
					return processedContent;
				}
			});

			// Process self-closing tags
			text = SelfClosingHtmlTagsRegex().Replace(text, match =>
			{
				var tagName = match.Groups[1].Value;
				var normalizedTag = NormalizeTag(tagName);

				if (IsSupported(normalizedTag))
				{
					return match.Value; // Keep the tag as-is with attributes for self-closing tags like <br/>
				}
				else
				{
					return string.Empty; // Remove unsupported self-closing tags
				}
			});

			if (text != originalText)
				changed = true;

		} while (changed);

		return text;
	}

	private string ConvertHtmlListsIfNotSupported(string text)
	{
		// We need to process nested lists from innermost to outermost
		// So we keep replacing until no more <ul> or <ol> tags are found
		var changed = true;
		while (changed)
		{
			var originalText = text;

			// Convert unordered lists <ul> to "- " prefixed items if ul/li not supported
			if (!IsSupported("ul") || !IsSupported("li"))
			{
				text = HtmlUnorderedListRegex().Replace(text, match =>
				{
					var listContent = match.Groups[1].Value;
					var items = HtmlListItemRegex().Matches(listContent);
					// Strip HTML tags from list items when converting to plain text
					var result = string.Join("\n", items.Cast<Match>().Select(m => $"- {StripHtmlTags(m.Groups[1].Value.Trim())}"));
					return result;
				});
			}

			// Convert ordered lists <ol> to numbered items if ol/li not supported
			if (!IsSupported("ol") || !IsSupported("li"))
			{
				text = HtmlOrderedListRegex().Replace(text, match =>
				{
					var listContent = match.Groups[1].Value;
					var items = HtmlListItemRegex().Matches(listContent);
					// Strip HTML tags from list items when converting to plain text
					var result = string.Join("\n", items.Cast<Match>().Select((m, i) => $"{i + 1}. {StripHtmlTags(m.Groups[1].Value.Trim())}"));
					return result;
				});
			}

			changed = text != originalText;
		}

		return text;
	}

	private static string StripHtmlTags(string text)
	{
		// Recursively strip all HTML tags but keep content
		var changed = true;
		while (changed)
		{
			var originalText = text;

			// Replace tags with their content
			text = HtmlTagsRegex().Replace(text, "$2");

			// Remove self-closing tags
			text = SelfClosingHtmlTagsRegex().Replace(text, string.Empty);

			changed = text != originalText;
		}

		return text;
	}

	private string ConvertColorSpansToDevExpressFormat(string text)
	{
		// Convert HTML <span style="color:xxx"> to DevExpress <color=xxx>
		if (IsSupported("color"))
		{
			text = HtmlColorSpanRegex().Replace(text, match =>
			{
				var color = ParseColorValue(match.Groups[1].Value);
				var content = match.Groups[2].Value;
				return $"<color={color}>{content}</color>";
			});
		}

		// Convert HTML <span style="background-color:xxx"> to DevExpress <backcolor=xxx>
		if (IsSupported("backcolor"))
		{
			text = HtmlBackColorSpanRegex().Replace(text, match =>
			{
				var color = ParseColorValue(match.Groups[1].Value);
				var content = match.Groups[2].Value;
				return $"<backcolor={color}>{content}</backcolor>";
			});
		}

		return text;
	}

	private static string ParseColorValue(string colorValue)
	{
		// Normalize color value - keep as is (named colors, hex, rgb)
		// DevExpress supports: red, #FF0000, 255,0,0, 255,255,0,0 (ARGB)
		colorValue = colorValue.Trim();

		// If it's rgb() or rgba() format, extract values
		var rgbMatch = RgbColorRegex().Match(colorValue);
		if (rgbMatch.Success)
		{
			var r = rgbMatch.Groups[1].Value;
			var g = rgbMatch.Groups[2].Value;
			var b = rgbMatch.Groups[3].Value;
			var a = rgbMatch.Groups[4].Success ? rgbMatch.Groups[4].Value : null;

			if (a != null)
			{
				// Convert alpha from 0-1 or 0-255 to 0-255
				var alphaValue = double.Parse(a);
				if (alphaValue <= 1.0)
					alphaValue *= 255;
				return $"{(int)alphaValue},{r},{g},{b}";
			}

			return $"{r},{g},{b}";
		}

		return colorValue;
	}

	private string ProcessHtmlTagsInner(string content)
	{
		// Single pass recursive processing for nested tags
		var processed = HtmlTagsRegex().Replace(content, match =>
		{
			var tagName = match.Groups[1].Value;
			var innerContent = match.Groups[2].Value;
			var normalizedTag = NormalizeTag(tagName);

			// Recursively process inner content
			var processedInner = ProcessHtmlTagsInner(innerContent);

			// Handle special conversions for HTML tags (same as in ProcessHtmlTags)

			// Convert <code> and <pre> to <font> if font is supported but code/pre are not
			if ((normalizedTag == "code" || normalizedTag == "pre") && !IsSupported(normalizedTag) && IsSupported("font") && !string.IsNullOrWhiteSpace(DefaultCodeFontName))
			{
				// Don't use HtmlAttributeEncode for font name - DevExpress uses special syntax
				var fontFace = DefaultCodeFontName.Replace("\"", "&quot;");
				return $"<font=\"{fontFace}\">{processedInner}</font>";
			}

			// Convert h1-h6 tags to size+bold if h-tags not supported but size/b are
			if (normalizedTag.Length == 2 && normalizedTag[0] == 'h' && char.IsDigit(normalizedTag[1]))
			{
				var level = int.Parse(normalizedTag[1].ToString());

				if (!IsSupported(normalizedTag))
				{
					// If h-tag not supported, use size+bold format (DevExpress style)
					var hasBold = IsSupported("b");
					var hasSize = IsSupported("size");

					if (!hasBold && !hasSize)
						return processedInner;

					var result = processedInner;

					if (hasBold)
						result = $"<b>{result}</b>";

					if (hasSize && level < 6)
					{
						var sizeIncrease = 6 - level; // h1=+5, h2=+4, h3=+3, h4=+2, h5=+1
						result = $"<size=+{sizeIncrease}>{result}</size>";
					}

					return result;
				}
			}

			if (IsSupported(normalizedTag))
			{
				// For nested tags, extract and keep essential attributes
				var attributes = "";

				if (normalizedTag == "a")
				{
					var hrefMatch = HrefAttributeRegex().Match(match.Value);
					if (hrefMatch.Success)
						attributes = $" href=\"{hrefMatch.Groups[1].Value}\"";
				}
				else if (normalizedTag == "img")
				{
					var srcMatch = SrcAttributeRegex().Match(match.Value);
					var altMatch = AltAttributeRegex().Match(match.Value);

					if (srcMatch.Success)
						attributes += $" src=\"{srcMatch.Groups[1].Value}\"";
					if (altMatch.Success)
						attributes += $" alt=\"{altMatch.Groups[1].Value}\"";

					attributes = attributes.Trim();
					if (attributes.Length > 0)
						attributes = " " + attributes;
				}
				else if (normalizedTag == "font")
				{
					var devExpressMatch = FontDevExpressFormatRegex().Match(match.Value);
					var faceMatch = FontFaceAttributeRegex().Match(match.Value);

					if (devExpressMatch.Success)
						attributes = $"=\"{devExpressMatch.Groups[1].Value}\"";
					else if (faceMatch.Success)
						attributes = $"=\"{faceMatch.Groups[1].Value}\"";
				}
				else if (normalizedTag == "size")
				{
					var sizeMatch = SizeAttributeRegex().Match(match.Value);
					if (sizeMatch.Success)
						attributes = $"={sizeMatch.Groups[1].Value}";
				}
				else if (normalizedTag == "color")
				{
					var colorMatch = ColorAttributeRegex().Match(match.Value);
					if (colorMatch.Success)
						attributes = $"={colorMatch.Groups[1].Value}";
				}
				else if (normalizedTag == "backcolor")
				{
					var backcolorMatch = BackColorAttributeRegex().Match(match.Value);
					if (backcolorMatch.Success)
						attributes = $"={backcolorMatch.Groups[1].Value}";
				}

				if (normalizedTag == "size" || normalizedTag == "color" || normalizedTag == "backcolor")
				{
					return $"<{normalizedTag}{attributes}>{processedInner}</{normalizedTag}>";
				}

				return $"<{normalizedTag}{attributes}>{processedInner}</{normalizedTag}>";
			}
			else
			{
				// Strip tag but keep content
				return processedInner;
			}
		});

		return processed;
	}

	private bool IsSupported(string tag)
	{
		return _supportedTags.Contains(NormalizeTag(tag));
	}

	private static string NormalizeTag(string tag)
	{
		if (_tagAliases.TryGetValue(tag, out var normalized))
			return normalized;
		return tag.ToLowerInvariant();
	}
}
