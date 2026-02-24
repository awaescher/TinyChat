using System.Drawing;
using System.IO;
using System.Text;
using System.Windows.Forms;
using DevExpress.Utils.Svg;
using DevExpress.XtraEditors;

namespace TinyChat;

partial class DXFunctionCallMessageControl
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
		lblToolIcon = new LabelControl();
		lblResultIcon = new LabelControl();
		lblTitle = new LabelControl();
		lblArguments = new LabelControl();
		lblResult = new LabelControl();
		tablePanel = new TableLayoutPanel();
		paddingPanel = new PanelControl();
		tablePanel.SuspendLayout();
		paddingPanel.SuspendLayout();
		SuspendLayout();

		// _callIconLabel
		lblToolIcon.AutoSize = false;
		lblToolIcon.Dock = DockStyle.Fill;
		lblToolIcon.ImageOptions.SvgImage = SvgImage.FromStream(new MemoryStream(Encoding.UTF8.GetBytes(ToolSvg)));
		lblToolIcon.ImageOptions.SvgImageSize = new System.Drawing.Size(14, 14);
		lblToolIcon.UseMnemonic = false;
		lblToolIcon.Width = ICON_WIDTH;

		// _resultIconLabel
		lblResultIcon.AutoSize = false;
		lblResultIcon.Dock = DockStyle.Fill;
		lblResultIcon.ImageOptions.SvgImage = SvgImage.FromStream(new MemoryStream(Encoding.UTF8.GetBytes(ResultSvg)));
		lblResultIcon.ImageOptions.SvgImageSize = new System.Drawing.Size(14, 14);
		lblResultIcon.UseMnemonic = false;
		lblResultIcon.Visible = false;
		lblResultIcon.Width = ICON_WIDTH;

		// _callTitleLabel
		lblTitle.AutoSize = true;
		lblTitle.UseMnemonic = false;

		// _argsLabel
		lblArguments.AutoSize = true;
		lblArguments.Padding = new Padding(0, 2, 0, 6);
		lblArguments.UseMnemonic = false;
		lblArguments.Visible = false;

		// _resultLabel
		lblResult.AutoSize = true;
		lblResult.UseMnemonic = false;
		lblResult.Visible = false;

		// _table
		// Col 0: fixed icon width | Col 1: remaining text
		// Row 0: call icon | function name
		// Row 1: (no cell 0) | arguments (collapsed=hidden)
		// Row 2: result icon | result    (collapsed=hidden)
		//
		// IMPORTANT: do NOT put any always-visible control in rows 1 or 2;
		// a hidden control contributes 0 height, so the row collapses automatically.
		tablePanel.AutoSize = true;
		tablePanel.CellBorderStyle = TableLayoutPanelCellBorderStyle.None;
		tablePanel.ColumnCount = 2;
		tablePanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, ICON_WIDTH));
		tablePanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
		tablePanel.Controls.Add(lblToolIcon, 0, 0);
		tablePanel.Controls.Add(lblTitle, 1, 0);
		// Row 1, col 0 intentionally left empty - no spacer, so the row fully
		// collapses when _argsLabel is hidden.
		tablePanel.Controls.Add(lblArguments, 1, 1);
		tablePanel.Controls.Add(lblResultIcon, 0, 2);
		tablePanel.Controls.Add(lblResult, 1, 2);
		tablePanel.Cursor = Cursors.Hand;
		tablePanel.Dock = DockStyle.Fill;
		tablePanel.Margin = Padding.Empty;
		tablePanel.Padding = Padding.Empty;
		tablePanel.RowCount = 3;
		tablePanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
		tablePanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
		tablePanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));

		// _borderPanel (provides the visible border and padding around the content)
		paddingPanel.AutoSize = true;
		paddingPanel.BorderStyle = DevExpress.XtraEditors.Controls.BorderStyles.NoBorder;
		paddingPanel.BackColor = Color.Transparent;
		paddingPanel.Controls.Add(tablePanel);
		paddingPanel.Cursor = Cursors.Hand;
		paddingPanel.Dock = DockStyle.Fill;
		paddingPanel.Padding = new Padding(0);

		// DXFunctionCallMessageControl
		AutoSize = true;
		BorderStyle = DevExpress.XtraEditors.Controls.BorderStyles.NoBorder;
		Controls.Add(paddingPanel);
		Cursor = Cursors.Hand;
		Padding = new Padding(3, 0, 0, 0);
		Click += Toggle;
		paddingPanel.Click += Toggle;
		tablePanel.Click += Toggle;
		lblToolIcon.Click += Toggle;
		lblTitle.Click += Toggle;
		lblArguments.Click += Toggle;
		lblResultIcon.Click += Toggle;
		lblResult.Click += Toggle;

		tablePanel.ResumeLayout(false);
		tablePanel.PerformLayout();
		paddingPanel.ResumeLayout(false);
		paddingPanel.PerformLayout();
		ResumeLayout(false);
		PerformLayout();
	}

	private TableLayoutPanel tablePanel;
	private LabelControl lblToolIcon;
	private LabelControl lblTitle;
	private LabelControl lblArguments;
	private LabelControl lblResultIcon;
	private PanelControl paddingPanel;
	private LabelControl lblResult;
}
