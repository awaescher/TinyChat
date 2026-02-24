using System.Drawing;
using System.IO;
using System.Text;
using System.Windows.Forms;
using DevExpress.Utils.Svg;
using DevExpress.XtraEditors;

namespace TinyChat;

partial class DXReasoningMessageControl
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
		lblIcon = new LabelControl();
		lblTitle = new LabelControl();
		lblDetail = new LabelControl();
		tableLayout = new TableLayoutPanel();
		paddingPanel = new PanelControl();
		tableLayout.SuspendLayout();
		paddingPanel.SuspendLayout();
		SuspendLayout();

		// _iconLabel
		lblIcon.AutoSize = false;
		lblIcon.Dock = DockStyle.Fill;
		lblIcon.ImageOptions.SvgImage = SvgImage.FromStream(new MemoryStream(Encoding.UTF8.GetBytes(ThinkSvg)));
		lblIcon.ImageOptions.SvgImageSize = new Size(14, 14);
		lblIcon.UseMnemonic = false;
		lblIcon.Width = IconColumnWidth;
		lblIcon.Cursor = Cursors.Hand;

		// _headerLabel
		lblTitle.AutoSize = true;
		lblTitle.UseMnemonic = false;
		lblTitle.Cursor = Cursors.Hand;

		// _detailLabel
		lblDetail.AllowHtmlString = true;
		lblDetail.AutoSizeMode = LabelAutoSizeMode.Vertical;
		lblDetail.Padding = new Padding(0, 2, 0, 6);
		lblDetail.UseMnemonic = true;
		lblDetail.Visible = false;
		lblDetail.Appearance.TextOptions.WordWrap = DevExpress.Utils.WordWrap.Wrap;
		lblDetail.Cursor = Cursors.Hand;

		// _table
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
		tableLayout.Controls.Add(lblTitle, 1, 0);
		// Row 1, col 0 intentionally left empty - no spacer, so the row fully
		// collapses when _detailLabel is hidden.
		tableLayout.Controls.Add(lblDetail, 1, 1);
		tableLayout.Cursor = Cursors.Hand;
		tableLayout.Dock = DockStyle.Fill;
		tableLayout.Margin = Padding.Empty;
		tableLayout.Padding = Padding.Empty;
		tableLayout.RowCount = 2;
		tableLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
		tableLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));

		// _paddingPanel (provides consistent inner padding around the content)
		paddingPanel.AutoSize = true;
		paddingPanel.BackColor = Color.Transparent;
		paddingPanel.BorderStyle = DevExpress.XtraEditors.Controls.BorderStyles.NoBorder;
		paddingPanel.Controls.Add(tableLayout);
		paddingPanel.Cursor = Cursors.Hand;
		paddingPanel.Dock = DockStyle.Fill;
		paddingPanel.Padding = new Padding(0);

		// DXReasoningMessageControl
		AutoSize = true;
		BorderStyle = DevExpress.XtraEditors.Controls.BorderStyles.NoBorder;
		Controls.Add(paddingPanel);
		Cursor = Cursors.Hand;
		Padding = new Padding(3, 0, 0, 0);
		Click += Toggle;
		paddingPanel.Click += Toggle;
		tableLayout.Click += Toggle;
		lblIcon.Click += Toggle;
		lblTitle.Click += Toggle;
		lblDetail.Click += Toggle;

		tableLayout.ResumeLayout(false);
		tableLayout.PerformLayout();
		paddingPanel.ResumeLayout(false);
		paddingPanel.PerformLayout();
		ResumeLayout(false);
		PerformLayout();
	}

	private TableLayoutPanel tableLayout;
	private LabelControl lblIcon;
	private LabelControl lblTitle;
	private LabelControl lblDetail;
	private PanelControl paddingPanel;
}
