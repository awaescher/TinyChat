using System;
using System.Threading;
using System.Windows.Forms;
using DevExpress.LookAndFeel;
using DevExpress.XtraBars.ToolbarForm;
using TinyChat;
using WinFormsDemo;

namespace DevExpressDemo;

/// <summary>
/// Main demonstration form that provides a DevExpress chat control interface with property inspection capabilities.
/// Inherits from ToolbarForm for DevExpress theming and implements IMessageFilter for mouse message handling.
/// </summary>
public partial class DemoForm : ToolbarForm, IMessageFilter
{
	/// <summary>
	/// Windows message constant for left mouse button up event.
	/// </summary>
	private const int WM_LBUTTONUP = 0x0202;

	/// <summary>
	/// Initializes a new instance of the DemoForm class.
	/// Sets up the form components and enables key preview for keyboard handling.
	/// </summary>
	public DemoForm()
	{
		InitializeComponent();
		KeyPreview = true;
	}

	/// <summary>
	/// Handles the form load event to initialize the demo environment.
	/// Sets up message filtering, applies Office 2010 Blue skin style, 
	/// initializes the chat control with demo data, and selects the chat control for property inspection.
	/// </summary>
	/// <param name="e">Event arguments containing event data.</param>
	protected override void OnLoad(EventArgs e)
	{
		base.OnLoad(e);

		Application.AddMessageFilter(this);

		UserLookAndFeel.Default.SetSkinStyle(SkinStyle.Office2010Blue);

		dxChatControl.IncludeFunctionCalls = true;
		dxChatControl.Messages = DemoData.Create(Environment.UserName);
		SelectControl(dxChatControl);
	}

	/// <summary>
	/// Handles key down events for the form.
	/// Provides Escape key functionality to select the parent control of the currently selected control.
	/// </summary>
	/// <param name="e">Key event arguments containing information about the pressed key.</param>
	protected override void OnKeyDown(KeyEventArgs e)
	{
		base.OnKeyDown(e);

		if (e.KeyCode == Keys.Escape)
			SelectControl((propertyGridControl.SelectedObject as Control)?.Parent);
	}

	/// <summary>
	/// Filters Windows messages to handle mouse clicks for control selection.
	/// Intercepts left mouse button up messages and selects the clicked control for property inspection,
	/// but only if the control is located in the left panel of the main splitter.
	/// </summary>
	/// <param name="m">The Windows message to filter.</param>
	/// <returns>Always returns false to allow normal message processing to continue.</returns>
	public bool PreFilterMessage(ref Message m)
	{
		if (m.Msg == WM_LBUTTONUP)
		{
			try
			{
				var control = Control.FromHandle(m.HWnd);
				var parent = control?.Parent;

				// only select controls that belong to the chat in the left side of the splitter
				var isOnLeftSplitter = false;
				while (parent != null)
				{
					if (parent == splitMain.Panel1)
						isOnLeftSplitter = true;

					parent = parent?.Parent;
				}

				if (isOnLeftSplitter)
					SelectControl(control);
			}
			catch
			{
			}
		}

		return false;
	}

	/// <summary>
	/// Selects a control for property inspection by updating the property grid and label.
	/// Sets the property grid's selected object to the specified control and updates 
	/// the label to display the control's type name.
	/// </summary>
	/// <param name="control">The control to select for property inspection. Can be null.</param>
	private void SelectControl(Control? control)
	{
		propertyGridControl.SelectedObject = control;
		typeLabelControl.Text = control?.GetType().Name ?? "";
	}

	private void DxChatControl_MessageSent(object sender, MessageSentEventArgs e)
	{
		var cts = new CancellationTokenSource();
		dxChatControl.AddStreamingMessage(new NamedSender(DemoData.AssistantName), DemoData.StreamAiAnswerWithFunctionCalls(e.Content, isDevExpress: true, cts.Token), cancellationToken: cts.Token);
	}
}
