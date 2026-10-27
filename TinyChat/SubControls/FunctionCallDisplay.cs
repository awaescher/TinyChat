namespace TinyChat;

/// <summary>
/// Provides the language-neutral glyphs and titles used to display function and skill calls.
/// </summary>
internal static class FunctionCallDisplay
{
	internal const string APPROVE_GLYPH = "\u2714";
	internal const string REJECT_GLYPH = "\u2716";

	internal static string GetIcon(FunctionCallMessageContent content) => content.Skill is null ? ChatIcons.Tool : ChatIcons.Skill;

	internal static string GetTitle(FunctionCallMessageContent content)
	{
		if (content.Skill is not { } skill)
			return content.Name;

		return skill.ItemName is null
			? skill.SkillName
			: $"{skill.SkillName} \u203a {skill.ItemName}";
	}

	internal static string GetStatus(FunctionCallMessageContent content)
	{
		if (content.ApprovalState == ToolApprovalState.Pending)
			return "?";

		if (content.ApprovalState == ToolApprovalState.Rejected)
			return REJECT_GLYPH;

		if (content.IsFunctionExecuting)
			return "...";

		return content.HasError ? "\u26a0" : APPROVE_GLYPH;
	}

	internal static string GetHeader(FunctionCallMessageContent content) => $"{GetTitle(content)} {GetStatus(content)}";
}
