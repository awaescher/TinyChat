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
		if (disposing && _message?.Content is FunctionCallMessageContent content)
			content.PropertyChanged -= OnContentPropertyChanged;
		if (disposing && (components != null))
			components.Dispose();
		base.Dispose(disposing);
	}

	private void InitializeComponent()
	{
		lblSender = new LabelControl();
		lblToolIcon = new LabelControl();
		lblResultIcon = new LabelControl();
		lblTitle = new LabelControl();
		lblArguments = new LabelControl();
		lblResult = new LabelControl();
		approvalPanel = new FlowLayoutPanel();
		btnApprove = new SimpleButton();
		btnReject = new SimpleButton();
		tableLayout = new TableLayoutPanel();
		paddingPanel = new PanelControl();
		approvalPanel.SuspendLayout();
		tableLayout.SuspendLayout();
		paddingPanel.SuspendLayout();
		SuspendLayout();

		// lblSender
		lblSender.AutoSizeMode = LabelAutoSizeMode.Vertical;
		lblSender.Dock = DockStyle.Top;
		lblSender.Appearance.Font = new Font(Font, FontStyle.Bold);
		lblSender.UseMnemonic = false;
		lblSender.Padding = new Padding(3, 0, 0, 3);

		// _callIconLabel
		lblToolIcon.AutoSize = false;
		lblToolIcon.AutoSizeMode = LabelAutoSizeMode.None;
		lblToolIcon.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
		lblToolIcon.Dock = DockStyle.None;
		lblToolIcon.ImageOptions.SvgImage = FluentIcons.Tool;
		lblToolIcon.ImageOptions.SvgImageSize = new Size(ICON_SIZE, ICON_SIZE);
		lblToolIcon.Margin = Padding.Empty;
		lblToolIcon.Size = new Size(ICON_WIDTH, ICON_SIZE);
		lblToolIcon.UseMnemonic = false;

		// _resultIconLabel
		lblResultIcon.AutoSize = false;
		lblResultIcon.AutoSizeMode = LabelAutoSizeMode.None;
		lblResultIcon.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
		lblResultIcon.Dock = DockStyle.None;
		lblResultIcon.ImageOptions.SvgImage = FluentIcons.Result;
		lblResultIcon.ImageOptions.SvgImageSize = new Size(ICON_SIZE, ICON_SIZE);
		lblResultIcon.Margin = Padding.Empty;
		lblResultIcon.Size = new Size(ICON_WIDTH, ICON_SIZE);
		lblResultIcon.UseMnemonic = false;
		lblResultIcon.Visible = false;

		// _callTitleLabel
		lblTitle.AutoSizeMode = LabelAutoSizeMode.Vertical;
		lblTitle.AllowHtmlString = true;
		lblTitle.UseMnemonic = false;

		// lblArguments
		lblArguments.AutoSizeMode = LabelAutoSizeMode.Vertical;
		lblArguments.Appearance.TextOptions.WordWrap = DevExpress.Utils.WordWrap.Wrap;
		lblArguments.Appearance.Options.UseTextOptions = true;
		lblArguments.Dock = DockStyle.Fill;
		lblArguments.Margin = new Padding(0, 2, 0, 6);
		lblArguments.UseMnemonic = false;
		lblArguments.Visible = false;

		// lblResult
		lblResult.AutoSizeMode = LabelAutoSizeMode.Vertical;
		lblResult.Appearance.TextOptions.WordWrap = DevExpress.Utils.WordWrap.Wrap;
		lblResult.Appearance.Options.UseTextOptions = true;
		lblResult.Dock = DockStyle.Fill;
		lblResult.UseMnemonic = false;
		lblResult.Visible = false;

		// btnApprove
		btnApprove.AutoSize = true;
		btnApprove.Cursor = Cursors.Default;
		btnApprove.Margin = new Padding(0, 4, 6, 0);
		btnApprove.Text = FunctionCallDisplay.APPROVE_GLYPH;
		btnApprove.Click += BtnApprove_Click;

		// btnReject
		btnReject.AutoSize = true;
		btnReject.Cursor = Cursors.Default;
		btnReject.Margin = new Padding(0, 4, 0, 0);
		btnReject.Text = FunctionCallDisplay.REJECT_GLYPH;
		btnReject.Click += BtnReject_Click;

		// approvalPanel
		approvalPanel.AutoSize = true;
		approvalPanel.AutoSizeMode = AutoSizeMode.GrowAndShrink;
		approvalPanel.Controls.Add(btnApprove);
		approvalPanel.Controls.Add(btnReject);
		approvalPanel.Cursor = Cursors.Default;
		approvalPanel.Dock = DockStyle.Top;
		approvalPanel.FlowDirection = FlowDirection.LeftToRight;
		approvalPanel.Margin = Padding.Empty;
		approvalPanel.Padding = Padding.Empty;
		approvalPanel.Visible = false;
		approvalPanel.WrapContents = false;

		// _table
		// Col 0: fixed icon width | Col 1: remaining text
		// Row 0: call icon | function name
		// Row 1: (no cell 0) | arguments (collapsed=hidden)
		// Row 2: result icon | result    (collapsed=hidden)
		//
		// IMPORTANT: do NOT put any always-visible control in rows 1 or 2;
		// a hidden control contributes 0 height, so the row collapses automatically.
		tableLayout.AutoSize = true;
		tableLayout.CellBorderStyle = TableLayoutPanelCellBorderStyle.None;
		tableLayout.ColumnCount = 2;
		tableLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, ICON_WIDTH));
		tableLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100f));
		tableLayout.Controls.Add(lblToolIcon, 0, 0);
		tableLayout.Controls.Add(lblTitle, 1, 0);

		// Row 1, col 0 intentionally left empty - no spacer, so the row fully
		// collapses when _argsLabel is hidden.
		tableLayout.Controls.Add(lblArguments, 1, 1);
		tableLayout.Controls.Add(lblResultIcon, 0, 2);
		tableLayout.Controls.Add(lblResult, 1, 2);
		// Row 3: approval buttons, only visible while an approval is pending.
		tableLayout.Controls.Add(approvalPanel, 1, 3);
		tableLayout.Dock = DockStyle.Fill;
		tableLayout.Margin = Padding.Empty;
		tableLayout.Padding = Padding.Empty;
		tableLayout.RowCount = 4;
		tableLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
		tableLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
		tableLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
		tableLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));

		// _borderPanel (provides the visible border and padding around the content)
		paddingPanel.AutoSize = true;
		paddingPanel.BorderStyle = DevExpress.XtraEditors.Controls.BorderStyles.NoBorder;
		paddingPanel.BackColor = Color.Transparent;
		paddingPanel.Controls.Add(tableLayout);
		paddingPanel.Cursor = Cursors.Hand;
		paddingPanel.Dock = DockStyle.Fill;
		paddingPanel.Padding = new Padding(0);

		// DXFunctionCallMessageControl
		AutoSize = true;
		BorderStyle = DevExpress.XtraEditors.Controls.BorderStyles.NoBorder;
		Controls.Add(lblSender);
		Controls.Add(paddingPanel);
		paddingPanel.BringToFront();
		Cursor = Cursors.Hand;
		Padding = new Padding(8);
		Click += Toggle;
		paddingPanel.Click += Toggle;
		tableLayout.Click += Toggle;
		lblToolIcon.Click += Toggle;
		lblTitle.Click += Toggle;
		lblArguments.Click += Toggle;
		lblResultIcon.Click += Toggle;
		lblResult.Click += Toggle;

		approvalPanel.ResumeLayout(false);
		approvalPanel.PerformLayout();
		tableLayout.ResumeLayout(false);
		tableLayout.PerformLayout();
		paddingPanel.ResumeLayout(false);
		paddingPanel.PerformLayout();
		ResumeLayout(false);
		PerformLayout();
	}

	private LabelControl lblSender;
	private TableLayoutPanel tableLayout;
	private LabelControl lblToolIcon;
	private LabelControl lblTitle;
	private LabelControl lblArguments;
	private LabelControl lblResultIcon;
	private PanelControl paddingPanel;
	private LabelControl lblResult;
	private FlowLayoutPanel approvalPanel;
	private SimpleButton btnApprove;
	private SimpleButton btnReject;
}
