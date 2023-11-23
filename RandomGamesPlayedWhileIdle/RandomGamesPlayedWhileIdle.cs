using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using AngleSharp.Dom;
using ArchiSteamFarm.Core;
using ArchiSteamFarm.Plugins.Interfaces;
using ArchiSteamFarm.Steam;
using ArchiSteamFarm.Steam.Integration;
using ArchiSteamFarm.Web.Responses;
using SteamKit2;

namespace RandomGamesPlayedWhileIdle {
	[Export(typeof(IPlugin))]
	public sealed partial class RandomGamesPlayedWhileIdlePlugin : IBotConnection {
		private const int MaxGamesPlayedConcurrently = 32;

		public string Name => nameof(RandomGamesPlayedWhileIdle);
		public Version Version => typeof(RandomGamesPlayedWhileIdlePlugin).Assembly.GetName().Version!;

		public Task OnLoaded() => Task.CompletedTask;
		public Task OnBotDisconnected(Bot bot, EResult reason) => Task.CompletedTask;

		public async Task OnBotLoggedOn(Bot bot) {
			ArgumentNullException.ThrowIfNull(bot);

			try {
				using HtmlDocumentResponse? response = await bot.ArchiWebHandler
					.UrlGetToHtmlDocumentWithSession(new Uri(ArchiWebHandler.SteamCommunityURL,
						$"profiles/{bot.SteamID}/games")).ConfigureAwait(false);

				if (response?.Content?.SelectSingleNode("""//*[@id="gameslist_config"]""") is Element element) {
					List<uint> list = GamesListRegex()
						.Matches(element.OuterHtml)
						.Select(static x => uint.Parse(x.Groups[1].Value, CultureInfo.InvariantCulture))
						.ToList();

					if (list.Count > 0) {
						bot.BotConfig.GetType().GetProperty("GamesPlayedWhileIdle")?.SetValue(bot.BotConfig,
							list.OrderBy(static _ => Guid.NewGuid())
								.Take(Math.Min(MaxGamesPlayedConcurrently, list.Count)).ToImmutableList());
					}
				}
			} catch (Exception e) {
				ASF.ArchiLogger.LogGenericException(e);
			}
		}

		[GeneratedRegex(@"{&quot;appid&quot;:(\d+),&quot;name&quot;:&quot;")]
		private static partial Regex GamesListRegex();
	}
}
