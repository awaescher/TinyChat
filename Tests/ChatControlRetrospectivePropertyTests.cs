using System.Diagnostics;
using System.Drawing;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Windows.Forms;
using DevExpress.XtraEditors;
using Microsoft.Extensions.AI;
using Shouldly;
using TinyChat;
using TinyChat.Messages;
using TinyChat.Messages.Formatting;

namespace Tests;

[Apartment(ApartmentState.STA)]
public class ChatControlRetrospectivePropertyTests
{
	public class AssistantSenderNameProperty
	{
		[TestCase(false)]
		[TestCase(true)]
		public void Renames_Existing_Automatic_Responses_In_Place_But_Not_Manual_Senders(bool useDevExpress)
		{
			var chatClient = new ImmediateResponseChatClient("Automatic response");
			using var control = CreateChatControl(useDevExpress);
			control.ServiceProvider = new SingleServiceProvider(chatClient);
			control.UseStreaming = false;

			control.SendMessage("Prompt").ShouldBeTrue();
			WaitUntil(
				() => chatClient.RequestCompleted.Task.IsCompleted && control.Messages.Count() == 2,
				"The automatic response was not added.");

			var automaticMessage = control.Messages.Single(message => message.Content.ToString() == "Automatic response");
			var automaticSender = automaticMessage.Sender;
			var automaticControl = FindMessageControl(control, automaticMessage);
			var manualControl = control.AddMessage(
				new NamedSender(control.AssistantSenderName),
				new StringMessageContent("Manual response"));
			var manualMessage = manualControl.Message!;
			var manualSender = manualMessage.Sender;
			var manualSenderLabel = FindSenderLabel(manualControl, "Assistant");

			manualSenderLabel.Visible.ShouldBeFalse();

			control.AssistantSenderName = "Renamed assistant";

			ReferenceEquals(automaticMessage, automaticControl.Message).ShouldBeTrue();
			ReferenceEquals(automaticSender, automaticMessage.Sender).ShouldBeTrue();
			ReferenceEquals(automaticControl, FindMessageControl(control, automaticMessage)).ShouldBeTrue();
			automaticMessage.Sender.Name.ShouldBe("Renamed assistant");
			FindSenderLabel(automaticControl, "Renamed assistant").Visible.ShouldBeTrue();

			ReferenceEquals(manualMessage, manualControl.Message).ShouldBeTrue();
			ReferenceEquals(manualSender, manualMessage.Sender).ShouldBeTrue();
			manualMessage.Sender.Name.ShouldBe("Assistant");
			manualSenderLabel.Text.ShouldBe("Assistant");
			manualSenderLabel.Visible.ShouldBeTrue();
		}
	}

	public class MessageFormatterProperty
	{
		[Test]
		public void Rejected_Null_Before_Handle_Does_Not_Prevent_DevExpress_Default()
		{
			using var control = new DXChatControl();

			Should.Throw<ArgumentNullException>(() => control.MessageFormatter = null!);

			InitializeControl(control);

			control.MessageFormatter.ShouldBeOfType<SimplifiedHtmlMessageFormatter>();
		}

		[TestCase(false)]
		[TestCase(true)]
		public void Re_Renders_Existing_Regular_And_Reasoning_Controls_In_Place(bool useDevExpress)
		{
			using var control = CreateChatControl(useDevExpress);
			var regularControl = control.AddMessage(
				new NamedSender("Assistant"),
				new StringMessageContent("Regular text"));
			var reasoningControl = control.AddMessage(
				new NamedSender("Assistant"),
				new ReasoningMessageContent("Reasoning text"));
			var regularMessage = regularControl.Message;
			var reasoningMessage = reasoningControl.Message;
			var formatter = new PrefixMessageFormatter("formatted: ");

			regularControl.ToString().ShouldBe("Regular text");
			reasoningControl.ToString().ShouldBe("Reasoning text");

			control.MessageFormatter = formatter;

			ReferenceEquals(regularControl, FindMessageControl(control, regularMessage!)).ShouldBeTrue();
			ReferenceEquals(reasoningControl, FindMessageControl(control, reasoningMessage!)).ShouldBeTrue();
			regularControl.ToString().ShouldBe("formatted: Regular text");
			reasoningControl.ToString().ShouldBe("formatted: Reasoning text");
			ReferenceEquals(((IChatMessageFormattingControl)regularControl).MessageFormatter, formatter).ShouldBeTrue();
			ReferenceEquals(((IChatMessageFormattingControl)reasoningControl).MessageFormatter, formatter).ShouldBeTrue();
		}
	}

