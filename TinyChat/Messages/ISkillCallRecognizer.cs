namespace TinyChat;

/// <summary>
/// Decides whether a function call represents a skill access.
/// </summary>
public interface ISkillCallRecognizer
{
	/// <summary>
	/// Inspects a function call and returns the skill access it represents.
	/// </summary>
	/// <param name="functionName">The name of the called function.</param>
	/// <param name="arguments">The arguments passed to the function.</param>
	/// <returns>The recognized skill access, or <see langword="null"/> for a regular function call.</returns>
	SkillCallInfo? Recognize(string functionName, IReadOnlyDictionary<string, object?>? arguments);
}
