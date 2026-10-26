using System.Drawing;
using System.Reflection;
using System.Windows.Forms;
using Shouldly;
using TinyChat;
using TinyChat.Messages;
using TinyChat.Messages.Formatting;

namespace Tests;

[Apartment(ApartmentState.STA)]
public class RenderPerformanceTests
{
	[TestCase(false)]
	[TestCase(true)]
	public void Adds_A_Message_With_One_Format_Pass(bool useDevExpress)
	{
		using var host = new Panel { Size = new Size(500, 500) };
		using var chatControl = useDevExpress ? (ChatControl)new DXChatControl() : new ChatControl();
		chatControl.Size = host.Size;
		if (useDevExpress)
		{
			host.Controls.Add(chatControl);
			host.CreateControl();
			chatControl.CreateControl();
			Application.DoEvents();
		}
		else
		{
			typeof(ChatControl)
				.GetMethod("OnCreateControl", BindingFlags.Instance | BindingFlags.NonPublic)!
				.Invoke(chatControl, null);
		}

		var formatter = new CountingFormatter();
		chatControl.MessageFormatter = formatter;
		formatter.Reset();

		chatControl.AddMessage(new NamedSender("Assistant"), new StringMessageContent("Rendered once"));

		formatter.CallCount.ShouldBe(1);
		formatter.TotalInputLength.ShouldBe("Rendered once".Length);

		formatter.Reset();
		chatControl.MessageFormatter = formatter;
		formatter.CallCount.ShouldBe(0);
	}

	[TestCase(false, false)]
	[TestCase(false, true)]
	[TestCase(true, false)]
	[TestCase(true, true)]
	public void Coalesces_A_Streaming_Burst_Into_One_Render(bool useDevExpress, bool useBackgroundThread)
	{
		var formatter = new CountingFormatter();
		var builder = new NotifyingStringBuilder();
		using var host = new Panel { Size = new Size(500, 500) };
		using var control = CreateMessageControl(useDevExpress, formatter, new ChangingMessageContent(builder));
		HostControl(host, control);
		((IChatMessageControl)control).SetIsReceivingStream(true);
		formatter.Reset();

		void AppendBurst()
		{
			for (var index = 0; index < 100; index++)
				builder.Append("x");
		}

		if (useBackgroundThread)
			Task.Run(AppendBurst).GetAwaiter().GetResult();
		else
			AppendBurst();

		formatter.CallCount.ShouldBe(0);
		Application.DoEvents();

		formatter.CallCount.ShouldBe(1);
		formatter.TotalInputLength.ShouldBe(100);
		control.ToString().ShouldBe(new string('x', 100));
		((IChatMessageControl)control).SetIsReceivingStream(false);
	}

	[TestCase(false)]
	[TestCase(true)]
	public void Flushes_The_Final_Streaming_Render_Synchronously(bool useDevExpress)
	{
		var formatter = new CountingFormatter();
		var builder = new NotifyingStringBuilder();
		using var host = new Panel { Size = new Size(500, 500) };
		using var control = CreateMessageControl(useDevExpress, formatter, new ChangingMessageContent(builder));
		HostControl(host, control);
		((IChatMessageControl)control).SetIsReceivingStream(true);
		formatter.Reset();

		for (var index = 0; index < 100; index++)
			builder.Append("x");

		((IChatMessageControl)control).SetIsReceivingStream(false);

		formatter.CallCount.ShouldBe(1);
		formatter.TotalInputLength.ShouldBe(100);
		control.ToString().ShouldBe(new string('x', 100));
		Application.DoEvents();
		formatter.CallCount.ShouldBe(1);
	}

