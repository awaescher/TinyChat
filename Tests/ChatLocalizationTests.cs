using System.Globalization;
using System.Resources;
using System.Text.RegularExpressions;
using Shouldly;
using TinyChat;

namespace Tests;

public partial class ChatLocalizationTests
{
	protected static readonly string[] Cultures = ["de", "es", "fr", "it", "pt", "nl", "pl", "ru", "ja", "zh-Hans", "ko"];
	private static readonly ResourceManager _resources = new("TinyChat.Localization.ChatStrings", typeof(ChatLocalization).Assembly);

	[GeneratedRegex(@"\{\d+(:[^}]*)?\}")]
	private static partial Regex PlaceholderRegex();

	public class Resources : ChatLocalizationTests
	{
		[TestCaseSource(nameof(Cultures))]
		public void Translate_Every_Text_With_The_Same_Placeholders(string cultureName)
		{
			var culture = CultureInfo.GetCultureInfo(cultureName);
			var neutral = _resources.GetResourceSet(CultureInfo.InvariantCulture, createIfNotExists: true, tryParents: false).ShouldNotBeNull();
			var localized = _resources.GetResourceSet(culture, createIfNotExists: true, tryParents: false).ShouldNotBeNull();

			foreach (System.Collections.DictionaryEntry entry in neutral)
			{
				var key = (string)entry.Key;
				var text = localized.GetString(key);
				text.ShouldNotBeNullOrWhiteSpace($"{cultureName} misses {key}");
				PlaceholderRegex().Matches(text).Select(match => match.Value).Order()
					.ShouldBe(PlaceholderRegex().Matches((string)entry.Value!).Select(match => match.Value).Order(), $"{cultureName}: {key}");
			}
		}

		[Test]
		public void Uses_The_Localized_Text_For_The_UI_Culture()
		{
			ChatLocalization.GetString("ShowFullText", CultureInfo.GetCultureInfo("fr-FR")).ShouldBe("Afficher le texte complet");
			ChatLocalization.GetString("ShowFullText", CultureInfo.GetCultureInfo("zh-CN")).ShouldBe("显示全文");
			ChatLocalization.GetString("ShowFullText", CultureInfo.GetCultureInfo("pt-BR")).ShouldBe("Mostrar texto completo");
		}
	}
}