	public class StreamingVisualizationProperties
	{
		[TestCase(false)]
		[TestCase(true)]
		public void Self_Assigning_Messages_Preserves_Hidden_Visualization_Origin(bool useDevExpress)
		{
			var chatClient = new StructuredStreamingChatClient();
			using var control = CreateChatControl(useDevExpress);
			control.ServiceProvider = new SingleServiceProvider(chatClient);
			control.IncludeFunctionCalls = false;
			control.IncludeReasoning = false;

			control.SendMessage("Prompt").ShouldBeTrue();
			WaitUntil(
				() =>
					chatClient.RequestCompleted.Task.IsCompleted &&
					control.Messages.Count() == 5 &&
					control.Messages.Any(message => message.Content.ToString() == "First answer") &&
					control.Messages.Any(message => message.Content.ToString() == "Second answer"),
				"The structured streaming response was not completely materialized.");

			var functionMessage = control.Messages.Single(message => message.Content is FunctionCallMessageContent);
			var reasoningMessage = control.Messages.Single(message => message.Content is ReasoningMessageContent);

			((Control)FindMessageControl(control, functionMessage)).Visible.ShouldBeFalse();
			((Control)FindMessageControl(control, reasoningMessage)).Visible.ShouldBeFalse();

			control.Messages = control.Messages;

			var rebuiltFunctionControl = FindMessageControl(control, functionMessage);
			var rebuiltReasoningControl = FindMessageControl(control, reasoningMessage);
			((Control)rebuiltFunctionControl).Visible.ShouldBeFalse();
			((Control)rebuiltReasoningControl).Visible.ShouldBeFalse();

			control.IncludeFunctionCalls = true;
			control.IncludeReasoning = true;

			((Control)rebuiltFunctionControl).Visible.ShouldBeTrue();
			((Control)rebuiltReasoningControl).Visible.ShouldBeTrue();
		}

		[TestCase(false)]
		[TestCase(true)]
		public void Retains_Hidden_Function_And_Reasoning_Controls_And_Toggles_Their_Visibility_In_Place(bool useDevExpress)
		{
			var chatClient = new StructuredStreamingChatClient();
			using var control = CreateChatControl(useDevExpress);
			control.ServiceProvider = new SingleServiceProvider(chatClient);
			control.IncludeFunctionCalls = false;
			control.IncludeReasoning = false;

			control.SendMessage("Prompt").ShouldBeTrue();
			WaitUntil(
				() =>
					chatClient.RequestCompleted.Task.IsCompleted &&
					control.Messages.Count() == 5 &&
					control.Messages.Any(message => message.Content.ToString() == "First answer") &&
					control.Messages.Any(message => message.Content.ToString() == "Second answer"),
				"The structured streaming response was not completely materialized.");

			var functionMessage = control.Messages.Single(message => message.Content is FunctionCallMessageContent);
			var reasoningMessage = control.Messages.Single(message => message.Content is ReasoningMessageContent);
			var functionControl = FindMessageControl(control, functionMessage);
			var reasoningControl = FindMessageControl(control, reasoningMessage);
			var assistantTextControls = control.Messages
				.Where(message =>
					message.Sender.Name == control.AssistantSenderName &&
					message.Content is not FunctionCallMessageContent &&
					message.Content is not ReasoningMessageContent)
				.Select(message => FindMessageControl(control, message))
				.ToArray();

			((Control)functionControl).Visible.ShouldBeFalse();
			((Control)reasoningControl).Visible.ShouldBeFalse();
			assistantTextControls.Length.ShouldBe(2);
			CountVisibleSenderHeaders(assistantTextControls, control.AssistantSenderName).ShouldBe(1);

			control.IncludeFunctionCalls = true;
			control.IncludeReasoning = true;

			ReferenceEquals(functionControl, FindMessageControl(control, functionMessage)).ShouldBeTrue();
			ReferenceEquals(reasoningControl, FindMessageControl(control, reasoningMessage)).ShouldBeTrue();
			((Control)functionControl).Visible.ShouldBeTrue();
			((Control)reasoningControl).Visible.ShouldBeTrue();
			CountVisibleSenderHeaders(
				[assistantTextControls[0], functionControl, reasoningControl, assistantTextControls[1]],
				control.AssistantSenderName).ShouldBe(1);

			control.IncludeFunctionCalls = false;
			control.IncludeReasoning = false;

			ReferenceEquals(functionControl, FindMessageControl(control, functionMessage)).ShouldBeTrue();
			ReferenceEquals(reasoningControl, FindMessageControl(control, reasoningMessage)).ShouldBeTrue();
			((Control)functionControl).Visible.ShouldBeFalse();
			((Control)reasoningControl).Visible.ShouldBeFalse();
			CountVisibleSenderHeaders(assistantTextControls, control.AssistantSenderName).ShouldBe(1);
		}
	}