	[TestCase(false)]
	[TestCase(true)]
	public void Flushes_The_Final_Streaming_Render_From_A_Worker_Before_Returning(bool useDevExpress)
	{
		var formatter = new CountingFormatter();
		var builder = new NotifyingStringBuilder();
		using var host = new Panel { Size = new Size(500, 500) };
		using var control = CreateMessageControl(useDevExpress, formatter, new ChangingMessageContent(builder));
		HostControl(host, control);
		((IChatMessageControl)control).SetIsReceivingStream(true);
		formatter.Reset();

		for (var index = 0; index < 100; index++)
			builder.Append("x");

		var flushTask = Task.Run(() => ((IChatMessageControl)control).SetIsReceivingStream(false));
		var deadline = DateTime.UtcNow.AddSeconds(5);
		while (!flushTask.IsCompleted && DateTime.UtcNow < deadline)
		{
			Application.DoEvents();
			Thread.Sleep(1);
		}

		flushTask.IsCompleted.ShouldBeTrue();
		flushTask.GetAwaiter().GetResult();
		formatter.CallCount.ShouldBe(1);
		formatter.TotalInputLength.ShouldBe(100);
		control.ToString().ShouldBe(new string('x', 100));
	}

	[TestCase(false)]
	[TestCase(true)]
	public void Defers_A_Background_Burst_Until_An_Unhosted_Control_Gets_A_Handle(bool useDevExpress)
	{
		var uiThreadId = Environment.CurrentManagedThreadId;
		var formatter = new CountingFormatter();
		var builder = new NotifyingStringBuilder();
		using var control = CreateMessageControl(useDevExpress, formatter, new ChangingMessageContent(builder));
		((IChatMessageControl)control).SetIsReceivingStream(true);
		formatter.Reset();

		Task.Run(() =>
		{
			for (var index = 0; index < 100; index++)
				builder.Append("x");
			((IChatMessageControl)control).SetIsReceivingStream(false);
		}).GetAwaiter().GetResult();

		formatter.CallCount.ShouldBe(0);
		using var host = new Panel { Size = new Size(500, 500) };
		HostControl(host, control);

		formatter.CallCount.ShouldBe(1);
		formatter.TotalInputLength.ShouldBe(100);
		formatter.LastThreadId.ShouldBe(uiThreadId);
		control.ToString().ShouldBe(new string('x', 100));
	}

	[TestCase(false)]
	[TestCase(true)]
	public void Rethrows_A_Queued_Render_Exception_During_The_Final_Flush(bool useDevExpress)
	{
		var formatter = new ThrowingFormatter();
		var builder = new NotifyingStringBuilder();
		using var host = new Panel { Size = new Size(500, 500) };
		using var control = CreateMessageControl(useDevExpress, formatter, new ChangingMessageContent(builder));
		HostControl(host, control);
		((IChatMessageControl)control).SetIsReceivingStream(true);
		formatter.ThrowOnNonEmpty = true;

		builder.Append("boom");
		Application.DoEvents();

		var exception = Should.Throw<InvalidOperationException>(
			() => ((IChatMessageControl)control).SetIsReceivingStream(false));
		exception.Message.ShouldBe(ThrowingFormatter.ERROR_MESSAGE);
	}

	[Test]
	public void Routes_A_Final_Coalesced_Render_Exception_Through_The_Streaming_Error_Path()
	{
		using var host = new Panel { Size = new Size(500, 500) };
		using var chatControl = new DXChatControl { Size = host.Size };
		host.Controls.Add(chatControl);
		host.CreateControl();
		chatControl.CreateControl();
		Application.DoEvents();

		chatControl.MessageFormatter = new ThrowingFormatter { ThrowOnNonEmpty = true };
		Exception? callbackException = null;
		chatControl.ErrorOccurred += (_, e) => e.Handled = true;

		chatControl.AddStreamingMessage(
			new NamedSender("Assistant"),
			SingleChunkStream(),
			new ImmediateSynchronizationContext(),
			exceptionCallback: ex => callbackException = ex);

		callbackException.ShouldBeOfType<InvalidOperationException>();
		callbackException.Message.ShouldBe(ThrowingFormatter.ERROR_MESSAGE);
	}

