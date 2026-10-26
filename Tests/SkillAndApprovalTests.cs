using System.Diagnostics;
using System.Drawing;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Windows.Forms;
using Microsoft.Extensions.AI;
using Shouldly;
using TinyChat;

namespace Tests;

[Apartment(ApartmentState.STA)]
public class SkillAndApprovalTests
{
	public class DefaultSkillCallRecognizerTests
	{
		[Test]
		public void Recognizes_Load_Skill_With_Skill_Name()
		{
			var result = new DefaultSkillCallRecognizer().Recognize(
				"load_skill",
				new Dictionary<string, object?> { ["skillName"] = "onboarding" });

			result.ShouldBe(new SkillCallInfo(SkillCallKind.Load, "onboarding"));
		}

		[Test]
		public void Recognizes_Resource_And_Script_With_Json_Arguments()
		{
			var recognizer = new DefaultSkillCallRecognizer();
			var resourceArguments = new Dictionary<string, object?>
			{
				["skillName"] = JsonSerializer.SerializeToElement("onboarding"),
				["resourceName"] = JsonSerializer.SerializeToElement("references/checklist.md")
			};
			var scriptArguments = new Dictionary<string, object?> { ["skillName"] = "onboarding", ["scriptName"] = "check" };

			recognizer.Recognize("read_skill_resource", resourceArguments)
				.ShouldBe(new SkillCallInfo(SkillCallKind.ReadResource, "onboarding", "references/checklist.md"));
			recognizer.Recognize("run_skill_script", scriptArguments)
				.ShouldBe(new SkillCallInfo(SkillCallKind.RunScript, "onboarding", "check"));
		}

		[Test]
		public void Ignores_Calls_With_Missing_Arguments_Or_Other_Names()
		{
			var recognizer = new DefaultSkillCallRecognizer();

			recognizer.Recognize("load_skill", null).ShouldBeNull();
			recognizer.Recognize("load_skill", new Dictionary<string, object?> { ["name"] = "x" }).ShouldBeNull();
			recognizer.Recognize("read_skill_resource", new Dictionary<string, object?> { ["skillName"] = "x" }).ShouldBeNull();
			recognizer.Recognize("lookup", new Dictionary<string, object?> { ["skillName"] = "x" }).ShouldBeNull();
		}

		[Test]
		public void Respects_Known_Skill_Names_And_Custom_Tool_Names()
		{
			var recognizer = new DefaultSkillCallRecognizer
			{
				LoadSkillToolName = "use_skill",
				KnownSkillNames = new HashSet<string> { "known" }
			};

			recognizer.Recognize("use_skill", new Dictionary<string, object?> { ["skillName"] = "known" }).ShouldNotBeNull();
			recognizer.Recognize("use_skill", new Dictionary<string, object?> { ["skillName"] = "other" }).ShouldBeNull();
			recognizer.Recognize("load_skill", new Dictionary<string, object?> { ["skillName"] = "known" }).ShouldBeNull();
		}
	}

	public class ChatIconsTests
	{
		[Explicit("Writes screenshots for a visual check")]
		[Test]
		public void Renders_DevExpress_Send_And_Stop_Buttons()
		{
			using var form = new Form { Size = new Size(320, 160) };
			using var input = new DXChatInputControl { Dock = DockStyle.Fill };
			form.Controls.Add(input);
			form.Show();
			Application.DoEvents();
			var button = DescendantsAndSelf(input).OfType<DevExpress.XtraEditors.SimpleButton>().First(b => b.Visible && b.Width == b.Height);

			foreach (var (name, receiving) in new[] { ("send", false), ("stop", true) })
			{
				((IChatInputControl)input).SetIsReceivingStream(receiving, allowCancellation: true);
				Application.DoEvents();
				using var bitmap = new Bitmap(button.Width, button.Height);
				button.DrawToBitmap(bitmap, new Rectangle(Point.Empty, bitmap.Size));
				bitmap.Save(Path.Combine(Path.GetTempPath(), "opencode", $"dx_{name}_button.png"));
			}
		}

