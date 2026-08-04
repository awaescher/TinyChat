using System.Net;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using DevExpress.Utils;
using DevExpress.Utils.Html;
using DevExpress.Utils.Html.Internal;
using DevExpress.XtraEditors;
using TinyChat.Messages.Formatting;

namespace TinyChat;

/// <summary>
/// Displays selectable, formatted message content in one permanently visible DevExpress HTML control.
/// </summary>
internal sealed class DXSelectableMessageTextControl : HtmlContentControl
{
	private const int CONTENT_PADDING = 2;
	private const string CONTENT_ELEMENT_ID = "message-content";
	private const string EMPTY_HTML = "&#8203;";
	private const string EMPTY_TEMPLATE = """<div id="message-content">&#8203;</div>""";
	private const string STYLES = """
		#message-content {
			display: block;
			width: 100%;
			margin: 0;
			padding: 0;
			white-space: pre-wrap;
			overflow-wrap: break-word;
			user-select: text;
		}
		p, h1, h2, h3, h4, h5, h6, ul, ol {
			margin-top: 0;
			margin-bottom: 4px;
		}
		.code {
			font-family: Consolas;
		}
		.size-plus-1 { font-size: 110%; }
		.size-plus-2 { font-size: 120%; }
		.size-plus-3 { font-size: 135%; }
		.size-plus-4 { font-size: 150%; }
		.size-plus-5 { font-size: 170%; }
		.size-minus-1 { font-size: 90%; }
		.size-minus-2 { font-size: 80%; }
		.size-minus-3, .size-minus-4, .size-minus-5 { font-size: 70%; }
		.subscript, .superscript { font-size: 75%; }
		""";

	private static WeakReference<DXSelectableMessageTextControl>? _activeSelectionOwner;

	private bool _adjustingHeight;
	private int _lastMeasuredWidth = -1;
	private int _preferredContentHeight;
	private string _displayHtml = string.Empty;

	/// <summary>
	/// Raised immediately before the control changes its height.
	/// </summary>
	internal event EventHandler? ContentHeightChanging;

	/// <summary>
	/// Raised immediately after the control changes its height.
	/// </summary>
	internal event EventHandler? ContentHeightChanged;

	/// <summary>
	/// Initializes a new instance of the <see cref="DXSelectableMessageTextControl"/> class.
	/// </summary>
	public DXSelectableMessageTextControl()
	{
		AllowContentSelection = DefaultBoolean.True;
		AllowFocus = true;
		AutoScroll = false;
		Cursor = Cursors.IBeam;
		Margin = Padding.Empty;
		Padding = Padding.Empty;
		TabStop = true;
		HtmlTemplate.Set(EMPTY_TEMPLATE, STYLES);
		SelectedTextChanged += SelectedMessageTextChanged;

		_preferredContentHeight = Font.Height + CONTENT_PADDING;
		Height = _preferredContentHeight;
	}

	/// <summary>
	/// Replaces formatted output while preserving the current selection when its DOM range remains valid.
	/// </summary>
	/// <param name="formattedText">The formatter output.</param>
	internal void SetFormattedTextPreservingSelection(string? formattedText)
	{
		var formatted = formattedText ?? string.Empty;
		SetContent(
			DXFormattedTextConverter.ToHtmlTemplate(formatted),
			DXFormattedTextConverter.ToPlainText(formatted));
	}

	/// <summary>
	/// Replaces plain text while preserving the current selection when its DOM range remains valid.
	/// </summary>
	/// <param name="text">The new plain text.</param>
	internal void SetTextPreservingSelection(string? text)
	{
		var plainText = text ?? string.Empty;
		SetContent(DXFormattedTextConverter.PlainTextToHtmlTemplate(plainText), plainText);
	}

