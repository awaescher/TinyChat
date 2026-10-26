using System.Drawing;
using System.Windows.Forms;
using Shouldly;
using TinyChat;
using TinyChat.Messages;
using TinyChat.Messages.Formatting;

namespace Tests;

[Apartment(ApartmentState.STA)]
public class NativeMessageResizeLayoutTests
{
	private const string LongText =
		"This is a fairly long chat message that must wrap across multiple lines when the " +
		"chat history is narrow and collapse back to fewer lines when the history is widened " +
		"again after the user resizes the window several times in a row.";

	public class TextHeightTracksWidth
	{
		[Test]
		public void Shrinks_Message_Height_When_History_Is_Widened_After_Being_Narrow()
		{
			using var history = new FlowLayoutMessageHistoryControl { ClientSize = new Size(250, 400) };
			using var messageControl = new ChatMessageControl
			{
				MessageFormatter = new PlainTextMessageFormatter(),
				Message = new TinyChat.ChatMessage(new NamedSender("Assistant"), new StringMessageContent(LongText))
			};

			history.AppendMessageControl(messageControl);
			history.PerformLayout();
			messageControl.PerformLayout();
			var narrowHeight = messageControl.Height;

			// Simulate the user enlarging the window.
			history.ClientSize = new Size(600, 400);
			history.PerformLayout();
			messageControl.PerformLayout();
			var wideHeight = messageControl.Height;

			wideHeight.ShouldBeLessThan(narrowHeight);
		}

		[Test]
		public void Restores_The_Height_After_Repeated_Resizes()
		{
			using var history = new FlowLayoutMessageHistoryControl { ClientSize = new Size(600, 400) };
			using var messageControl = new ChatMessageControl
			{
				MessageFormatter = new PlainTextMessageFormatter(),
				Message = new TinyChat.ChatMessage(new NamedSender("Assistant"), new StringMessageContent(LongText))
			};

			history.AppendMessageControl(messageControl);
			history.PerformLayout();
			messageControl.PerformLayout();
			var initialWideHeight = messageControl.Height;

			foreach (var width in new[] { 250, 600, 300, 550, 240, 600 })
			{
				history.ClientSize = new Size(width, 400);
				history.PerformLayout();
				messageControl.PerformLayout();
			}

			messageControl.Height.ShouldBe(initialWideHeight);
		}

		[Test]
		public void Message_Text_Never_Exceeds_The_Available_Width_After_Resize()
		{
			using var history = new FlowLayoutMessageHistoryControl { ClientSize = new Size(600, 400) };
			using var messageControl = new ChatMessageControl
			{
				MessageFormatter = new PlainTextMessageFormatter(),
				Message = new TinyChat.ChatMessage(new NamedSender("Assistant"), new StringMessageContent(LongText))
			};

			history.AppendMessageControl(messageControl);

			foreach (var width in new[] { 250, 600, 300, 550, 240 })
			{
				history.ClientSize = new Size(width, 400);
				history.PerformLayout();
				messageControl.PerformLayout();

				var textControl = messageControl.Controls.OfType<TextBox>().Single();
				textControl.Width.ShouldBeLessThanOrEqualTo(messageControl.ClientSize.Width);
			}
		}

		[Test]
		public void Message_Control_Fully_Contains_Its_Text_After_Repeated_Resizes()
		{
			using var history = new FlowLayoutMessageHistoryControl { ClientSize = new Size(600, 400) };
			using var messageControl = new ChatMessageControl
			{
				MessageFormatter = new PlainTextMessageFormatter(),
				Message = new TinyChat.ChatMessage(new NamedSender("Assistant"), new StringMessageContent(LongText))
			};

			history.AppendMessageControl(messageControl);

			foreach (var width in new[] { 600, 250, 600, 300, 240, 600 })
			{
				history.ClientSize = new Size(width, 400);
				history.PerformLayout();
				messageControl.PerformLayout();
			}

			var senderLabel = messageControl.Controls.OfType<Label>().First();
			var textControl = messageControl.Controls.OfType<TextBox>().Single();

			// The docked text must sit below the sender header (no vertical overlap) and the panel
			// must be tall enough to fully contain the wrapped text. A stale height cache would leave
			// the text overflowing past the panel bottom, painting over the following message.
			textControl.Top.ShouldBeGreaterThanOrEqualTo(senderLabel.Bottom);
			textControl.Bottom.ShouldBeLessThanOrEqualTo(messageControl.ClientSize.Height);
		}
	}

