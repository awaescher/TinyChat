namespace TinyChat;

partial class FunctionCallMessageControl
{
	private System.ComponentModel.IContainer components = null;

	protected override void Dispose(bool disposing)
	{
		if (disposing && (components != null))
			components.Dispose();
		base.Dispose(disposing);
	}

	private void InitializeComponent()
	{
		_callIconLabel = new Label();
		_resultIconLabel = new Label();
		_callTitleLabel = new Label();
		_argsLabel = new Label();
		_resultLabel = new Label();
		_table = new TableLayoutPanel();
		_table.SuspendLayout();
		SuspendLayout();

		// _callIconLabel
		_callIconLabel.AutoSize = false;
		_callIconLabel.Dock = DockStyle.Fill;
		_callIconLabel.Text = TOOL_CALL_ICON;
		_callIconLabel.TextAlign = ContentAlignment.MiddleCenter;
		_callIconLabel.UseMnemonic = false;
		_callIconLabel.Width = ICON_WIDTH;

		// _resultIconLabel
		_resultIconLabel.AutoSize = false;
		_resultIconLabel.Dock = DockStyle.Fill;
		_resultIconLabel.Text = RESULT_ICON;
		_resultIconLabel.TextAlign = ContentAlignment.MiddleCenter;
		_resultIconLabel.UseMnemonic = false;
		_resultIconLabel.Visible = false;
		_resultIconLabel.Width = ICON_WIDTH;

		// _callTitleLabel
		_callTitleLabel.AutoSize = true;
		_callTitleLabel.Dock = DockStyle.Fill;
		_callTitleLabel.Cursor = Cursors.Hand;
		_callTitleLabel.UseMnemonic = false;
		_callTitleLabel.TextAlign = ContentAlignment.MiddleLeft;

		// _argsLabel
		_argsLabel.AutoSize = true;
		_argsLabel.Cursor = Cursors.Hand;
		_argsLabel.Dock = DockStyle.Fill;
		_argsLabel.Padding = new Padding(0, 2, 0, 6);
		_argsLabel.UseMnemonic = false;
		_argsLabel.TextAlign = ContentAlignment.TopLeft;
		_argsLabel.Visible = false;

		// _resultLabel
		_resultLabel.AutoSize = true;
		_resultLabel.Cursor = Cursors.Hand;
		_resultLabel.Dock = DockStyle.Fill;
		_resultLabel.UseMnemonic = false;
		_resultLabel.TextAlign = ContentAlignment.MiddleLeft;
		_resultLabel.Visible = false;

		// _table
		// Col 0: fixed icon width | Col 1: remaining text
		// Row 0: call icon | function name
		// Row 1: (no cell 0) | arguments (collapsed=hidden)
		// Row 2: result icon | result (collapsed=hidden)
		//
		// IMPORTANT: do NOT put any always-visible control in rows 1 or 2;
		// a hidden control contributes 0 height, so the row collapses automatically.
		_table.AutoSize = true;
		_table.CellBorderStyle = TableLayoutPanelCellBorderStyle.None;
		_table.ColumnCount = 2;
		_table.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, ICON_WIDTH));
		_table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
		_table.Controls.Add(_callIconLabel, 0, 0);
		_table.Controls.Add(_callTitleLabel, 1, 0);
		// Row 1, col 0 intentionally left empty - no spacer, so the row fully
		// collapses when _argsLabel is hidden.
		_table.Controls.Add(_argsLabel, 1, 1);
		_table.Controls.Add(_resultIconLabel, 0, 2);
		_table.Controls.Add(_resultLabel, 1, 2);
		_table.Cursor = Cursors.Hand;
		_table.Dock = DockStyle.Fill;
		_table.Margin = Padding.Empty;
		_table.Padding = new Padding(8, 0, 0, 0);
		_table.RowCount = 3;
		_table.RowStyles.Add(new RowStyle(SizeType.AutoSize));
		_table.RowStyles.Add(new RowStyle(SizeType.AutoSize));
		_table.RowStyles.Add(new RowStyle(SizeType.AutoSize));

		// FunctionCallMessageControl
		AutoSize = true;
		BorderStyle = BorderStyle.None;
		Cursor = Cursors.Hand;
		Margin = new Padding(3);
		Padding = new Padding(0);
		Controls.Add(_table);
		Click += Toggle;
		_table.Click += Toggle;
		_callIconLabel.Click += Toggle;
		_callTitleLabel.Click += Toggle;
		_argsLabel.Click += Toggle;
		_resultIconLabel.Click += Toggle;
		_resultLabel.Click += Toggle;

		_table.ResumeLayout(false);
		_table.PerformLayout();
		ResumeLayout(false);
		PerformLayout();
	}

	private TableLayoutPanel _table;
	private Label _callIconLabel;
	private Label _callTitleLabel;
	private Label _argsLabel;
	private Label _resultIconLabel;
	private Label _resultLabel;
}