	/// <summary>
	/// Applies the message font and colours to the HTML presentation surface.
	/// </summary>
	internal void ApplyTextAppearance(Font font, Color foreColor, Color backColor)
	{
		Font = font;
		ForeColor = foreColor;
		BackColor = backColor;

		Appearance.Font = font;
		Appearance.ForeColor = foreColor;
		Appearance.BackColor = backColor;
		Appearance.Options.UseFont = true;
		Appearance.Options.UseForeColor = true;
		Appearance.Options.UseBackColor = true;
		UpdateContentHeight();
	}

	/// <summary>
	/// Recalculates the height for the current HTML, font, and width.
	/// </summary>
	internal void UpdateContentHeight()
	{
		if (_adjustingHeight)
			return;

		var availableWidth = ClientSize.Width;
		if (availableWidth <= 0)
			availableWidth = MaximumSize.Width;
		if (availableWidth <= 0)
			availableWidth = Width;
		if (availableWidth <= 0)
			return;

		_lastMeasuredWidth = availableWidth;
		var requiredHeight = Math.Max(Font.Height + CONTENT_PADDING, ViewInfo.CalcBestSize(availableWidth).Height);
		_preferredContentHeight = requiredHeight;
		if (Height == requiredHeight)
			return;

		ContentHeightChanging?.Invoke(this, EventArgs.Empty);
		_adjustingHeight = true;
		try
		{
			Height = requiredHeight;
		}
		finally
		{
			_adjustingHeight = false;
		}
		ContentHeightChanged?.Invoke(this, EventArgs.Empty);
	}

	/// <summary>
	/// Clears this control's current HTML text selection.
	/// </summary>
	internal void ClearContentSelection()
	{
		ViewInfo.Element?.Selection.Clear();
	}

	/// <inheritdoc />
	public override Size GetPreferredSize(Size proposedSize)
	{
		var preferredSize = base.GetPreferredSize(proposedSize);
		return new Size(preferredSize.Width, Math.Max(_preferredContentHeight, Font.Height + CONTENT_PADDING));
	}

	/// <inheritdoc />
	protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
	{
		if (keyData == (Keys.Control | Keys.A))
		{
			SelectAllContent();
			return true;
		}

		if (keyData is (Keys.Control | Keys.C) or (Keys.Control | Keys.Insert) &&
			!string.IsNullOrEmpty(SelectedText))
		{
			try
			{
				Clipboard.SetText(SelectedText);
				return true;
			}
			catch (ExternalException)
			{
				// The clipboard may temporarily be locked by another process.
			}
		}

		return base.ProcessCmdKey(ref msg, keyData);
	}

	/// <inheritdoc />
	protected override void OnFontChanged(EventArgs e)
	{
		base.OnFontChanged(e);
		UpdateContentHeight();
	}

	/// <inheritdoc />
	protected override void OnHandleCreated(EventArgs e)
	{
		base.OnHandleCreated(e);
		EnsureContentElement();
		UpdateContentHeight();
	}

	/// <inheritdoc />
	protected override void OnGotFocus(EventArgs e)
	{
		ActivateSelectionOwner();
		base.OnGotFocus(e);
	}

	/// <inheritdoc />
	protected override void OnLostFocus(EventArgs e)
	{
		base.OnLostFocus(e);
		ClearContentSelection();
	}

	/// <inheritdoc />
	protected override void OnMouseDown(MouseEventArgs e)
	{
		ActivateSelectionOwner();
		base.OnMouseDown(e);
	}

	/// <inheritdoc />
	protected override void OnSizeChanged(EventArgs e)
	{
		base.OnSizeChanged(e);

		if (!_adjustingHeight && ClientSize.Width != _lastMeasuredWidth)
			UpdateContentHeight();
	}

	/// <inheritdoc />
	protected override void Dispose(bool disposing)
	{
		if (disposing)
			SelectedTextChanged -= SelectedMessageTextChanged;

		base.Dispose(disposing);
	}

