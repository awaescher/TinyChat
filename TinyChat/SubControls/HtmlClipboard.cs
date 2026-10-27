using System.Globalization;
using System.Net;
using System.Text;

namespace TinyChat;

/// <summary>
/// Writes an HTML fragment together with its plain text to the clipboard using the CF_HTML format,
/// so that formatting is kept when pasting into applications such as Word or Outlook.
/// </summary>
internal static class HtmlClipboard
{
	private const string HEADER_TEMPLATE = "Version:0.9\r\nStartHTML:{0:D10}\r\nEndHTML:{1:D10}\r\nStartFragment:{2:D10}\r\nEndFragment:{3:D10}\r\n";
	private const string FRAGMENT_START = "<!--StartFragment-->";
	private const string FRAGMENT_END = "<!--EndFragment-->";

	/// <summary>
	/// Creates the clipboard data containing the HTML fragment and its plain text.
	/// </summary>
	/// <param name="htmlFragment">The HTML to paste.</param>
	/// <param name="plainText">The text used by applications that do not accept HTML.</param>
	/// <param name="font">The font the fragment is displayed with, or <see langword="null"/>.</param>
	internal static DataObject CreateDataObject(string htmlFragment, string plainText, Font? font)
	{
		var data = new DataObject();
		data.SetData(DataFormats.Html, CreateCfHtml(htmlFragment, font));
		data.SetData(DataFormats.UnicodeText, plainText);
		return data;
	}

	/// <summary>
	/// Wraps an HTML fragment into the CF_HTML clipboard format, including the byte offsets it requires.
	/// </summary>
	internal static string CreateCfHtml(string htmlFragment, Font? font)
	{
		var style = font is null
			? string.Empty
			: string.Create(
				CultureInfo.InvariantCulture,
				$" style=\"font-family:'{WebUtility.HtmlEncode(font.FontFamily.Name)}';font-size:{font.SizeInPoints:0.##}pt\"");
		var prefix = $"<html><body>{FRAGMENT_START}<div{style}>";
		var suffix = $"</div>{FRAGMENT_END}</body></html>";

		var headerLength = string.Format(CultureInfo.InvariantCulture, HEADER_TEMPLATE, 0, 0, 0, 0).Length;
		var startHtml = headerLength;
		var startFragment = startHtml + Encoding.UTF8.GetByteCount($"<html><body>{FRAGMENT_START}");
		var endFragment = startHtml + Encoding.UTF8.GetByteCount(prefix + htmlFragment + "</div>");
		var endHtml = startHtml + Encoding.UTF8.GetByteCount(prefix + htmlFragment + suffix);

		return string.Format(CultureInfo.InvariantCulture, HEADER_TEMPLATE, startHtml, endHtml, startFragment, endFragment) +
			prefix + htmlFragment + suffix;
	}
}
