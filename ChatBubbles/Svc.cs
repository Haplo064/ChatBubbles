using Dalamud.Data;
using Dalamud.Game;
using Dalamud.Game.ClientState;
using Dalamud.Game.ClientState.Buddy;
using Dalamud.Game.ClientState.Conditions;
using Dalamud.Game.ClientState.Fates;
using Dalamud.Game.ClientState.JobGauge;
using Dalamud.Game.ClientState.Keys;
using Dalamud.Game.ClientState.Objects;
using Dalamud.Game.ClientState.Party;
using Dalamud.Game.Command;
using Dalamud.Game.DutyState;
using Dalamud.Game.Gui;
using Dalamud.Game.Gui.FlyText;
using Dalamud.Game.Gui.PartyFinder;
using Dalamud.Game.Gui.Toast;
using Dalamud.Game.Libc;
using Dalamud.Game.Network;
using Dalamud.IoC;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;

namespace ChatBubbles
{
    public class Svc
    {
		#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
	    [PluginService] static internal IGameInteropProvider gameInteropProvider { get; private set; }
        [PluginService] static internal DalamudPluginInterface pluginInterface { get; private set; }
		[PluginService] static internal IBuddyList buddyList { get; private set; }
		[PluginService] static internal IPluginLog pluginLog { get; private set; }
		[PluginService] static internal IChatGui chatGui { get; private set; }
		//[PluginService] static internal ChatHandlers chatHandlers { get; private set; }
		[PluginService] static internal IClientState clientState { get; private set; }
		[PluginService] static internal ICommandManager commandManager { get; private set; }
		[PluginService] static internal ICondition condition { get; private set; }
		[PluginService] static internal IDutyState dutyState { get; private set; }
		[PluginService] static internal IDataManager dataManager { get; private set; }
		[PluginService] static internal IFateTable fateTable { get; private set; }
		[PluginService] static internal IFlyTextGui flyTextGui { get; private set; }
		[PluginService] static internal IFramework framework { get; private set; }
		[PluginService] static internal IGameGui gameGui { get; private set; }
		[PluginService] static internal IGameNetwork gameNetwork { get; private set; }
		[PluginService] static internal IJobGauges jobGauges { get; private set; }
		[PluginService] static internal IKeyState keyState { get; private set; }
		[PluginService] static internal ILibcFunction libcFunction { get; private set; }
		[PluginService] static internal IObjectTable objectTable { get; private set; }
		[PluginService] static internal IPartyFinderGui partyFinderGui { get; private set; }
		[PluginService] static internal IPartyList partyList { get; private set; }
		[PluginService] static internal ISigScanner sigScannerD { get; private set; }
		[PluginService] static internal ITargetManager targetManager { get; private set; }
		[PluginService] static internal IToastGui toastGui { get; private set; }
		#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
    }
}
