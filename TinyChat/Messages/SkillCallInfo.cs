namespace TinyChat;

/// <summary>
/// Describes which kind of skill access a function call represents.
/// </summary>
public enum SkillCallKind
{
	/// <summary>The instructions of a skill are loaded.</summary>
	Load,

	/// <summary>A resource bundled with a skill is read.</summary>
	ReadResource,

	/// <summary>A script bundled with a skill is executed.</summary>
	RunScript
}

/// <summary>
/// Describes a function call that was recognized as a skill access.
/// </summary>
/// <param name="Kind">The kind of skill access.</param>
/// <param name="SkillName">The name of the accessed skill.</param>
/// <param name="ItemName">The name of the resource or script, if any.</param>
public sealed record SkillCallInfo(SkillCallKind Kind, string SkillName, string? ItemName = null);
