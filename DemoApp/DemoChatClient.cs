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
/// Skill load → Skill resource → Reasoning → Text → Tool Call → Text → Reasoning → Tool Call → Final Text
/// → Skill script waiting for approval → (after the decision) script result → Text.
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
		// ── Follow-up after the user decided on the script approval ───────
		var approvalResponse = messages.LastOrDefault()?.Contents.OfType<ToolApprovalResponseContent>().FirstOrDefault();
		if (approvalResponse is not null)
		{
			await foreach (var update in ContinueAfterApprovalAsync(approvalResponse, cancellationToken))
				yield return update;
			yield break;
		}

		var userQuestion = messages.LastOrDefault(m => m.Role == ChatRole.User)?.Text ?? "your question";
		var shortTopic = userQuestion.Length > 80 ? userQuestion[..80] + "…" : userQuestion;

		// ── Shortcut: export requests go straight to the approval ─────────
		if (userQuestion.Contains("export", StringComparison.OrdinalIgnoreCase))
		{
			await Task.Delay(400, cancellationToken);
			yield return CreateUpdate(new ToolApprovalRequestContent("approval_export_1", CreateExportScriptCall("Transformer architecture")));
			yield break;
		}

		// ── Phase 0: Skills — load instructions and read a resource ──────
		yield return CreateFunctionCallUpdate(
			"call_skill_load",
			"load_skill",
			new Dictionary<string, object?> { ["skillName"] = "research-assistant" });

		await Task.Delay(600, cancellationToken);

		yield return CreateFunctionResultUpdate(
			"call_skill_load",
			"# Research assistant\n1. Read the `references/sources.md` resource.\n2. Search the knowledge base.\n3. Run the `export-summary` script to save the result.");

		await Task.Delay(300, cancellationToken);

		yield return CreateFunctionCallUpdate(
			"call_skill_resource",
			"read_skill_resource",
			new Dictionary<string, object?>
			{
				["skillName"] = "research-assistant",
				["resourceName"] = "references/sources.md"
			});

		await Task.Delay(600, cancellationToken);

		yield return CreateFunctionResultUpdate(
			"call_skill_resource",
			"Trusted sources: peer-reviewed journals, official documentation, encyclopedias.");

		await Task.Delay(300, cancellationToken);

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

		await Task.Delay(300, cancellationToken);

		// ── Phase 8: Skill script that requires the user's approval ──────
		// The turn ends here; ChatControl shows the approve/reject buttons and
		// sends the decision back in a follow-up request.
		yield return CreateUpdate(new ToolApprovalRequestContent(
			"approval_export_1",
			CreateExportScriptCall(shortTopic)));
	}

	private static async IAsyncEnumerable<ChatResponseUpdate> ContinueAfterApprovalAsync(
		ToolApprovalResponseContent approvalResponse,
		[EnumeratorCancellation] CancellationToken cancellationToken)
	{
		if (approvalResponse.ToolCall is not FunctionCallContent scriptCall)
			yield break;

		string text;
		if (approvalResponse.Approved)
		{
			yield return CreateUpdate(new FunctionCallContent(scriptCall.CallId, scriptCall.Name, scriptCall.Arguments));
			await Task.Delay(1200, cancellationToken);
			yield return CreateFunctionResultUpdate(scriptCall.CallId, "Summary exported to research-summary.md");
			text = "Done! I exported the summary to research-summary.md. \u2705";
		}
		else
		{
			yield return CreateFunctionResultUpdate(scriptCall.CallId, "Tool call invocation was rejected by the approver.");
			text = "Okay, I did not export the summary.";
		}

		await Task.Delay(300, cancellationToken);

		foreach (var chunk in ChunkText(text))
		{
			yield return CreateTextUpdate(chunk);
			await Task.Delay(40, cancellationToken);
		}
	}

	private static FunctionCallContent CreateExportScriptCall(string topic) => new(
		"call_skill_script",
		"run_skill_script",
		new Dictionary<string, object?>
		{
			["skillName"] = "research-assistant",
			["scriptName"] = "export-summary",
			["arguments"] = $"{{ \"topic\": \"{topic}\" }}"
		});

	private static ChatResponseUpdate CreateUpdate(AIContent content)
	{
		var update = new ChatResponseUpdate { Role = ChatRole.Assistant };
		update.Contents.Add(content);
		return update;
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
