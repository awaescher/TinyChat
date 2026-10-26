using System.ComponentModel;

namespace TinyChat;

/// <summary>
/// Represents a function call content item within a chat message, optionally including the function result.
/// </summary>
public class FunctionCallMessageContent : IChatMessageContent
{
	/// <summary>
	/// Occurs when the value of the message content changes.
	/// </summary>
	public event PropertyChangedEventHandler? PropertyChanged;

	/// <summary>
	/// Initializes a new instance of the <see cref="FunctionCallMessageContent"/> class.
	/// </summary>
	/// <param name="callId">The identifier of the function call.</param>
	/// <param name="name">The name of the function being called.</param>
	/// <param name="arguments">The arguments passed to the function.</param>
	/// <param name="result">The optional result returned by the function.</param>
	public FunctionCallMessageContent(string callId, string name, IDictionary<string, object?>? arguments, object? result = null)
	{
		CallId = callId;
		Name = name;
		Arguments = arguments is not null
			? new System.Collections.ObjectModel.ReadOnlyDictionary<string, object?>(new Dictionary<string, object?>(arguments))
			: null;
		Result = result;
	}

	/// <summary>
	/// Gets or sets wheather the function is currently beeing executed.
	/// </summary>
	public bool IsFunctionExecuting { get; set; }

	/// <summary>
	/// Gets the identifier of the function call.
	/// </summary>
	public string CallId { get; }

	/// <summary>
	/// Gets the name of the function being called.
	/// </summary>
	public string Name { get; }

	/// <summary>
	/// Gets the arguments passed to the function.
	/// </summary>
	public IReadOnlyDictionary<string, object?>? Arguments { get; }

	/// <summary>
	/// Gets the result returned by the function, or <see langword="null"/> if no result is available yet.
	/// </summary>
	public object? Result { get; private set; }

	/// <summary>
	/// Gets whether the function reported an error as its result.
	/// </summary>
	public bool HasError { get; private set; }

	/// <summary>
	/// Gets or sets the skill access this function call represents, or <see langword="null"/> for a regular function call.
	/// </summary>
	public SkillCallInfo? Skill { get; set; }

	/// <summary>
	/// Gets the identifier of the approval request, or <see langword="null"/> if the call does not require approval.
	/// </summary>
	public string? ApprovalRequestId { get; private set; }

	/// <summary>
	/// Gets the approval state of the function call.
	/// </summary>
	public ToolApprovalState ApprovalState { get; private set; }

	/// <inheritdoc />
	public object? Content => this;

	/// <inheritdoc />
	public override string ToString()
	{
		var args = FormatArgs();
		return Result is not null
			? $"{{Tool: {Name}({args}) = {Result}}}"
			: $"[Calling: {Name}({args})]";
	}

	private string FormatArgs()
	{
		if (Arguments is null || Arguments.Count == 0)
			return string.Empty;
		return string.Join(", ", Arguments.Select(kv => $"{kv.Key}: {kv.Value}"));
	}

	/// <summary>
	/// Sets the function result and updates the <see cref="IsFunctionExecuting"/>.
	/// </summary>
	/// <param name="result">The result of the function</param>
	/// <param name="hasError">Whether the result describes an error.</param>
	public void SetResult(object? result, bool hasError = false)
	{
		Result = result;
		HasError = hasError;
		IsFunctionExecuting = false;

		PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Result)));
		PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsFunctionExecuting)));
	}

	/// <summary>
	/// Marks the function call as waiting for the user's approval.
	/// </summary>
	/// <param name="requestId">The identifier of the approval request.</param>
	public void RequireApproval(string requestId)
	{
		ArgumentException.ThrowIfNullOrEmpty(requestId);

		ApprovalRequestId = requestId;
		IsFunctionExecuting = false;
		SetApprovalState(ToolApprovalState.Pending);
	}

	/// <summary>
	/// Approves a pending function call so that it can be executed.
	/// </summary>
	public void Approve()
	{
		if (ApprovalState != ToolApprovalState.Pending)
			return;

		IsFunctionExecuting = true;
		SetApprovalState(ToolApprovalState.Approved);
	}

	/// <summary>
	/// Rejects a pending function call so that it will not be executed.
	/// </summary>
	public void Reject()
	{
		if (ApprovalState != ToolApprovalState.Pending)
			return;

		SetApprovalState(ToolApprovalState.Rejected);
	}

	private void SetApprovalState(ToolApprovalState state)
	{
		ApprovalState = state;
		PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ApprovalState)));
	}
}

/// <summary>
/// Describes the approval state of a function call.
/// </summary>
public enum ToolApprovalState
{
	/// <summary>The function call does not require approval.</summary>
	None,

	/// <summary>The function call waits for the user's decision.</summary>
	Pending,

	/// <summary>The user approved the function call.</summary>
	Approved,

	/// <summary>The user rejected the function call.</summary>
	Rejected
}