	public class MessagesProperty
	{
		[TestCase(false)]
		[TestCase(true)]
		public void Cancels_An_Active_Chat_Client_Operation(bool useDevExpress)
		{
			var chatClient = new BlockingStreamingChatClient();
			using var control = CreateChatControl(useDevExpress);
			control.ServiceProvider = new SingleServiceProvider(chatClient);

			control.SendMessage("Prompt").ShouldBeTrue();
			WaitUntil(
				() => chatClient.Waiting.Task.IsCompleted && control.Messages.Count() >= 2,
				"The streaming request did not enter its cancellable wait.");
			var messagesBeforeAssignment = control.Messages.ToArray();

			control.Messages = control.Messages;

			WaitUntil(
				() => chatClient.Cancelled.Task.IsCompleted,
				"Assigning Messages did not cancel the active chat-client request.");
			chatClient.RequestToken.IsCancellationRequested.ShouldBeTrue();
			control.Messages.Count().ShouldBe(messagesBeforeAssignment.Length);
			control.Messages.Zip(messagesBeforeAssignment)
				.ShouldAllBe(pair => ReferenceEquals(pair.First, pair.Second));
		}
	}

	public class RemoveMessageMethod
	{
		[TestCase(false)]
		[TestCase(true)]
		public void Removes_Exactly_The_Requested_Value_Equal_Message_Instance(bool useDevExpress)
		{
			using var control = CreateChatControl(useDevExpress);
			var sender = new NamedSender("Assistant");
			var content = new StringMessageContent("Equal content");
			var firstMessage = new TinyChat.ChatMessage(sender, content);
			var secondMessage = new TinyChat.ChatMessage(sender, content);
			firstMessage.ShouldBe(secondMessage);
			ReferenceEquals(firstMessage, secondMessage).ShouldBeFalse();
			control.Messages = [firstMessage, secondMessage];
			var firstControl = FindMessageControl(control, firstMessage);
			var secondControl = FindMessageControl(control, secondMessage);

			control.RemoveMessage(secondMessage);

			control.Messages.Count().ShouldBe(1);
			ReferenceEquals(control.Messages.Single(), firstMessage).ShouldBeTrue();
			ReferenceEquals(FindMessageControl(control, firstMessage), firstControl).ShouldBeTrue();
			DescendantsAndSelf(control.MessageHistoryControl!)
				.OfType<IChatMessageControl>()
				.ShouldAllBe(messageControl => !ReferenceEquals(messageControl.Message, secondMessage));
			secondControl.Message.ShouldBeNull();
			((Control)secondControl).IsDisposed.ShouldBeTrue();
		}
	}