	[Test]
	public async Task Waits_For_The_Text_Consumer_To_Finalise_Its_Render()
	{
		using var chatControl = new DelayedTextConsumerChatControl();
		var method = typeof(ChatControl).GetMethod(
			"HandleStreamingResponseAsync",
			BindingFlags.Instance | BindingFlags.NonPublic)!;
		var handlingTask = (Task)method.Invoke(
			chatControl,
			[
				new NamedSender("Assistant"),
				SingleResponseStream(),
				CancellationToken.None
			])!;

		await chatControl.TextConsumed.WaitAsync(TimeSpan.FromSeconds(5));
		handlingTask.IsCompleted.ShouldBeFalse();

		chatControl.ReleaseFinalRender();
		await handlingTask.WaitAsync(TimeSpan.FromSeconds(5));
	}

	[TestCase(false)]
	[TestCase(true)]
	public void Defers_Collapsed_Reasoning_Until_It_Is_Expanded(bool useDevExpress)
	{
		var formatter = new CountingFormatter();
		var reasoningContent = new ReasoningMessageContent("Initial");
		using var host = new Panel { Size = new Size(500, 500) };
		using var control = CreateReasoningControl(useDevExpress, formatter, reasoningContent);
		HostControl(host, control);
		((IChatMessageControl)control).SetIsReceivingStream(true);
		formatter.Reset();

		for (var index = 0; index < 100; index++)
			reasoningContent.AppendText(" x");
		Application.DoEvents();

		formatter.CallCount.ShouldBe(0);
		InvokeToggle(control);

		formatter.CallCount.ShouldBe(1);
		formatter.TotalInputLength.ShouldBe(reasoningContent.ToString().Length);
		control.ToString().ShouldBe(reasoningContent.ToString());
		((IChatMessageControl)control).SetIsReceivingStream(false);
	}

	[Test]
	public void No_Op_StringBuilder_Mutations_Do_Not_Notify()
	{
		var builder = new NotifyingStringBuilder();
		var notificationCount = 0;
		builder.PropertyChanged += (_, _) => notificationCount++;

		builder.Append((string?)null);
		builder.Append(string.Empty);
		builder.Append((object?)null);
		builder.Insert(0, null);
		builder.Insert(0, string.Empty);
		builder.Remove(0, 0);
		builder.Clear();

		notificationCount.ShouldBe(0);

		builder.Append('x');
		notificationCount = 0;
		builder[0] = 'x';
		builder.Insert(0, string.Empty);
		builder.Remove(0, 0);

		notificationCount.ShouldBe(0);
		Should.Throw<ArgumentOutOfRangeException>(() => builder.Insert(2, string.Empty));
		Should.Throw<ArgumentOutOfRangeException>(() => builder.Remove(2, 0));
	}

	private static Control CreateMessageControl(
		bool useDevExpress,
		IMessageFormatter formatter,
		IChatMessageContent content)
	{
		var control = useDevExpress
			? (Control)new DXChatMessageControl { MessageFormatter = formatter }
			: new ChatMessageControl { MessageFormatter = formatter };
		((IChatMessageControl)control).Message = new ChatMessage(new NamedSender("Assistant"), content);
		return control;
	}

	private static Control CreateReasoningControl(
		bool useDevExpress,
		IMessageFormatter formatter,
		ReasoningMessageContent content)
	{
		var assembly = useDevExpress ? typeof(DXChatControl).Assembly : typeof(ChatControl).Assembly;
		var typeName = useDevExpress
			? "TinyChat.DXReasoningMessageControl"
			: "TinyChat.SubControls.ReasoningMessageControl";
		var control = (Control)Activator.CreateInstance(assembly.GetType(typeName)!, nonPublic: true)!;
		((IChatMessageFormattingControl)control).MessageFormatter = formatter;
		((IChatMessageControl)control).Message = new ChatMessage(new NamedSender("Assistant"), content);
		return control;
	}

	private static void HostControl(Control host, Control control)
	{
		control.MinimumSize = new Size(460, 0);
		control.MaximumSize = new Size(460, 0);
		host.Controls.Add(control);
		host.CreateControl();
		control.CreateControl();
		foreach (var descendant in Descendants(control))
			descendant.CreateControl();
		host.PerformLayout();
		control.PerformLayout();
		Application.DoEvents();
	}

