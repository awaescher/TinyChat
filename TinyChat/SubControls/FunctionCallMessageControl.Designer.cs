namespace TinyChat;

partial class FunctionCallMessageControl
{
	private System.ComponentModel.IContainer components = null;

	protected override void Dispose(bool disposing)
	{
		if (disposing && _message?.Content is FunctionCallMessageContent content)
			content.PropertyChanged -= OnContentPropertyChanged;
		if (disposing && (components != null))
			components.Dispose();
		base.Dispose(disposing);
	}

	private void InitializeComponent()
	{
		lblSender = new Label();
		lblIcon = new Label();
		lblResultIcon = new Label();
		lblTitle = new Label();
		lblArgs = new SelectableMessageTextControl();
		lblResult = new SelectableMessageTextControl();
		tableLayout = new TableLayoutPanel();
		tableLayout.SuspendLayout();
		SuspendLayout();

		// lblSender
		lblSender.AutoSize = true;
		lblSender.Dock = DockStyle.Top;
		lblSender.Font = new Font(Font, FontStyle.Bold);
		lblSender.UseMnemonic = false;
		lblSender.Padding = new Padding(0, 0, 0, 3);

		// lblIcon
		lblIcon.AutoSize = false;
		lblIcon.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
		lblIcon.Dock = DockStyle.None;
		lblIcon.Height = Font.Height;
		lblIcon.Margin = Padding.Empty;
		lblIcon.Text = TOOL_CALL_ICON;
		lblIcon.TextAlign = ContentAlignment.MiddleCenter;
		lblIcon.UseMnemonic = false;
		lblIcon.Width = ICON_WIDTH;

		// lblResultIcon
		lblResultIcon.AutoSize = false;
		lblResultIcon.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
		lblResultIcon.Dock = DockStyle.None;
		lblResultIcon.Height = Font.Height;
		lblResultIcon.Margin = Padding.Empty;
		lblResultIcon.Text = RESULT_ICON;
		lblResultIcon.TextAlign = ContentAlignment.MiddleCenter;
		lblResultIcon.UseMnemonic = false;
		lblResultIcon.Visible = false;
		lblResultIcon.Width = ICON_WIDTH;

		// lblTitle
		lblTitle.AutoSize = true;
		lblTitle.Dock = DockStyle.Fill;
		lblTitle.UseMnemonic = false;
		lblTitle.TextAlign = ContentAlignment.MiddleLeft;

		// lblArgs
		lblArgs.AutoSize = true;
		lblArgs.Dock = DockStyle.Top;
		lblArgs.Margin = new Padding(0, 2, 0, 6);
		lblArgs.Visible = false;

		// lblResult
		lblResult.AutoSize = true;
		lblResult.Dock = DockStyle.Top;
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
		tableLayout.AutoSizeMode = AutoSizeMode.GrowAndShrink;
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

		tableLayout.Dock = DockStyle.Fill;
		tableLayout.Margin = Padding.Empty;
		tableLayout.Padding = Padding.Empty;
		tableLayout.RowCount = 3;
		tableLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
		tableLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
		tableLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));

		// FunctionCallMessageControl
		AutoSize = true;
		AutoSizeMode = AutoSizeMode.GrowAndShrink;
		BorderStyle = BorderStyle.None;
		Cursor = Cursors.Hand;
		Margin = new Padding(3);
		Padding = new Padding(8);
		Controls.Add(lblSender);
		Controls.Add(tableLayout);
		tableLayout.BringToFront();
		Click += Toggle;
		tableLayout.Click += Toggle;
		lblIcon.Click += Toggle;
		lblTitle.Click += Toggle;
		lblResultIcon.Click += Toggle;

		tableLayout.ResumeLayout(false);
		tableLayout.PerformLayout();
		ResumeLayout(false);
		PerformLayout();
	}

	private Label lblSender;
	private TableLayoutPanel tableLayout;
	private Label lblIcon;
	private Label lblTitle;
	private SelectableMessageTextControl lblArgs;
	private Label lblResultIcon;
	private SelectableMessageTextControl lblResult;
}
