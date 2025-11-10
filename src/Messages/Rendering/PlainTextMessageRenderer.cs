using System.Text.RegularExpressions;

namespace TinyChat.Messages.Rendering;

/// <summary>
/// Renders message content as plain text by stripping common formatting like Markdown and HTML.
/// </summary>
public partial class PlainTextMessageRenderer : IMessageRenderer
{
	[GeneratedRegex(@"<[^>]+>", RegexOptions.Compiled)]
	private static partial Regex HtmlTagsRegex();

	[GeneratedRegex(@"!\[([^\]]*)\]\([^\)]+\)", RegexOptions.Compiled)]
	private static partial Regex MarkdownImagesRegex();

	[GeneratedRegex(@"\[([^\]]+)\]\([^\)]+\)", RegexOptions.Compiled)]
	private static partial Regex MarkdownLinksRegex();

	[GeneratedRegex(@"^#{1,6}\s+", RegexOptions.Compiled | RegexOptions.Multiline)]
	private static partial Regex MarkdownHeadersRegex();

	[GeneratedRegex(@"(\*\*|__)(.*?)\1", RegexOptions.Compiled)]
	private static partial Regex MarkdownBoldRegex();

	[GeneratedRegex(@"(\*|_)(.*?)\1", RegexOptions.Compiled)]
	private static partial Regex MarkdownItalicRegex();

	[GeneratedRegex(@"~~(.*?)~~", RegexOptions.Compiled)]
	private static partial Regex MarkdownStrikethroughRegex();

	[GeneratedRegex(@"`([^`]+)`", RegexOptions.Compiled)]
	private static partial Regex MarkdownInlineCodeRegex();

	[GeneratedRegex(@"```[\s\S]*?```", RegexOptions.Compiled)]
	private static partial Regex MarkdownCodeBlockRegex();

	/// <summary>
	/// Renders the message content as plain text by removing formatting.
	/// </summary>
	/// <param name="content">The message content to render.</param>
	/// <returns>Plain text representation of the content.</returns>
	public string Render(IChatMessageContent content)
	{
		if (content is StringMessageContent stringContent)
			return Render(stringContent.ToString());

		throw new NotSupportedException($"Only {nameof(StringMessageContent)} is supported by {nameof(PlainTextMessageRenderer)}.");
	}

	/// <summary>
	/// Renders the message content as plain text by removing formatting.
	/// </summary>
	/// <param name="content">The message content to render.</param>
	/// <returns>Plain text representation of the content.</returns>
	public string Render(string content)
	{
		var text = content ?? string.Empty;

		// Remove Markdown code blocks
		text = MarkdownCodeBlockRegex().Replace(text, string.Empty);

		// Remove Markdown inline code
		text = MarkdownInlineCodeRegex().Replace(text, "$1");

		// Remove Markdown images (keep alt text)
		text = MarkdownImagesRegex().Replace(text, "$1");

		// Remove Markdown links (keep link text)
		text = MarkdownLinksRegex().Replace(text, "$1");

		// Remove Markdown headers
		text = MarkdownHeadersRegex().Replace(text, string.Empty);

		// Remove Markdown bold
		text = MarkdownBoldRegex().Replace(text, "$2");

		// Remove Markdown italic
		text = MarkdownItalicRegex().Replace(text, "$2");

		// Remove Markdown strikethrough
		text = MarkdownStrikethroughRegex().Replace(text, "$1");

		// Remove HTML tags
		text = HtmlTagsRegex().Replace(text, string.Empty);

		return text.Trim();
	}
}