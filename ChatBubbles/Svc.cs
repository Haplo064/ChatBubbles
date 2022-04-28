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
using Dalamud.Game.Gui;
using Dalamud.Game.Gui.FlyText;
using Dalamud.Game.Gui.PartyFinder;
using Dalamud.Game.Gui.Toast;
using Dalamud.Game.Libc;
using Dalamud.Game.Network;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.IoC;
using Dalamud.Plugin;

namespace ChatBubbles
{
    public class Svc
    {
	    [PluginService] static internal DalamudPluginInterface pluginInterface { get; private set; }
		[PluginService] static internal BuddyList buddyList { get; private set; }
		[PluginService] static internal ChatGui chatGui { get; private set; }
		[PluginService] static internal ChatHandlers chatHandlers { get; private set; }
		[PluginService] static internal ClientState clientState { get; private set; }
		[PluginService] static internal CommandManager commandManager { get; private set; }
		[PluginService] static internal Condition condition { get; private set; }
		[PluginService] static internal DataManager dataManager { get; private set; }
		[PluginService] static internal FateTable fateTable { get; private set; }
		[PluginService] static internal FlyTextGui flyTextGui { get; private set; }
		[PluginService] static internal Framework framework { get; private set; }
		[PluginService] static internal GameGui gameGui { get; private set; }
		[PluginService] static internal GameNetwork gameNetwork { get; private set; }
		[PluginService] static internal JobGauges jobGauges { get; private set; }
		[PluginService] static internal KeyState keyState { get; private set; }
		[PluginService] static internal LibcFunction libcFunction { get; private set; }
		[PluginService] static internal ObjectTable objectTable { get; private set; }
		[PluginService] static internal PartyFinderGui partyFinderGui { get; private set; }
		[PluginService] static internal PartyList partyList { get; private set; }
		[PluginService] static internal SeStringManager seStringManager { get; private set; }
		[PluginService] static internal SigScanner sigScannerD { get; private set; }
		[PluginService] static internal TargetManager targetManager { get; private set; }
		[PluginService] static internal ToastGui toastGui { get; private set; }
	}
}
