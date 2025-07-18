using TinyChat;

namespace WinFormsDemo
{
    partial class DemoForm
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        ///  Clean up any resources being used.
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
		///  Required method for Designer support - do not modify
		///  the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			chatControl = new ChatControl();
			splitContainer = new SplitContainer();
			propertyGrid = new PropertyGrid();
			typeLabel = new Label();
			((System.ComponentModel.ISupportInitialize)splitContainer).BeginInit();
			splitContainer.Panel1.SuspendLayout();
			splitContainer.Panel2.SuspendLayout();
			splitContainer.SuspendLayout();
			SuspendLayout();
			// 
			// chatControl
			// 
			chatControl.Dock = DockStyle.Fill;
			chatControl.Location = new Point(0, 0);
			chatControl.Name = "chatControl";
			chatControl.Size = new Size(313, 628);
			chatControl.TabIndex = 0;
			chatControl.MessageSent += chatControl_MessageSent;
			// 
			// splitContainer
			// 
			splitContainer.Dock = DockStyle.Fill;
			splitContainer.FixedPanel = FixedPanel.Panel2;
			splitContainer.Location = new Point(0, 0);
			splitContainer.Name = "splitContainer";
			// 
			// splitContainer.Panel1
			// 
			splitContainer.Panel1.Controls.Add(chatControl);
			// 
			// splitContainer.Panel2
			// 
			splitContainer.Panel2.Controls.Add(propertyGrid);
			splitContainer.Panel2.Controls.Add(typeLabel);
			splitContainer.Size = new Size(648, 628);
			splitContainer.SplitterDistance = 313;
			splitContainer.TabIndex = 1;
			// 
			// propertyGrid
			// 
			propertyGrid.BackColor = SystemColors.Control;
			propertyGrid.Dock = DockStyle.Fill;
			propertyGrid.Location = new Point(0, 23);
			propertyGrid.Name = "propertyGrid";
			propertyGrid.Size = new Size(331, 605);
			propertyGrid.TabIndex = 0;
			// 
			// typeLabel
			// 
			typeLabel.Dock = DockStyle.Top;
			typeLabel.Font = new Font("Segoe UI", 9.75F, FontStyle.Regular, GraphicsUnit.Point, 0);
			typeLabel.Location = new Point(0, 0);
			typeLabel.Name = "typeLabel";
			typeLabel.Size = new Size(331, 23);
			typeLabel.TabIndex = 1;
			typeLabel.Text = "Control";
			// 
			// DemoForm
			// 
			AutoScaleDimensions = new SizeF(7F, 15F);
			AutoScaleMode = AutoScaleMode.Font;
			ClientSize = new Size(648, 628);
			Controls.Add(splitContainer);
			Name = "DemoForm";
			Text = "TinyChat WinForms Demo";
			splitContainer.Panel1.ResumeLayout(false);
			splitContainer.Panel2.ResumeLayout(false);
			((System.ComponentModel.ISupportInitialize)splitContainer).EndInit();
			splitContainer.ResumeLayout(false);
			ResumeLayout(false);
		}

		#endregion

		private ChatControl chatControl;
		private SplitContainer splitContainer;
		private PropertyGrid propertyGrid;
		private Label typeLabel;
	}
}
