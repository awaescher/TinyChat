using System.Drawing;
using System.Windows.Forms;
using DevExpress.XtraEditors;
using Shouldly;
using TinyChat;

namespace Tests;

[Apartment(ApartmentState.STA)]
public class ChatInputSendButtonLayoutTests
{
	[TestCase(false)]
	[TestCase(true)]
	public void Keeps_Send_Button_Six_Logical_Pixels_Inside_The_Visible_Editor(
		bool useDevExpress)
	{
		using var host = new Panel { Size = new Size(500, 300) };
		using var input = useDevExpress ? (Control)new DXChatInputControl() : new ChatInputControl();
		input.Size = new Size(320, 90);
		host.Controls.Add(input);
		host.CreateControl();
		input.CreateControl();

		AssertButtonInset(input, useDevExpress);

		((IChatInputLayoutControl)input).ContentPadding = new Padding(5, 7, 19, 13);
		input.Size = new Size(410, 150);
		AssertButtonInset(input, useDevExpress);

		var editorTopBeforeAttachment = GetEditorBounds(input, useDevExpress).Top;
		((IChatAttachmentInputControl)input).AddAttachments(
			[new ChatFileAttachment("notes.txt", "text/plain", "Hello"u8.ToArray())]);
		AssertButtonInset(input, useDevExpress);
		GetEditorBounds(input, useDevExpress).Top.ShouldBeGreaterThan(editorTopBeforeAttachment);

		input.Size = new Size(270, 130);
		AssertButtonInset(input, useDevExpress);
	}

	private static void AssertButtonInset(Control input, bool useDevExpress)
	{
		PerformLayout(input);
		var editorBounds = GetEditorBounds(input, useDevExpress);
		var sendButton = useDevExpress
			? (Control)input.Controls.OfType<SimpleButton>().Single()
			: input.Controls.OfType<Button>().Single();
		var expectedInset = Math.Max(1, (int)Math.Round(6 * input.DeviceDpi / 96f));

		(editorBounds.Right - sendButton.Right).ShouldBe(expectedInset);
		(editorBounds.Bottom - sendButton.Bottom).ShouldBe(expectedInset);
	}

	private static Rectangle GetEditorBounds(Control input, bool useDevExpress)
	{
		var editor = useDevExpress
			? (Control)Descendants(input).OfType<MemoEdit>().Single()
			: Descendants(input).OfType<TextBox>().Single();
		var location = editor.Location;
		for (var parent = editor.Parent; parent is not null && parent != input; parent = parent.Parent)
			location.Offset(parent.Location);

		return new Rectangle(location, editor.Size);
	}

	private static void PerformLayout(Control input)
	{
		input.Parent?.PerformLayout();
		input.PerformLayout();
		foreach (var descendant in Descendants(input).Reverse())
			descendant.PerformLayout();
		input.PerformLayout();
		Application.DoEvents();
	}

	private static IEnumerable<Control> Descendants(Control parent)
	{
		foreach (Control child in parent.Controls)
		{
			yield return child;

			foreach (var descendant in Descendants(child))
				yield return descendant;
		}
	}
}
