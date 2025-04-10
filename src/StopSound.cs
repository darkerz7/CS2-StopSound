using CounterStrikeSharp.API;
using CounterStrikeSharp.API.Core;
using CounterStrikeSharp.API.Core.Attributes.Registration;
using CounterStrikeSharp.API.Core.Capabilities;
using CounterStrikeSharp.API.Core.Translations;
using CounterStrikeSharp.API.Modules.Commands;
using CounterStrikeSharp.API.Modules.UserMessages;
using CounterStrikeSharp.API.Modules.Utils;
using Microsoft.Extensions.Localization;
using PlayerSettings;

namespace CS2_StopSound
{
	public class StopSound : BasePlugin
	{
		static int[] g_iStopsound = new int[65];
		private ISettingsApi? _PlayerSettingsAPI;
		private readonly PluginCapability<ISettingsApi?> _PlayerSettingsAPICapability = new("settings:nfcore");
		static IStringLocalizer? Strlocalizer;

		public override string ModuleName => "Stop Weapon Sounds";
		public override string ModuleDescription => "Allows players to modify hearing weapon sounds";
		public override string ModuleAuthor => "DarkerZ [RUS]";
		public override string ModuleVersion => "1.DZ.4.2";

		public override void OnAllPluginsLoaded(bool hotReload)
		{
			_PlayerSettingsAPI = _PlayerSettingsAPICapability.Get();
			if (_PlayerSettingsAPI == null)
				PrintToConsole("PlayerSettings core not found...");

			if (hotReload)
			{
				Utilities.GetPlayers().Where(p => p is { IsValid: true, IsBot: false, IsHLTV: false }).ToList().ForEach(player =>
				{
					GetValue(player);
				});
			}
		}
		public override void Load(bool hotReload)
		{
			Strlocalizer = Localizer;
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
			var wi = um.ReadUInt("weapon_id");
			var st = um.ReadInt("sound_type");
			var idi = um.ReadUInt("item_def_index");

			//Console.WriteLine($"weapon_id:{wi} sound_type:{st} item_def_index:{idi}");

			um.SetUInt("weapon_id", 0);
			um.SetInt("sound_type", 9);
			um.SetUInt("item_def_index", 60); //60 - M4A1-s, 61 - usp-s

			um.Recipients = GetRecipients(2); //Silence
			um.Send();

			um.SetUInt("weapon_id", wi);
			um.SetInt("sound_type", st);
			um.SetUInt("item_def_index", idi);

			um.Recipients = GetRecipients(0); //Enable sounds

			return HookResult.Continue;
		}

		RecipientFilter GetRecipients(int iType)
		{
			var rf = new RecipientFilter();

			Utilities.GetPlayers().ForEach(player =>
			{
				if (player != null && player.IsValid && g_iStopsound[player.Slot] == iType)
				{
					rf.Add(player);
				}
			});

			return rf;
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
		[CommandHelper(minArgs: 0, usage: "[number 0:Disable 1:Enable 2:Silence]", whoCanExecute: CommandUsage.CLIENT_ONLY)]
		public void OnCommandStopSound(CCSPlayerController? player, CommandInfo command)
		{
			if (player == null || !player.IsValid) return;
			if (!Int32.TryParse(command.GetArg(1), out int number)) number = g_iStopsound[player.Slot]+1;
			if (number < 0 || number > 2) number = 0;
			g_iStopsound[player.Slot] = number;
			SetValue(player);
			switch (number)
			{
				case 0: ReplyToCommand(player, command.CallingContext == CommandCallingContext.Console, "Message_Stopsound_Disabled"); return;
				case 1: ReplyToCommand(player, command.CallingContext == CommandCallingContext.Console, "Message_Stopsound_Enabled"); return;
				case 2: ReplyToCommand(player, command.CallingContext == CommandCallingContext.Console, "Message_Stopsound_Silenced"); return;
			}
		}

		void GetValue(CCSPlayerController? player)
		{
			if (player == null || !player.IsValid) return;
			if (_PlayerSettingsAPI != null)
			{
				string sValue = _PlayerSettingsAPI.GetPlayerSettingsValue(player, "StopSound", "1");
				if (string.IsNullOrEmpty(sValue) || !Int32.TryParse(sValue, out int iValue)) iValue = 1;
				if (iValue <= 0) iValue = 0;
				else if(iValue >= 2) iValue = 2;
				g_iStopsound[player.Slot] = iValue;
			}
		}

		void SetValue(CCSPlayerController? player)
		{
			if (player == null || !player.IsValid) return;
			if (_PlayerSettingsAPI != null)
			{
				_PlayerSettingsAPI?.SetPlayerSettingsValue(player, "StopSound", g_iStopsound[player.Slot].ToString());
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
		public static void PrintToConsole(string sMessage, int iColor = 1)
		{
			Console.ForegroundColor = (ConsoleColor)8;
			Console.Write("[");
			Console.ForegroundColor = (ConsoleColor)6;
			Console.Write("StopSound");
			Console.ForegroundColor = (ConsoleColor)8;
			Console.Write("] ");
			Console.ForegroundColor = (ConsoleColor)iColor;
			Console.WriteLine(sMessage, false);
			Console.ResetColor();
			/* Colors:
				* 0 - No color		1 - White		2 - Red-Orange		3 - Orange
				* 4 - Yellow		5 - Dark Green	6 - Green			7 - Light Green
				* 8 - Cyan			9 - Sky			10 - Light Blue		11 - Blue
				* 12 - Violet		13 - Pink		14 - Light Red		15 - Red */
		}
	}
}
