using System.Runtime.CompilerServices;
using System.Windows.Forms;
using Microsoft.Extensions.AI;
using Shouldly;
using TinyChat;
using TinyChat.Messages;

namespace Tests;

[Apartment(ApartmentState.STA)]
public class ErrorContentTests
{
	public class ChatClientResponses : ErrorContentTests
	{
		[TestCase(false)]
		[TestCase(true)]
		public async Task Displays_Error_Content_And_Raises_The_Error_Event(bool useStreaming)
		{
			using var client = new ErrorChatClient();
			using var control = new ChatControl
			{
				UseStreaming = useStreaming,
				ServiceProvider = new SingleServiceProvider(client)
			};
			Exception? reportedError = null;
			control.ErrorOccurred += (_, args) => reportedError = args.Exception;

			control.SendMessage("Hello");
			await control.ChatClientSendCompletion.WaitAsync(TimeSpan.FromSeconds(3));

			reportedError.ShouldNotBeNull().Message.ShouldBe("Server unavailable");
			control.Messages.Count().ShouldBe(2);
			control.Messages.Last().Sender.Name.ShouldBeEmpty();
			control.Messages.Last().Content.ShouldBeOfType<ChatNoticeMessageContent>().Notice.Code.ShouldBe(ChatNoticeCode.GeneralError);
		}

		[TestCase(false)]
		[TestCase(true)]
		public async Task Suppresses_The_Default_Message_When_The_Error_Is_Handled(bool useStreaming)
		{
			using var client = new ErrorChatClient();
			using var control = new ChatControl
			{
				UseStreaming = useStreaming,
				ServiceProvider = new SingleServiceProvider(client)
			};
			control.ErrorOccurred += (_, args) => args.Handled = true;

			control.SendMessage("Hello");
			await control.ChatClientSendCompletion.WaitAsync(TimeSpan.FromSeconds(3));

			control.Messages.Count().ShouldBe(1);
		}

		[TestCase(false)]
		[TestCase(true)]
		public async Task Continues_After_Nonfatal_Error_Content(bool useStreaming)
		{
			using var client = new ErrorChatClient(includeReasoning: true);
			using var control = new ChatControl
			{
				UseStreaming = useStreaming,
				IncludeReasoning = true,
				ServiceProvider = new SingleServiceProvider(client)
			};

			control.SendMessage("Hello");
			await control.ChatClientSendCompletion.WaitAsync(TimeSpan.FromSeconds(3));

			control.Messages.Count().ShouldBe(4);
			control.Messages.Select(message => message.Content).OfType<ReasoningMessageContent>()
				.ShouldAllBe(content => !content.IsThinking);
			control.Messages.Last().Content.Content.ShouldBe("Further reasoning");
		}
	}

	private sealed class SingleServiceProvider(IChatClient client) : IServiceProvider
	{
		public object? GetService(Type serviceType)
		{
			return serviceType == typeof(IChatClient) ? client : null;
		}
	}

	private sealed class ErrorChatClient(bool includeReasoning = false) : IChatClient
	{
		public void Dispose() { }

		public object? GetService(Type serviceType, object? serviceKey = null)
		{
			return serviceType == typeof(IChatClient) ? this : null;
		}

		public Task<ChatResponse> GetResponseAsync(IEnumerable<Microsoft.Extensions.AI.ChatMessage> messages, ChatOptions? options = null, CancellationToken cancellationToken = default)
		{
			return Task.FromResult(new ChatResponse(new Microsoft.Extensions.AI.ChatMessage(ChatRole.Assistant, GetContents())));
		}

		public async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(IEnumerable<Microsoft.Extensions.AI.ChatMessage> messages, ChatOptions? options = null, [EnumeratorCancellation] CancellationToken cancellationToken = default)
		{
			await Task.CompletedTask;
			foreach (var content in GetContents())
			{
				cancellationToken.ThrowIfCancellationRequested();
				yield return new ChatResponseUpdate(ChatRole.Assistant, [content]);
			}
		}

		private List<AIContent> GetContents()
		{
			return includeReasoning
				? [new TextReasoningContent("Initial reasoning"), new ErrorContent("Server unavailable"), new TextReasoningContent("Further reasoning")]
				: [new ErrorContent("Server unavailable")];
		}
	}
}
