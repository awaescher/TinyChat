using System.Diagnostics;
using System.Drawing;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Windows.Forms;
using Microsoft.Extensions.AI;
using Shouldly;
using TinyChat;

namespace Tests;

[Apartment(ApartmentState.STA)]
public class StreamingThreadAffinityTests
{
	[Test]
	public void Streaming_Source_Runs_On_The_Ui_Thread_So_Tools_Can_Access_Controls()
	{
		SynchronizationContext.SetSynchronizationContext(new WindowsFormsSynchronizationContext());
		var uiThreadId = Environment.CurrentManagedThreadId;
		var chatClient = new ToolInvokingChatClient();

		using var control = new ChatControl
		{
			Size = new Size(400, 420),
			ServiceProvider = new SingleServiceProvider(chatClient)
		};
		typeof(ChatControl)
			.GetMethod("OnCreateControl", BindingFlags.Instance | BindingFlags.NonPublic)!
			.Invoke(control, null);

		control.SendMessage("Prompt");
		WaitUntil(() => control.ChatClientSendCompletion.IsCompleted);

		chatClient.ThreadIds.ShouldNotBeEmpty();
		chatClient.ThreadIds.ShouldAllBe(id => id == uiThreadId);
	}

	private static void WaitUntil(Func<bool> predicate)
	{
		var stopwatch = Stopwatch.StartNew();
		while (!predicate() && stopwatch.Elapsed < TimeSpan.FromSeconds(5))
		{
			Application.DoEvents();
			Thread.Sleep(10);
		}

		predicate().ShouldBeTrue();
	}

	private sealed class SingleServiceProvider(IChatClient chatClient) : IServiceProvider
	{
		public object? GetService(Type serviceType) => serviceType == typeof(IChatClient) ? chatClient : null;
	}

	private sealed class ToolInvokingChatClient : IChatClient
	{
		public List<int> ThreadIds { get; } = [];

		public void Dispose() { }

		public Task<ChatResponse> GetResponseAsync(
			IEnumerable<Microsoft.Extensions.AI.ChatMessage> messages,
			ChatOptions? options = null,
			CancellationToken cancellationToken = default)
		{
			return Task.FromResult(new ChatResponse(new Microsoft.Extensions.AI.ChatMessage(ChatRole.Assistant, "Done")));
		}

		public async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
			IEnumerable<Microsoft.Extensions.AI.ChatMessage> messages,
			ChatOptions? options = null,
			[EnumeratorCancellation] CancellationToken cancellationToken = default)
		{
			// Mirrors FunctionInvokingChatClient: tools run inside the stream, between updates.
			ThreadIds.Add(Environment.CurrentManagedThreadId);
			yield return new ChatResponseUpdate(ChatRole.Assistant, [new FunctionCallContent("call-1", "GetOpenForms")]);

			ThreadIds.Add(Environment.CurrentManagedThreadId);
			await Task.Delay(10, cancellationToken);
			ThreadIds.Add(Environment.CurrentManagedThreadId);
			yield return new ChatResponseUpdate(ChatRole.Tool, [new FunctionResultContent("call-1", "Form1")]);

			ThreadIds.Add(Environment.CurrentManagedThreadId);
			yield return new ChatResponseUpdate(ChatRole.Assistant, "Done");
			ThreadIds.Add(Environment.CurrentManagedThreadId);
		}

		public object? GetService(Type serviceType, object? serviceKey = null) =>
			serviceType == typeof(IChatClient) ? this : null;
	}
}
