using System;
using System.Collections.Generic;
using System.Globalization;

namespace DemoApp;

/// <summary>
/// Splits demo responses without cutting surrogate pairs or multi-code-point emoji sequences.
/// </summary>
internal static class DemoTextChunker
{
	public static IEnumerable<string> Chunk(string text, int maximumUtf16Length = 4)
	{
		ArgumentNullException.ThrowIfNull(text);
		ArgumentOutOfRangeException.ThrowIfNegativeOrZero(maximumUtf16Length);

		var textElements = StringInfo.GetTextElementEnumerator(text);
		var chunkStart = 0;
		var chunkLength = 0;
		while (textElements.MoveNext())
		{
			var element = (string)textElements.Current;
			if (chunkLength > 0 && chunkLength + element.Length > maximumUtf16Length)
			{
				yield return text.Substring(chunkStart, chunkLength);
				chunkStart = textElements.ElementIndex;
				chunkLength = 0;
			}

			chunkLength += element.Length;
		}

		if (chunkLength > 0)
			yield return text.Substring(chunkStart, chunkLength);
	}
}
