using System;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using DevExpress.XtraEditors;

namespace DevExpressDemo;

/// <summary>
/// A DevExpress-styled collapsible panel that displays "Thinking..." content that can be expanded/collapsed.
/// Used to show AI thinking/reasoning content in a compact, interactive way.
/// </summary>
public class DXCollapsibleThinkPanel : PanelControl
{
	private bool _isExpanded;
	private readonly LabelControl _headerLabel;
	private readonly LabelControl _contentLabel;
	private readonly SimpleButton _toggleButton;
	private readonly PanelControl _contentPanel;
	private string _thinkContent = string.Empty;

	/// <summary>
	/// Gets or sets whether the think content is expanded.
	/// </summary>
	[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
	public bool IsExpanded
	{
		get => _isExpanded;
		set
		{
			if (_isExpanded == value)
				return;

			_isExpanded = value;
			UpdateExpandedState();
		}
	}

	/// <summary>
	/// Gets or sets the think content to display when expanded.
	/// </summary>
	[DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
	public string ThinkContent
	{
		get => _thinkContent;
		set
		{
			_thinkContent = value ?? string.Empty;
			_contentLabel.Text = _thinkContent;
		}
	}

	/// <summary>
	/// Event raised when the expanded state changes.
	/// </summary>
	public event EventHandler? ExpandedChanged;

	/// <summary>
	/// Initializes a new instance of the <see cref="DXCollapsibleThinkPanel"/> class.
	/// </summary>
	public DXCollapsibleThinkPanel()
	{
		AutoSize = true;
		Padding = new Padding(4);
		BorderStyle = DevExpress.XtraEditors.Controls.BorderStyles.Simple;
		Appearance.BackColor = Color.FromArgb(248, 248, 248);
		Margin = new Padding(0, 4, 0, 4);

		// Header panel with toggle button and "Thinking..." label
		var headerPanel = new PanelControl
		{
			Dock = DockStyle.Top,
			Height = 26,
			AutoSize = false,
			BorderStyle = DevExpress.XtraEditors.Controls.BorderStyles.NoBorder
		};

		_toggleButton = new SimpleButton
		{
			Text = "▶",
			Size = new Size(24, 22),
			Location = new Point(0, 0),
			Cursor = Cursors.Hand,
			TabStop = false,
			AllowFocus = false
		};
		_toggleButton.Appearance.Options.UseTextOptions = true;
		_toggleButton.Appearance.TextOptions.HAlignment = DevExpress.Utils.HorzAlignment.Center;
		_toggleButton.Click += (_, _) => IsExpanded = !IsExpanded;

		_headerLabel = new LabelControl
		{
			Text = "Thinking...",
			AutoSizeMode = LabelAutoSizeMode.Default,
			Location = new Point(28, 4),
			Cursor = Cursors.Hand,
			UseMnemonic = false
		};
		_headerLabel.Appearance.ForeColor = Color.FromArgb(100, 100, 100);
		_headerLabel.Appearance.Font = new Font(Font.FontFamily, Font.Size, FontStyle.Italic);
		_headerLabel.Click += (_, _) => IsExpanded = !IsExpanded;

		headerPanel.Controls.Add(_toggleButton);
		headerPanel.Controls.Add(_headerLabel);

		// Content panel (initially hidden)
		_contentPanel = new PanelControl
		{
			Dock = DockStyle.Top,
			AutoSize = true,
			Visible = false,
			Padding = new Padding(28, 4, 4, 4),
			BorderStyle = DevExpress.XtraEditors.Controls.BorderStyles.NoBorder
		};

		_contentLabel = new LabelControl
		{
			Dock = DockStyle.Fill,
			AutoSizeMode = LabelAutoSizeMode.Vertical,
			UseMnemonic = false,
			AllowHtmlString = true
		};
		_contentLabel.Appearance.ForeColor = Color.FromArgb(80, 80, 80);

		_contentPanel.Controls.Add(_contentLabel);

		Controls.Add(_contentPanel);
		Controls.Add(headerPanel);
	}

	/// <inheritdoc />
	[DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
	public override Size MaximumSize
	{
		get => base.MaximumSize;
		set
		{
			base.MaximumSize = value;
			_contentLabel.MaximumSize = new Size(Math.Max(0, value.Width - Padding.Horizontal - 32), 0);
			_headerLabel.MaximumSize = new Size(Math.Max(0, value.Width - Padding.Horizontal - 32), 0);
		}
	}

	private void UpdateExpandedState()
	{
		_contentPanel.Visible = _isExpanded;
		_toggleButton.Text = _isExpanded ? "▼" : "▶";
		_headerLabel.Text = _isExpanded ? "Thinking" : "Thinking...";

		ExpandedChanged?.Invoke(this, EventArgs.Empty);
	}
}
