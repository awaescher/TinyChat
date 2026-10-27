using System.Diagnostics;
using System.Drawing;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Windows.Forms;
using Microsoft.Extensions.AI;
using Shouldly;
using TinyChat;
using TinyChat.Messages;

namespace Tests;

[Apartment(ApartmentState.STA)]
public class MessageActionsTests
{
	public class RevertToMessageMethod
	{
		[TestCase(false)]
		[TestCase(true)]
		public void Reverting_To_An_Assistant_Message_Keeps_It_And_Clears_The_Input(bool useDevExpress)
		{
			var chatClient = new ScriptedChatClient(request => [new TextContent($"Answer {request + 1}")]);
			using var control = CreateChatControl(useDevExpress, chatClient);
			SendAndWait(control, "First question", "Answer 1");
			SendAndWait(control, "Second question", "Answer 2");
			GetInputEditor(control).Text = "Unsent draft";
			var firstAnswer = control.Messages.Single(message => message.Content.ToString() == "Answer 1");
			ConversationRevertedEventArgs? reverted = null;
			control.ConversationReverted += (_, e) => reverted = e;

			control.RevertToMessage(firstAnswer);

			control.Messages.Select(message => message.Content.ToString()).ShouldBe(["First question", "Answer 1"]);
			GetInputEditor(control).Text.ShouldBeEmpty();
			GetMessageControls(control).Select(messageControl => messageControl.Message).ShouldBe(control.Messages);
			reverted.ShouldNotBeNull();
			reverted.Message.ShouldBeSameAs(firstAnswer);
			reverted.RemovedMessages.Select(message => message.Content.ToString()).ShouldBe(["Second question", "Answer 2"]);
		}

		[TestCase(false)]
		[TestCase(true)]
		public void Reverting_To_A_User_Message_Removes_It_And_Prefills_The_Input(bool useDevExpress)
		{
			var chatClient = new ScriptedChatClient(request => [new TextContent($"Answer {request + 1}")]);
			using var control = CreateChatControl(useDevExpress, chatClient);
			SendAndWait(control, "First question", "Answer 1");
			SendAndWait(control, "Second question\nwith two lines", "Answer 2");
			var secondQuestion = control.Messages.Single(message => message.Content.ToString()!.StartsWith("Second", StringComparison.Ordinal));

			control.RevertToMessage(secondQuestion);

			control.Messages.Select(message => message.Content.ToString()).ShouldBe(["First question", "Answer 1"]);
			GetInputEditor(control).Text.ShouldBe("Second question\r\nwith two lines");
		}

		[TestCase(false)]
		[TestCase(true)]
		public void Removed_Messages_Are_Not_Sent_To_The_Chat_Client_Again(bool useDevExpress)
		{
			var chatClient = new ScriptedChatClient(request => request switch
			{
				1 =>
				[
					new TextReasoningContent("Thinking about it"),
					new FunctionCallContent("call-1", "lookup", new Dictionary<string, object?> { ["query"] = "x" }),
					new FunctionResultContent("call-1", "Found"),
					new TextContent("Answer 2")
				],
				_ => [new TextContent($"Answer {request + 1}")]
			});
			using var control = CreateChatControl(useDevExpress, chatClient);
			SendAndWait(control, "First question", "Answer 1");
			SendAndWait(control, "Second question", "Answer 2");
			control.Messages.ShouldContain(message => message.Content is FunctionCallMessageContent);
			control.Messages.ShouldContain(message => message.Content is ReasoningMessageContent);
			var secondQuestion = control.Messages.Single(message => message.Content.ToString() == "Second question");

			control.RevertToMessage(secondQuestion);
			SendAndWait(control, "Edited question", "Answer 3");

			var lastRequest = chatClient.Requests.Last();
			var sentTexts = lastRequest.Select(message => message.Text).ToArray();
			sentTexts.ShouldBe(["First question", "Answer 1", "Edited question"]);
			lastRequest.SelectMany(message => message.Contents)
				.Any(content => content is FunctionCallContent or FunctionResultContent)
				.ShouldBeFalse();
			control.Messages.Any(message => message.Content is FunctionCallMessageContent or ReasoningMessageContent).ShouldBeFalse();
		}