	public class RenderedTextHeight
	{
		private const string ParagraphText =
			"The Transformer is a neural network architecture.\n\nToday, Transformers power virtually all modern large language models.\n\nWould you like me to dive deeper?";

		[Test]
		public void Forwards_The_Mouse_Wheel_From_Text_To_The_History()
		{
			using var form = CreateForm(320);
			var history = form.Controls.OfType<FlowLayoutMessageHistoryControl>().Single();
			for (var index = 0; index < 30; index++)
				AddMessage(form, ParagraphText);
			history.AutoScrollPosition = Point.Empty;
			var textControl = history.Controls.OfType<ChatMessageControl>().First().Controls.OfType<TextBox>().Single();

			const int WM_MOUSEWHEEL = 0x020A;
			var wheelDown = new IntPtr(-120 << 16);
			var message = Message.Create(textControl.Handle, WM_MOUSEWHEEL, wheelDown, IntPtr.Zero);
			typeof(Control).GetMethod("WndProc", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!
				.Invoke(textControl, [message]);
			Application.DoEvents();

			history.VerticalScroll.Value.ShouldBeGreaterThan(0);
		}

		[TestCase("reasoning")]
		[TestCase("function")]
		public void Expanded_Details_Are_Fully_Visible_When_Messages_Are_Assigned_Before_Showing(string kind)
		{
			using var form = new Form { ClientSize = new Size(300, 500) };
			using var control = new ChatControl { Dock = DockStyle.Fill, IncludeFunctionCalls = true, IncludeReasoning = true };
			IChatMessageContent content = kind == "reasoning"
				? new ReasoningMessageContent("Good results. Let me verify the claim about the publication year and the key innovation of self-attention.").SetDone()
				: new FunctionCallMessageContent("1", "search_knowledge_base", new Dictionary<string, object?> { ["query"] = "Transformer architecture AI deep learning" }, "Found: The Transformer is a deep learning architecture introduced in 2017.");
			var message = new TinyChat.ChatMessage(new NamedSender("Assistant"), content);
			control.Messages = [new TinyChat.ChatMessage(new NamedSender("Assistant"), new StringMessageContent("Hello")), message];
			form.Controls.Add(control);
			form.Show();
			Application.DoEvents();

			var messageControl = control.MessageHistoryControl!.Controls.OfType<Control>()
				.Single(child => child is IChatMessageControl chatMessageControl && ReferenceEquals(chatMessageControl.Message, message));
			messageControl.GetType().GetMethod("Toggle", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!
				.Invoke(messageControl, [messageControl, EventArgs.Empty]);
			for (var index = 0; index < 10; index++)
			{
				Application.DoEvents();
				Thread.Sleep(10);
			}

			var table = messageControl.Controls.OfType<TableLayoutPanel>().Single();
			foreach (var detail in table.Controls.OfType<TextBox>().Where(textBox => textBox.Visible))
			{
				AssertHeightMatchesRenderedLines(detail);
				table.GetRowHeights()[table.GetRow(detail)].ShouldBeGreaterThanOrEqualTo(detail.Height + detail.Margin.Vertical);
				detail.Bottom.ShouldBeLessThanOrEqualTo(table.Height);
			}
		}

		[Test]
		public void Expanded_Reasoning_Detail_Grows_While_Text_Streams_In()
		{
			using var form = new Form { ClientSize = new Size(260, 600) };
			using var control = new ChatControl { Dock = DockStyle.Fill, IncludeReasoning = true };
			form.Controls.Add(control);
			form.Show();
			var reasoning = new ReasoningMessageContent("The user");
			var messageControl = (Control)control.AddMessage(new NamedSender("Assistant"), reasoning);
			((IChatMessageControl)messageControl).SetIsReceivingStream(true);
			messageControl.GetType().GetMethod("Toggle", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!
				.Invoke(messageControl, [messageControl, EventArgs.Empty]);

			foreach (var word in "wants to know about the Transformer architecture. I should search for a concise and accurate explanation from reliable sources.".Split(' '))
			{
				reasoning.AppendText(" " + word);
				Application.DoEvents();
				Thread.Sleep(15);
			}

			((IChatMessageControl)messageControl).SetIsReceivingStream(false);
			reasoning.SetDone();
			for (var index = 0; index < 10; index++)
			{
				Application.DoEvents();
				Thread.Sleep(10);
			}

			var detail = messageControl.Controls.OfType<TableLayoutPanel>().Single().Controls.OfType<TextBox>().Single();
			detail.Visible.ShouldBeTrue();
			AssertHeightMatchesRenderedLines(detail);
			var table = messageControl.Controls.OfType<TableLayoutPanel>().Single();
			detail.Bottom.ShouldBeLessThanOrEqualTo(table.Height);
			table.Bottom.ShouldBeLessThanOrEqualTo(messageControl.ClientSize.Height);
		}

		[Test]
		public void Shows_Every_Paragraph_Of_Text_With_Bare_Line_Feeds()
		{
			using var form = CreateForm(320);
			var messageControl = AddMessage(form, ParagraphText);
			var textControl = messageControl.Controls.OfType<TextBox>().Single();

			textControl.Lines.Length.ShouldBe(5);
			AssertHeightMatchesRenderedLines(textControl);
		}

		[TestCase("Summary exported to research-summary.md")]
		[TestCase("Trusted sources: peer-reviewed journals, official documentation, encyclopedias.")]
		public void Keeps_Every_Wrapped_Line_Visible_While_Resizing(string text)
		{
			using var form = CreateForm(600);
			var messageControl = AddMessage(form, text);
			var textControl = messageControl.Controls.OfType<TextBox>().Single();

			for (var width = 600; width >= 140; width -= 7)
			{
				form.ClientSize = new Size(width, 400);
				Application.DoEvents();

				AssertHeightMatchesRenderedLines(textControl);
			}
		}

		private static Form CreateForm(int width)
		{
			var form = new Form { ClientSize = new Size(width, 400) };
			var history = new FlowLayoutMessageHistoryControl { Dock = DockStyle.Fill };
			form.Controls.Add(history);
			form.Show();
			return form;
		}

		private static ChatMessageControl AddMessage(Form form, string text)
		{
			var history = form.Controls.OfType<FlowLayoutMessageHistoryControl>().Single();
			var messageControl = new ChatMessageControl
			{
				MessageFormatter = new PlainTextMessageFormatter(),
				Message = new TinyChat.ChatMessage(new NamedSender("Assistant"), new StringMessageContent(text))
			};
			history.AppendMessageControl(messageControl);
			Application.DoEvents();
			return messageControl;
		}

		/// <summary>
		/// The text box must end right below its last rendered line: no line may be clipped, and no more
		/// than the line gap may be left as empty space.
		/// </summary>
		private static void AssertHeightMatchesRenderedLines(TextBox textControl)
		{
			var lastLine = textControl.GetLineFromCharIndex(textControl.TextLength);
			var lastLineTop = textControl.GetPositionFromCharIndex(textControl.GetFirstCharIndexFromLine(lastLine)).Y;
			var renderedBottom = lastLineTop + textControl.Font.Height;

			textControl.ClientSize.Height.ShouldBeGreaterThanOrEqualTo(renderedBottom, $"Width {textControl.Width}: last line clipped");
			(textControl.ClientSize.Height - renderedBottom).ShouldBeLessThanOrEqualTo(2, $"Width {textControl.Width}: empty space below the text");
		}
	}
}
