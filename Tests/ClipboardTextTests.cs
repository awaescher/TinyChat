using System.Reflection;
using System.Windows.Forms;
using Shouldly;
using TinyChat;

namespace Tests;

[Apartment(ApartmentState.STA)]
public class ClipboardTextTests
{
	public class NormalizeMethod : ClipboardTextTests
	{
		[Test]
		public void Converts_Every_Line_Ending_To_Environment_NewLine()
		{
			ClipboardText.Normalize("a\nb\r\nc\rd").ShouldBe($"a{Environment.NewLine}b{Environment.NewLine}c{Environment.NewLine}d");
		}
	}

	public class Clipboard : ClipboardTextTests
	{
		[TestCase(false)]
		[TestCase(true)]
		public void Copies_Messages_With_Environment_NewLine(bool devExpress)
		{
			using var chat = devExpress ? (ChatControl)new DXChatControl() : new ChatControl();
			var messageControl = chat.AddMessage(new NamedSender("Assistant"), new StringMessageContent("Heading\n- first\n- second"));

			typeof(ChatControl).GetMethod("CopyMessage", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(chat, [messageControl]);

			System.Windows.Forms.Clipboard.GetText().ShouldBe($"Heading{Environment.NewLine}- first{Environment.NewLine}- second");
		}

		[TestCase(false)]
		[TestCase(true)]
		public void Pastes_Text_With_Unix_Line_Endings_On_Separate_Lines(bool devExpress)
		{
			var input = devExpress ? (Control)new DXChatInputControl() : new ChatInputControl();
			using var _ = input;
			var editor = (Control)input.GetType().GetField("_textBox", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(input)!;
			System.Windows.Forms.Clipboard.SetText("first\nsecond");

			editor.GetType().GetMethod("TryPasteNormalizedText", BindingFlags.Instance | BindingFlags.NonPublic)!
				.Invoke(editor, null).ShouldBe(true);

			editor.Text.ShouldBe($"first{Environment.NewLine}second");
		}

		[Test]
		public void Leaves_Environment_NewLine_To_The_Editor()
		{
			System.Windows.Forms.Clipboard.SetText($"first{Environment.NewLine}second");
			ClipboardText.GetTextToNormalize().ShouldBeNull();
		}
	}
}