		[Test]
		public void Reverting_To_A_User_Message_Restores_Its_Attachments()
		{
			using var control = CreateChatControl(useDevExpress: false, new ScriptedChatClient(_ => [new TextContent("Seen")]));
			var attachment = new ChatFileAttachment("notes.txt", "text/plain", Encoding.UTF8.GetBytes("notes"));
			control.SendMessage(control.Sender, new FileAttachmentMessageContent([attachment], "Please read"));
			WaitUntil(() => control.Messages.Any(message => message.Content.ToString() == "Seen"), "No answer.");

			control.RevertToMessage(control.Messages.First());

			control.Messages.ShouldBeEmpty();
			GetInputEditor(control).Text.ShouldBe("Please read");
			((IChatAttachmentInputControl)control.InputControl!).PendingAttachments.ShouldBe([attachment]);
		}

		[Test]
		public void Ignores_Messages_That_Are_Not_Part_Of_The_Conversation()
		{
			using var control = CreateChatControl(useDevExpress: false, new ScriptedChatClient(_ => [new TextContent("Answer")]));
			SendAndWait(control, "Question", "Answer");

			control.RevertToMessage(new TinyChat.ChatMessage(new NamedSender("Other"), new StringMessageContent("Unknown")));

			control.Messages.Count().ShouldBe(2);
		}
	}

	public class MessageActionBar
	{
		[Test]
		public void Offers_Copy_And_Revert_For_Native_Messages()
		{
			using var control = new ActionsChatControl();
			var messageControl = CreateNativeMessageControl("Hello");

			control.Actions(messageControl).Select(action => action.Kind)
				.ShouldBe([ChatMessageActionKind.Copy, ChatMessageActionKind.Revert]);
		}

		[Test]
		public void Offers_Copy_With_Formatting_For_DevExpress_Messages()
		{
			using var control = new DXActionsChatControl();
			var messageControl = new DXChatMessageControl
			{
				MessageFormatter = new TinyChat.Messages.Formatting.PlainTextMessageFormatter(),
				Message = new TinyChat.ChatMessage(new NamedSender("Assistant"), new StringMessageContent("Hello"))
			};

			control.Actions(messageControl).Select(action => action.Kind)
				.ShouldBe([ChatMessageActionKind.Copy, ChatMessageActionKind.CopyFormatted, ChatMessageActionKind.Revert]);
		}

		[Test]
		public void Copies_Markdown_Messages_As_Readable_Plain_Text()
		{
			using var control = new ActionsChatControl();
			var messageControl = CreateNativeMessageControl("**Bold** and [link](https://example.com)");

			control.PlainText(messageControl).ShouldBe("Bold and link");
		}

		[TestCase(false, ChatMessageActionKind.Copy)]
		[TestCase(false, ChatMessageActionKind.Revert)]
		[TestCase(true, ChatMessageActionKind.Copy)]
		[TestCase(true, ChatMessageActionKind.CopyFormatted)]
		[TestCase(true, ChatMessageActionKind.Revert)]
		public void Shows_Icon_Only_Buttons(bool useDevExpress, ChatMessageActionKind kind)
		{
			using var control = useDevExpress ? (ChatControl)new DXActionsChatControl() : new ActionsChatControl();
			var action = new ChatMessageAction(kind, () => { });

			using var button = useDevExpress
				? ((DXActionsChatControl)control).Button(action)
				: ((ActionsChatControl)control).Button(action);

			if (useDevExpress)
			{
				var simpleButton = button.ShouldBeOfType<DevExpress.XtraEditors.SimpleButton>();
				simpleButton.Text.ShouldBeNullOrEmpty();
				simpleButton.ToolTip.ShouldBeNullOrEmpty();
				simpleButton.ImageOptions.SvgImage.ShouldNotBeNull();
			}
			else
			{
				button.Text.ShouldBeEmpty();
				((string)button.GetType().GetProperty("Glyph")!.GetValue(button)!).Length.ShouldBe(1);
				button.Font.Name.ShouldBe(TinyChat.ChatIcons.IconFontName ?? button.Font.Name);
			}
		}

