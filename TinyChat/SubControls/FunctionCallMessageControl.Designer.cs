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
		lblStatus = new Label();
		headerPanel = new FlowLayoutPanel();
		lblArgs = new Label();
		lblResult = new Label();
		approvalPanel = new FlowLayoutPanel();
		btnApprove = new Button();
		btnReject = new Button();
		tableLayout = new TableLayoutPanel();
		tableLayout.SuspendLayout();
		approvalPanel.SuspendLayout();
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
		lblIcon.Text = ChatIcons.Tool;
		lblIcon.TextAlign = ContentAlignment.MiddleCenter;
		lblIcon.UseMnemonic = false;
		lblIcon.Width = ICON_WIDTH;

		// lblResultIcon
		lblResultIcon.AutoSize = false;
		lblResultIcon.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
		lblResultIcon.Dock = DockStyle.None;
		lblResultIcon.Height = Font.Height;
		lblResultIcon.Margin = Padding.Empty;
		lblResultIcon.Text = ChatIcons.Result;
		lblResultIcon.TextAlign = ContentAlignment.MiddleCenter;
		lblResultIcon.UseMnemonic = false;
		lblResultIcon.Visible = false;
		lblResultIcon.Width = ICON_WIDTH;

		// lblTitle
		lblTitle.AutoSize = true;
		lblTitle.Margin = Padding.Empty;
		lblTitle.UseMnemonic = false;
		lblTitle.TextAlign = ContentAlignment.MiddleLeft;

		// lblStatus
		lblStatus.AutoSize = true;
		lblStatus.Margin = Padding.Empty;
		lblStatus.TextAlign = ContentAlignment.MiddleLeft;
		lblStatus.UseMnemonic = false;

		// headerPanel
		headerPanel.AutoSize = true;
		headerPanel.AutoSizeMode = AutoSizeMode.GrowAndShrink;
		headerPanel.Controls.Add(lblTitle);
		headerPanel.Controls.Add(lblStatus);
		headerPanel.Dock = DockStyle.Fill;
		headerPanel.FlowDirection = FlowDirection.LeftToRight;
		headerPanel.Margin = Padding.Empty;
		headerPanel.Padding = Padding.Empty;
		headerPanel.WrapContents = false;

		// lblArgs
		lblArgs.AutoSize = true;
		lblArgs.Dock = DockStyle.Fill;
		lblArgs.Margin = new Padding(0, 2, 0, 6);
		lblArgs.UseMnemonic = false;
		lblArgs.Visible = false;

		// lblResult
		lblResult.AutoSize = true;
		lblResult.Dock = DockStyle.Fill;
		lblResult.UseMnemonic = false;
		lblResult.Visible = false;

		// btnApprove
		btnApprove.AutoSize = true;
		btnApprove.AutoSizeMode = AutoSizeMode.GrowAndShrink;
		btnApprove.Cursor = Cursors.Default;
		btnApprove.Margin = new Padding(0, 4, 6, 0);
		btnApprove.Text = FunctionCallDisplay.APPROVE_GLYPH;
		btnApprove.UseMnemonic = false;
		btnApprove.Click += BtnApprove_Click;

		// btnReject
		btnReject.AutoSize = true;
		btnReject.AutoSizeMode = AutoSizeMode.GrowAndShrink;
		btnReject.Cursor = Cursors.Default;
		btnReject.Margin = new Padding(0, 4, 0, 0);
		btnReject.Text = FunctionCallDisplay.REJECT_GLYPH;
		btnReject.UseMnemonic = false;
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
		tableLayout.Controls.Add(headerPanel, 1, 0);
		// Row 1, col 0 intentionally left empty - no spacer, so the row fully
		// collapses when _argsLabel is hidden.
		tableLayout.Controls.Add(lblArgs, 1, 1);
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
		headerPanel.Click += Toggle;
		lblStatus.Click += Toggle;
		lblArgs.Click += Toggle;
		lblResultIcon.Click += Toggle;
		lblResult.Click += Toggle;

		approvalPanel.ResumeLayout(false);
		approvalPanel.PerformLayout();
		tableLayout.ResumeLayout(false);
		tableLayout.PerformLayout();
		ResumeLayout(false);
		PerformLayout();
	}

	private Label lblSender;
	private TableLayoutPanel tableLayout;
	private Label lblIcon;
	private Label lblTitle;
	private Label lblStatus;
	private FlowLayoutPanel headerPanel;
	private Label lblArgs;
	private Label lblResultIcon;
	private Label lblResult;
	private FlowLayoutPanel approvalPanel;
	private Button btnApprove;
	private Button btnReject;
}
