using System.Text.RegularExpressions;

namespace TinyChat;

/// <summary>
/// Splits chat message content into regular text and &lt;Think&gt; segments so UI layers can render them differently.
/// </summary>
public static class ThinkTagParser
{
	private static readonly Regex _thinkTagRegex = new("<\\s*think\\s*>(.*?)<\\s*/\\s*think\\s*>", RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.Compiled);

	/// <summary>
	/// Splits the provided content into ordered segments highlighting think tokens.
	/// </summary>
	/// <param name="rawContent">Original chat message content.</param>
	/// <returns>Ordered segment list representing the original content.</returns>
	public static IReadOnlyList<ThinkSegment> Split(string? rawContent)
	{
		var text = rawContent ?? string.Empty;

		if (text.Length == 0)
			return Array.Empty<ThinkSegment>();

		var matches = _thinkTagRegex.Matches(text);
		if (matches.Count == 0)
			return new[] { new ThinkSegment(text, false) };

		var segments = new List<ThinkSegment>(matches.Count * 2 + 1);
		var lastIndex = 0;

		foreach (Match match in matches)
		{
			if (match.Index > lastIndex)
				segments.Add(new ThinkSegment(text.Substring(lastIndex, match.Index - lastIndex), false));

			segments.Add(new ThinkSegment(match.Groups[1].Value, true));
			lastIndex = match.Index + match.Length;
		}

		if (lastIndex < text.Length)
			segments.Add(new ThinkSegment(text[lastIndex..], false));

		return segments;
	}
}

/// <summary>
/// Represents a continuous range of chat content describing either normal text or a think section.
/// </summary>
/// <param name="Content">Original segment content without formatting.</param>
/// <param name="IsThinkSegment">Value indicating whether the segment comes from a &lt;Think&gt; tag.</param>
public readonly record struct ThinkSegment(string Content, bool IsThinkSegment);