		[Test]
		public void Does_Not_Offer_Or_Perform_Revert_While_A_Response_Streams()
		{
			using var control = CreateChatControl(useDevExpress: false, new ScriptedChatClient(_ => []));
			var question = control.AddMessage(control.Sender, new StringMessageContent("Question")).Message!;
			var stream = new StreamGate();
			var answerControl = control.AddStreamingMessage(new NamedSender("Assistant"), stream.ReadAsync());
			Application.DoEvents();

			control.IsBusy.ShouldBeTrue();
			GetActions(control, answerControl).Select(action => action.Kind).ShouldBe([ChatMessageActionKind.Copy]);
			control.RevertToMessage(question).ShouldBeFalse();
			control.Messages.Count().ShouldBe(2);

			stream.Complete();
			WaitUntil(() => !control.IsBusy, "The stream did not finish.");

			GetActions(control, answerControl).Select(action => action.Kind)
				.ShouldBe([ChatMessageActionKind.Copy, ChatMessageActionKind.Revert]);
			control.RevertToMessage(question).ShouldBeTrue();
			control.Messages.ShouldBeEmpty();
		}

		[TestCase(false)]
		[TestCase(true)]
		public void A_Pending_Approval_Keeps_The_Chat_Idle_And_Is_Rejected_By_Going_Back(bool useDevExpress)
		{
			var chatClient = new ScriptedChatClient(request => request switch
			{
				0 => [new TextContent("Answer 1")],
				_ => [new ToolApprovalRequestContent("request-1", new FunctionCallContent("call-1", "export"))]
			});
			using var control = CreateChatControl(useDevExpress, chatClient);
			SendAndWait(control, "First question", "Answer 1");
			var firstAnswer = control.Messages.Last();
			control.SendMessage("Please export");
			WaitUntil(() => control.IsWaitingForApproval, "No approval was requested.");
			var approval = control.Messages.Select(message => message.Content).OfType<FunctionCallMessageContent>().Single();

			control.IsBusy.ShouldBeFalse();
			GetActions(control, GetMessageControls(control).First()).Select(action => action.Kind)
				.ShouldContain(ChatMessageActionKind.Revert);

			control.RevertToMessage(firstAnswer).ShouldBeTrue();
			WaitUntil(() => control.ChatClientSendCompletion.IsCompleted, "The approval turn did not end.");

			approval.ApprovalState.ShouldBe(ToolApprovalState.Rejected);
			control.Messages.Select(message => message.Content.ToString()).ShouldBe(["First question", "Answer 1"]);
			chatClient.Requests.Count.ShouldBe(2);
			control.IsBusy.ShouldBeFalse();
		}

		[Test]
		public void Sending_During_A_Pending_Approval_Rejects_It_And_Starts_A_New_Turn()
		{
			var chatClient = new ScriptedChatClient(request => request switch
			{
				0 => [new ToolApprovalRequestContent("request-1", new FunctionCallContent("call-1", "export"))],
				_ => [new TextContent("Answer 2")]
			});
			using var control = CreateChatControl(useDevExpress: false, chatClient);
			control.SendMessage("Please export");
			WaitUntil(() => control.IsWaitingForApproval, "No approval was requested.");
			var approval = control.Messages.Select(message => message.Content).OfType<FunctionCallMessageContent>().Single();

			SendAndWait(control, "Never mind, something else", "Answer 2");

			approval.ApprovalState.ShouldBe(ToolApprovalState.Rejected);
			chatClient.Requests.Count.ShouldBe(2);
			var sentContents = chatClient.Requests[1].SelectMany(message => message.Contents).ToArray();
			sentContents.OfType<ToolApprovalResponseContent>().Single().Approved.ShouldBeFalse();
			chatClient.Requests[1].Last().Text.ShouldBe("Never mind, something else");
		}

		[Test]
		public void Uses_The_Default_Cursor_For_The_Buttons()
		{
			using var control = CreateChatControl(useDevExpress: false, new ScriptedChatClient(_ => []));
			var messageControl = control.AddMessage(new NamedSender("Assistant"), new StringMessageContent("Hello"));
			typeof(ChatControl).GetMethod("PopulateMessageActionBar", BindingFlags.Instance | BindingFlags.NonPublic)!
				.Invoke(control, [GetActions(control, messageControl).ToArray()]);

			var bar = GetActionBar(control);
			bar.Controls.Count.ShouldBe(2);
			bar.Controls.Cast<Control>().ShouldAllBe(button => button.Cursor == Cursors.Default);
		}

		private static ChatMessageControl CreateNativeMessageControl(string text) => new()
		{
			MessageFormatter = new TinyChat.Messages.Formatting.PlainTextMessageFormatter(),
			Message = new TinyChat.ChatMessage(new NamedSender("Assistant"), new StringMessageContent(text))
		};