		[Explicit("Writes screenshots for a visual check")]
		[Test]
		public void Renders_DevExpress_Result_Row()
		{
			using var form = new Form { Size = new Size(320, 220) };
			using var control = new DXChatControl { Dock = DockStyle.Fill, IncludeFunctionCalls = true };
			form.Controls.Add(control);
			form.Show();
			var messageControl = (Control)control.AddMessage(new NamedSender("Assistant"), new FunctionCallMessageContent("1", "verify_facts", new Dictionary<string, object?> { ["claim"] = "x" }, "Confirmed"));
			messageControl.GetType().GetMethod("Toggle", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(messageControl, [messageControl, EventArgs.Empty]);
			for (var index = 0; index < 20; index++)
			{
				Application.DoEvents();
				Thread.Sleep(10);
			}

			using var bitmap = new Bitmap(form.ClientSize.Width, form.ClientSize.Height);
			form.DrawToBitmap(bitmap, new Rectangle(Point.Empty, bitmap.Size));
			bitmap.Save(Path.Combine(Path.GetTempPath(), "opencode", "dx_result.png"));
			var icons = DescendantsAndSelf(messageControl).OfType<DevExpress.XtraEditors.LabelControl>().Where(label => label.ImageOptions.SvgImage is not null).ToArray();
			TestContext.Out.WriteLine(string.Join(" | ", icons.Select(icon => $"{icon.Bounds} svg={icon.ImageOptions.SvgImageSize} align={icon.Appearance.ImageAlign}")));
		}

		[Explicit("Writes screenshots for a visual check")]
		[TestCase("light", 255, 255, 255, 0, 0, 0)]
		[TestCase("dark", 32, 32, 32, 230, 230, 230)]
		public void Renders_Icons_In_Light_And_Dark_Themes(string name, int br, int bg, int bb, int fr, int fg, int fb)
		{
			using var form = new Form { Size = new Size(360, 420), BackColor = Color.FromArgb(br, bg, bb), ForeColor = Color.FromArgb(fr, fg, fb) };
			using var control = new ChatControl { Dock = DockStyle.Fill, BackColor = form.BackColor, ForeColor = form.ForeColor, IncludeFunctionCalls = true, IncludeReasoning = true };
			form.Controls.Add(control);
			form.Show();
			control.AddMessage(new NamedSender("Assistant"), new TinyChat.Messages.ReasoningMessageContent("Thinking").SetDone());
			control.AddMessage(new NamedSender("Assistant"), new FunctionCallMessageContent("1", "lookup", null, "ok"));
			control.AddMessage(new NamedSender("Assistant"), new FunctionCallMessageContent("2", "load_skill", null, "ok") { Skill = new SkillCallInfo(SkillCallKind.Load, "research-assistant") });
			Application.DoEvents();

			var sendButton = DescendantsAndSelf(control.InputControl!).OfType<Button>().Single(button => button.Visible);
			using var inputBitmap = new Bitmap(sendButton.Width, sendButton.Height);
			sendButton.DrawToBitmap(inputBitmap, new Rectangle(Point.Empty, inputBitmap.Size));
			inputBitmap.Save(Path.Combine(Path.GetTempPath(), "opencode", $"theme_{name}_input.png"));

			using var bitmap = new Bitmap(form.ClientSize.Width, form.ClientSize.Height);
			form.DrawToBitmap(bitmap, new Rectangle(Point.Empty, bitmap.Size));
			bitmap.Save(Path.Combine(Path.GetTempPath(), "opencode", $"theme_{name}.png"));
		}

		[Test]
		public void Provides_All_Fluent_Icons_For_DevExpress()
		{
			var fluentIcons = typeof(DXChatControl).Assembly.GetType("TinyChat.FluentIcons")!;
			var icons = new[] { "Tool", "Skill", "Reasoning", "Result", "Send", "Stop" }
				.Select(name => (DevExpress.Utils.Svg.SvgImage)fluentIcons.GetProperty(name, BindingFlags.Static | BindingFlags.NonPublic)!.GetValue(null)!)
				.ToArray();

			icons.ShouldAllBe(icon => icon.Width == 16 && icon.Height == 16);
		}
	}

