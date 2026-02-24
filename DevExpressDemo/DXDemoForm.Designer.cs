using TinyChat;

namespace DevExpressDemo
{
	partial class DXDemoForm
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
			propertyGridControl = new DevExpress.XtraVerticalGrid.PropertyGridControl();
			typeLabelControl = new DevExpress.XtraEditors.LabelControl();
			toolbarFormControl = new DevExpress.XtraBars.ToolbarForm.ToolbarFormControl();
			toolbarFormManager = new DevExpress.XtraBars.ToolbarForm.ToolbarFormManager(components);
			barDockControlTop = new DevExpress.XtraBars.BarDockControl();
			barDockControlBottom = new DevExpress.XtraBars.BarDockControl();
			barDockControlLeft = new DevExpress.XtraBars.BarDockControl();
			barDockControlRight = new DevExpress.XtraBars.BarDockControl();
			skinDropDownButtonItem = new DevExpress.XtraBars.SkinDropDownButtonItem();
			((System.ComponentModel.ISupportInitialize)splitMain).BeginInit();
			((System.ComponentModel.ISupportInitialize)splitMain.Panel1).BeginInit();
			splitMain.Panel1.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)splitMain.Panel2).BeginInit();
			splitMain.Panel2.SuspendLayout();
			splitMain.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)propertyGridControl).BeginInit();
			((System.ComponentModel.ISupportInitialize)toolbarFormControl).BeginInit();
			((System.ComponentModel.ISupportInitialize)toolbarFormManager).BeginInit();
			SuspendLayout();
			// 
			// dxChatControl
			// 
			dxChatControl.Dock = System.Windows.Forms.DockStyle.Fill;
			dxChatControl.Location = new System.Drawing.Point(0, 0);
			dxChatControl.Name = "dxChatControl";
			dxChatControl.Size = new System.Drawing.Size(336, 643);
			dxChatControl.TabIndex = 1;
			dxChatControl.MessageSent += DxChatControl_MessageSent;
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
			splitMain.Panel2.Controls.Add(propertyGridControl);
			splitMain.Panel2.Controls.Add(typeLabelControl);
			splitMain.Panel2.Text = "Panel2";
			splitMain.Size = new System.Drawing.Size(687, 643);
			splitMain.SplitterPosition = 341;
			splitMain.TabIndex = 2;
			// 
			// propertyGridControl
			// 
			propertyGridControl.Dock = System.Windows.Forms.DockStyle.Fill;
			propertyGridControl.Location = new System.Drawing.Point(0, 26);
			propertyGridControl.Name = "propertyGridControl";
			propertyGridControl.OptionsView.AllowReadOnlyRowAppearance = DevExpress.Utils.DefaultBoolean.True;
			propertyGridControl.Size = new System.Drawing.Size(341, 617);
			propertyGridControl.TabIndex = 0;
			// 
			// typeLabelControl
			// 
			typeLabelControl.Appearance.Font = new System.Drawing.Font("Tahoma", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, 0);
			typeLabelControl.Appearance.Options.UseFont = true;
			typeLabelControl.AutoSizeMode = DevExpress.XtraEditors.LabelAutoSizeMode.None;
			typeLabelControl.Dock = System.Windows.Forms.DockStyle.Top;
			typeLabelControl.Location = new System.Drawing.Point(0, 0);
			typeLabelControl.Name = "typeLabelControl";
			typeLabelControl.Padding = new System.Windows.Forms.Padding(8);
			typeLabelControl.Size = new System.Drawing.Size(341, 26);
			typeLabelControl.TabIndex = 1;
			typeLabelControl.Text = "Control";
			// 
			// toolbarFormControl
			// 
			toolbarFormControl.Location = new System.Drawing.Point(0, 0);
			toolbarFormControl.Manager = toolbarFormManager;
			toolbarFormControl.Name = "toolbarFormControl";
			toolbarFormControl.Size = new System.Drawing.Size(687, 31);
			toolbarFormControl.TabIndex = 3;
			toolbarFormControl.TabStop = false;
			toolbarFormControl.TitleItemLinks.Add(skinDropDownButtonItem);
			toolbarFormControl.ToolbarForm = this;
			// 
			// toolbarFormManager
			// 
			toolbarFormManager.DockControls.Add(barDockControlTop);
			toolbarFormManager.DockControls.Add(barDockControlBottom);
			toolbarFormManager.DockControls.Add(barDockControlLeft);
			toolbarFormManager.DockControls.Add(barDockControlRight);
			toolbarFormManager.Form = this;
			toolbarFormManager.Items.AddRange(new DevExpress.XtraBars.BarItem[] { skinDropDownButtonItem });
			toolbarFormManager.MaxItemId = 3;
			// 
			// barDockControlTop
			// 
			barDockControlTop.CausesValidation = false;
			barDockControlTop.Dock = System.Windows.Forms.DockStyle.Top;
			barDockControlTop.Location = new System.Drawing.Point(0, 31);
			barDockControlTop.Manager = toolbarFormManager;
			barDockControlTop.Size = new System.Drawing.Size(687, 0);
			// 
			// barDockControlBottom
			// 
			barDockControlBottom.CausesValidation = false;
			barDockControlBottom.Dock = System.Windows.Forms.DockStyle.Bottom;
			barDockControlBottom.Location = new System.Drawing.Point(0, 674);
			barDockControlBottom.Manager = toolbarFormManager;
			barDockControlBottom.Size = new System.Drawing.Size(687, 0);
			// 
			// barDockControlLeft
			// 
			barDockControlLeft.CausesValidation = false;
			barDockControlLeft.Dock = System.Windows.Forms.DockStyle.Left;
			barDockControlLeft.Location = new System.Drawing.Point(0, 31);
			barDockControlLeft.Manager = toolbarFormManager;
			barDockControlLeft.Size = new System.Drawing.Size(0, 643);
			// 
			// barDockControlRight
			// 
			barDockControlRight.CausesValidation = false;
			barDockControlRight.Dock = System.Windows.Forms.DockStyle.Right;
			barDockControlRight.Location = new System.Drawing.Point(687, 31);
			barDockControlRight.Manager = toolbarFormManager;
			barDockControlRight.Size = new System.Drawing.Size(0, 643);
			// 
			// skinDropDownButtonItem
			// 
			skinDropDownButtonItem.Alignment = DevExpress.XtraBars.BarItemLinkAlignment.Right;
			skinDropDownButtonItem.Id = 2;
			skinDropDownButtonItem.Name = "skinDropDownButtonItem";
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
			Controls.Add(toolbarFormControl);
			Name = "DemoForm";
			Text = "TinyChat DevExpress Demo";
			ToolbarFormControl = toolbarFormControl;
			((System.ComponentModel.ISupportInitialize)splitMain.Panel1).EndInit();
			splitMain.Panel1.ResumeLayout(false);
			((System.ComponentModel.ISupportInitialize)splitMain.Panel2).EndInit();
			splitMain.Panel2.ResumeLayout(false);
			((System.ComponentModel.ISupportInitialize)splitMain).EndInit();
			splitMain.ResumeLayout(false);
			((System.ComponentModel.ISupportInitialize)propertyGridControl).EndInit();
			((System.ComponentModel.ISupportInitialize)toolbarFormControl).EndInit();
			((System.ComponentModel.ISupportInitialize)toolbarFormManager).EndInit();
			ResumeLayout(false);
			PerformLayout();

		}

		#endregion

		private DXChatControl dxChatControl;
		private DevExpress.XtraEditors.SplitContainerControl splitMain;
		private DevExpress.XtraVerticalGrid.PropertyGridControl propertyGridControl;
		private DevExpress.XtraEditors.LabelControl typeLabelControl;
		private DevExpress.XtraBars.ToolbarForm.ToolbarFormControl toolbarFormControl;
		private DevExpress.XtraBars.ToolbarForm.ToolbarFormManager toolbarFormManager;
		private DevExpress.XtraBars.BarDockControl barDockControlTop;
		private DevExpress.XtraBars.BarDockControl barDockControlBottom;
		private DevExpress.XtraBars.BarDockControl barDockControlLeft;
		private DevExpress.XtraBars.BarDockControl barDockControlRight;
		private DevExpress.XtraBars.SkinDropDownButtonItem skinDropDownButtonItem;
	}
}