		private static IEnumerable<ChatMessageAction> GetActions(ChatControl control, IChatMessageControl messageControl) =>
			(IEnumerable<ChatMessageAction>)typeof(ChatControl)
				.GetMethod("GetMessageActions", BindingFlags.Instance | BindingFlags.NonPublic)!
				.Invoke(control, [messageControl])!;

		[TestCase(false)]
		[TestCase(true)]
		public void Keeps_The_Action_Bar_Outside_Of_The_Message_And_Hides_It_On_Scroll(bool useDevExpress)
		{
			using var control = CreateChatControl(useDevExpress, new ScriptedChatClient(_ => []));
			var messageControl = (Control)control.AddMessage(new NamedSender("Assistant"), new StringMessageContent("Hello"));
			Application.DoEvents();

			var bar = GetActionBar(control);
			bar.Parent.ShouldBe(control);
			DescendantsAndSelf(messageControl).ShouldNotContain(bar);

			bar.Visible = true;
			RaiseViewportChanged(control.MessageHistoryControl!);

			bar.Visible.ShouldBeFalse();
		}

		[Test]
		public void Hides_The_Action_Bar_When_It_Is_Disabled()
		{
			using var control = CreateChatControl(useDevExpress: false, new ScriptedChatClient(_ => []));
			control.AddMessage(new NamedSender("Assistant"), new StringMessageContent("Hello"));
			var bar = GetActionBar(control);
			bar.Visible = true;

			control.ShowMessageActions = false;

			bar.Visible.ShouldBeFalse();
		}

		private static Control GetActionBar(ChatControl control) => (Control)typeof(ChatControl)
			.GetField("_messageActionBar", BindingFlags.Instance | BindingFlags.NonPublic)!
			.GetValue(control)!;
	}

	public class FormattedCopy
	{
		[Test]
		public void Converts_DevExpress_Markup_To_Html()
		{
			var html = ConvertToHtml("<b>Bold</b> <i>it</i> <color=red>red</color> <size=+2>big</size><br><font=\"Consolas\">code &amp; more</font>");

			html.ShouldBe("<b>Bold</b> <i>it</i> <span style=\"color:red\">red</span> <span style=\"font-size:120%\">big</span><br><span style=\"font-family:&#39;Consolas&#39;\">code &amp; more</span>");
		}

		[Test]
		public void Encodes_Text_And_Drops_Unsafe_Links()
		{
			var html = ConvertToHtml("<a href=\"javascript:alert(1)\">x</a> <a href=\"https://example.com\">ok</a> 1 < 2\nnext");

			html.ShouldBe("<a>x</a> <a href=\"https://example.com\">ok</a> 1 &lt; 2<br>next");
		}

		[Test]
		public void Removes_Markup_For_Plain_Text()
		{
			var plainText = ConvertToPlainText("<b>Bold</b><br><color=255,0,0>red</color> &amp; done");

			plainText.ShouldBe($"Bold{Environment.NewLine}red & done");
		}

		[Test]
		public void Creates_Cf_Html_With_Correct_Byte_Offsets()
		{
			var cfHtml = (string)typeof(ChatControl).Assembly.GetType("TinyChat.HtmlClipboard")!
				.GetMethod("CreateCfHtml", BindingFlags.Static | BindingFlags.NonPublic)!
				.Invoke(null, ["<b>Grüße</b>", null])!;
			var bytes = Encoding.UTF8.GetBytes(cfHtml);

			int Offset(string name) => int.Parse(cfHtml.Split("\r\n").Single(line => line.StartsWith(name + ":", StringComparison.Ordinal))[(name.Length + 1)..]);
			string Slice(int start, int end) => Encoding.UTF8.GetString(bytes, start, end - start);

			Slice(Offset("StartHTML"), Offset("EndHTML")).ShouldStartWith("<html>");
			Slice(Offset("StartHTML"), Offset("EndHTML")).ShouldEndWith("</html>");
			Slice(Offset("StartFragment"), Offset("EndFragment")).ShouldBe("<div><b>Grüße</b></div>");
		}

		private static string ConvertToHtml(string formattedText) => (string)GetConverter()
			.GetMethod("ToHtml", BindingFlags.Static | BindingFlags.NonPublic)!
			.Invoke(null, [formattedText])!;

		private static string ConvertToPlainText(string formattedText) => (string)GetConverter()
			.GetMethod("ToPlainText", BindingFlags.Static | BindingFlags.NonPublic)!
			.Invoke(null, [formattedText])!;

