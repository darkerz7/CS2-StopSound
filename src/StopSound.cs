using ClientPrefsAPI;
using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Core.Translations;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.UserMessages;
using Microsoft.Extensions.Localization;

namespace CS2_StopSound
{
	public class StopSound : BasePlugin
	{
		static int[] g_iStopsound = new int[65];
		static IClientPrefsApi? _CP_api;
		static IStringLocalizer? Strlocalizer;

		public override string ModuleName => "Stop Weapon Sounds";
		public override string ModuleDescription => "Allows clients to modify hearing weapon sounds";
		public override string ModuleAuthor => "DarkerZ [RUS]";
		public override string ModuleVersion => "1.DZ.1";

		public override void Load(bool hotReload)
		{
			_CP_api = IClientPrefsApi.Capability.Get();
			Strlocalizer = Localizer;
			if (hotReload)
			{
				Utilities.GetPlayers().ForEach(player =>
				{
					GetValue(player);
				});
			}
			RegisterEventHandler<EventPlayerConnectFull>(OnEventPlayerConnectFull);
			RegisterEventHandler<EventPlayerDisconnect>(OnEventPlayerDisconnect);
			HookUserMessage(452, OnWeaponSound, HookMode.Pre);
		}

		public override void Unload(bool hotReload)
		{
			RemoveCommand("css_stopsound", OnCommandStopSound);
			DeregisterEventHandler<EventPlayerConnectFull>(OnEventPlayerConnectFull);
			DeregisterEventHandler<EventPlayerDisconnect>(OnEventPlayerDisconnect);
			UnhookUserMessage(452, OnWeaponSound, HookMode.Pre);
		}

		private HookResult OnWeaponSound(UserMessage um)
		{
			Utilities.GetPlayers().ForEach(player =>
			{
				if (player != null && player.IsValid && g_iStopsound[player.Slot] > 0)
				{
					um.Recipients.Remove(player);
				}
			});

			um.Send();

			um.SetUInt("weapon_id", 0);
			um.SetInt("sound_type", 9);
			um.SetUInt("item_def_index", 61);

			um.Recipients.Clear();
			Utilities.GetPlayers().ForEach(player =>
			{
				if (player != null && player.IsValid && g_iStopsound[player.Slot] == 2)
				{
					um.Recipients.Add(player);
				}
			});

			return HookResult.Continue;
		}

		private HookResult OnEventPlayerDisconnect(EventPlayerDisconnect @event, GameEventInfo info)
		{
			CCSPlayerController? player = @event.Userid;
			if (player != null && player.IsValid) g_iStopsound[player.Slot] = 0;
			return HookResult.Continue;
		}

		private HookResult OnEventPlayerConnectFull(EventPlayerConnectFull @event, GameEventInfo info)
		{
			GetValue(@event.Userid);
			return HookResult.Continue;
		}

		[ConsoleCommand("css_stopsound", "Toggle hearing weapon sounds")]
		[CommandHelper(minArgs: 1, usage: "[number 0:Disable 1:Enable 2:Silence]", whoCanExecute: CommandUsage.CLIENT_ONLY)]
		public void OnCommandStopSound(CCSPlayerController? player, CommandInfo command)
		{
			if (player == null || !player.IsValid) return;
			int number;
			if (!Int32.TryParse(command.GetArg(1), out number)) number = 0;
			if (number <= 0) number = 0;
			else if (number >= 2) number = 2;
			g_iStopsound[player.Slot] = number;
			SetValue(player);
			switch (number)
			{
				case 0: ReplyToCommand(player, command.CallingContext == CommandCallingContext.Console, "Message_Stopsound_Disabled"); return;
				case 1: ReplyToCommand(player, command.CallingContext == CommandCallingContext.Console, "Message_Stopsound_Enabled"); return;
				case 2: ReplyToCommand(player, command.CallingContext == CommandCallingContext.Console, "Message_Stopsound_Silenced"); return;
			}
		}

		async void GetValue(CCSPlayerController? player)
		{
			if (player == null || !player.IsValid) return;
			if (_CP_api != null)
			{
				string sValue = await _CP_api.GetClientCookie(player.SteamID.ToString(), "StopSound");
				int iValue;
				if (string.IsNullOrEmpty(sValue) || !Int32.TryParse(sValue, out iValue)) iValue = 0;
				if (iValue <= 0) iValue = 0;
				else if(iValue >= 2) iValue = 2;
				g_iStopsound[player.Slot] = iValue;
			}
		}

		async void SetValue(CCSPlayerController? player)
		{
			if (player == null || !player.IsValid) return;
			if (_CP_api != null)
			{
				await _CP_api.SetClientCookie(player.SteamID.ToString(), "StopSound", g_iStopsound[player.Slot].ToString());
			}
		}

		static void ReplyToCommand(CCSPlayerController player, bool bConsole, string sMessage, params object[] arg)
		{
			if (Strlocalizer == null) return;
			Server.NextFrame(() =>
			{
				if (player is { IsValid: true, IsBot: false, IsHLTV: false })
				{
					using (new WithTemporaryCulture(player.GetLanguage()))
					{
						if (!bConsole) player.PrintToChat($" \x0B[\x04StopSound\x0B]\x01{Strlocalizer[sMessage, arg]}");
						else player.PrintToConsole($"[StopSound]{Strlocalizer[sMessage, arg]}");
					}
				}
			});
		}
	}
}
