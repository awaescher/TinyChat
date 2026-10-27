using System.Text.Json;

namespace TinyChat;

/// <summary>
/// Recognizes the skill tools of the Agent Skills specification (<c>load_skill</c>, <c>read_skill_resource</c>
/// and <c>run_skill_script</c>) by their name and their required arguments.
/// </summary>
public class DefaultSkillCallRecognizer : ISkillCallRecognizer
{
	/// <summary>The default name of the tool that loads a skill.</summary>
	public const string LOAD_SKILL_TOOL_NAME = "load_skill";

	/// <summary>The default name of the tool that reads a skill resource.</summary>
	public const string READ_SKILL_RESOURCE_TOOL_NAME = "read_skill_resource";

	/// <summary>The default name of the tool that runs a skill script.</summary>
	public const string RUN_SKILL_SCRIPT_TOOL_NAME = "run_skill_script";

	/// <summary>Gets or sets the name of the tool that loads a skill.</summary>
	public string LoadSkillToolName { get; set; } = LOAD_SKILL_TOOL_NAME;

	/// <summary>Gets or sets the name of the tool that reads a skill resource.</summary>
	public string ReadSkillResourceToolName { get; set; } = READ_SKILL_RESOURCE_TOOL_NAME;

	/// <summary>Gets or sets the name of the tool that runs a skill script.</summary>
	public string RunSkillScriptToolName { get; set; } = RUN_SKILL_SCRIPT_TOOL_NAME;

	/// <summary>
	/// Gets or sets the names of the skills known to the application.
	/// When set, only calls for these skills are recognized; when <see langword="null"/>, any skill name is accepted.
	/// </summary>
	public ISet<string>? KnownSkillNames { get; set; }

	/// <inheritdoc />
	public virtual SkillCallInfo? Recognize(string functionName, IReadOnlyDictionary<string, object?>? arguments)
	{
		if (string.Equals(functionName, LoadSkillToolName, StringComparison.Ordinal))
			return Create(SkillCallKind.Load, arguments, itemArgumentName: null);

		if (string.Equals(functionName, ReadSkillResourceToolName, StringComparison.Ordinal))
			return Create(SkillCallKind.ReadResource, arguments, "resourceName");

		if (string.Equals(functionName, RunSkillScriptToolName, StringComparison.Ordinal))
			return Create(SkillCallKind.RunScript, arguments, "scriptName");

		return null;
	}

	private SkillCallInfo? Create(SkillCallKind kind, IReadOnlyDictionary<string, object?>? arguments, string? itemArgumentName)
	{
		var skillName = GetString(arguments, "skillName");
		if (string.IsNullOrWhiteSpace(skillName))
			return null;

		if (KnownSkillNames is not null && !KnownSkillNames.Contains(skillName))
			return null;

		if (itemArgumentName is null)
			return new SkillCallInfo(kind, skillName);

		var itemName = GetString(arguments, itemArgumentName);
		return string.IsNullOrWhiteSpace(itemName)
			? null
			: new SkillCallInfo(kind, skillName, itemName);
	}

	private static string? GetString(IReadOnlyDictionary<string, object?>? arguments, string name)
	{
		if (arguments is null || !arguments.TryGetValue(name, out var value))
			return null;

		return value is JsonElement { ValueKind: JsonValueKind.String } element
			? element.GetString()
			: value?.ToString();
	}
}
