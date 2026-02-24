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
		_iconLabel = new Label();
		_headerLabel = new Label();
		_detailLabel = new Label();
		_table = new TableLayoutPanel();
		_table.SuspendLayout();
		SuspendLayout();

		// _iconLabel
		_iconLabel.AutoSize = false;
		_iconLabel.Dock = DockStyle.Fill;
		_iconLabel.Text = IconGlyph;
		_iconLabel.TextAlign = ContentAlignment.MiddleCenter;
		_iconLabel.UseMnemonic = false;
		_iconLabel.Width = IconColumnWidth;
		_iconLabel.Cursor = Cursors.Hand;

		// _headerLabel
		_headerLabel.AutoSize = true;
		_headerLabel.Cursor = Cursors.Hand;
		_headerLabel.Dock = DockStyle.Fill;
		_headerLabel.UseMnemonic = false;
		_headerLabel.TextAlign = ContentAlignment.MiddleLeft;

		// _detailLabel
		_detailLabel.AutoSize = true;
		_detailLabel.Cursor = Cursors.Hand;
		_detailLabel.Dock = DockStyle.Fill;
		_detailLabel.Padding = new Padding(0, 2, 0, 6);
		_detailLabel.UseMnemonic = false;
		_detailLabel.TextAlign = ContentAlignment.TopLeft;
		_detailLabel.Visible = false;

		// _table
		// Col 0: fixed icon width | Col 1: remaining text
		// Row 0: icon | header / status text
		// Row 1: (no cell 0) | detail text (collapsed = hidden)
		//
		// IMPORTANT: do NOT put any always-visible control in row 1;
		// a hidden control contributes 0 height, so the row collapses automatically.
		_table.AutoSize = true;
		_table.CellBorderStyle = TableLayoutPanelCellBorderStyle.None;
		_table.ColumnCount = 2;
		_table.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, IconColumnWidth));
		_table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
		_table.Controls.Add(_iconLabel, 0, 0);
		_table.Controls.Add(_headerLabel, 1, 0);
		// Row 1, col 0 intentionally left empty - no spacer, so the row fully
		// collapses when _detailLabel is hidden.
		_table.Controls.Add(_detailLabel, 1, 1);
		_table.Cursor = Cursors.Hand;
		_table.Dock = DockStyle.Fill;
		_table.Margin = Padding.Empty;
		_table.Padding = new Padding(8, 0, 0, 0);
		_table.RowCount = 2;
		_table.RowStyles.Add(new RowStyle(SizeType.AutoSize));
		_table.RowStyles.Add(new RowStyle(SizeType.AutoSize));

		// ReasoningMessageControl
		AutoSize = true;
		BorderStyle = BorderStyle.None;
		Cursor = Cursors.Hand;
		Margin = new Padding(3);
		Padding = new Padding(0);
		Controls.Add(_table);
		Click += Toggle;
		_table.Click += Toggle;
		_iconLabel.Click += Toggle;
		_headerLabel.Click += Toggle;
		_detailLabel.Click += Toggle;

		_table.ResumeLayout(false);
		_table.PerformLayout();
		ResumeLayout(false);
		PerformLayout();
	}

	private TableLayoutPanel _table;
	private Label _iconLabel;
	private Label _headerLabel;
	private Label _detailLabel;
}
