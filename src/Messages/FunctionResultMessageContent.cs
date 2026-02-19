using System.ComponentModel;

namespace TinyChat;

/// <summary>
/// Represents the result of a function call within a chat message.
/// </summary>
public class FunctionResultMessageContent : IChatMessageContent
{
	/// <summary>
	/// Occurs when the value of the message content changes.
	/// </summary>
	public event PropertyChangedEventHandler? PropertyChanged;

	/// <summary>
	/// Initializes a new instance of the <see cref="FunctionResultMessageContent"/> class.
	/// </summary>
	/// <param name="callId">The identifier of the function call this result corresponds to.</param>
	/// <param name="result">The result returned by the function.</param>
	public FunctionResultMessageContent(string callId, object? result)
	{
		CallId = callId;
		Result = result;
	}

	/// <summary>
	/// Gets the identifier of the function call this result corresponds to.
	/// </summary>
	public string CallId { get; }

	/// <summary>
	/// Gets the result returned by the function.
	/// </summary>
	public object? Result { get; }

	/// <inheritdoc />
	public object? Content => this;

	/// <inheritdoc />
	public override string ToString() => $"[Result: {Result}]";
}
