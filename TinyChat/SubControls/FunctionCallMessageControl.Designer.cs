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
		lblIcon = new Label();
		lblResultIcon = new Label();
		lblTitle = new Label();
		lblArgs = new Label();
		lblResult = new Label();
		tableLayout = new TableLayoutPanel();
		tableLayout.SuspendLayout();
		SuspendLayout();

		// lblIcon
		lblIcon.AutoSize = false;
		lblIcon.Dock = DockStyle.Fill;
		lblIcon.Text = TOOL_CALL_ICON;
		lblIcon.TextAlign = ContentAlignment.MiddleCenter;
		lblIcon.UseMnemonic = false;
		lblIcon.Width = ICON_WIDTH;

		// lblResultIcon
		lblResultIcon.AutoSize = false;
		lblResultIcon.Dock = DockStyle.Fill;
		lblResultIcon.Text = RESULT_ICON;
		lblResultIcon.TextAlign = ContentAlignment.MiddleCenter;
		lblResultIcon.MaximumSize = new Size(0, ICON_WIDTH);
		lblResultIcon.UseMnemonic = false;
		lblResultIcon.Visible = false;
		lblResultIcon.Width = ICON_WIDTH;

		// lblTitle
		lblTitle.AutoSize = true;
		lblTitle.Dock = DockStyle.Fill;
		lblTitle.Cursor = Cursors.Hand;
		lblTitle.UseMnemonic = false;
		lblTitle.TextAlign = ContentAlignment.MiddleLeft;

		// lblArgs
		lblArgs.AutoSize = true;
		lblArgs.Cursor = Cursors.Hand;
		lblArgs.Dock = DockStyle.Fill;
		lblArgs.Padding = new Padding(0, 2, 0, 6);
		lblArgs.UseMnemonic = false;
		lblArgs.TextAlign = ContentAlignment.TopLeft;
		lblArgs.Visible = false;

		// lblResult
		lblResult.AutoSize = true;
		lblResult.Cursor = Cursors.Hand;
		lblResult.Dock = DockStyle.Fill;
		lblResult.UseMnemonic = false;
		lblResult.TextAlign = ContentAlignment.MiddleLeft;
		lblResult.Visible = false;

		// _table
		// Col 0: fixed icon width | Col 1: remaining text
		// Row 0: call icon | function name
		// Row 1: (no cell 0) | arguments (collapsed=hidden)
		// Row 2: result icon | result (collapsed=hidden)
		//
		// IMPORTANT: do NOT put any always-visible control in rows 1 or 2;
		// a hidden control contributes 0 height, so the row collapses automatically.
		tableLayout.AutoSize = true;
		tableLayout.CellBorderStyle = TableLayoutPanelCellBorderStyle.None;
		tableLayout.ColumnCount = 2;
		tableLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, ICON_WIDTH));
		tableLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
		tableLayout.Controls.Add(lblIcon, 0, 0);
		tableLayout.Controls.Add(lblTitle, 1, 0);
		// Row 1, col 0 intentionally left empty - no spacer, so the row fully
		// collapses when _argsLabel is hidden.
		tableLayout.Controls.Add(lblArgs, 1, 1);
		tableLayout.Controls.Add(lblResultIcon, 0, 2);
		tableLayout.Controls.Add(lblResult, 1, 2);
		tableLayout.Cursor = Cursors.Hand;
		tableLayout.Dock = DockStyle.Fill;
		tableLayout.Margin = Padding.Empty;
		tableLayout.Padding = new Padding(8, 0, 0, 0);
		tableLayout.RowCount = 3;
		tableLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
		tableLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
		tableLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));

		// FunctionCallMessageControl
		AutoSize = true;
		BorderStyle = BorderStyle.None;
		Cursor = Cursors.Hand;
		Margin = new Padding(3);
		Padding = new Padding(0);
		Controls.Add(tableLayout);
		Click += Toggle;
		tableLayout.Click += Toggle;
		lblIcon.Click += Toggle;
		lblTitle.Click += Toggle;
		lblArgs.Click += Toggle;
		lblResultIcon.Click += Toggle;
		lblResult.Click += Toggle;

		tableLayout.ResumeLayout(false);
		tableLayout.PerformLayout();
		ResumeLayout(false);
		PerformLayout();
	}

	private TableLayoutPanel tableLayout;
	private Label lblIcon;
	private Label lblTitle;
	private Label lblArgs;
	private Label lblResultIcon;
	private Label lblResult;
}
