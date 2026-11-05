using Microsoft.Extensions.AI;
using TinyChat.Messages;

namespace TinyChat;

public partial class ChatControl
{
	/// <summary>Replaces the displayed conversation with structured chat history, including images and tool results.</summary>
	public void LoadChatHistory(IEnumerable<Microsoft.Extensions.AI.ChatMessage> messages)
	{
		ArgumentNullException.ThrowIfNull(messages);
		var history = new List<IChatMessage>();
		var calls = new Dictionary<string, FunctionCallMessageContent>();
		foreach (var message in messages)
		{
			ISender sender = message.Role == ChatRole.User ? Sender : message.Role == ChatRole.System ? new NamedSender("System") : _assistantSender;
			var ordinary = new List<AIContent>();
			void FlushOrdinary()
			{
				if (ordinary.Count == 0)
					return;
				history.Add(CreateChatMessage(sender, new AIMessageContent(ordinary)));
				ordinary.Clear();
			}
			foreach (var item in message.Contents)
			{
				if (item is FunctionCallContent call)
				{
					FlushOrdinary();
					var content = CreateFunctionCallMessageContent(call);
					calls[call.CallId] = content;
					history.Add(CreateChatMessage(_assistantSender, content));
				}
				else if (item is FunctionResultContent result)
				{
					FlushOrdinary();
					if (!calls.TryGetValue(result.CallId, out var content))
					{
						content = new FunctionCallMessageContent(result.CallId, ChatLocalization.GetString("ToolResult"), null);
						calls[result.CallId] = content;
						history.Add(CreateChatMessage(_assistantSender, content));
					}
					content.SetResult(result.Result, IsErrorResult(content, result));
				}
				else if (item is TextReasoningContent reasoning)
				{
					FlushOrdinary();
					history.Add(CreateChatMessage(sender, new ReasoningMessageContent(reasoning.Text ?? string.Empty)));
				}
				else
					ordinary.Add(item);
			}
			FlushOrdinary();
		}
		Messages = history;
	}
}
