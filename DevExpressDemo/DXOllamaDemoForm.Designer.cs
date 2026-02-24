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
			chatControl = new DXChatControl();
			topPanel = new DevExpress.XtraEditors.PanelControl();
			newChatButton = new DevExpress.XtraEditors.SimpleButton();
			streamingCheckEdit = new DevExpress.XtraEditors.CheckEdit();
			statusLabel = new DevExpress.XtraEditors.LabelControl();
			((System.ComponentModel.ISupportInitialize)topPanel).BeginInit();
			topPanel.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)streamingCheckEdit.Properties).BeginInit();
			SuspendLayout();
			//
			// _chatControl
			//
			chatControl.Dock = System.Windows.Forms.DockStyle.Fill;
			chatControl.Enabled = false;
			chatControl.IncludeFunctionCalls = true;
			chatControl.IncludeReasoning = true;
			chatControl.Location = new System.Drawing.Point(0, 60);
			chatControl.Name = "chatControl";
			chatControl.Size = new System.Drawing.Size(484, 501);
			chatControl.TabIndex = 1;
			//
			// _topPanel
			//
			topPanel.BorderStyle = DevExpress.XtraEditors.Controls.BorderStyles.NoBorder;
			topPanel.Controls.Add(newChatButton);
			topPanel.Controls.Add(streamingCheckEdit);
			topPanel.Controls.Add(statusLabel);
			topPanel.Dock = System.Windows.Forms.DockStyle.Top;
			topPanel.Location = new System.Drawing.Point(0, 0);
			topPanel.Name = "topPanel";
			topPanel.Size = new System.Drawing.Size(484, 60);
			topPanel.TabIndex = 0;
			//
			// _newChatButton
			//
			newChatButton.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right;
			newChatButton.Location = new System.Drawing.Point(382, 28);
			newChatButton.Name = "newChatButton";
			newChatButton.Size = new System.Drawing.Size(90, 26);
			newChatButton.TabIndex = 2;
			newChatButton.Text = "New Chat";
			newChatButton.Click += NewChatButton_Click;
			//
			// _streamingCheckEdit
			//
			streamingCheckEdit.Location = new System.Drawing.Point(12, 31);
			streamingCheckEdit.Name = "streamingCheckEdit";
			streamingCheckEdit.Properties.Caption = "Use Streaming";
			streamingCheckEdit.Checked = true;
			streamingCheckEdit.Size = new System.Drawing.Size(140, 19);
			streamingCheckEdit.TabIndex = 1;
			streamingCheckEdit.CheckedChanged += StreamingCheckEdit_CheckedChanged;
			//
			// _statusLabel
			//
			statusLabel.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
			statusLabel.Location = new System.Drawing.Point(12, 10);
			statusLabel.Name = "statusLabel";
			statusLabel.Size = new System.Drawing.Size(462, 18);
			statusLabel.TabIndex = 0;
			statusLabel.Text = "Connecting...";
			//
			// DXOllamaDemoForm
			//
			AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
			AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			ClientSize = new System.Drawing.Size(484, 561);
			Controls.Add(chatControl);
			Controls.Add(topPanel);
			Name = "DXOllamaDemoForm";
			StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
			Text = "TinyChat - Ollama Demo (DevExpress)";
			topPanel.ResumeLayout(false);
			((System.ComponentModel.ISupportInitialize)topPanel).EndInit();
			((System.ComponentModel.ISupportInitialize)streamingCheckEdit.Properties).EndInit();
			ResumeLayout(false);
		}

		#endregion

		private DXChatControl chatControl;
		private DevExpress.XtraEditors.PanelControl topPanel;
		private DevExpress.XtraEditors.LabelControl statusLabel;
		private DevExpress.XtraEditors.CheckEdit streamingCheckEdit;
		private DevExpress.XtraEditors.SimpleButton newChatButton;
	}
}
