using System.Reflection;
using System.Windows.Forms;
using Microsoft.Extensions.AI;
using Shouldly;
using TinyChat;
using TinyChat.Messages;

namespace Tests;

[Apartment(ApartmentState.STA)]
public class DetailTextTruncationTests
{
	public class TryTruncateMethod : DetailTextTruncationTests
	{
		[Test]
		public void Keeps_Short_Text()
		{
			DetailTextTruncation.TryTruncate("short", 10, 2, out var text).ShouldBeFalse();
			text.ShouldBe("short");
		}

		[Test]
		public void Shortens_By_Length()
		{
			DetailTextTruncation.TryTruncate(new string('x', 20), 10, 0, out var text).ShouldBeTrue();
			text.ShouldBe(new string('x', 10) + "\u2026");
		}

		[Test]
		public void Shortens_By_Lines()
		{
			DetailTextTruncation.TryTruncate("1\n2\n3\n4", 0, 2, out var text).ShouldBeTrue();
			text.ShouldBe("1\n2\u2026");
		}

		[Test]
		public void Ignores_Trailing_Whitespace_And_Disabled_Limits()
		{
			DetailTextTruncation.TryTruncate("1\n2\n\n", 0, 2, out _).ShouldBeFalse();
			DetailTextTruncation.TryTruncate(new string('x', 5000), 0, 0, out _).ShouldBeFalse();
		}

		[Test]
		public void Does_Not_Split_Surrogate_Pairs()
		{
			DetailTextTruncation.TryTruncate("ab\U0001F600cd", 3, 0, out var text).ShouldBeTrue();
			text.ShouldBe("ab\u2026");
		}
	}

	public class MessageControls : DetailTextTruncationTests
	{
		[TestCase(false)]
		[TestCase(true)]
		public void Shortens_Tool_Results_And_Shows_The_Full_Text_In_A_Dialog(bool devExpress)
		{
			var chat = new RecordingChatControl(devExpress) { MaximumDetailTextLength = 50 };
			using var _ = chat;
			var longResult = string.Join(" ", Enumerable.Range(0, 100).Select(index => $"word{index}"));
			var control = (Control)chat.AddMessage(new NamedSender("Assistant"), new FunctionCallMessageContent("call", "search", new Dictionary<string, object?> { ["query"] = "x" }, longResult));
			Toggle(control);

			var button = FindFullTextButton(control);
			button.Visible.ShouldBeTrue();
			button.AccessibleName.ShouldBe(chat.DisplayTexts.ShowFullText);
			control.ToString().ShouldContain(longResult);

			button.GetType().GetMethod("OnClick", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(button, [EventArgs.Empty]);
			chat.ShownTitle.ShouldBe("search");
			chat.ShownText!.ShouldContain(longResult);
			chat.ShownText.ShouldNotBeNull().ShouldContain("query: x");
		}

		[TestCase(false)]
		[TestCase(true)]
		public void Shortens_Reasoning_And_Shows_The_Full_Text_In_A_Dialog(bool devExpress)
		{
			var chat = new RecordingChatControl(devExpress) { MaximumDetailTextLines = 2 };
			using var _ = chat;
			var reasoning = "line 1\nline 2\nline 3\nline 4";
			var control = (Control)chat.AddMessage(new NamedSender("Assistant"), new ReasoningMessageContent(reasoning).SetDone());
			Toggle(control);

			var button = FindFullTextButton(control);
			button.Visible.ShouldBeTrue();
			button.GetType().GetMethod("OnClick", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(button, [EventArgs.Empty]);
			chat.ShownTitle.ShouldBe(chat.DisplayTexts.Reasoning);
			chat.ShownText.ShouldBe(reasoning);

			chat.MaximumDetailTextLines = 0;
			button.Visible.ShouldBeFalse();
		}

		[TestCase(false)]
		[TestCase(true)]
		public void Does_Not_Offer_The_Dialog_For_Short_Texts(bool devExpress)
		{
			using var chat = new RecordingChatControl(devExpress);
			var control = (Control)chat.AddMessage(new NamedSender("Assistant"), new FunctionCallMessageContent("call", "search", null, "short"));
			Toggle(control);
			FindFullTextButton(control).Visible.ShouldBeFalse();
		}

		[TestCase(false)]
		[TestCase(true)]
		public void Full_Text_Dialog_Contains_The_Complete_Text(bool devExpress)
		{
			using var chat = new RecordingChatControl(devExpress);
			var text = new string('x', 10000);
			using var dialog = chat.CreateDialog("Title", text);
			dialog.Text.ShouldBe("Title");
			Descendants(dialog).Any(control => control is ButtonBase or DevExpress.XtraEditors.SimpleButton).ShouldBeFalse();
			Descendants(dialog).OfType<TextBoxBase>().Concat<Control>(Descendants(dialog).OfType<DevExpress.XtraEditors.MemoEdit>())
				.First().Text.ShouldBe(text);
		}

		private static void Toggle(Control control) =>
			control.GetType().GetMethod("Toggle", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(control, [null, EventArgs.Empty]);

		private static Control FindFullTextButton(Control control) =>
			(Control)control.GetType().GetField("btnFullText", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(control)!;

		private static IEnumerable<Control> Descendants(Control parent)
		{
			foreach (Control child in parent.Controls)
			{
				yield return child;
				foreach (var nested in Descendants(child))
					yield return nested;
			}
		}
	}

	private sealed class RecordingChatControl : ChatControl
	{
		private readonly ChatControl? _devExpress;

		public RecordingChatControl(bool devExpress)
		{
			if (devExpress)
				_devExpress = new DXChatControl();
		}

		public string? ShownTitle { get; private set; }
		public string? ShownText { get; private set; }

		public Form CreateDialog(string title, string text) => _devExpress is null
			? CreateFullTextDialog(title, text)
			: (Form)typeof(DXChatControl).GetMethod(nameof(CreateFullTextDialog), BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(_devExpress, [title, text])!;

		protected override IFunctionCallMessageControl CreateFunctionCallMessageControl(IChatMessage message) => _devExpress is null
			? base.CreateFunctionCallMessageControl(message)
			: (IFunctionCallMessageControl)typeof(DXChatControl).GetMethod(nameof(CreateFunctionCallMessageControl), BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(_devExpress, [message])!;

		protected override IChatMessageControl CreateReasoningMessageControl(IChatMessage message) => _devExpress is null
			? base.CreateReasoningMessageControl(message)
			: (IChatMessageControl)typeof(DXChatControl).GetMethod(nameof(CreateReasoningMessageControl), BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(_devExpress, [message])!;

		protected override void ShowFullText(string title, string text)
		{
			ShownTitle = title;
			ShownText = text;
		}

		protected override void Dispose(bool disposing)
		{
			if (disposing)
				_devExpress?.Dispose();
			base.Dispose(disposing);
		}
	}
}
