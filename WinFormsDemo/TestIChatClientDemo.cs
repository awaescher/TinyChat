using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using System.Runtime.CompilerServices;
using AIChatMessage = Microsoft.Extensions.AI.ChatMessage;

namespace WinFormsDemo;

/// <summary>
/// A simple test to demonstrate IChatClient integration
/// </summary>
public static class TestIChatClientDemo
{
	/// <summary>
	/// Creates a service provider with a mock IChatClient
	/// </summary>
	public static IServiceProvider CreateServiceProviderWithMockChatClient()
	{
		var services = new ServiceCollection();

		// Register a mock IChatClient
		services.AddSingleton<IChatClient>(new MockChatClient());

		// Register a keyed mock IChatClient
		services.AddKeyedSingleton<IChatClient>("premium", new MockChatClient(isPremium: true));

		return services.BuildServiceProvider();
	}

	/// <summary>
	/// A mock implementation of IChatClient for testing
	/// </summary>
	private class MockChatClient : IChatClient
	{
		private readonly bool _isPremium;

		public MockChatClient(bool isPremium = false)
		{
			_isPremium = isPremium;
		}

		public async Task<ChatResponse> GetResponseAsync(IEnumerable<AIChatMessage> messages, ChatOptions? options = null, CancellationToken cancellationToken = default)
		{
			// Simulate a delay
			await Task.Delay(500, cancellationToken);

			var lastUserMessage = messages.LastOrDefault(m => m.Role == ChatRole.User)?.Text ?? "nothing";
			var responseText = _isPremium
				? $"[PREMIUM] Thank you for your message: '{lastUserMessage}'. This is a premium response!"
				: $"I received your message: '{lastUserMessage}'. This is a standard response.";

			return new ChatResponse(new AIChatMessage(ChatRole.Assistant, responseText));
		}

		public async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
			IEnumerable<AIChatMessage> messages,
			ChatOptions? options = null,
			[EnumeratorCancellation] CancellationToken cancellationToken = default)
		{
			var lastUserMessage = messages.LastOrDefault(m => m.Role == ChatRole.User)?.Text ?? "nothing";
			var responseText = _isPremium
				? $"[PREMIUM STREAMING] You said: '{lastUserMessage}'. Here's a streaming premium response with more details!"
				: $"[STREAMING] You said: '{lastUserMessage}'. Here's a streaming response!";

			// Simulate streaming by yielding chunks
			for (int i = 0; i < responseText.Length; i += 3)
			{
				if (cancellationToken.IsCancellationRequested)
					yield break;

				var chunk = responseText.Substring(i, Math.Min(3, responseText.Length - i));
				yield return new ChatResponseUpdate(ChatRole.Assistant, chunk);

				await Task.Delay(50, cancellationToken);
			}
		}

		public object? GetService(Type serviceType, object? serviceKey = null) => null;

		public void Dispose()
		{
			// Nothing to dispose
		}
	}
}