	private void SetContent(string html, string plainText)
	{
		if (string.Equals(_displayHtml, html, StringComparison.Ordinal) &&
			string.Equals(Text, plainText, StringComparison.Ordinal))
		{
			return;
		}

		_ = EnsureContentElement();
		var previousSelection = (IDxHtmlSelection?)ViewInfo.Element?.Selection;
		var previousRange = previousSelection?.Current ?? DxHtmlSelectionRange.Empty;
		var hadSelection = previousSelection is { IsEmpty: false };
		previousSelection?.Clear();

		var renderedHtml = string.IsNullOrWhiteSpace(html) ? EMPTY_HTML : html;
		HtmlTemplate.Set($"""<div id="{CONTENT_ELEMENT_ID}">{renderedHtml}</div>""", STYLES);
		_displayHtml = html;
		Text = plainText;

		if (hadSelection)
		{
			_ = EnsureContentElement();
			var currentSelection = (IDxHtmlSelection?)ViewInfo.Element?.Selection;
			TryRestoreSelection(currentSelection, previousRange);
		}

		UpdateContentHeight();
		Invalidate();
	}

	private DxHtmlElement EnsureContentElement()
	{
		if (FindElementById(CONTENT_ELEMENT_ID) is { } existing)
			return existing;

		var availableWidth = MaximumSize.Width > 0
			? MaximumSize.Width
			: Math.Max(1, Width);
		_ = ViewInfo.CalcBestSize(availableWidth);

		return FindElementById(CONTENT_ELEMENT_ID)
			?? throw new InvalidOperationException("The message HTML content element could not be created.");
	}

	private void ActivateSelectionOwner()
	{
		if (_activeSelectionOwner?.TryGetTarget(out var previous) == true &&
			!ReferenceEquals(previous, this) &&
			!previous.IsDisposed)
		{
			previous.ClearContentSelection();
		}

		_activeSelectionOwner = new WeakReference<DXSelectableMessageTextControl>(this);
	}

	private void SelectedMessageTextChanged(object? sender, EventArgs e)
	{
		if (!string.IsNullOrEmpty(SelectedText))
			ActivateSelectionOwner();
	}

	private void SelectAllContent()
	{
		var contentElement = EnsureContentElement();
		var textElements = EnumerateNodes(contentElement)
			.OfType<DxHtmlTextElementBase>()
			.Where(element => !string.IsNullOrEmpty(element.Text))
			.ToArray();
		if (textElements.Length == 0)
			return;

		var first = textElements[0];
		var last = textElements[^1];
		var range = new DxHtmlSelectionRange(first, last, 0, last.Text.Length);
		((IDxHtmlSelection)ViewInfo.Element.Selection).Current = range;
	}

	private static IEnumerable<DxHtmlNode> EnumerateNodes(DxHtmlNode node)
	{
		foreach (DxHtmlNode child in node.ChildNodes)
		{
			yield return child;

			foreach (var descendant in EnumerateNodes(child))
				yield return descendant;
		}
	}

	private static void TryRestoreSelection(IDxHtmlSelection? selection, DxHtmlSelectionRange range)
	{
		if (selection is null || range.IsEmpty)
			return;

		try
		{
			selection.Current = range;
		}
		catch (ArgumentOutOfRangeException)
		{
			// Streaming can restructure an incomplete markup range.
		}
		catch (IndexOutOfRangeException)
		{
			// Streaming can remove the DOM nodes referenced by the old range.
		}
		catch (InvalidOperationException)
		{
			// A transient, incomplete HTML fragment may not accept the previous range.
		}
	}
}

