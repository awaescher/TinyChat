using System.Text.RegularExpressions;
using DevExpress.XtraEditors;

namespace DevExpressDemo;

/// <summary>
/// A DevExpress control that displays a collapsible "thinking" section using HTML formatting.
/// Shows "Thinking ..." by default, expandable on click to show full content.
/// </summary>
public partial class DXThinkingControl : PanelControl
{
	private static readonly Regex _thinkPlaceholderRegex = new(@"\{THINK\|([^|]*)\|([^}]*)\}", RegexOptions.Compiled);

	private readonly LabelControl _label;
	private readonly string _expandedContent;
	private bool _isExpanded;

	public DXThinkingControl(string collapsedText, string expandedContent)
	{
		BorderStyle = DevExpress.XtraEditors.Controls.BorderStyles.Simple;
		AutoSize = true;
		Padding = new System.Windows.Forms.Padding(4);

		_expandedContent = expandedContent;

		_label = new LabelControl
		{
			AllowHtmlString = true,
			AutoSizeMode = LabelAutoSizeMode.Vertical,
			Dock = System.Windows.Forms.DockStyle.Fill,
			Cursor = System.Windows.Forms.Cursors.Hand,
			UseMnemonic = false
		};

		UpdateLabelText(collapsedText);

		Controls.Add(_label);

		_label.Click += OnLabelClick;
		Click += OnLabelClick;
	}

	private void UpdateLabelText(string collapsedText)
	{
		if (_isExpanded)
		{
			_label.Text = $"<i><color=gray>▼ Thinking ...</color></i><br/>{_expandedContent}";
		}
		else
		{
			_label.Text = $"<i><color=gray>▶ {collapsedText}</color></i>";
		}
	}

	private void OnLabelClick(object? sender, System.EventArgs e)
	{
		_isExpanded = !_isExpanded;
		UpdateLabelText("Thinking ...");
	}

	public static bool HasThinkPlaceholders(string text) => _thinkPlaceholderRegex.IsMatch(text);

	public static string ExtractTextWithoutThinkPlaceholders(string text) => _thinkPlaceholderRegex.Replace(text, "");

	public static System.Collections.Generic.IEnumerable<DXThinkingControl> CreateThinkingControls(string text)
	{
		var matches = _thinkPlaceholderRegex.Matches(text);
		foreach (System.Text.RegularExpressions.Match match in matches)
		{
			var collapsedText = match.Groups[1].Value;
			var expandedContent = match.Groups[2].Value;
			yield return new DXThinkingControl(collapsedText, expandedContent);
		}
	}
}
