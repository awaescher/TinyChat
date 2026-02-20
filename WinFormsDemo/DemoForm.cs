using TinyChat;

namespace WinFormsDemo;

/// <summary>
/// A demonstration form that implements a property grid inspector for WinForms controls.
/// Allows users to click on controls to inspect their properties and navigate using keyboard shortcuts.
/// </summary>
public partial class DemoForm : Form, IMessageFilter
{
	/// <summary>
	/// Windows message constant for left mouse button up event.
	/// </summary>
	private const int WM_LBUTTONUP = 0x0202;

	/// <summary>
	/// Initializes a new instance of the <see cref="DemoForm"/> class.
	/// </summary>
	public DemoForm()
	{
		InitializeComponent();
		KeyPreview = true;
	}

	/// <summary>
	/// Raises the <see cref="Form.Load"/> event and initializes the form components.
	/// </summary>
	/// <param name="e">An <see cref="EventArgs"/> that contains the event data.</param>
	protected override void OnLoad(EventArgs e)
	{
		base.OnLoad(e);

		Application.AddMessageFilter(this);

		chatControl.IncludeFunctionCalls = true;
		chatControl.Messages = DemoData.Create(Environment.UserName);
		SelectControl(chatControl);

		// start a second demo showcasing the IChatClient implementation
		new OllamaDemoForm().Show();
	}

	/// <summary>
	/// Processes key down events for the form. Handles the Escape key to navigate to the parent control.
	/// </summary>
	/// <param name="e">A <see cref="KeyEventArgs"/> that contains the event data.</param>
	protected override void OnKeyDown(KeyEventArgs e)
	{
		base.OnKeyDown(e);

		if (e.KeyCode == Keys.Escape)
			SelectControl((propertyGrid.SelectedObject as Control)?.Parent);
	}

	/// <summary>
	/// Filters out a message before it is dispatched. Intercepts left mouse button up messages
	/// to automatically select controls in the property grid when clicked.
	/// </summary>
	/// <param name="m">The message to be dispatched. You cannot modify this message.</param>
	/// <returns>Always returns false to allow the message to continue to the next filter or control.</returns>
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
					if (parent == splitContainer.Panel1)
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
	/// Selects the specified control in the property grid and updates the control type label.
	/// </summary>
	/// <param name="control">The control to select. Can be null.</param>
	private void SelectControl(Control? control)
	{
		propertyGrid.SelectedObject = control;
		typeLabel.Text = control?.GetType().Name ?? "";
	}

	private void ChatControl_MessageSent(object sender, MessageSentEventArgs e)
	{
		var cts = new CancellationTokenSource();
		chatControl.AddStreamingMessage(new NamedSender(DemoData.AssistantName), DemoData.StreamAiAnswerWithFunctionCalls(e.Content, isDevExpress: false, cts.Token), cancellationToken: cts.Token);
	}
}