		private static Type GetConverter() => typeof(DXChatControl).Assembly.GetType("TinyChat.DXFormattedText")!;
	}

	public class Screenshots
	{
		[Explicit("Writes screenshots for a visual check")]
		[TestCase(false)]
		[TestCase(true)]
		public void Renders_The_Action_Bar_Over_A_Message(bool useDevExpress)
		{
			{
				using var form = new Form { ClientSize = new Size(380, 260), StartPosition = FormStartPosition.Manual, Location = new Point(40, 40) };
				using var control = useDevExpress ? new DXChatControl { Dock = DockStyle.Fill } : new ChatControl { Dock = DockStyle.Fill };
				form.Controls.Add(control);
				form.Show();
				Application.DoEvents();
				control.AddMessage(control.Sender, new StringMessageContent("What is a Transformer?"));
				var answer = (Control)control.AddMessage(new NamedSender("Assistant"), new StringMessageContent(
					"The **Transformer** is a neural network architecture introduced in the paper \"Attention Is All You Need\"."));
				for (var index = 0; index < 20; index++)
					Application.DoEvents();

				Cursor.Position = answer.PointToScreen(new Point(answer.Width / 2, answer.Height / 2));
				typeof(ChatControl).GetMethod("StartMessageActionsHover", BindingFlags.Instance | BindingFlags.NonPublic)!
					.Invoke(control, [answer]);
				typeof(ChatControl).GetMethod("DisplayMessageActionBar", BindingFlags.Instance | BindingFlags.NonPublic)!
					.Invoke(control, null);
				for (var index = 0; index < 20; index++)
					Application.DoEvents();

				var bar = (Control)typeof(ChatControl).GetField("_messageActionBar", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(control)!;
				TestContext.Out.WriteLine($"bar visible={bar.Visible} bounds={bar.Bounds} buttons={bar.Controls.Count} cursor={Cursor.Position} answer={answer.RectangleToScreen(answer.ClientRectangle)}");

				form.Activate();
				form.Refresh();
				for (var index = 0; index < 10; index++)
				{
					Cursor.Position = answer.PointToScreen(new Point(answer.Width / 2, answer.Height / 2));
					Application.DoEvents();
					Thread.Sleep(20);
				}

				foreach (var timerName in new[] { "_messageActionsHoverTimer", "_messageActionsHideTimer" })
					((System.Windows.Forms.Timer?)typeof(ChatControl).GetField(timerName, BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(control))?.Stop();
				bar.Visible = true;
				bar.Refresh();
				var screenBounds = form.RectangleToScreen(form.ClientRectangle);
				using var bitmap = new Bitmap(screenBounds.Width, screenBounds.Height);
				using (var graphics = Graphics.FromImage(bitmap))
					graphics.CopyFromScreen(screenBounds.Location, Point.Empty, screenBounds.Size);
				bitmap.Save(Path.Combine(Path.GetTempPath(), "opencode", $"action_bar_{(useDevExpress ? "dx" : "native")}.png"));

				var barBounds = bar.RectangleToScreen(bar.ClientRectangle);
				using var barBitmap = new Bitmap(barBounds.Width, barBounds.Height);
				using (var graphics = Graphics.FromImage(barBitmap))
					graphics.CopyFromScreen(barBounds.Location, Point.Empty, barBounds.Size);
				using var zoomed = new Bitmap(barBounds.Width * 6, barBounds.Height * 6);
				using (var graphics = Graphics.FromImage(zoomed))
				{
					graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.NearestNeighbor;
					graphics.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.Half;
					graphics.DrawImage(barBitmap, new Rectangle(Point.Empty, zoomed.Size));
				}
				zoomed.Save(Path.Combine(Path.GetTempPath(), "opencode", $"action_bar_{(useDevExpress ? "dx" : "native")}_zoom.png"));
			}
		}
	}

	private static ChatControl CreateChatControl(bool useDevExpress, IChatClient chatClient)
	{
		// Mirror a WinForms application, where continuations return to the UI thread.
		SynchronizationContext.SetSynchronizationContext(new WindowsFormsSynchronizationContext());

		ChatControl control = useDevExpress ? new DXChatControl() : new ChatControl();
		control.Size = new Size(400, 420);
		control.UseStreaming = false;
		control.ServiceProvider = new SingleServiceProvider(chatClient);

		var host = new Panel { Size = control.Size };
		control.Dock = DockStyle.Fill;
		host.Controls.Add(control);
		host.CreateControl();
		control.CreateControl();
		Application.DoEvents();
		control.Disposed += (_, _) => host.Dispose();
		return control;
	}

	private static void SendAndWait(ChatControl control, string text, string expectedAnswer)
	{
		control.SendMessage(text);
		WaitUntil(
			() => control.Messages.Any(message => message.Content.ToString() == expectedAnswer) && control.ChatClientSendCompletion.IsCompleted,
			$"No answer '{expectedAnswer}'.");
	}

	private static Control GetInputEditor(ChatControl control)
	{
		return DescendantsAndSelf(control.InputControl!)
			.First(child => child is TextBox or DevExpress.XtraEditors.MemoEdit);
	}

	private static IChatMessageControl[] GetMessageControls(ChatControl control)
	{
		return DescendantsAndSelf(control.MessageHistoryControl!).OfType<IChatMessageControl>().ToArray();
	}

	private static void RaiseViewportChanged(Control historyControl)
	{
		var field = historyControl.GetType().GetField(
			nameof(IChatMessageHistoryViewport.ViewportChanged),
			BindingFlags.Instance | BindingFlags.NonPublic)!;
		((EventHandler?)field.GetValue(historyControl))?.Invoke(historyControl, EventArgs.Empty);
	}

	private static IEnumerable<Control> DescendantsAndSelf(Control parent)
	{
		yield return parent;
		foreach (Control child in parent.Controls)
		{
			foreach (var descendant in DescendantsAndSelf(child))
				yield return descendant;
		}
	}

	private static void WaitUntil(Func<bool> predicate, string failureMessage)
	{
		var stopwatch = Stopwatch.StartNew();
		while (!predicate() && stopwatch.Elapsed < TimeSpan.FromSeconds(5))
		{
			Application.DoEvents();
			Thread.Sleep(10);
		}

		predicate().ShouldBeTrue(failureMessage);
	}

	private sealed class ActionsChatControl : ChatControl
	{
		public IEnumerable<ChatMessageAction> Actions(IChatMessageControl messageControl) => GetMessageActions(messageControl);

		public string PlainText(IChatMessageControl messageControl) => GetMessagePlainText(messageControl);

		public Control Button(ChatMessageAction action) => CreateMessageActionButton(action);
	}

	private sealed class DXActionsChatControl : DXChatControl
	{
		public IEnumerable<ChatMessageAction> Actions(IChatMessageControl messageControl) => GetMessageActions(messageControl);

		public Control Button(ChatMessageAction action) => CreateMessageActionButton(action);
	}

	private sealed class StreamGate
	{
		private readonly TaskCompletionSource _completion = new(TaskCreationOptions.RunContinuationsAsynchronously);

		public void Complete() => _completion.TrySetResult();

		public async IAsyncEnumerable<string> ReadAsync()
		{
			yield return "Partial";
			await _completion.Task;
			yield return " answer";
		}
	}

	private sealed class SingleServiceProvider(IChatClient chatClient) : IServiceProvider
	{
		public object? GetService(Type serviceType) => serviceType == typeof(IChatClient) ? chatClient : null;
	}

	private sealed class ScriptedChatClient(Func<int, AIContent[]> script) : IChatClient
	{
		public List<List<Microsoft.Extensions.AI.ChatMessage>> Requests { get; } = [];

		public void Dispose() { }

		public Task<ChatResponse> GetResponseAsync(
			IEnumerable<Microsoft.Extensions.AI.ChatMessage> messages,
			ChatOptions? options = null,
			CancellationToken cancellationToken = default)
		{
			Requests.Add([.. messages]);
			var contents = script(Requests.Count - 1);
			return Task.FromResult(new ChatResponse(new Microsoft.Extensions.AI.ChatMessage(ChatRole.Assistant, contents)));
		}

		public async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
			IEnumerable<Microsoft.Extensions.AI.ChatMessage> messages,
			ChatOptions? options = null,
			[EnumeratorCancellation] CancellationToken cancellationToken = default)
		{
			await Task.Yield();
			Requests.Add([.. messages]);
			foreach (var content in script(Requests.Count - 1))
				yield return new ChatResponseUpdate { Role = ChatRole.Assistant, Contents = [content] };
		}

		public object? GetService(Type serviceType, object? serviceKey = null) =>
			serviceType == typeof(IChatClient) ? this : null;
	}
}