	public class ExpansionProperties
	{
		[TestCase(false)]
		[TestCase(true)]
		public void Apply_Live_Collapse_And_Block_Reopening(bool useDevExpress)
		{
			using var control = CreateChatControl(useDevExpress);
			var functionControl = control.AddMessage(
				new NamedSender("Assistant"),
				new FunctionCallMessageContent(
					"call-1",
					"lookup",
					new Dictionary<string, object?> { ["query"] = "TinyChat" },
					"Found"));
			var reasoningControl = control.AddMessage(
				new NamedSender("Assistant"),
				new ReasoningMessageContent("Detailed reasoning"));
			var functionEditors = FindDetailEditors((Control)functionControl, useDevExpress);
			var reasoningEditors = FindDetailEditors((Control)reasoningControl, useDevExpress);

			InvokeToggle((Control)functionControl);
			InvokeToggle((Control)reasoningControl);
			functionEditors.ShouldAllBe(editor => editor.Visible);
			reasoningEditors.ShouldAllBe(editor => editor.Visible);

			control.AllowExpandFunctionCalls = false;
			control.AllowExpandReasoning = false;
			((IFunctionCallMessageControl)functionControl).AllowExpand.ShouldBeFalse();
			((IReasoningMessageControl)reasoningControl).AllowExpand.ShouldBeFalse();
			functionEditors.ShouldAllBe(editor => !editor.Visible);
			reasoningEditors.ShouldAllBe(editor => !editor.Visible);

			InvokeToggle((Control)functionControl);
			InvokeToggle((Control)reasoningControl);
			functionEditors.ShouldAllBe(editor => !editor.Visible);
			reasoningEditors.ShouldAllBe(editor => !editor.Visible);

			control.AllowExpandFunctionCalls = true;
			control.AllowExpandReasoning = true;
			InvokeToggle((Control)functionControl);
			InvokeToggle((Control)reasoningControl);
			functionEditors.ShouldAllBe(editor => editor.Visible);
			reasoningEditors.ShouldAllBe(editor => editor.Visible);
		}
	}

	public class AllowFileAttachmentsProperty
	{
		[TestCase(false)]
		[TestCase(true)]
		public void Blocks_Clipboard_Attachment_Requests_When_Disabled(bool useDevExpress)
		{
			using var control = CreatePasteTestChatControl(useDevExpress);
			var pasteInput = ((IPasteTestChatControl)control).PasteInput;
			control.AllowFileAttachments = false;
			using var bitmap = new Bitmap(2, 2);
			var data = new DataObject();
			data.SetData(DataFormats.Bitmap, bitmap);

			pasteInput.RaiseAttachmentPaste(data);
			PumpMessages(TimeSpan.FromMilliseconds(100));

			control.AllowDrop.ShouldBeFalse();
			pasteInput.AllowDrop.ShouldBeFalse();
			pasteInput.PendingAttachments.ShouldBeEmpty();
			pasteInput.AddAttachmentsCallCount.ShouldBe(0);
		}
	}

	private static ChatControl CreateChatControl(bool useDevExpress)
	{
		var control = useDevExpress
			? (ChatControl)new DevExpressTestChatControl()
			: new NativeTestChatControl();
		InitializeControl(control);
		return control;
	}

	private static ChatControl CreatePasteTestChatControl(bool useDevExpress)
	{
		var control = useDevExpress
			? (ChatControl)new DevExpressPasteTestChatControl()
			: new NativePasteTestChatControl();
		InitializeControl(control);
		return control;
	}

	private static void InitializeControl(ChatControl control)
	{
		control.Size = new Size(400, 420);
		if (control is not DXChatControl)
		{
			typeof(ChatControl)
				.GetMethod("OnCreateControl", BindingFlags.Instance | BindingFlags.NonPublic)!
				.Invoke(control, null);
			return;
		}

		var host = new Panel { Size = control.Size };
		control.Dock = DockStyle.Fill;
		host.Controls.Add(control);
		host.CreateControl();
		control.CreateControl();
		Application.DoEvents();
		control.Disposed += (_, _) => host.Dispose();
	}

	private static IChatMessageControl FindMessageControl(ChatControl control, IChatMessage message)
	{
		return DescendantsAndSelf(control.MessageHistoryControl!)
			.OfType<IChatMessageControl>()
			.Single(messageControl => ReferenceEquals(messageControl.Message, message));
	}

