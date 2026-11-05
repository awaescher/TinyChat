using System.Diagnostics;
using System.Drawing;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Windows.Forms;
using DevExpress.XtraEditors;
using Microsoft.Extensions.AI;
using Shouldly;
using TinyChat;

namespace Tests;

[Apartment(ApartmentState.STA)]
public class StreamScrollFollowTests
{
	[TestCase(false)]
	[TestCase(true)]
	public void Follows_The_Stream_To_Its_End_While_Scrolled_To_The_Bottom(bool devExpress)
	{
		using var host = CreateChat(devExpress, out var chat);
		chat.SendMessage("Prompt");
		WaitForResponse(chat);

		var (value, bottom) = ReadScroll(chat.MessageHistoryControl!);
		(bottom - value).ShouldBeLessThanOrEqualTo(1, "the history must stay at the end of the answer");
	}

	[TestCase(false, false)]
	[TestCase(true, false)]
	[TestCase(false, true)]
	[TestCase(true, true)]
	public void Stops_Following_After_The_User_Scrolled_Up(bool devExpress, bool endWithApproval)
	{
		using var host = CreateChat(devExpress, out var chat);
		var client = (StreamingClient)chat.ServiceProvider!.GetService(typeof(IChatClient))!;
		client.EndWithApproval = endWithApproval;
		chat.SendMessage("Prompt");
		var history = chat.MessageHistoryControl!;
		var watch = Stopwatch.StartNew();
		while (client.Chunks < 30 && watch.Elapsed < TimeSpan.FromSeconds(10))
			Pump(5);

		var userPosition = ScrollUp(history, 150);
		WaitForResponse(chat);

		ReadScroll(history).Value.ShouldBe(userPosition, "new content must not move the history away from what the user is reading");
	}

	[TestCase(false)]
	[TestCase(true)]
	public void Shows_An_Approval_Request_While_Following_The_Stream(bool devExpress)
	{
		using var host = CreateChat(devExpress, out var chat);
		var client = (StreamingClient)chat.ServiceProvider!.GetService(typeof(IChatClient))!;
		client.EndWithApproval = true;
		chat.SendMessage("Prompt");
		WaitForResponse(chat);

		var (value, bottom) = ReadScroll(chat.MessageHistoryControl!);
		(bottom - value).ShouldBeLessThanOrEqualTo(1, "the approval buttons must be visible");
	}

	private static Form CreateChat(bool devExpress, out ChatControl chat)
	{
		SynchronizationContext.SetSynchronizationContext(new WindowsFormsSynchronizationContext());
		var form = new Form { ClientSize = new Size(360, 420), StartPosition = FormStartPosition.Manual, Location = new Point(-4000, 0), ShowInTaskbar = false };
		chat = devExpress ? new DXChatControl() : new ChatControl();
		chat.Dock = DockStyle.Fill;
		chat.IncludeFunctionCalls = true;
		chat.IncludeReasoning = true;
		chat.ServiceProvider = new Provider(new StreamingClient());
		form.Controls.Add(chat);
		form.Show();
		var assistant = new NamedSender("Assistant");
		var history = new List<IChatMessage>();
		for (var index = 0; index < 8; index++)
		{
			history.Add(new TinyChat.ChatMessage(chat.Sender, new StringMessageContent("Question " + index)));
			history.Add(new TinyChat.ChatMessage(assistant, new StringMessageContent("An earlier answer that is long enough to wrap onto a second line in the history.")));
		}

		chat.Messages = history;
		Pump(300);
		return form;
	}

	private static void WaitForResponse(ChatControl chat)
	{
		var watch = Stopwatch.StartNew();
		while (!chat.ChatClientSendCompletion.IsCompleted && !chat.IsWaitingForApproval && watch.Elapsed < TimeSpan.FromSeconds(30))
			Pump(5);

		(chat.ChatClientSendCompletion.IsCompleted || chat.IsWaitingForApproval).ShouldBeTrue();
		Pump(500);
	}