/// <summary>
/// Converts formatter output into safe HTML/CSS-template markup and readable copy text.
/// </summary>
internal static class DXFormattedTextConverter
{
	private static readonly PlainTextMessageFormatter _plainTextFormatter = new();
	private static readonly Regex _htmlTagRegex = new(
		"""<\s*(?<closing>/)?\s*(?<tag>backcolor|blockquote|color|font|nbsp|size|span|div|h[1-6]|table|thead|tbody|tfoot|tr|th|td|ul|ol|li|sub|sup|b|i|s|u|p|a|br|img)\b(?<attributes>[^>]*)>""",
		RegexOptions.Compiled | RegexOptions.IgnoreCase);
	private static readonly Regex _attributeRegex = new(
		"""(?<name>[a-z][a-z0-9-]*)\s*=\s*(?:"(?<double>[^"]*)"|'(?<single>[^']*)')""",
		RegexOptions.Compiled | RegexOptions.IgnoreCase);

	/// <summary>
	/// Converts sanitized formatter output, including the legacy DevExpress tag dialect, to template HTML.
	/// </summary>
	internal static string ToHtmlTemplate(string formattedText)
	{
		if (string.IsNullOrEmpty(formattedText))
			return string.Empty;

		var result = new StringBuilder(formattedText.Length);
		var offset = 0;
		foreach (Match match in _htmlTagRegex.Matches(formattedText))
		{
			AppendEncodedText(result, formattedText.AsSpan(offset, match.Index - offset));
			result.Append(ConvertTag(match));
			offset = match.Index + match.Length;
		}

		AppendEncodedText(result, formattedText.AsSpan(offset));
		return result.ToString();
	}

	/// <summary>
	/// Encodes plain text for the HTML template renderer.
	/// </summary>
	internal static string PlainTextToHtmlTemplate(string plainText)
	{
		var result = new StringBuilder(plainText.Length);
		AppendEncodedText(result, plainText.AsSpan());
		return result.ToString();
	}

	/// <summary>
	/// Removes display markup while retaining common rendered line breaks.
	/// </summary>
	internal static string ToPlainText(string formattedText)
	{
		var textWithLineBreaks = formattedText
			.Replace("<br>", Environment.NewLine, StringComparison.OrdinalIgnoreCase)
			.Replace("<br/>", Environment.NewLine, StringComparison.OrdinalIgnoreCase)
			.Replace("<br />", Environment.NewLine, StringComparison.OrdinalIgnoreCase)
			.Replace("<nbsp>", "\u00a0", StringComparison.OrdinalIgnoreCase)
			.Replace("<nbsp/>", "\u00a0", StringComparison.OrdinalIgnoreCase)
			.Replace("</p>", Environment.NewLine, StringComparison.OrdinalIgnoreCase)
			.Replace("</div>", Environment.NewLine, StringComparison.OrdinalIgnoreCase)
			.Replace("</li>", Environment.NewLine, StringComparison.OrdinalIgnoreCase);

		return WebUtility.HtmlDecode(_plainTextFormatter.Format(textWithLineBreaks)).TrimEnd();
	}

	private static string ConvertTag(Match match)
	{
		var closing = match.Groups["closing"].Success;
		var tag = match.Groups["tag"].Value.ToLowerInvariant();
		var attributes = match.Groups["attributes"].Value;

		if (tag == "br")
			return closing ? string.Empty : "<br>";
		if (tag == "nbsp")
			return closing ? string.Empty : "&nbsp;";
		if (tag == "img")
			return closing ? string.Empty : EncodeImageAltText(attributes);

		if (tag is "color" or "backcolor" or "font" or "size" or "sub" or "sup")
			return ConvertLegacyFormattingTag(tag, attributes, closing);

		if (closing)
			return $"</{tag}>";

		return tag switch
		{
			"a" => $"<a{SanitizeAnchorAttributes(attributes)}>",
			"ol" => $"<ol{SanitizeListAttributes(attributes, ordered: true)}>",
			"li" => $"<li{SanitizeListItemAttributes(attributes)}>",
			"p" => $"<p{SanitizeAlignmentAttribute(attributes)}>",
			"span" => $"<span{SanitizeSpanAttributes(attributes)}>",
			_ => $"<{tag}>"
		};
	}

