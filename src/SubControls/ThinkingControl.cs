using System.Text.RegularExpressions;

namespace TinyChat;

/// <summary>
/// A control that displays a collapsible "thinking" section.
/// Shows "Thinking ..." by default, expandable on click to show full content.
/// </summary>
public partial class ThinkingControl : Panel
{
	private static readonly Regex _thinkPlaceholderRegex = new(@"\{THINK\|([^|]*)\|([^}]*)\}", RegexOptions.Compiled);

	private readonly Label _headerLabel;
	private readonly Label _contentLabel;
	private bool _isExpanded;

	public ThinkingControl(string collapsedText, string expandedContent)
	{
		BorderStyle = BorderStyle.FixedSingle;
		BackColor = SystemColors.Control;
		AutoSize = true;
		Cursor = Cursors.Hand;
		Padding = new Padding(4);

		_headerLabel = new Label
		{
			Text = "▶ " + collapsedText,
			AutoSize = true,
			Dock = DockStyle.Top,
			Font = new Font(Font, FontStyle.Italic),
			ForeColor = SystemColors.GrayText,
			UseMnemonic = false
		};

		_contentLabel = new Label
		{
			Text = expandedContent,
			AutoSize = true,
			Dock = DockStyle.Top,
			Visible = false,
			Padding = new Padding(0, 4, 0, 0),
			UseMnemonic = false
		};

		Controls.Add(_contentLabel);
		Controls.Add(_headerLabel);

		_headerLabel.Click += OnHeaderClick;
		Click += OnHeaderClick;
	}

	private void OnHeaderClick(object? sender, EventArgs e)
	{
		_isExpanded = !_isExpanded;
		_contentLabel.Visible = _isExpanded;
		_headerLabel.Text = _isExpanded ? "▼ Thinking ..." : "▶ Thinking ...";
	}

	public static bool HasThinkPlaceholders(string text) => _thinkPlaceholderRegex.IsMatch(text);

	public static string ExtractTextWithoutThinkPlaceholders(string text) => _thinkPlaceholderRegex.Replace(text, "");

	public static IEnumerable<ThinkingControl> CreateThinkingControls(string text)
	{
		var matches = _thinkPlaceholderRegex.Matches(text);
		foreach (Match match in matches)
		{
			var collapsedText = match.Groups[1].Value;
			var expandedContent = match.Groups[2].Value;
			yield return new ThinkingControl(collapsedText, expandedContent);
		}
	}
}