	public class ChatClientIntegration
	{
		[TestCase(false, true)]
		[TestCase(false, false)]
		[TestCase(true, true)]
		public void Shows_Skill_Calls_Even_When_Function_Calls_Are_Hidden(bool useDevExpress, bool useStreaming)
		{
			var chatClient = new ScriptedChatClient(_ =>
			[
				new FunctionCallContent("call-1", "load_skill", new Dictionary<string, object?> { ["skillName"] = "onboarding" }),
				new FunctionResultContent("call-1", "Skill instructions"),
				new FunctionCallContent("call-2", "lookup", new Dictionary<string, object?> { ["query"] = "x" }),
				new FunctionResultContent("call-2", "Found"),
				new TextContent("Done")
			]);
			using var control = CreateChatControl(useDevExpress, chatClient);
			control.UseStreaming = useStreaming;

			control.SendMessage("Prompt");
			WaitUntil(() => control.Messages.Any(message => message.Content.ToString() == "Done"), "No answer.");

			var skillMessage = control.Messages.Single(message => message.Content is FunctionCallMessageContent { Skill: not null });
			var functionMessage = control.Messages.Single(message => message.Content is FunctionCallMessageContent { Skill: null });
			((FunctionCallMessageContent)skillMessage.Content).Skill!.SkillName.ShouldBe("onboarding");
			((Control)FindMessageControl(control, skillMessage)).Visible.ShouldBeTrue();
			((Control)FindMessageControl(control, functionMessage)).Visible.ShouldBeFalse();

			control.IncludeSkillCalls = false;
			((Control)FindMessageControl(control, skillMessage)).Visible.ShouldBeFalse();
		}

		[Test]
		public void Shows_Skill_Tools_As_Function_Calls_When_Recognition_Is_Disabled()
		{
			var chatClient = new ScriptedChatClient(_ =>
			[
				new FunctionCallContent("call-1", "load_skill", new Dictionary<string, object?> { ["skillName"] = "onboarding" }),
				new FunctionResultContent("call-1", "Skill instructions"),
				new TextContent("Done")
			]);
			using var control = CreateChatControl(false, chatClient);
			control.RecognizeSkillCalls = false;

			control.SendMessage("Prompt");
			WaitUntil(() => control.Messages.Any(message => message.Content.ToString() == "Done"), "No answer.");

			control.Messages.Select(message => message.Content).OfType<FunctionCallMessageContent>().Single().Skill.ShouldBeNull();
		}

		[Test]
		public void Marks_Skill_Results_Starting_With_Error_As_Failed()
		{
			var chatClient = new ScriptedChatClient(_ =>
			[
				new FunctionCallContent("call-1", "load_skill", new Dictionary<string, object?> { ["skillName"] = "missing" }),
				new FunctionResultContent("call-1", "Error: Skill 'missing' not found."),
				new TextContent("Done")
			]);
			using var control = CreateChatControl(false, chatClient);

			control.SendMessage("Prompt");
			WaitUntil(() => control.Messages.Any(message => message.Content.ToString() == "Done"), "No answer.");

			control.Messages.Select(message => message.Content).OfType<FunctionCallMessageContent>().Single().HasError.ShouldBeTrue();
		}