	private static Control FindSenderLabel(IChatMessageControl messageControl, string senderName)
	{
		return DescendantsAndSelf((Control)messageControl)
			.Single(control => control is Label or LabelControl && control.Text == senderName);
	}

	private static int CountVisibleSenderHeaders(
		IEnumerable<IChatMessageControl> messageControls,
		string senderName)
	{
		return messageControls.Count(messageControl => FindSenderLabel(messageControl, senderName).Visible);
	}

	private static Control[] FindDetailEditors(Control messageControl, bool useDevExpress)
	{
		// The detail rows (arguments, result, reasoning) are the text labels that start collapsed.
		var editors = DescendantsAndSelf(messageControl)
			.OfType<TableLayoutPanel>()
			.SelectMany(table => table.Controls.Cast<Control>()
				.Where(control => table.GetColumn(control) == 1 && table.GetRow(control) > 0))
			.Where(control => (useDevExpress ? control is LabelControl : control is Label) && !control.Visible)
			.ToArray();
		editors.ShouldNotBeEmpty();
		return editors;
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

	private static void InvokeToggle(Control control)
	{
		control.GetType()
			.GetMethod("Toggle", BindingFlags.Instance | BindingFlags.NonPublic)!
			.Invoke(control, [control, EventArgs.Empty]);
		control.Parent?.PerformLayout();
		control.PerformLayout();
		Application.DoEvents();
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

	private static void PumpMessages(TimeSpan duration)
	{
		var stopwatch = Stopwatch.StartNew();
		while (stopwatch.Elapsed < duration)
		{
			Application.DoEvents();
			Thread.Sleep(10);
		}
	}

	private sealed class PrefixMessageFormatter(string prefix) : IMessageFormatter
	{
		public string Format(IChatMessageContent content) => Format(content.ToString() ?? string.Empty);

		public string Format(string content) => prefix + content;
	}

	private sealed class SingleServiceProvider(IChatClient chatClient) : IServiceProvider
	{
		public object? GetService(Type serviceType) => serviceType == typeof(IChatClient) ? chatClient : null;
	}

	private sealed class ImmediateResponseChatClient(string responseText) : IChatClient
	{
		public TaskCompletionSource RequestCompleted { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

		public void Dispose() { }

		public Task<ChatResponse> GetResponseAsync(
			IEnumerable<Microsoft.Extensions.AI.ChatMessage> messages,
			ChatOptions? options = null,
			CancellationToken cancellationToken = default)
		{
			RequestCompleted.TrySetResult();
			return Task.FromResult(new ChatResponse(
				new Microsoft.Extensions.AI.ChatMessage(ChatRole.Assistant, responseText)));
		}

		public async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
			IEnumerable<Microsoft.Extensions.AI.ChatMessage> messages,
			ChatOptions? options = null,
			[EnumeratorCancellation] CancellationToken cancellationToken = default)
		{
			await Task.CompletedTask;
			yield break;
		}

		public object? GetService(Type serviceType, object? serviceKey = null) =>
			serviceType == typeof(IChatClient) ? this : null;
	}

	private sealed class StructuredStreamingChatClient : IChatClient
	{
		public TaskCompletionSource RequestCompleted { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

		public void Dispose() { }

		public Task<ChatResponse> GetResponseAsync(
			IEnumerable<Microsoft.Extensions.AI.ChatMessage> messages,
			ChatOptions? options = null,
			CancellationToken cancellationToken = default)
		{
			return Task.FromResult(new ChatResponse(
				new Microsoft.Extensions.AI.ChatMessage(ChatRole.Assistant, "Unused")));
		}

		public async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
			IEnumerable<Microsoft.Extensions.AI.ChatMessage> messages,
			ChatOptions? options = null,
			[EnumeratorCancellation] CancellationToken cancellationToken = default)
		{
			await Task.CompletedTask;
			try
			{
				yield return CreateUpdate(new TextContent("First answer"));
				yield return CreateUpdate(new FunctionCallContent(
					"call-1",
					"lookup",
					new Dictionary<string, object?> { ["query"] = "TinyChat" }));
				yield return CreateUpdate(new TextReasoningContent("Reasoning detail"));
				yield return CreateUpdate(new TextContent("Second answer"));
			}
			finally
			{
				RequestCompleted.TrySetResult();
			}
		}

		public object? GetService(Type serviceType, object? serviceKey = null) =>
			serviceType == typeof(IChatClient) ? this : null;

		private static ChatResponseUpdate CreateUpdate(AIContent content) => new()
		{
			Role = ChatRole.Assistant,
			Contents = [content]
		};
	}

	private sealed class BlockingStreamingChatClient : IChatClient
	{
		public TaskCompletionSource Waiting { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

		public TaskCompletionSource Cancelled { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

		public CancellationToken RequestToken { get; private set; }

		public void Dispose() { }

		public Task<ChatResponse> GetResponseAsync(
			IEnumerable<Microsoft.Extensions.AI.ChatMessage> messages,
			ChatOptions? options = null,
			CancellationToken cancellationToken = default)
		{
			return Task.FromResult(new ChatResponse(
				new Microsoft.Extensions.AI.ChatMessage(ChatRole.Assistant, "Unused")));
		}

		public async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
			IEnumerable<Microsoft.Extensions.AI.ChatMessage> messages,
			ChatOptions? options = null,
			[EnumeratorCancellation] CancellationToken cancellationToken = default)
		{
			RequestToken = cancellationToken;
			try
			{
				yield return new ChatResponseUpdate
				{
					Role = ChatRole.Assistant,
					Contents = [new TextContent("Partial response")]
				};
				Waiting.TrySetResult();
				await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
			}
			finally
			{
				Cancelled.TrySetResult();
			}
		}

		public object? GetService(Type serviceType, object? serviceKey = null) =>
			serviceType == typeof(IChatClient) ? this : null;
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
			return base.AddStreamingMessage(
				sender,
				stream,
				synchronizationContext ?? new SynchronizationContext(),
				completionCallback,
				exceptionCallback,
				cancellationToken);
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
			return base.AddStreamingMessage(
				sender,
				stream,
				synchronizationContext ?? new SynchronizationContext(),
				completionCallback,
				exceptionCallback,
				cancellationToken);
		}
	}

	private interface IPasteTestChatControl
	{
		PasteTestInputControl PasteInput { get; }
	}

	private sealed class NativePasteTestChatControl : ChatControl, IPasteTestChatControl
	{
		public PasteTestInputControl PasteInput { get; } = new();

		protected override IChatInputControl CreateChatInputControl() => PasteInput;
	}

	private sealed class DevExpressPasteTestChatControl : DXChatControl, IPasteTestChatControl
	{
		public PasteTestInputControl PasteInput { get; } = new();

		protected override IChatInputControl CreateChatInputControl() => PasteInput;
	}

	private sealed class PasteTestInputControl : Control, IChatInputControl, IChatAttachmentInputControl
	{
		private readonly List<ChatFileAttachment> _attachments = [];

		public event EventHandler? CancellationRequested { add { } remove { } }

		public event EventHandler<MessageSendingEventArgs> MessageSending { add { } remove { } }

		public event EventHandler<AttachmentPasteRequestedEventArgs>? AttachmentPasteRequested;

		public event EventHandler? AttachmentsChanged;

		public IReadOnlyList<ChatFileAttachment> PendingAttachments => _attachments.AsReadOnly();

		public int AttachmentDisplayHeight => 0;

		public int AddAttachmentsCallCount { get; private set; }

		public void AddAttachments(IEnumerable<ChatFileAttachment> attachments)
		{
			AddAttachmentsCallCount++;
			_attachments.AddRange(attachments);
			AttachmentsChanged?.Invoke(this, EventArgs.Empty);
		}

		public void ClearAttachments()
		{
			_attachments.Clear();
			AttachmentsChanged?.Invoke(this, EventArgs.Empty);
		}

		public void RaiseAttachmentPaste(IDataObject data) =>
			AttachmentPasteRequested?.Invoke(this, new AttachmentPasteRequestedEventArgs(data));

		public void SetIsReceivingStream(bool isReceiving, bool allowCancellation) { }
	}
}