	private static string ConvertLegacyFormattingTag(string tag, string attributes, bool closing)
	{
		if (closing)
			return "</span>";

		return tag switch
		{
			"color" => CreateColorSpan("color", attributes),
			"backcolor" => CreateColorSpan("background-color", attributes),
			"font" => CreateFontSpan(attributes),
			"size" => CreateSizeSpan(attributes),
			"sub" => """<span class="subscript">""",
			"sup" => """<span class="superscript">""",
			_ => "<span>"
		};
	}

	private static string CreateColorSpan(string propertyName, string attributes)
	{
		var separator = attributes.IndexOf('=');
		if (separator < 0)
			return "<span>";

		var color = NormalizeCssColor(WebUtility.HtmlDecode(attributes[(separator + 1)..].Trim().Trim('"', '\'')));
		return color is null
			? "<span>"
			: $"<span style=\"{propertyName}: {EncodeAttribute(color)};\">";
	}

	private static string CreateFontSpan(string attributes)
	{
		var fontName = ExtractLegacyValue(attributes, "face");
		if (string.IsNullOrWhiteSpace(fontName))
			return """<span class="code">""";

		var safeFontName = new string(fontName
			.Where(character => char.IsLetterOrDigit(character) || character is ' ' or '-' or '_')
			.ToArray());
		if (string.IsNullOrWhiteSpace(safeFontName))
			return """<span class="code">""";

		return $"<span style=\"font-family: &quot;{EncodeAttribute(safeFontName)}&quot;;\">";
	}

	private static string CreateSizeSpan(string attributes)
	{
		var rawValue = ExtractLegacyValue(attributes, null);
		if (!int.TryParse(rawValue, out var relativeSize) || relativeSize == 0)
			return "<span>";

		relativeSize = Math.Clamp(relativeSize, -5, 5);
		var sign = relativeSize > 0 ? "plus" : "minus";
		return $"<span class=\"size-{sign}-{Math.Abs(relativeSize)}\">";
	}

	private static string SanitizeAnchorAttributes(string attributes)
	{
		var href = ExtractAttribute(attributes, "href");
		if (href is null || !IsSafeLink(href))
			return string.Empty;

		return $" href=\"{EncodeAttribute(href)}\"";
	}

