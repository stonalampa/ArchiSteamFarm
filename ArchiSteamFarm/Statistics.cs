﻿/*
    _                _      _  ____   _                           _____
   / \    _ __  ___ | |__  (_)/ ___| | |_  ___   __ _  _ __ ___  |  ___|__ _  _ __  _ __ ___
  / _ \  | '__|/ __|| '_ \ | |\___ \ | __|/ _ \ / _` || '_ ` _ \ | |_  / _` || '__|| '_ ` _ \
 / ___ \ | |  | (__ | | | || | ___) || |_|  __/| (_| || | | | | ||  _|| (_| || |   | | | | | |
/_/   \_\|_|   \___||_| |_||_||____/  \__|\___| \__,_||_| |_| |_||_|   \__,_||_|   |_| |_| |_|

 Copyright 2015-2016 Łukasz "JustArchi" Domeradzki
 Contact: JustArchi@JustArchi.net

 Licensed under the Apache License, Version 2.0 (the "License");
 you may not use this file except in compliance with the License.
 You may obtain a copy of the License at

 http://www.apache.org/licenses/LICENSE-2.0
					
 Unless required by applicable law or agreed to in writing, software
 distributed under the License is distributed on an "AS IS" BASIS,
 WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 See the License for the specific language governing permissions and
 limitations under the License.

*/

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SteamKit2;

namespace ArchiSteamFarm {
	internal sealed class Statistics {
		private const byte MinHeartBeatTTL = 5; // Minimum amount of minutes we must wait before sending next HeartBeat

		private readonly Bot Bot;

		private DateTime LastHeartBeat = DateTime.MinValue;

		internal Statistics(Bot bot) {
			if (bot == null) {
				throw new ArgumentNullException(nameof(bot));
			}

			Bot = bot;
		}

		internal async Task OnHeartBeat() {
			if (DateTime.Now < LastHeartBeat.AddMinutes(MinHeartBeatTTL)) {
				return;
			}

			const string request = SharedInfo.StatisticsServer + "/api/HeartBeat";
			Dictionary<string, string> data = new Dictionary<string, string>(1) {
				{ "SteamID", Bot.SteamID.ToString() }
			};

			// We don't need retry logic here
			if (await Program.WebBrowser.UrlPost(request, data).ConfigureAwait(false)) {
				LastHeartBeat = DateTime.Now;
			}
		}

		internal async Task OnLoggedOn() {
			await Bot.ArchiWebHandler.JoinGroup(SharedInfo.ASFGroupSteamID).ConfigureAwait(false);

			const string request = SharedInfo.StatisticsServer + "/api/LoggedOn";
			Dictionary<string, string> data = new Dictionary<string, string>(4) {
				{ "SteamID", Bot.SteamID.ToString() },
				{ "HasMobileAuthenticator", Bot.HasMobileAuthenticator ? "1" : "0" },
				{ "SteamTradeMatcher", Bot.BotConfig.TradingPreferences.HasFlag(BotConfig.ETradingPreferences.SteamTradeMatcher) ? "1" : "0" },
				{ "MatchEverything", Bot.BotConfig.TradingPreferences.HasFlag(BotConfig.ETradingPreferences.MatchEverything) ? "1" : "0" }
			};

			// We don't need retry logic here
			await Program.WebBrowser.UrlPost(request, data).ConfigureAwait(false);
		}

		internal async Task OnPersonaState(SteamFriends.PersonaStateCallback callback) {
			if (callback == null) {
				ASF.ArchiLogger.LogNullError(nameof(callback));
				return;
			}

			string avatarHash = BitConverter.ToString(callback.AvatarHash).Replace("-", "").ToLowerInvariant();

			const string request = SharedInfo.StatisticsServer + "/api/PersonaState";
			Dictionary<string, string> data = new Dictionary<string, string>(2) {
				{ "SteamID", Bot.SteamID.ToString() },
				{ "AvatarHash", avatarHash }
			};

			// We don't need retry logic here
			await Program.WebBrowser.UrlPost(request, data).ConfigureAwait(false);
		}
	}
}