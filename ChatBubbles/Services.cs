using Dalamud.Game;
using Dalamud.Game.ClientState.Objects;
using Dalamud.IoC;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;

namespace ChatBubbles
{
    public class Services
    {
		#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
	    [PluginService] static internal IGameInteropProvider GameInteropProvider { get; private set; }
        [PluginService] static internal IDalamudPluginInterface PluginInterface { get; private set; }
		[PluginService] static internal IBuddyList BuddyList { get; private set; }
		[PluginService] static internal IPluginLog PluginLog { get; private set; }
		[PluginService] static internal IChatGui ChatGui { get; private set; }
		//[PluginService] static internal ChatHandlers chatHandlers { get; private set; }
		[PluginService] static internal IClientState ClientState { get; private set; }
		[PluginService] static internal ICommandManager CommandManager { get; private set; }
		[PluginService] static internal ICondition Condition { get; private set; }
		[PluginService] static internal IDutyState DutyState { get; private set; }
		[PluginService] static internal IDataManager DataManager { get; private set; }
		[PluginService] static internal IFateTable FateTable { get; private set; }
		[PluginService] static internal IFlyTextGui FlyTextGui { get; private set; }
		[PluginService] static internal IFramework Framework { get; private set; }
		[PluginService] static internal IGameGui GameGui { get; private set; }
		[PluginService] static internal IGameNetwork GameNetwork { get; private set; }
		[PluginService] static internal IJobGauges JobGauges { get; private set; }
		[PluginService] static internal IKeyState KeyState { get; private set; }
		[PluginService] static internal IObjectTable ObjectTable { get; private set; }
		[PluginService] static internal IPartyFinderGui PartyFinderGui { get; private set; }
		[PluginService] static internal IPartyList PartyList { get; private set; }
		[PluginService] static internal ISigScanner SigScannerD { get; private set; }
		[PluginService] static internal ITargetManager TargetManager { get; private set; }
		[PluginService] static internal IToastGui ToastGui { get; private set; }
		#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider declaring as nullable.
    }
}
