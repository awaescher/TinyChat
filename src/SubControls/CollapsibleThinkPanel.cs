using System.ComponentModel;

namespace TinyChat;

/// <summary>
/// A collapsible panel that displays "Thinking..." content that can be expanded/collapsed.
/// Used to show AI thinking/reasoning content in a compact, interactive way.
/// </summary>
public class CollapsibleThinkPanel : Panel
{
	private bool _isExpanded;
	private readonly Label _headerLabel;
	private readonly Label _contentLabel;
	private readonly Button _toggleButton;
	private readonly Panel _contentPanel;
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
	/// Initializes a new instance of the <see cref="CollapsibleThinkPanel"/> class.
	/// </summary>
	public CollapsibleThinkPanel()
	{
		AutoSize = true;
		Padding = new Padding(4);
		BackColor = Color.FromArgb(245, 245, 245);
		BorderStyle = BorderStyle.FixedSingle;
		Margin = new Padding(0, 4, 0, 4);

		// Header panel with toggle button and "Thinking..." label
		var headerPanel = new Panel
		{
			Dock = DockStyle.Top,
			Height = 24,
			AutoSize = false
		};

		_toggleButton = new Button
		{
			Text = "▶",
			Size = new Size(24, 20),
			FlatStyle = FlatStyle.Flat,
			Location = new Point(0, 0),
			Cursor = Cursors.Hand,
			TabStop = false
		};
		_toggleButton.FlatAppearance.BorderSize = 0;
		_toggleButton.Click += (_, _) => IsExpanded = !IsExpanded;

		_headerLabel = new Label
		{
			Text = "Thinking...",
			AutoSize = true,
			Location = new Point(26, 3),
			ForeColor = Color.FromArgb(100, 100, 100),
			Font = new Font(Font.FontFamily, Font.Size, FontStyle.Italic),
			Cursor = Cursors.Hand,
			UseMnemonic = false
		};
		_headerLabel.Click += (_, _) => IsExpanded = !IsExpanded;

		headerPanel.Controls.Add(_toggleButton);
		headerPanel.Controls.Add(_headerLabel);

		// Content panel (initially hidden)
		_contentPanel = new Panel
		{
			Dock = DockStyle.Top,
			AutoSize = true,
			Visible = false,
			Padding = new Padding(26, 4, 4, 4)
		};

		_contentLabel = new Label
		{
			Dock = DockStyle.Fill,
			AutoSize = true,
			ForeColor = Color.FromArgb(80, 80, 80),
			UseMnemonic = false
		};

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
			_contentLabel.MaximumSize = new Size(Math.Max(0, value.Width - Padding.Horizontal - 30), 0);
			_headerLabel.MaximumSize = new Size(Math.Max(0, value.Width - Padding.Horizontal - 30), 0);
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
