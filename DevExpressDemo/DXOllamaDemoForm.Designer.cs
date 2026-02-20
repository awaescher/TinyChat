namespace DevExpressDemo
{
	partial class DXOllamaDemoForm
	{
		/// <summary>
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.IContainer components = null;

		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		/// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
		protected override void Dispose(bool disposing)
		{
			if (disposing && (components != null))
			{
				components.Dispose();
			}
			base.Dispose(disposing);
		}

		#region Windows Form Designer generated code

		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			_topPanel = new DevExpress.XtraEditors.PanelControl();
			_newChatButton = new DevExpress.XtraEditors.SimpleButton();
			_streamingCheckEdit = new DevExpress.XtraEditors.CheckEdit();
			_statusLabel = new DevExpress.XtraEditors.LabelControl();
			_chatControl = new DXChatControl();
			((System.ComponentModel.ISupportInitialize)_topPanel).BeginInit();
			_topPanel.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)_streamingCheckEdit.Properties).BeginInit();
			SuspendLayout();
			// 
			// _topPanel
			// 
			_topPanel.BorderStyle = DevExpress.XtraEditors.Controls.BorderStyles.NoBorder;
			_topPanel.Controls.Add(_newChatButton);
			_topPanel.Controls.Add(_streamingCheckEdit);
			_topPanel.Controls.Add(_statusLabel);
			_topPanel.Dock = System.Windows.Forms.DockStyle.Top;
			_topPanel.Location = new System.Drawing.Point(0, 0);
			_topPanel.Name = "_topPanel";
			_topPanel.Size = new System.Drawing.Size(498, 65);
			_topPanel.TabIndex = 0;
			// 
			// _newChatButton
			// 
			_newChatButton.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right;
			_newChatButton.Location = new System.Drawing.Point(396, 31);
			_newChatButton.Name = "_newChatButton";
			_newChatButton.Size = new System.Drawing.Size(90, 26);
			_newChatButton.TabIndex = 0;
			_newChatButton.Text = "New Chat";
			_newChatButton.Click += NewChatButton_Click;
			// 
			// _streamingCheckEdit
			// 
			_streamingCheckEdit.EditValue = true;
			_streamingCheckEdit.Location = new System.Drawing.Point(10, 34);
			_streamingCheckEdit.Name = "_streamingCheckEdit";
			_streamingCheckEdit.Properties.Caption = "Use Streaming";
			_streamingCheckEdit.Size = new System.Drawing.Size(130, 20);
			_streamingCheckEdit.TabIndex = 1;
			_streamingCheckEdit.CheckedChanged += StreamingCheckEdit_CheckedChanged;
			// 
			// _statusLabel
			// 
			_statusLabel.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
			_statusLabel.AutoSizeMode = DevExpress.XtraEditors.LabelAutoSizeMode.None;
			_statusLabel.Location = new System.Drawing.Point(12, 10);
			_statusLabel.Name = "_statusLabel";
			_statusLabel.Size = new System.Drawing.Size(474, 18);
			_statusLabel.TabIndex = 0;
			_statusLabel.Text = "Connecting...";
			// 
			// _chatControl
			// 
			_chatControl.Dock = System.Windows.Forms.DockStyle.Fill;
			_chatControl.Enabled = false;
			_chatControl.IncludeFunctionCalls = true;
			_chatControl.Location = new System.Drawing.Point(0, 65);
			_chatControl.Name = "_chatControl";
			_chatControl.Size = new System.Drawing.Size(498, 503);
			_chatControl.TabIndex = 0;
			// 
			// DXOllamaDemoForm
			// 
			AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			ClientSize = new System.Drawing.Size(498, 568);
			Controls.Add(_chatControl);
			Controls.Add(_topPanel);
			Name = "DXOllamaDemoForm";
			StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
			Text = "TinyChat - Ollama Demo (DevExpress)";
			((System.ComponentModel.ISupportInitialize)_topPanel).EndInit();
			_topPanel.ResumeLayout(false);
			((System.ComponentModel.ISupportInitialize)_streamingCheckEdit.Properties).EndInit();
			ResumeLayout(false);
		}

		#endregion

		private DevExpress.XtraEditors.PanelControl _topPanel;
		private DevExpress.XtraEditors.LabelControl _statusLabel;
		private DevExpress.XtraEditors.CheckEdit _streamingCheckEdit;
		private DevExpress.XtraEditors.SimpleButton _newChatButton;
		private DXChatControl _chatControl;
	}
}