	private static int ScrollUp(Control history, int pixels)
	{
		if (history is XtraScrollableControl dx)
		{
			var oldValue = dx.VerticalScroll.Value;
			var newValue = Math.Max(0, oldValue - pixels);
			dx.VerticalScroll.Value = newValue;
			history.GetType().GetMethods(BindingFlags.Instance | BindingFlags.NonPublic)
				.First(method => method.Name == "OnScroll" && method.GetParameters().Length == 2)
				.Invoke(history, [history, new XtraScrollEventArgs(ScrollEventType.ThumbPosition, oldValue, newValue, DevExpress.XtraEditors.ScrollOrientation.VerticalScroll)]);
			Pump(50);
			return ReadScroll(history).Value;
		}

		var scrollable = (ScrollableControl)history;
		var old = ReadScroll(history).Value;
		var target = Math.Max(0, old - pixels);
		scrollable.AutoScrollPosition = new Point(0, target);
		history.GetType().GetMethod("OnScroll", BindingFlags.Instance | BindingFlags.NonPublic, [typeof(ScrollEventArgs)])!
			.Invoke(history, [new ScrollEventArgs(ScrollEventType.ThumbPosition, old, target, System.Windows.Forms.ScrollOrientation.VerticalScroll)]);
		Pump(50);
		return ReadScroll(history).Value;
	}

	private static (int Value, int Bottom) ReadScroll(Control history)
	{
		if (history is XtraScrollableControl dx)
			return (dx.VerticalScroll.Value, Math.Max(0, dx.VerticalScroll.Maximum - dx.VerticalScroll.LargeChange + 1));

		// The display rectangle is what is shown; the scrollbar value of a ScrollableControl can lag behind it.
		var scrollable = (ScrollableControl)history;
		return (-scrollable.DisplayRectangle.Y, Math.Max(0, scrollable.DisplayRectangle.Height - scrollable.ClientSize.Height));
	}

	private static void Pump(int milliseconds)
	{
		var watch = Stopwatch.StartNew();
		do
		{
			Application.DoEvents();
			Thread.Sleep(2);
		}
		while (watch.ElapsedMilliseconds < milliseconds);
	}

	private sealed class Provider(IChatClient client) : IServiceProvider
	{
		public object? GetService(Type serviceType) => serviceType == typeof(IChatClient) ? client : null;
	}

	private sealed class StreamingClient : IChatClient
	{
		public int Chunks;

		public void Dispose() { }

		public Task<ChatResponse> GetResponseAsync(IEnumerable<Microsoft.Extensions.AI.ChatMessage> messages, ChatOptions? options = null, CancellationToken cancellationToken = default) =>
			Task.FromResult(new ChatResponse(new Microsoft.Extensions.AI.ChatMessage(ChatRole.Assistant, "x")));

		public async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(IEnumerable<Microsoft.Extensions.AI.ChatMessage> messages, ChatOptions? options = null, [EnumeratorCancellation] CancellationToken cancellationToken = default)
		{
			foreach (var word in "Let me think about this carefully.".Split(' '))
			{
				yield return new ChatResponseUpdate(ChatRole.Assistant, [new TextReasoningContent(word + " ")]);
				await Task.Delay(5, cancellationToken);
			}

			yield return new ChatResponseUpdate(ChatRole.Assistant, [new FunctionCallContent("call", "search")]);
			await Task.Delay(20, cancellationToken);
			yield return new ChatResponseUpdate(ChatRole.Tool, [new FunctionResultContent("call", "found")]);

			var text = string.Concat(Enumerable.Repeat("Based on my research, y is a fascinating topic. The knowledge base contains several well-sourced articles.\n\n", 3));
			foreach (var word in text.Split(' '))
			{
				Interlocked.Increment(ref Chunks);
				yield return new ChatResponseUpdate(ChatRole.Assistant, word + " ");
				await Task.Delay(5, cancellationToken);
			}

			yield return new ChatResponseUpdate(ChatRole.Assistant, [new FunctionCallContent("call-2", "verify")]);
			await Task.Delay(20, cancellationToken);
			yield return new ChatResponseUpdate(ChatRole.Tool, [new FunctionResultContent("call-2", "ok")]);
			yield return new ChatResponseUpdate(ChatRole.Assistant, [new TextReasoningContent("Checking once more. ")]);
			await Task.Delay(20, cancellationToken);

			// Many providers send the end of an answer in a single, larger update.
			yield return new ChatResponseUpdate(ChatRole.Assistant, "\n\nWould you like me to dive deeper into any specific aspect? A final paragraph that adds several more lines to the message at the very end of the stream.");

			if (EndWithApproval)
			{
				await Task.Delay(20, cancellationToken);
				yield return new ChatResponseUpdate(ChatRole.Assistant, [new ToolApprovalRequestContent("approval", new FunctionCallContent("call-3", "export", new Dictionary<string, object?> { ["topic"] = "y" }))]);
			}
		}

		public bool EndWithApproval { get; set; }

		public object? GetService(Type serviceType, object? serviceKey = null) => serviceType == typeof(IChatClient) ? this : null;
	}
}
