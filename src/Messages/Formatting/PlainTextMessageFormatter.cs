using System.Text.RegularExpressions;

namespace TinyChat.Messages.Formatting;

/// <summary>
/// Renders message content as plain text by stripping common formatting like Markdown and HTML.
/// </summary>
public partial class PlainTextMessageFormatter : IMessageFormatter
{
	[GeneratedRegex(@"<[^>]*>", RegexOptions.Compiled)]
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

	[GeneratedRegex(@"`([^`]*)`", RegexOptions.Compiled)]
	private static partial Regex MarkdownInlineCodeRegex();

	[GeneratedRegex(@"```(?:\w+)?(?:\s*\n)?([\s\S]*?)```", RegexOptions.Compiled)]
	private static partial Regex MarkdownCodeBlockRegex();

	[GeneratedRegex(@"<ul[^>]*>(.*?)</ul>", RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.Singleline)]
	private static partial Regex HtmlUnorderedListRegex();

	[GeneratedRegex(@"<ol[^>]*>(.*?)</ol>", RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.Singleline)]
	private static partial Regex HtmlOrderedListRegex();

	[GeneratedRegex(@"<li[^>]*>(.*?)</li>", RegexOptions.Compiled | RegexOptions.IgnoreCase | RegexOptions.Singleline)]
	private static partial Regex HtmlListItemRegex();

	/// <summary>
	/// Renders the message content as plain text by removing formatting.
	/// </summary>
	/// <param name="content">The message content to format.</param>
	/// <returns>Plain text representation of the content.</returns>
	public string Format(IChatMessageContent content)
	{
		if (content is StringMessageContent stringContent)
			return Format(stringContent.ToString());

		if (content is FunctionCallMessageContent or FunctionResultMessageContent)
			return Format(content.ToString() ?? string.Empty);

		throw new NotSupportedException($"Only {nameof(StringMessageContent)}, {nameof(FunctionCallMessageContent)}, and {nameof(FunctionResultMessageContent)} are supported by {nameof(PlainTextMessageFormatter)}.");
	}

	/// <summary>
	/// Renders the message content as plain text by removing formatting.
	/// </summary>
	/// <param name="content">The message content to format.</param>
	/// <returns>Plain text representation of the content.</returns>
	public string Format(string content)
	{
		var text = content ?? string.Empty;

		// Convert HTML lists to plain text BEFORE removing other HTML tags
		text = ConvertHtmlListsToPlainText(text);

		// Replace Markdown code blocks with blank-line-separated content (process first to avoid interference with other patterns)
		text = MarkdownCodeBlockRegex().Replace(text, match =>
		{
			var codeContent = match.Groups[1].Value;
			// Trim trailing newline from code content to avoid double newlines
			if (codeContent.EndsWith('\n'))
				codeContent = codeContent[..^1];
			return $"\n{codeContent}\n";
		});

		// Remove Markdown inline code (keep content)
		text = MarkdownInlineCodeRegex().Replace(text, "$1");

		// Remove Markdown images (keep alt text)
		text = MarkdownImagesRegex().Replace(text, "$1");

		// Remove Markdown links (keep link text)
		text = MarkdownLinksRegex().Replace(text, "$1");

		// Remove Markdown headers (process before bold/italic to avoid conflicts)
		text = MarkdownHeadersRegex().Replace(text, string.Empty);

		// Remove Markdown bold (process before italic to handle *** correctly)
		text = MarkdownBoldRegex().Replace(text, "$2");

		// Remove Markdown italic
		text = MarkdownItalicRegex().Replace(text, "$2");

		// Remove Markdown strikethrough
		text = MarkdownStrikethroughRegex().Replace(text, "$1");

		// Remove HTML tags (process last to catch any HTML in the original content)
		text = HtmlTagsRegex().Replace(text, string.Empty);

		return text.Trim();
	}

	private static string ConvertHtmlListsToPlainText(string text)
	{
		// We need to process nested lists from innermost to outermost
		// So we keep replacing until no more <ul> or <ol> tags are found
		var changed = true;
		while (changed)
		{
			var originalText = text;

			// Convert unordered lists <ul> to "- " prefixed items
			text = HtmlUnorderedListRegex().Replace(text, match =>
			{
				var listContent = match.Groups[1].Value;
				var items = HtmlListItemRegex().Matches(listContent);
				var result = string.Join("\n", items.Cast<Match>().Select(m => $"- {m.Groups[1].Value.Trim()}"));
				return result;
			});

			// Convert ordered lists <ol> to numbered items
			text = HtmlOrderedListRegex().Replace(text, match =>
			{
				var listContent = match.Groups[1].Value;
				var items = HtmlListItemRegex().Matches(listContent);
				var result = string.Join("\n", items.Cast<Match>().Select((m, i) => $"{i + 1}. {m.Groups[1].Value.Trim()}"));
				return result;
			});

			changed = text != originalText;
		}

		return text;
	}
}