	private static void InvokeToggle(Control control)
	{
		control.GetType()
			.GetMethod("Toggle", BindingFlags.Instance | BindingFlags.NonPublic)!
			.Invoke(control, [control, EventArgs.Empty]);
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

	private static async IAsyncEnumerable<string> SingleChunkStream()
	{
		await Task.CompletedTask;
		yield return "boom";
	}

	private static async IAsyncEnumerable<Microsoft.Extensions.AI.ChatResponseUpdate> SingleResponseStream()
	{
		await Task.CompletedTask;
		yield return new Microsoft.Extensions.AI.ChatResponseUpdate
		{
			Role = Microsoft.Extensions.AI.ChatRole.Assistant,
			Contents = [new Microsoft.Extensions.AI.TextContent("done")]
		};
	}

	private sealed class CountingFormatter : IMessageFormatter
	{
		public int CallCount { get; private set; }

		public int LastThreadId { get; private set; }

		public int TotalInputLength { get; private set; }

		public string Format(IChatMessageContent content)
		{
			return Record(content.Content?.ToString() ?? string.Empty);
		}

		public string Format(string content)
		{
			return Record(content ?? string.Empty);
		}

		public void Reset()
		{
			CallCount = 0;
			LastThreadId = 0;
			TotalInputLength = 0;
		}

		private string Record(string text)
		{
			CallCount++;
			LastThreadId = Environment.CurrentManagedThreadId;
			TotalInputLength += text.Length;
			return text;
		}
	}

	private sealed class ThrowingFormatter : IMessageFormatter
	{
		public const string ERROR_MESSAGE = "Rendering failed.";

		public bool ThrowOnNonEmpty { get; set; }

		public string Format(IChatMessageContent content) => Format(content.Content?.ToString() ?? string.Empty);

		public string Format(string content)
		{
			if (ThrowOnNonEmpty && !string.IsNullOrEmpty(content))
				throw new InvalidOperationException(ERROR_MESSAGE);

			return content;
		}
	}

	private sealed class ImmediateSynchronizationContext : SynchronizationContext
	{
		public override void Post(SendOrPostCallback d, object? state)
		{
			d(state);
		}
	}

	private sealed class DelayedTextConsumerChatControl : ChatControl
	{
		private readonly TaskCompletionSource _releaseFinalRender = new(TaskCreationOptions.RunContinuationsAsynchronously);
		private readonly TaskCompletionSource _textConsumed = new(TaskCreationOptions.RunContinuationsAsynchronously);

		public Task TextConsumed => _textConsumed.Task;

		public void ReleaseFinalRender()
		{
			_releaseFinalRender.TrySetResult();
		}

		public override IChatMessageControl AddStreamingMessage(
			ISender sender,
			IAsyncEnumerable<string> stream,
			SynchronizationContext? synchronizationContext = default,
			Action<string>? completionCallback = default,
			Action<Exception>? exceptionCallback = default,
			CancellationToken cancellationToken = default)
		{
			_ = ConsumeAsync();
			return new StubMessageControl();

			async Task ConsumeAsync()
			{
				try
				{
					var chunks = new List<string>();
					await foreach (var chunk in stream.WithCancellation(cancellationToken))
						chunks.Add(chunk);
					_textConsumed.TrySetResult();
					await _releaseFinalRender.Task;
					completionCallback?.Invoke(string.Concat(chunks));
				}
				catch (Exception ex)
				{
					exceptionCallback?.Invoke(ex);
				}
			}
		}
	}

	private sealed class StubMessageControl : IChatMessageControl
	{
		public event EventHandler? BeforeLayoutChange
		{
			add { }
			remove { }
		}

		public event EventHandler? AfterLayoutChange
		{
			add { }
			remove { }
		}

		public event EventHandler? SizeUpdatedWhileStreaming
		{
			add { }
			remove { }
		}

		public IChatMessage? Message { get; set; }

		public void SetIsReceivingStream(bool isReceiving) { }

		public void ShowSenderHeader(bool show) { }
	}
}
