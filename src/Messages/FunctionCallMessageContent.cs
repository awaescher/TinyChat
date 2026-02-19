using System.ComponentModel;
using System.Text.Json;

namespace TinyChat;

/// <summary>
/// Represents a function call content item within a chat message.
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
	public FunctionCallMessageContent(string callId, string name, IDictionary<string, object?>? arguments)
	{
		CallId = callId;
		Name = name;
		Arguments = arguments is not null
			? new System.Collections.ObjectModel.ReadOnlyDictionary<string, object?>(new Dictionary<string, object?>(arguments))
			: null;
	}

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

	/// <inheritdoc />
	public object? Content => this;

	/// <inheritdoc />
	public override string ToString()
	{
		var args = Arguments is not null
			? JsonSerializer.Serialize(Arguments)
			: string.Empty;
		return $"[Calling: {Name}({args})]";
	}
}
