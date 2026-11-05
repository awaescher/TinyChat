namespace TinyChat;

/// <summary>
/// Shortens long reasoning and tool results in the conversation. The complete text stays available in a dialog.
/// </summary>
internal static class DetailTextTruncation
{
	internal const int DEFAULT_MAXIMUM_LENGTH = 1500;
	internal const int DEFAULT_MAXIMUM_LINES = 15;
	private const string ELLIPSIS = "\u2026";

	/// <summary>
	/// Shortens <paramref name="text"/> to <paramref name="maximumLength"/> characters and <paramref name="maximumLines"/> lines.
	/// A limit of zero or less is ignored.
	/// </summary>
	/// <returns><see langword="true"/> if the text was shortened.</returns>
	internal static bool TryTruncate(string text, int maximumLength, int maximumLines, out string shortened)
	{
		shortened = text;
		var cut = text.Length;
		if (maximumLength > 0 && cut > maximumLength)
			cut = maximumLength;

		if (maximumLines > 0)
		{
			var lines = 1;
			for (var index = 0; index < cut; index++)
			{
				if (text[index] != '\n')
					continue;

				if (lines == maximumLines)
				{
					cut = index;
					break;
				}

				lines++;
			}
		}

		if (cut >= text.Length || text.AsSpan(cut).IsWhiteSpace())
			return false;

		if (cut > 0 && char.IsHighSurrogate(text[cut - 1]))
			cut--;

		shortened = text[..cut].TrimEnd() + ELLIPSIS;
		return true;
	}
}
