using System.Drawing;
using System.Windows.Forms;
using TinyChat;

namespace DemoApp
{
	partial class NativeOllamaDemoForm
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
			topPanel = new Panel();
			newChatButton = new Button();
			streamingCheckBox = new CheckBox();
			statusLabel = new Label();
			chatControl = new ChatControl();
			topPanel.SuspendLayout();
			SuspendLayout();
			// 
			// topPanel
			// 
			topPanel.BackColor = SystemColors.Control;
			topPanel.Controls.Add(newChatButton);
			topPanel.Controls.Add(streamingCheckBox);
			topPanel.Controls.Add(statusLabel);
			topPanel.Dock = DockStyle.Top;
			topPanel.Location = new Point(0, 0);
			topPanel.Name = "topPanel";
			topPanel.Size = new Size(484, 60);
			topPanel.TabIndex = 0;
			// 
			// newChatButton
			// 
			newChatButton.Anchor = AnchorStyles.Top | AnchorStyles.Right;
			newChatButton.Location = new Point(382, 28);
			newChatButton.Name = "newChatButton";
			newChatButton.Size = new Size(90, 26);
			newChatButton.TabIndex = 2;
			newChatButton.Text = "New Chat";
			newChatButton.Click += NewChatButton_Click;
			// 
			// streamingCheckBox
			// 
			streamingCheckBox.Checked = true;
			streamingCheckBox.CheckState = CheckState.Checked;
			streamingCheckBox.Location = new Point(12, 31);
			streamingCheckBox.Name = "streamingCheckBox";
			streamingCheckBox.Size = new Size(130, 19);
			streamingCheckBox.TabIndex = 1;
			streamingCheckBox.Text = "Use Streaming";
			streamingCheckBox.CheckedChanged += StreamingCheckBox_CheckedChanged;
			// 
			// statusLabel
			// 
			statusLabel.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
			statusLabel.Location = new Point(12, 10);
			statusLabel.Name = "statusLabel";
			statusLabel.Size = new Size(462, 18);
			statusLabel.TabIndex = 0;
			statusLabel.Text = "Connecting...";
			// 
			// chatControl
			// 
			chatControl.Dock = DockStyle.Fill;
			chatControl.Enabled = false;
			chatControl.IncludeFunctionCalls = true;
			chatControl.IncludeReasoning = true;
			chatControl.Location = new Point(0, 60);
			chatControl.Name = "chatControl";
			chatControl.Size = new Size(484, 501);
			chatControl.TabIndex = 1;
			// 
			// OllamaDemoForm
			// 
			AutoScaleDimensions = new SizeF(7F, 15F);
			AutoScaleMode = AutoScaleMode.Font;
			ClientSize = new Size(484, 561);
			Controls.Add(chatControl);
			Controls.Add(topPanel);
			Name = "OllamaDemoForm";
			StartPosition = FormStartPosition.CenterScreen;
			Text = "TinyChat - Ollama Demo";
			topPanel.ResumeLayout(false);
			ResumeLayout(false);
		}

		#endregion

		private Panel topPanel;
		private Label statusLabel;
		private CheckBox streamingCheckBox;
		private Button newChatButton;
		private ChatControl chatControl;
	}
}
