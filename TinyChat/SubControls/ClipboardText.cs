namespace TinyChat;

/// <summary>
/// Exchanges text with the clipboard using <see cref="Environment.NewLine"/>, so that the structure of messages is kept
/// in Windows Forms and DevExpress text editors, which do not break lines at a single LF.
/// </summary>
internal static class ClipboardText
{
	/// <summary>Converts every line ending to <see cref="Environment.NewLine"/>.</summary>
	internal static string Normalize(string text) => text.ReplaceLineEndings();

	/// <summary>Places text with <see cref="Environment.NewLine"/> line endings on the clipboard.</summary>
	internal static void SetText(string text)
	{
		if (!string.IsNullOrEmpty(text))
			Clipboard.SetText(Normalize(text));
	}

	/// <summary>
	/// Gets the clipboard text when its line endings have to be converted before pasting, otherwise <see langword="null"/>.
	/// </summary>
	internal static string? GetTextToNormalize()
	{
		try
		{
			if (!Clipboard.ContainsText())
				return null;

			var text = Clipboard.GetText();
			var normalized = Normalize(text);
			return string.Equals(text, normalized, StringComparison.Ordinal) ? null : normalized;
		}
		catch (System.Runtime.InteropServices.ExternalException)
		{
			// The clipboard can temporarily be locked by another application; paste as usual then.
			return null;
		}
	}
}
