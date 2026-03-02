namespace TinyChat.SubControls;

partial class ReasoningMessageControl
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
		lblHeader = new Label();
		lblDetail = new Label();
		tableLayout = new TableLayoutPanel();
		tableLayout.SuspendLayout();
		SuspendLayout();

		// lblIcon
		lblIcon.AutoSize = false;
		lblIcon.Dock = DockStyle.Fill;
		lblIcon.Text = IconGlyph;
		lblIcon.TextAlign = ContentAlignment.MiddleCenter;
		lblIcon.UseMnemonic = false;
		lblIcon.Width = IconColumnWidth;
		lblIcon.Cursor = Cursors.Hand;

		// lblHeader
		lblHeader.AutoSize = true;
		lblHeader.Cursor = Cursors.Hand;
		lblHeader.Dock = DockStyle.Fill;
		lblHeader.UseMnemonic = false;
		lblHeader.TextAlign = ContentAlignment.MiddleLeft;

		// lblDetail
		lblDetail.AutoSize = true;
		lblDetail.Cursor = Cursors.Hand;
		lblDetail.Dock = DockStyle.Fill;
		lblDetail.Padding = new Padding(0, 2, 0, 6);
		lblDetail.UseMnemonic = false;
		lblDetail.TextAlign = ContentAlignment.TopLeft;
		lblDetail.Visible = false;

		// tableLayout
		// Col 0: fixed icon width | Col 1: remaining text
		// Row 0: icon | header / status text
		// Row 1: (no cell 0) | detail text (collapsed = hidden)
		//
		// IMPORTANT: do NOT put any always-visible control in row 1;
		// a hidden control contributes 0 height, so the row collapses automatically.
		tableLayout.AutoSize = true;
		tableLayout.CellBorderStyle = TableLayoutPanelCellBorderStyle.None;
		tableLayout.ColumnCount = 2;
		tableLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, IconColumnWidth));
		tableLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
		tableLayout.Controls.Add(lblIcon, 0, 0);
		tableLayout.Controls.Add(lblHeader, 1, 0);
		// Row 1, col 0 intentionally left empty - no spacer, so the row fully
		// collapses when _detailLabel is hidden.
		tableLayout.Controls.Add(lblDetail, 1, 1);
		tableLayout.Cursor = Cursors.Hand;
		tableLayout.Dock = DockStyle.Fill;
		tableLayout.Margin = Padding.Empty;
		tableLayout.Padding = new Padding(8, 0, 0, 0);
		tableLayout.RowCount = 2;
		tableLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
		tableLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));

		// ReasoningMessageControl
		AutoSize = true;
		BorderStyle = BorderStyle.None;
		Cursor = Cursors.Hand;
		Margin = new Padding(3);
		Padding = new Padding(0);
		Controls.Add(tableLayout);
		Click += Toggle;
		tableLayout.Click += Toggle;
		lblIcon.Click += Toggle;
		lblHeader.Click += Toggle;
		lblDetail.Click += Toggle;

		tableLayout.ResumeLayout(false);
		tableLayout.PerformLayout();
		ResumeLayout(false);
		PerformLayout();
	}

	private TableLayoutPanel tableLayout;
	private Label lblIcon;
	private Label lblHeader;
	private Label lblDetail;
}