		[TestCase(false, true, true)]
		[TestCase(false, false, true)]
		[TestCase(false, true, false)]
		[TestCase(true, true, true)]
		public void Sends_The_Approval_Decision_Back_And_Continues(bool useDevExpress, bool useStreaming, bool approve)
		{
			var functionCall = new FunctionCallContent("call-1", "run_skill_script", new Dictionary<string, object?>
			{
				["skillName"] = "onboarding",
				["scriptName"] = "check"
			});
			var chatClient = new ScriptedChatClient(request => request switch
			{
				0 => [new ToolApprovalRequestContent("request-1", functionCall)],
				_ => [new FunctionCallContent("call-1", functionCall.Name, functionCall.Arguments), new FunctionResultContent("call-1", "ok"), new TextContent("Finished")]
			});
			using var control = CreateChatControl(useDevExpress, chatClient);
			control.UseStreaming = useStreaming;
			FunctionCallMessageContent? requested = null;
			control.ToolApprovalRequested += (_, e) => requested = e.Content;

			control.SendMessage("Prompt");
			WaitUntil(() => requested?.ApprovalState == ToolApprovalState.Pending, "No approval was requested.");

			var approvalMessage = control.Messages.Single(message => ReferenceEquals(message.Content, requested));
			var messageControl = (Control)FindMessageControl(control, approvalMessage);
			messageControl.Visible.ShouldBeTrue();
			var buttons = DescendantsAndSelf(messageControl).OfType<ButtonBase>().Concat<Control>(
				DescendantsAndSelf(messageControl).Where(c => c is DevExpress.XtraEditors.SimpleButton)).ToArray();
			buttons.Length.ShouldBe(2);
			buttons.ShouldAllBe(button => button.Visible);
			chatClient.RequestCount.ShouldBe(1);

			if (approve)
				requested!.Approve();
			else
				requested!.Reject();

			WaitUntil(() => control.Messages.Any(message => message.Content.ToString() == "Finished"), "No follow-up answer.");

			chatClient.RequestCount.ShouldBe(2);
			var approvalResponse = chatClient.Requests[1]
				.SelectMany(message => message.Contents)
				.OfType<ToolApprovalResponseContent>()
				.Single();
			approvalResponse.RequestId.ShouldBe("request-1");
			approvalResponse.Approved.ShouldBe(approve);
			control.Messages.Count(message => message.Content is FunctionCallMessageContent).ShouldBe(1);
			requested.Result.ShouldBe("ok");
			buttons.ShouldAllBe(button => !button.Visible);
		}

		[Test]
		public void Native_Approval_Buttons_Are_Fully_Visible_In_The_History()
		{
			var chatClient = new ScriptedChatClient(_ =>
				[new ToolApprovalRequestContent("request-1", new FunctionCallContent("call-1", "run_skill_script", new Dictionary<string, object?>
				{
					["skillName"] = "research-assistant",
					["scriptName"] = "export-summary",
					["arguments"] = "{ \"topic\": \"Transformer architecture\" }"
				}))]);
			using var form = new Form { Size = new Size(320, 400) };
			using var control = CreateChatControl(false, chatClient);
			control.Dock = DockStyle.Fill;
			form.Controls.Add(control);
			form.Show();
			for (var index = 0; index < 20; index++)
				control.AddMessage(new NamedSender("Assistant"), new StringMessageContent("Filler line " + index));

			FunctionCallMessageContent? requested = null;
			control.ToolApprovalRequested += (_, e) => requested = e.Content;
			control.SendMessage("Prompt");
			WaitUntil(() => requested is not null, "No approval was requested.");
			var stopwatch = Stopwatch.StartNew();
			while (stopwatch.ElapsedMilliseconds < 500)
			{
				Application.DoEvents();
				Thread.Sleep(10);
			}

			var history = control.MessageHistoryControl!;
			var messageControl = (Control)FindMessageControl(control, control.Messages.Single(m => ReferenceEquals(m.Content, requested)));
			var button = DescendantsAndSelf(messageControl).OfType<Button>().First();
			var args = DescendantsAndSelf(messageControl).OfType<TextBox>().First(t => t.Visible);
			var buttonBounds = history.RectangleToClient(button.RectangleToScreen(button.ClientRectangle));
			var argsBounds = history.RectangleToClient(args.RectangleToScreen(args.ClientRectangle));
			var messageBounds = history.RectangleToClient(messageControl.RectangleToScreen(messageControl.ClientRectangle));

			buttonBounds.Top.ShouldBeGreaterThanOrEqualTo(argsBounds.Bottom);
			buttonBounds.Bottom.ShouldBeLessThanOrEqualTo(messageBounds.Bottom);
			buttonBounds.Bottom.ShouldBeLessThanOrEqualTo(history.ClientSize.Height);
		}

		[Test]
		public void Cancelling_Rejects_Pending_Approvals_Without_A_Follow_Up_Request()
		{
			var chatClient = new ScriptedChatClient(_ =>
				[new ToolApprovalRequestContent("request-1", new FunctionCallContent("call-1", "lookup"))]);
			using var control = CreateChatControl(false, chatClient);
			FunctionCallMessageContent? requested = null;
			control.ToolApprovalRequested += (_, e) => requested = e.Content;

			control.SendMessage("Prompt");
			WaitUntil(() => requested is not null, "No approval was requested.");

			control.CancelCurrentOperation();
			WaitUntil(() => control.ChatClientSendCompletion.IsCompleted, "The operation did not end.");

			requested!.ApprovalState.ShouldBe(ToolApprovalState.Rejected);
			chatClient.RequestCount.ShouldBe(1);
		}
	}

