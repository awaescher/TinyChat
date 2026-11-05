using Microsoft.Extensions.AI;

namespace TinyChat;

/// <summary>Contains model input and separate UI-only diagnostics from an attachment conversion.</summary>
public sealed class AttachmentConversionResult(IEnumerable<AIContent> contents, IEnumerable<ChatNotice>? notices = null)
{
	/// <summary>Gets the content sent to the model.</summary>
	public IReadOnlyList<AIContent> Contents { get; } = contents.ToArray();
	/// <summary>Gets diagnostics that must not be sent as conversation messages.</summary>
	public IReadOnlyList<ChatNotice> Notices { get; } = notices?.ToArray() ?? [];
}
