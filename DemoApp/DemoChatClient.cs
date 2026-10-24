using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.AI;

namespace DemoApp;

/// <summary>
/// A fake <see cref="IChatClient"/> that simulates an AI assistant response with interleaved
/// reasoning, tool calls, and text — without requiring a real LLM backend.
/// <para>
/// The response sequence demonstrates the full interleaving pattern:
/// Reasoning → Text → Tool Call → Text → Reasoning → Tool Call → Final Text.
/// </para>
/// </summary>
internal sealed class DemoChatClient : IChatClient
{
	public void Dispose() { }

	public Task<ChatResponse> GetResponseAsync(
		IEnumerable<ChatMessage> messages,
		ChatOptions? options = null,
		CancellationToken cancellationToken = default)
	{
		return Task.FromResult(new ChatResponse(new ChatMessage(ChatRole.Assistant, "Please enable streaming for the full demo experience.")));
	}

	public async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
		IEnumerable<ChatMessage> messages,
		ChatOptions? options = null,
		[EnumeratorCancellation] CancellationToken cancellationToken = default)
	{
		var userQuestion = messages.LastOrDefault(m => m.Role == ChatRole.User)?.Text ?? "your question";
		var shortTopic = userQuestion.Length > 80 ? userQuestion[..80] + "…" : userQuestion;

		// ── Phase 1: Reasoning ────────────────────────────────────────────
		foreach (var chunk in ChunkText(
			$"The user is asking: \"{shortTopic}\". " +
			"Let me search my knowledge base for accurate and up-to-date information on this topic."))
		{
			yield return CreateReasoningUpdate(chunk);
			await Task.Delay(25, cancellationToken);
		}

		await Task.Delay(400, cancellationToken);

		// ── Phase 2: Initial text ─────────────────────────────────────────
		foreach (var chunk in ChunkText("Great question! Let me look into that for you.\n\n"))
		{
			yield return CreateTextUpdate(chunk);
			await Task.Delay(50, cancellationToken);
		}

		// ── Phase 3: Tool call — search ───────────────────────────────────
		yield return CreateFunctionCallUpdate(
			"call_search_1",
			"search_knowledge_base",
			new Dictionary<string, object?> { ["query"] = shortTopic });

		await Task.Delay(1500, cancellationToken);

		yield return CreateFunctionResultUpdate(
			"call_search_1",
			$"Found 3 relevant articles about \"{shortTopic}\". Top result: comprehensive overview with verified sources.");

		await Task.Delay(300, cancellationToken);

		// ── Phase 4: Continuation text ────────────────────────────────────
		foreach (var chunk in ChunkText("I found some relevant information. "))
		{
			yield return CreateTextUpdate(chunk);
			await Task.Delay(50, cancellationToken);
		}

		// ── Phase 5: Second reasoning ─────────────────────────────────────
		foreach (var chunk in ChunkText(
			"The search results look promising. " +
			"I should verify the key claims before presenting them to ensure accuracy."))
		{
			yield return CreateReasoningUpdate(chunk);
			await Task.Delay(25, cancellationToken);
		}

		await Task.Delay(300, cancellationToken);

		// ── Phase 6: Tool call — verify ───────────────────────────────────
		yield return CreateFunctionCallUpdate(
			"call_verify_1",
			"verify_facts",
			new Dictionary<string, object?>
			{
				["claim"] = $"Key facts about: {shortTopic}",
				["sources"] = "academic papers"
			});

		await Task.Delay(1200, cancellationToken);

		yield return CreateFunctionResultUpdate(
			"call_verify_1",
			"All key claims verified against peer-reviewed sources. Confidence: high.");

		await Task.Delay(200, cancellationToken);

		// ── Phase 7: Final detailed text ──────────────────────────────────
		var finalResponse =
			$"Here's what I found:\n\n" +
			$"Based on my research, **{shortTopic}** is a fascinating topic. " +
			$"The knowledge base contains several well-sourced articles that confirm the key details. " +
			$"The facts have been verified against peer-reviewed sources with high confidence.\n\n" +
			$"Would you like me to dive deeper into any specific aspect? \U0001F50D";

		foreach (var chunk in ChunkText(finalResponse))
		{
			yield return CreateTextUpdate(chunk);
			await Task.Delay(40, cancellationToken);
		}
	}

	public object? GetService(Type serviceType, object? serviceKey = null)
		=> serviceType == typeof(IChatClient) ? this : null;

	private static IEnumerable<string> ChunkText(string text, int chunkSize = 4)
	{
		return DemoTextChunker.Chunk(text, chunkSize);
	}

	private static ChatResponseUpdate CreateTextUpdate(string text)
	{
		var update = new ChatResponseUpdate { Role = ChatRole.Assistant };
		update.Contents.Add(new TextContent(text));
		return update;
	}

	private static ChatResponseUpdate CreateReasoningUpdate(string text)
	{
		var update = new ChatResponseUpdate { Role = ChatRole.Assistant };
		update.Contents.Add(new TextReasoningContent(text));
		return update;
	}

	private static ChatResponseUpdate CreateFunctionCallUpdate(string callId, string name, Dictionary<string, object?> args)
	{
		var update = new ChatResponseUpdate { Role = ChatRole.Assistant };
		update.Contents.Add(new FunctionCallContent(callId, name, args));
		return update;
	}

	private static ChatResponseUpdate CreateFunctionResultUpdate(string callId, string result)
	{
		var update = new ChatResponseUpdate { Role = ChatRole.Assistant };
		update.Contents.Add(new FunctionResultContent(callId, result));
		return update;
	}
}