	private static ChatControl CreateChatControl(bool useDevExpress, IChatClient chatClient)
	{
		// Mirror a WinForms application, where continuations return to the UI thread.
		SynchronizationContext.SetSynchronizationContext(new WindowsFormsSynchronizationContext());

		ChatControl control = useDevExpress ? new DevExpressTestChatControl() : new NativeTestChatControl();
		control.Size = new Size(400, 420);
		control.ServiceProvider = new SingleServiceProvider(chatClient);
		if (!useDevExpress)
		{
			typeof(ChatControl)
				.GetMethod("OnCreateControl", BindingFlags.Instance | BindingFlags.NonPublic)!
				.Invoke(control, null);
			return control;
		}

		var host = new Panel { Size = control.Size };
		control.Dock = DockStyle.Fill;
		host.Controls.Add(control);
		host.CreateControl();
		control.CreateControl();
		Application.DoEvents();
		control.Disposed += (_, _) => host.Dispose();
		return control;
	}

	private static IChatMessageControl FindMessageControl(ChatControl control, IChatMessage message)
	{
		return DescendantsAndSelf(control.MessageHistoryControl!)
			.OfType<IChatMessageControl>()
			.Single(messageControl => ReferenceEquals(messageControl.Message, message));
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

	private sealed class SingleServiceProvider(IChatClient chatClient) : IServiceProvider
	{
		public object? GetService(Type serviceType) => serviceType == typeof(IChatClient) ? chatClient : null;
	}

	private sealed class ScriptedChatClient(Func<int, AIContent[]> script) : IChatClient
	{
		public List<List<Microsoft.Extensions.AI.ChatMessage>> Requests { get; } = [];

		public int RequestCount => Requests.Count;

		public void Dispose() { }

		public Task<ChatResponse> GetResponseAsync(
			IEnumerable<Microsoft.Extensions.AI.ChatMessage> messages,
			ChatOptions? options = null,
			CancellationToken cancellationToken = default)
		{
			var contents = Next(messages);
			return Task.FromResult(new ChatResponse(new Microsoft.Extensions.AI.ChatMessage(ChatRole.Assistant, contents)));
		}

		public async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
			IEnumerable<Microsoft.Extensions.AI.ChatMessage> messages,
			ChatOptions? options = null,
			[EnumeratorCancellation] CancellationToken cancellationToken = default)
		{
			await Task.Yield();
			foreach (var content in Next(messages))
				yield return new ChatResponseUpdate { Role = ChatRole.Assistant, Contents = [content] };
		}

		public object? GetService(Type serviceType, object? serviceKey = null) =>
			serviceType == typeof(IChatClient) ? this : null;

		private AIContent[] Next(IEnumerable<Microsoft.Extensions.AI.ChatMessage> messages)
		{
			Requests.Add([.. messages]);
			return script(Requests.Count - 1);
		}
	}

	private sealed class NativeTestChatControl : ChatControl
	{
		public override IChatMessageControl AddStreamingMessage(
			ISender sender,
			IAsyncEnumerable<string> stream,
			SynchronizationContext? synchronizationContext = default,
			Action<string>? completionCallback = default,
			Action<Exception>? exceptionCallback = default,
			CancellationToken cancellationToken = default)
		{
			return base.AddStreamingMessage(sender, stream, synchronizationContext ?? new SynchronizationContext(), completionCallback, exceptionCallback, cancellationToken);
		}
	}

	private sealed class DevExpressTestChatControl : DXChatControl
	{
		public override IChatMessageControl AddStreamingMessage(
			ISender sender,
			IAsyncEnumerable<string> stream,
			SynchronizationContext? synchronizationContext = default,
			Action<string>? completionCallback = default,
			Action<Exception>? exceptionCallback = default,
			CancellationToken cancellationToken = default)
		{
			return base.AddStreamingMessage(sender, stream, synchronizationContext ?? new SynchronizationContext(), completionCallback, exceptionCallback, cancellationToken);
		}
	}
}
