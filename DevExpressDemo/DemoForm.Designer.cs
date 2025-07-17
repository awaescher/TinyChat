namespace DevExpressDemo
{
	partial class DemoForm
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
			components = new System.ComponentModel.Container();
			dxChatControl = new DXChatControl();
			splitMain = new DevExpress.XtraEditors.SplitContainerControl();
			propertyGridControl1 = new DevExpress.XtraVerticalGrid.PropertyGridControl();
			labelControl1 = new DevExpress.XtraEditors.LabelControl();
			toolbarFormControl1 = new DevExpress.XtraBars.ToolbarForm.ToolbarFormControl();
			toolbarFormManager1 = new DevExpress.XtraBars.ToolbarForm.ToolbarFormManager(components);
			barDockControlTop = new DevExpress.XtraBars.BarDockControl();
			barDockControlBottom = new DevExpress.XtraBars.BarDockControl();
			barDockControlLeft = new DevExpress.XtraBars.BarDockControl();
			barDockControlRight = new DevExpress.XtraBars.BarDockControl();
			skinDropDownButtonItem1 = new DevExpress.XtraBars.SkinDropDownButtonItem();
			((System.ComponentModel.ISupportInitialize)splitMain).BeginInit();
			((System.ComponentModel.ISupportInitialize)splitMain.Panel1).BeginInit();
			splitMain.Panel1.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)splitMain.Panel2).BeginInit();
			splitMain.Panel2.SuspendLayout();
			splitMain.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)propertyGridControl1).BeginInit();
			((System.ComponentModel.ISupportInitialize)toolbarFormControl1).BeginInit();
			((System.ComponentModel.ISupportInitialize)toolbarFormManager1).BeginInit();
			SuspendLayout();
			// 
			// dxChatControl
			// 
			dxChatControl.Dock = System.Windows.Forms.DockStyle.Fill;
			dxChatControl.Location = new System.Drawing.Point(0, 0);
			dxChatControl.Name = "dxChatControl";
			dxChatControl.Size = new System.Drawing.Size(336, 643);
			dxChatControl.TabIndex = 1;
			// 
			// splitMain
			// 
			splitMain.Dock = System.Windows.Forms.DockStyle.Fill;
			splitMain.FixedPanel = DevExpress.XtraEditors.SplitFixedPanel.Panel2;
			splitMain.Location = new System.Drawing.Point(0, 31);
			splitMain.Name = "splitMain";
			// 
			// splitMain.Panel1
			// 
			splitMain.Panel1.Controls.Add(dxChatControl);
			splitMain.Panel1.Text = "Panel1";
			// 
			// splitMain.Panel2
			// 
			splitMain.Panel2.Controls.Add(propertyGridControl1);
			splitMain.Panel2.Controls.Add(labelControl1);
			splitMain.Panel2.Text = "Panel2";
			splitMain.Size = new System.Drawing.Size(687, 643);
			splitMain.SplitterPosition = 341;
			splitMain.TabIndex = 2;
			// 
			// propertyGridControl1
			// 
			propertyGridControl1.Dock = System.Windows.Forms.DockStyle.Fill;
			propertyGridControl1.Location = new System.Drawing.Point(0, 26);
			propertyGridControl1.Name = "propertyGridControl1";
			propertyGridControl1.OptionsView.AllowReadOnlyRowAppearance = DevExpress.Utils.DefaultBoolean.True;
			propertyGridControl1.Size = new System.Drawing.Size(341, 617);
			propertyGridControl1.TabIndex = 0;
			// 
			// labelControl1
			// 
			labelControl1.Appearance.Font = new System.Drawing.Font("Tahoma", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 0);
			labelControl1.Appearance.Options.UseFont = true;
			labelControl1.AutoSizeMode = DevExpress.XtraEditors.LabelAutoSizeMode.None;
			labelControl1.Dock = System.Windows.Forms.DockStyle.Top;
			labelControl1.Location = new System.Drawing.Point(0, 0);
			labelControl1.Name = "labelControl1";
			labelControl1.Padding = new System.Windows.Forms.Padding(8);
			labelControl1.Size = new System.Drawing.Size(341, 26);
			labelControl1.TabIndex = 1;
			labelControl1.Text = "Control";
			// 
			// toolbarFormControl1
			// 
			toolbarFormControl1.Location = new System.Drawing.Point(0, 0);
			toolbarFormControl1.Manager = toolbarFormManager1;
			toolbarFormControl1.Name = "toolbarFormControl1";
			toolbarFormControl1.Size = new System.Drawing.Size(687, 31);
			toolbarFormControl1.TabIndex = 3;
			toolbarFormControl1.TabStop = false;
			toolbarFormControl1.TitleItemLinks.Add(skinDropDownButtonItem1);
			toolbarFormControl1.ToolbarForm = this;
			// 
			// toolbarFormManager1
			// 
			toolbarFormManager1.DockControls.Add(barDockControlTop);
			toolbarFormManager1.DockControls.Add(barDockControlBottom);
			toolbarFormManager1.DockControls.Add(barDockControlLeft);
			toolbarFormManager1.DockControls.Add(barDockControlRight);
			toolbarFormManager1.Form = this;
			toolbarFormManager1.Items.AddRange(new DevExpress.XtraBars.BarItem[] { skinDropDownButtonItem1 });
			toolbarFormManager1.MaxItemId = 3;
			// 
			// barDockControlTop
			// 
			barDockControlTop.CausesValidation = false;
			barDockControlTop.Dock = System.Windows.Forms.DockStyle.Top;
			barDockControlTop.Location = new System.Drawing.Point(0, 31);
			barDockControlTop.Manager = toolbarFormManager1;
			barDockControlTop.Size = new System.Drawing.Size(687, 0);
			// 
			// barDockControlBottom
			// 
			barDockControlBottom.CausesValidation = false;
			barDockControlBottom.Dock = System.Windows.Forms.DockStyle.Bottom;
			barDockControlBottom.Location = new System.Drawing.Point(0, 674);
			barDockControlBottom.Manager = toolbarFormManager1;
			barDockControlBottom.Size = new System.Drawing.Size(687, 0);
			// 
			// barDockControlLeft
			// 
			barDockControlLeft.CausesValidation = false;
			barDockControlLeft.Dock = System.Windows.Forms.DockStyle.Left;
			barDockControlLeft.Location = new System.Drawing.Point(0, 31);
			barDockControlLeft.Manager = toolbarFormManager1;
			barDockControlLeft.Size = new System.Drawing.Size(0, 643);
			// 
			// barDockControlRight
			// 
			barDockControlRight.CausesValidation = false;
			barDockControlRight.Dock = System.Windows.Forms.DockStyle.Right;
			barDockControlRight.Location = new System.Drawing.Point(687, 31);
			barDockControlRight.Manager = toolbarFormManager1;
			barDockControlRight.Size = new System.Drawing.Size(0, 643);
			// 
			// skinDropDownButtonItem1
			// 
			skinDropDownButtonItem1.Alignment = DevExpress.XtraBars.BarItemLinkAlignment.Right;
			skinDropDownButtonItem1.Id = 2;
			skinDropDownButtonItem1.Name = "skinDropDownButtonItem1";
			// 
			// DemoForm
			// 
			AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			ClientSize = new System.Drawing.Size(687, 674);
			Controls.Add(splitMain);
			Controls.Add(barDockControlLeft);
			Controls.Add(barDockControlRight);
			Controls.Add(barDockControlBottom);
			Controls.Add(barDockControlTop);
			Controls.Add(toolbarFormControl1);
			Name = "DemoForm";
			Text = "TinyChat DevExpress Demo";
			ToolbarFormControl = toolbarFormControl1;
			((System.ComponentModel.ISupportInitialize)splitMain.Panel1).EndInit();
			splitMain.Panel1.ResumeLayout(false);
			((System.ComponentModel.ISupportInitialize)splitMain.Panel2).EndInit();
			splitMain.Panel2.ResumeLayout(false);
			((System.ComponentModel.ISupportInitialize)splitMain).EndInit();
			splitMain.ResumeLayout(false);
			((System.ComponentModel.ISupportInitialize)propertyGridControl1).EndInit();
			((System.ComponentModel.ISupportInitialize)toolbarFormControl1).EndInit();
			((System.ComponentModel.ISupportInitialize)toolbarFormManager1).EndInit();
			ResumeLayout(false);
			PerformLayout();

		}

		#endregion

		private DXChatControl dxChatControl;
		private DevExpress.XtraEditors.SplitContainerControl splitMain;
		private DevExpress.XtraVerticalGrid.PropertyGridControl propertyGridControl1;
		private DevExpress.XtraEditors.LabelControl labelControl1;
		private DevExpress.XtraBars.ToolbarForm.ToolbarFormControl toolbarFormControl1;
		private DevExpress.XtraBars.ToolbarForm.ToolbarFormManager toolbarFormManager1;
		private DevExpress.XtraBars.BarDockControl barDockControlTop;
		private DevExpress.XtraBars.BarDockControl barDockControlBottom;
		private DevExpress.XtraBars.BarDockControl barDockControlLeft;
		private DevExpress.XtraBars.BarDockControl barDockControlRight;
		private DevExpress.XtraBars.SkinDropDownButtonItem skinDropDownButtonItem1;
	}
}