	private static string SanitizeSpanAttributes(string attributes)
	{
		var style = ExtractAttribute(attributes, "style");
		if (string.IsNullOrWhiteSpace(style))
			return string.Empty;

		var safeDeclarations = new List<string>();
		foreach (var declaration in style.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
		{
			var separator = declaration.IndexOf(':');
			if (separator <= 0)
				continue;

			var name = declaration[..separator].Trim().ToLowerInvariant();
			var value = declaration[(separator + 1)..].Trim();
			switch (name)
			{
				case "color":
					if (NormalizeCssColor(value) is { } color)
						safeDeclarations.Add($"color: {color}");
					break;
				case "background-color":
					if (NormalizeCssColor(value) is { } backColor)
						safeDeclarations.Add($"background-color: {backColor}");
					break;
				case "font-weight" when value is "bold" or "normal":
					safeDeclarations.Add($"font-weight: {value}");
					break;
				case "font-style" when value is "italic" or "normal":
					safeDeclarations.Add($"font-style: {value}");
					break;
				case "text-decoration" when value is "underline" or "line-through" or "none":
					safeDeclarations.Add($"text-decoration: {value}");
					break;
			}
		}

		return safeDeclarations.Count == 0
			? string.Empty
			: $" style=\"{EncodeAttribute(string.Join("; ", safeDeclarations))}\"";
	}

	private static string SanitizeListAttributes(string attributes, bool ordered)
	{
		if (!ordered)
			return string.Empty;

		var result = new StringBuilder();
		var start = ExtractAttribute(attributes, "start");
		if (int.TryParse(start, out var startValue))
			result.Append($" start=\"{startValue}\"");

		var type = ExtractAttribute(attributes, "type");
		if (type is "1" or "a" or "A" or "i" or "I")
			result.Append($" type=\"{type}\"");

		return result.ToString();
	}

	private static string SanitizeListItemAttributes(string attributes)
	{
		var value = ExtractAttribute(attributes, "value");
		return int.TryParse(value, out var itemValue)
			? $" value=\"{itemValue}\""
			: string.Empty;
	}

	private static string SanitizeAlignmentAttribute(string attributes)
	{
		var alignment = ExtractAttribute(attributes, "align")?.ToLowerInvariant();
		return alignment is "left" or "right" or "center" or "justify"
			? $" align=\"{alignment}\""
			: string.Empty;
	}

	private static string EncodeImageAltText(string attributes)
	{
		var altText = ExtractAttribute(attributes, "alt");
		if (string.IsNullOrEmpty(altText))
			return string.Empty;

		var result = new StringBuilder(altText.Length);
		AppendEncodedText(result, altText.AsSpan());
		return result.ToString();
	}

	private static string? NormalizeCssColor(string value)
	{
		var color = value.Trim();
		if (Regex.IsMatch(color, """^#(?:[0-9a-f]{3}|[0-9a-f]{6}|[0-9a-f]{8})$""", RegexOptions.IgnoreCase))
			return color;
		if (Regex.IsMatch(color, "^[a-z]+$", RegexOptions.IgnoreCase))
			return color;

		var components = color.Split(',', StringSplitOptions.TrimEntries);
		if (components.Length is not (3 or 4) ||
			components.Any(component => !int.TryParse(component, out var number) || number is < 0 or > 255))
		{
			return null;
		}

		if (components.Length == 3)
			return $"rgb({string.Join(",", components)})";

		var alpha = int.Parse(components[0]) / 255d;
		return FormattableString.Invariant($"rgba({components[1]},{components[2]},{components[3]},{alpha:0.###})");
	}

	private static string? ExtractLegacyValue(string attributes, string? attributeName)
	{
		if (attributeName is not null && ExtractAttribute(attributes, attributeName) is { } attributeValue)
			return WebUtility.HtmlDecode(attributeValue);

		var separator = attributes.IndexOf('=');
		if (separator < 0)
			return null;

		return WebUtility.HtmlDecode(attributes[(separator + 1)..].Trim().Trim('"', '\''));
	}

	private static string? ExtractAttribute(string attributes, string name)
	{
		foreach (Match match in _attributeRegex.Matches(attributes))
		{
			if (!string.Equals(match.Groups["name"].Value, name, StringComparison.OrdinalIgnoreCase))
				continue;

			return match.Groups["double"].Success
				? WebUtility.HtmlDecode(match.Groups["double"].Value)
				: WebUtility.HtmlDecode(match.Groups["single"].Value);
		}

		return null;
	}

	private static bool IsSafeLink(string? href)
	{
		if (string.IsNullOrWhiteSpace(href))
			return false;
		if (!Uri.TryCreate(href, UriKind.RelativeOrAbsolute, out var uri))
			return false;
		if (!uri.IsAbsoluteUri)
			return true;

		return string.Equals(uri.Scheme, Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase) ||
			string.Equals(uri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase) ||
			string.Equals(uri.Scheme, Uri.UriSchemeMailto, StringComparison.OrdinalIgnoreCase);
	}

	private static string EncodeAttribute(string value)
	{
		return WebUtility.HtmlEncode(value).Replace("$", "&#36;", StringComparison.Ordinal);
	}

	private static void AppendEncodedText(StringBuilder result, ReadOnlySpan<char> text)
	{
		if (text.IsEmpty)
			return;

		var decoded = WebUtility.HtmlDecode(text.ToString());
		var encoded = WebUtility.HtmlEncode(decoded).Replace("$", "&#36;", StringComparison.Ordinal);
		result.Append(encoded);
	}
}
