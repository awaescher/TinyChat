using System.Globalization;
using System.Resources;

namespace TinyChat;

/// <summary>Provides the default localized UI strings. Hosts can override notice wording per control.</summary>
public static class ChatLocalization
{
	private static readonly ResourceManager _resources = new("TinyChat.Localization.ChatStrings", typeof(ChatLocalization).Assembly);

	/// <summary>Gets a resource for the current UI culture, with the neutral resource as fallback.</summary>
	public static string GetString(string key, CultureInfo? culture = null) => _resources.GetString(key, culture ?? CultureInfo.CurrentUICulture) ?? key;

	/// <summary>Formats a diagnostic using localized resources and structured arguments.</summary>
	public static string Format(ChatNotice notice) => string.Format(CultureInfo.CurrentCulture, GetString(notice.Code.ToString()), notice.FileName ?? string.Empty, notice.Limit ?? 0);
}
