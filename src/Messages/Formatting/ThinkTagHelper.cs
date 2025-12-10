using System.Text.RegularExpressions;

namespace TinyChat.Messages.Formatting;

/// <summary>
/// Provides utilities for extracting and processing think tags from message content.
/// Think tags (e.g., &lt;think&gt;...&lt;/think&gt; or &lt;Think&gt;...&lt;/Think&gt;) contain
/// AI reasoning content that can be displayed in a collapsible format.
/// </summary>
public static partial class ThinkTagHelper
{
	private static readonly Regex ThinkTagPattern = new(@"<[Tt]hink(?:ing)?>([\s\S]*?)</[Tt]hink(?:ing)?>", RegexOptions.Compiled);

	/// <summary>
	/// Represents extracted think content and the remaining message content.
	/// </summary>
	/// <param name="ThinkSections">List of think content sections found in the message.</param>
	/// <param name="ContentWithoutThink">The message content with think tags removed.</param>
	public record ThinkExtractionResult(IReadOnlyList<ThinkSection> ThinkSections, string ContentWithoutThink);

	/// <summary>
	/// Represents a single think section with its content and position.
	/// </summary>
	/// <param name="Content">The content inside the think tags.</param>
	/// <param name="OriginalStartIndex">The start index in the original string.</param>
	/// <param name="OriginalLength">The length of the full think tag including delimiters.</param>
	public record ThinkSection(string Content, int OriginalStartIndex, int OriginalLength);

	/// <summary>
	/// Extracts all think tag sections from the given content.
	/// </summary>
	/// <param name="content">The message content to process.</param>
	/// <returns>A result containing the extracted think sections and content without think tags.</returns>
	public static ThinkExtractionResult ExtractThinkSections(string content)
	{
		if (string.IsNullOrEmpty(content))
			return new ThinkExtractionResult([], content ?? string.Empty);

		var matches = ThinkTagPattern.Matches(content);
		if (matches.Count == 0)
			return new ThinkExtractionResult([], content);

		var sections = new List<ThinkSection>();
		foreach (Match match in matches)
		{
			sections.Add(new ThinkSection(
				match.Groups[1].Value.Trim(),
				match.Index,
				match.Length
			));
		}

		var contentWithoutThink = ThinkTagPattern.Replace(content, string.Empty).Trim();
		return new ThinkExtractionResult(sections, contentWithoutThink);
	}

	/// <summary>
	/// Checks if the content contains any think tags.
	/// </summary>
	/// <param name="content">The content to check.</param>
	/// <returns>True if think tags are present, false otherwise.</returns>
	public static bool ContainsThinkTags(string content)
	{
		if (string.IsNullOrEmpty(content))
			return false;

		return ThinkTagPattern.IsMatch(content);
	}

	/// <summary>
	/// Removes all think tags from the content, returning only the non-think content.
	/// </summary>
	/// <param name="content">The content to process.</param>
	/// <returns>Content with all think tags removed.</returns>
	public static string RemoveThinkTags(string content)
	{
		if (string.IsNullOrEmpty(content))
			return content ?? string.Empty;

		return ThinkTagPattern.Replace(content, string.Empty).Trim();
	}
}
