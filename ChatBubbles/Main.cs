using Dalamud.Configuration;
using Dalamud.Game.Text;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Game.Command;
using Dalamud.Hooking;
using Dalamud.Plugin;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Logging;
using Lumina.Excel.GeneratedSheets;
using ImGuiNET;
using System.Numerics;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using Balloon = FFXIVClientStructs.FFXIV.Client.Game.Balloon;
using FFXIVClientStructs.FFXIV.Component.GUI;
using Framework = FFXIVClientStructs.FFXIV.Client.System.Framework.Framework;
using Num = System.Numerics;
using Dalamud.Game.ClientState.Objects.Enums;
using Dalamud.Interface.Windowing;

namespace ChatBubbles
{
    internal class UiColorComparer : IEqualityComparer<UIColor>
    {
        public bool Equals(UIColor? x, UIColor? y)
        {
            return x?.UIForeground == y?.UIForeground; // based on variable i
        }
        public int GetHashCode(UIColor obj)
        {
            return obj.UIForeground.GetHashCode(); // hashcode of variable to compare
        }
    }

    public unsafe partial class ChatBubbles : IDalamudPlugin
    {
        public string Name => "Chat Bubbles";
        private readonly List<UIColor> _uiColours;
        private readonly Config _configuration;
        private bool _picker;
        private readonly List<CharData> _charDatas = new();
        private int _timer;
        private UiColorPick _chooser;
        private int _queue;
        private int _bubbleFunctionality;
        private bool _hide;
        private bool _assBubbles;
        private bool _chaosMode;
        private bool _friendsOnly;
        private bool _fcOnly;
        private bool _partyOnly;
        private readonly bool _textScale;
        private readonly List<Vector4> _bubbleColours;
        private readonly List<Vector4> _bubbleColours2;
        private float _defaultScale;
        private float _positionY;
        private bool _switch;
        private float _bubbleSize;
        private readonly bool _selfLock;
        private AtkResNode* _listOfBubbles;
        private int _playerBubble = 99;
        private float _playerBubbleX;
        private int _dirtyHack;
        private bool _config;
        private bool _debug;
        //TODO : check pauser usage ; uncomment below if found
        //private int pauser = 0;
        //#Pride
        private bool _f1;
        private bool _f2;
        private bool _f3;
        private bool _pride;
        //Distance
        private int _yalmCap;
        private int _attachmentPointID;
        // 1 = forehead 
        // 6 = right shoulder
        // 7 = left shoulder
        // 8 = right elbow
        // 9 = left elbow
        // 10 = right knee
        // 11 = left knee
        // 27 = face
        // 27 = face
        // 28 = breasts
        // 29->31 = ass
        // 32 = right hand
        // 33 = left hand
        // 34 = right feet
        // 35 = left feet
        // 43 = right eye
        // 44 = left eye
        // 45 = high head
        // 46-47 = ass
        // 48 = lower ass
        // 49 = breast
        // 50 = between thighs
        // 51 = forehead
        // 52 = left breast offset in front
        // 53 = mouth offset in front
        // 54 = belly offset in front
        // 55 = genitals offset in front
        // 62 = mouth offset behind
        // 63 = ideal fart position
        // 64 = inside core body
        // 66 = right breast offset behind



        private readonly List<XivChatType> _channels;

        private readonly List<XivChatType> _order = new()
        {
            XivChatType.None, XivChatType.None, XivChatType.None, XivChatType.None, XivChatType.Say,
            XivChatType.Shout, XivChatType.TellOutgoing, XivChatType.TellIncoming, XivChatType.Party,
            XivChatType.Alliance,
            XivChatType.Ls1, XivChatType.Ls2, XivChatType.Ls3, XivChatType.Ls4, XivChatType.Ls5,
            XivChatType.Ls6, XivChatType.Ls7, XivChatType.Ls8, XivChatType.FreeCompany, XivChatType.NoviceNetwork,
            XivChatType.CustomEmote, XivChatType.StandardEmote, XivChatType.Yell, XivChatType.CrossParty,
            XivChatType.PvPTeam,
            XivChatType.CrossLinkShell1, XivChatType.Echo, XivChatType.None, XivChatType.None, XivChatType.None,
            XivChatType.None, XivChatType.None, XivChatType.None, XivChatType.None, XivChatType.CrossLinkShell2, XivChatType.CrossLinkShell3,
            XivChatType.CrossLinkShell4,
            XivChatType.CrossLinkShell5, XivChatType.CrossLinkShell6, XivChatType.CrossLinkShell7,
            XivChatType.CrossLinkShell8
        };
        
         private readonly bool[] _yesno =
        {
            false, false, false, false, true,
            true, true, true, true, true,
            true, true, true, true, true,
            true, true, true, true, true,
            true, true, true, true, true,
            true, true, false, false, false,
            false, false, false, false, true,
            true, true,true, true, true,
            true
        };

        private readonly XivChatType[] _allowedChannels =
            {
            XivChatType.Say, XivChatType.Shout, XivChatType.TellOutgoing, XivChatType.TellIncoming, XivChatType.Party,
            XivChatType.Alliance, XivChatType.Ls1, XivChatType.Ls2, XivChatType.Ls3, XivChatType.Ls4, XivChatType.Ls5,
            XivChatType.Ls6, XivChatType.Ls7, XivChatType.Ls8, XivChatType.FreeCompany, XivChatType.NoviceNetwork,
            XivChatType.CustomEmote, XivChatType.StandardEmote, XivChatType.Yell, XivChatType.CrossParty, XivChatType.CrossLinkShell1,
            XivChatType.CrossLinkShell2, XivChatType.CrossLinkShell3, XivChatType.CrossLinkShell4, XivChatType.CrossLinkShell5,
            XivChatType.CrossLinkShell6, XivChatType.CrossLinkShell7, XivChatType.CrossLinkShell8
        };

        //TODO : check bubbleNumber usage ; uncomment below if found
        //private int bubbleNumber = 0;

        private readonly bool[] _bubbleActive = {false, false, false, false, false, false, false, false, false, false, false};
        private readonly XivChatType[] _bubbleActiveType = {XivChatType.Debug, XivChatType.Debug, XivChatType.Debug, XivChatType.Debug, XivChatType.Debug, XivChatType.Debug, XivChatType.Debug, XivChatType.Debug, XivChatType.Debug, XivChatType.Debug, XivChatType.Debug};
        private readonly BalloonSlotState[] _slots = new BalloonSlotState[10];
        private readonly AtkResNode*[] _bubblesAtk = new AtkResNode*[10];
        private readonly AtkResNode*[] _bubblesAtk2 = new AtkResNode*[10];
        private readonly UiColorPick[] _textColour;


        [UnmanagedFunctionPointer(CallingConvention.ThisCall, CharSet = CharSet.Ansi)]
        private delegate IntPtr UpdateBubble(Balloon* bubble, IntPtr actor, IntPtr dunnoA, IntPtr dunnoB);
        private readonly Hook<UpdateBubble> _updateBubbleFuncHook;


        [UnmanagedFunctionPointer(CallingConvention.ThisCall, CharSet = CharSet.Ansi)]
        private delegate IntPtr OpenBubble(IntPtr self, IntPtr actor, IntPtr textPtr, bool notSure, int attachmentPointID);
        private readonly Hook<OpenBubble> _openBubbleFuncHook;
        
        public ChatBubbles(IDalamudPluginInterface pluginInt)
        {
            pluginInt.Create<Services>();
 
            _configuration = Services.PluginInterface.GetPluginConfig() as Config ?? new Config();
            _timer = _configuration.Timer;
            _channels = _configuration.Channels;
            _textColour = _configuration.TextColour;
            _queue = _configuration.Queue;
            _bubbleFunctionality = _configuration.BubbleFunctionality;
            _hide = _configuration.Hide;
            _fcOnly = _configuration.fcOnly;
            _partyOnly = _configuration.partyOnly;
            _friendsOnly = _configuration.friendsOnly;
            _textScale = _configuration.TextScale;
            _bubbleColours = _configuration.BubbleColours;
            _bubbleColours2 = _configuration.BubbleColours2;
            _bubbleSize = _configuration.BubbleSize;
            _selfLock = _configuration.SelfLock;
            _defaultScale = _configuration.DefaultScale;
            _assBubbles = _configuration.assBubbles;
            _chaosMode = _configuration.chaosMode;
            _switch = _configuration.Switch;
            _yalmCap = _configuration.YalmCap;
            _attachmentPointID = _configuration.AttachmentPointID;

            //Added two enums in dalamud update
            if (_bubbleColours.Count == 39)
            {
                _bubbleColours.Insert(32, new Vector4(1, 1, 1, 0));
                _bubbleColours.Insert(32, new Vector4(1, 1, 1, 0));
                _bubbleColours2.Insert(32, new Vector4(0,0, 0, 0));
                _bubbleColours2.Insert(32, new Vector4(0, 0, 0, 0));

                var temp = new UiColorPick[_bubbleColours.Count];
                for (int i =0; i<39; i++)
                {
                    if(i >= 32)
                    {
                        temp[i+2] = _textColour[i];
                    }
                    else
                    {
                        temp[i] = _textColour[i];
                    }
                }
                temp[32] =  new() { Choice = 0, Option = 0 };
                temp[33] = new() { Choice = 0, Option = 0 };

                _textColour = temp;
            }

            while (_bubbleColours.Count < 41) _bubbleColours.Add(new Vector4(1,1,1,0));
            while (_bubbleColours2.Count < 41) _bubbleColours2.Add(new Vector4(0,0,0,0));
            
            var list = new List<UIColor>(Services.DataManager.Excel.GetSheet<UIColor>()!.Distinct(new UiColorComparer()));
            list.Sort((a, b) =>
            {
                var colorA = ConvertUIColorToColor(a);
                var colorB = ConvertUIColorToColor(b);
                ImGui.ColorConvertRGBtoHSV(colorA.X, colorA.Y, colorA.Z, out var aH, out var aS, out var aV);
                ImGui.ColorConvertRGBtoHSV(colorB.X, colorB.Y, colorB.Z, out var bH, out var bS, out var bV);

                var hue = aH.CompareTo(bH);
                if (hue != 0) { return hue; }

                var saturation = aS.CompareTo(bS);
                if (saturation != 0) { return saturation; }

                var value = aV.CompareTo(bV);
                return value != 0 ? value : 0;
            });
            _uiColours = list;


            

            Services.Framework.Update += OnceUponAFrame;
            Services.ChatGui.ChatMessage += Chat_OnChatMessage;
            Services.PluginInterface.UiBuilder.Draw += BubbleConfigUi;
            Services.PluginInterface.UiBuilder.OpenConfigUi += BubbleConfig;
            Services.CommandManager.AddHandler("/bub", new CommandInfo(Command)
            {
                HelpMessage = "Opens the Chat Bubble config menu"
            });
            
            var updateBubblePtr = Services.SigScannerD.ScanText("48 85 D2 0F 84 ?? ?? ?? ?? 48 89 6C 24 ?? 56 48 83 EC 30 8B 41 0C");
            UpdateBubble updateBubbleFunc = UpdateBubbleFuncFunc;
            try
            {
                _updateBubbleFuncHook = Services.GameInteropProvider.HookFromAddress<UpdateBubble>(updateBubblePtr + 0x9, updateBubbleFunc);
                _updateBubbleFuncHook.Enable();
                Services.PluginLog.Debug("GOOD");
            }
            catch (Exception e)
            { Services.PluginLog.Error("BAD\n" + e); }

            
            var openBubblePtr = Services.SigScannerD.ScanText("E8 ?? ?? ?? FF 48 8B 7C 24 48 C7 46 0C 01 00 00 00");
            OpenBubble openBubbleFunc = OpenBubbleFuncFunc;
            try
            {
                _openBubbleFuncHook = Services.GameInteropProvider.HookFromAddress<OpenBubble>(openBubblePtr, openBubbleFunc);
                _openBubbleFuncHook.Enable();
                Services.PluginLog.Debug("GOOD2");
            }
            catch (Exception e)
            { Services.PluginLog.Error("BAD2\n" + e); }
        }

        private Vector4 ConvertUIColorToColor(UIColor uiColor)
        {
            var temp = BitConverter.GetBytes(uiColor.UIForeground);
            return new Vector4((float)temp[3] / 255,
                (float)temp[2] / 255,
                (float)temp[1] / 255,
                (float)temp[0] / 255);
        }

        private void SaveConfig()
        {
            _configuration.Timer = _timer;
            _configuration.Channels = _channels;
            _configuration.TextColour = _textColour;
            _configuration.Queue = _queue;
            _configuration.BubbleFunctionality = _bubbleFunctionality;
            _configuration.Hide = _hide;
            _configuration.assBubbles = _assBubbles;
            _configuration.chaosMode = _chaosMode;
            _configuration.fcOnly = _fcOnly;
            _configuration.friendsOnly = _friendsOnly;
            _configuration.partyOnly = _partyOnly;
            _configuration.BubbleColours = _bubbleColours;
            _configuration.BubbleColours2 = _bubbleColours2;
            _configuration.TextScale = false;
            _configuration.BubbleSize = _bubbleSize;
            _configuration.SelfLock = _selfLock;
            _configuration.DefaultScale = _defaultScale;
            _configuration.Switch = _switch;
            _configuration.YalmCap = _yalmCap;
            //_configuration.AttachmentPointID = _attachmentPointID;
            Services.PluginInterface.SavePluginConfig(_configuration);
        }


        private Vector4 GetBubbleColour(XivChatType type)
        {
            for (int i = 0; i < _order.Count; i++)
            {
                if (type == _order[i])
                {
                    return _bubbleColours[i];
                }
            }

            return new Vector4(0, 0, 0, 0);
        }
        private Vector4 GetBubbleColour2(XivChatType type)
        {
            for (int i = 0; i < _order.Count; i++)
            {
                if (type == _order[i])
                {
                    return _bubbleColours2[i];
                }
            }

            return new Vector4(0, 0, 0, 0);
        }

        private void cleanBubbles()
        {
            var logBubble = (AgentScreenLog*)Framework.Instance()->GetUIModule()->GetAgentModule()->GetAgentByInternalId(AgentId.ScreenLog);
            var slots = new BalloonSlotState[10];
            for (int k = 0; k < 10; k++)
            {
                slots[k] = new BalloonSlotState();
            }
            
            for (ulong j = 0; j < logBubble->BalloonQueue.MySize; j++)
            {
                var balloonInfo = logBubble->BalloonQueue.Get(j);
                slots[balloonInfo.Slot].ID = balloonInfo.BalloonId;
                slots[balloonInfo.Slot].Active = true;
            }
                    
                    
            var addonPtr = IntPtr.Zero;
            addonPtr =  Services.GameGui.GetAddonByName("_MiniTalk",1);
            if (addonPtr != IntPtr.Zero)
            {
                AtkUnitBase* miniTalk = (AtkUnitBase*) addonPtr;
                _listOfBubbles = miniTalk->RootNode;
                
                _bubblesAtk[0] = _listOfBubbles->ChildNode;
                for (int k = 1; k < 10; k++)
                {
                    _bubblesAtk[k] = _bubblesAtk[k - 1]->PrevSiblingNode;
                    _bubblesAtk[k]->AddRed = 0;
                    _bubblesAtk[k]->AddBlue = 0;
                    _bubblesAtk[k]->AddGreen = 0;
                    _bubblesAtk[k]->ScaleX = 1f;
                    _bubblesAtk[k]->ScaleY = 1f;
                    var resNodeNineGrid = ((AtkComponentNode*) _bubblesAtk[k])->Component->UldManager
                        .SearchNodeById(5);
                    var resNodeDangly = ((AtkComponentNode*) _bubblesAtk[k])->Component->UldManager
                        .SearchNodeById(4);

                    resNodeDangly->Color.R = (byte) (255);
                    resNodeDangly->Color.G = (byte) (255);
                    resNodeDangly->Color.B = (byte) (255);
                    resNodeNineGrid->Color.R = (byte) (255);
                    resNodeNineGrid->Color.G = (byte) (255);
                    resNodeNineGrid->Color.B = (byte) (255);
                }
            }
        }
        
        private IntPtr UpdateBubbleFuncFunc(Balloon* bubble, IntPtr actor, IntPtr dunnoA, IntPtr dunnoB)
        {
            var logBubble = (AgentScreenLog*) Framework.Instance()->GetUIModule()->GetAgentModule()->GetAgentByInternalId(
                    AgentId.ScreenLog);

            for (int k = 0; k < 10; k++)
            {
                _slots[k] = new BalloonSlotState();
            }

            if (logBubble != null)
            {
                for (ulong j = 0; j < logBubble->BalloonQueue.MySize; j++)
                {
                    var balloonInfo = logBubble->BalloonQueue.Get(j);

                    _slots[9 - j].ID = balloonInfo.BalloonId - 1;
                    _slots[9 - j].Active = true;
                }
            }

            if (actor != null)
            {
                const int idOffset = 116;
                var actorId = Marshal.ReadInt32(actor + idOffset);


                foreach (var cd in _charDatas.Where(cd => actorId == cd.ActorId))
                {
                    if (bubble->State == BalloonState.Inactive && _switch && !Services.ClientState.IsPvP)
                    {

                        //Get the slot that will turn into the bubble
                        var freeSlot = GetFreeBubbleSlot();
                        if (freeSlot == -1)
                        {
                            break;
                        }

                        _bubbleActive[freeSlot] = true;
                        _bubbleActiveType[freeSlot] = cd.Type;

                        
                        if (cd.Name == Services.ClientState.LocalPlayer?.Name.TextValue)
                        {
                            _playerBubble = freeSlot;
                        }

                        bubble->State = BalloonState.Closing;

                        if (_textScale)
                        {
                            var val = (double) cd.Message?.TextValue.Length / 10;
                            if ((float) (_timer * val) < _timer)
                            {
                                bubble->PlayTimer = _timer;
                            }
                            else
                            {
                                bubble->PlayTimer = (float) (_timer * val);
                            }
                        }
                        else
                        {
                            bubble->PlayTimer = _timer;
                        }
                    }

                    if (bubble->State == BalloonState.Active && cd.NewMessage)
                    {
                        bubble->State = BalloonState.Inactive;
                        bubble->PlayTimer = 0;
                        cd.NewMessage = false;
                    }

                    break;
                }
            }


            return _updateBubbleFuncHook.Original(bubble, actor, dunnoA, dunnoB);
        }
        
        private int GetFreeBubbleSlot()
        {
            var addonPtr2 =  Services.GameGui.GetAddonByName("_MiniTalk",1);
            if (addonPtr2 != IntPtr.Zero)
            {
                for (int i = 0; i < 10; i++)
                {
                    if (_bubblesAtk2[i]->IsVisible())
                    {
                        continue;
                    }
                    return i;
                }
                return -1;
            }
            else return -1;
        }

        private IntPtr OpenBubbleFuncFunc(IntPtr self, IntPtr actor, IntPtr textPtr, bool notSure, int attachmentPointID)
        {
            // S Rank Atticus the Primogenitor I hate you.
            // This check lets the hook go through since most NPCs & Players have the 25 APID
            if (attachmentPointID != 25 && attachmentPointID != 0)
            {
                Services.PluginLog.Warning($"Unusual AttachmentPoint Detected: {attachmentPointID.ToString()}");
                return _openBubbleFuncHook.Original(self, actor, textPtr, notSure, attachmentPointID);
            }

            const int idOffset = 116;
            var actorId = Marshal.ReadInt32(actor, idOffset);

            foreach (var cd in _charDatas.Where(cd => actorId == cd.ActorId))
            {
                var freeSlot = GetFreeBubbleSlot();
                if (freeSlot == -1)
                {
                    break;
                }

                _bubbleActiveType[freeSlot] = cd.Type;
                _bubbleActive[freeSlot] = true;

                if (cd.Message?.TextValue.Length > 0)
                {
                    var bytes = cd.Message.Encode();
                    var newPointer = Marshal.AllocHGlobal(bytes.Length + 1);
                    Marshal.Copy(bytes, 0, newPointer, bytes.Length);
                    Marshal.WriteByte(newPointer, bytes.Length, 0);
                    textPtr = newPointer;
                }

                break;
            }
            if (textPtr.ToString().Contains("fart") || _configuration.assBubbles)
                attachmentPointID = 63;

            if(_configuration.chaosMode)
            {
                int[] chaos = [1, 6, 7, 8, 9, 10, 11, 25, 27, 28, 30, 32, 33, 34, 35, 43, 44, 45, 48, 49, 50, 51, 52, 53, 54, 55, 62, 63, 64];
                new Random().Shuffle(chaos);
                attachmentPointID = chaos[1];

            }

            //// Kept for debug purposes
            //else 
            //    attachmentPointID = _configuration.AttachmentPointID;
            //Services.PluginLog.Debug(attachmentPointID.ToString());

            return _openBubbleFuncHook.Original(self, actor, textPtr, notSure, attachmentPointID);
        }

        void IDisposable.Dispose()
        {
            Services.ChatGui.ChatMessage -= Chat_OnChatMessage;
            Services.PluginInterface.UiBuilder.Draw -= BubbleConfigUi;
            Services.PluginInterface.UiBuilder.OpenConfigUi -= BubbleConfig;
            Services.CommandManager.RemoveHandler("/bub");
            _updateBubbleFuncHook.Dispose();
            _openBubbleFuncHook.Dispose();
            cleanBubbles();
        }

        private void BubbleConfig()
        {
            if(!_config)
            {
                _config = true;
            }
            else
            {
                _config = !_config;
            }
        } 


        // What to do when command is called
        private void Command(string command, string arguments)
        {
            if (arguments == "clean")
            {
                var chat = new XivChatEntry();
                chat.Message = "Cleaning Bubbles";
                Services.ChatGui.Print(chat);
                cleanBubbles();
            }
            else if (arguments == "toggle")
            {
                var tog = "ON";
                if (_switch)
                {
                    tog = "OFF";
                }
                var chat = new XivChatEntry();
                chat.Message = $"Toggling Bubbles {tog}";
                Services.ChatGui.Print(chat);
                _switch = !_switch;
            }
            else
            {
                _config = !_config;
            }
        }


        private uint GetActorId(string nameInput)
        {
            if (_hide && nameInput == Services.ClientState.LocalPlayer?.Name.TextValue) return 0;

            foreach (var t in Services.ObjectTable)
            {
                if (!(t is IPlayerCharacter pc)) continue;
                if (pc.Name.TextValue == nameInput) return pc.EntityId;
            }
            return 0;
        }

        private bool IsFriend(string nameInput)
        {
            if (nameInput == Services.ClientState.LocalPlayer?.Name.TextValue) return true;

            foreach (var t in Services.ObjectTable)
            {
                if (!(t is IPlayerCharacter pc)) continue;

                if (pc.Name.TextValue == nameInput)
                {
                    return pc.StatusFlags.HasFlag(StatusFlags.Friend);
                }
            }
            return false;
        }

        private bool IsPartyMember(string nameInput)
        {
            if (nameInput == Services.ClientState.LocalPlayer?.Name.TextValue) return true;

            foreach (var t in Services.ObjectTable)
            {
                if (!(t is IPlayerCharacter pc)) continue;
                if (pc.Name.TextValue == nameInput)
                {
                    return pc.StatusFlags.HasFlag(StatusFlags.PartyMember);
                }
            }
            return false;
        }

        private bool IsFC(string nameInput)
        { 
            if (nameInput == Services.ClientState.LocalPlayer?.Name.TextValue) return true;

			foreach (var t in Services.ObjectTable)
            { 
                if (!(t is IPlayerCharacter pc)) continue;
                if (pc.Name.TextValue == nameInput)

                {
					return Services.ClientState.LocalPlayer?.CompanyTag.TextValue == pc.CompanyTag.TextValue;
                }
            }
            return false;
        }

        private int GetActorDistance(string name)
        {
            if (name == Services.ClientState.LocalPlayer?.Name.TextValue) return 0;

            foreach (var t in Services.ObjectTable)
            {
                if (!(t is IPlayerCharacter pc)) continue;
                if (pc.Name.TextValue == name)
                {
                    return (int)Math.Sqrt( Math.Pow(pc.YalmDistanceX, 2) + Math.Pow(pc.YalmDistanceZ, 2));
                }
            }
            return 0;            
            
        }
        
        private class CharData
        {
            public SeString? Message;
            public XivChatType Type;
            public uint ActorId;
            public DateTime MessageDateTime;
            public string? Name;
            public bool NewMessage { get; set; }
            public int BubbleNumber = -1;
            public bool KillMe { get; set; } = false;
        }
    }

    public class BalloonSlotState
    {
        public bool Active { get; set; } = false;
        public int ID { get; set; } = 0;
    }
    
    public class UiColorPick
    {
        public uint Choice { get; set; }
        public uint Option { get; set; }
    }

    public class Config : IPluginConfiguration
    {
        public int Version { get; set; } = 0;
        public List<XivChatType> Channels { get; set; } = new() {XivChatType.Say};
        public int Timer { get; set; } = 7;
        public int BubbleFunctionality { get; set; } = 0;
		public bool Hide { get; set; } = false;
		public bool friendsOnly { get; set; } = false;
		public bool fcOnly { get; set; } = false;
		public bool partyOnly { get; set; } = false;
		public bool TextScale { get; set; } = false;
        public bool SelfLock { get; set; } = false;
        public float BubbleSize { get; set; } = 1f;
        public float DefaultScale { get; set; } = 1f;
        public bool Switch { get; set; } = true;
        public int YalmCap { get; set; } = 99;
        public int AttachmentPointID { get; set; } = 0;
        public bool assBubbles { get; set; } = false;
        public bool chaosMode { get; set; } = false;

        public UiColorPick[] TextColour { get; set; } =
        {
            new() { Choice = 0, Option =0 }, new() { Choice = 0, Option =0 }, new () { Choice = 0, Option =0 },
            new() { Choice = 0, Option =0 }, new() { Choice = 0, Option =0 }, new () { Choice = 0, Option =0 },
            new() { Choice = 0, Option =0 }, new() { Choice = 0, Option =0 }, new () { Choice = 0, Option =0 },
            new() { Choice = 0, Option =0 }, new() { Choice = 0, Option =0 }, new () { Choice = 0, Option =0 },
            new() { Choice = 0, Option =0 }, new() { Choice = 0, Option =0 }, new () { Choice = 0, Option =0 },
            new() { Choice = 0, Option =0 }, new() { Choice = 0, Option =0 }, new () { Choice = 0, Option =0 },
            new() { Choice = 0, Option =0 }, new() { Choice = 0, Option =0 }, new () { Choice = 0, Option =0 },
            new() { Choice = 0, Option =0 }, new() { Choice = 0, Option =0 }, new () { Choice = 0, Option =0 },
            new() { Choice = 0, Option =0 }, new() { Choice = 0, Option =0 }, new () { Choice = 0, Option =0 },
            new() { Choice = 0, Option =0 }, new() { Choice = 0, Option =0 }, new () { Choice = 0, Option =0 },
            new() { Choice = 0, Option =0 }, new() { Choice = 0, Option =0 }, new () { Choice = 0, Option =0 },
            new() { Choice = 0, Option =0 }, new() { Choice = 0, Option =0 }, new () { Choice = 0, Option =0 },
            new() { Choice = 0, Option =0 }, new() { Choice = 0, Option =0 }, new () { Choice = 0, Option =0 },
            new() { Choice = 0, Option =0 }, new() { Choice = 0, Option =0 }
        };

        public List<Vector4> BubbleColours { get; set; } = new List<Num.Vector4>();
        public List<Vector4> BubbleColours2 { get; set; } = new List<Num.Vector4>();

        public int Queue { get; set; } = 3;
    }
    
    [StructLayout(LayoutKind.Explicit, Size = 0x468)]
    public unsafe struct AddonMiniTalk
    {
        [FieldOffset(0x248)] public AtkResNode* ChatBubble0;
        [FieldOffset(0x280)] public AtkResNode* ChatBubble1;
        [FieldOffset(0x2B8)] public AtkResNode* ChatBubble2;
        [FieldOffset(0x2F0)] public AtkResNode* ChatBubble3;
        [FieldOffset(0x328)] public AtkResNode* ChatBubble4;
        [FieldOffset(0x360)] public AtkResNode* ChatBubble5;
        [FieldOffset(0x398)] public AtkResNode* ChatBubble6;
        [FieldOffset(0x3D0)] public AtkResNode* ChatBubble7;
        [FieldOffset(0x408)] public AtkResNode* ChatBubble8;
        [FieldOffset(0x440)] public AtkResNode* ChatBubble9;
    }
}

/*
                            var rand = new Random();
                            
                            if(f1) {bub->AddBlue += (ushort)rand.Next(0, 10);}
                            else {bub->AddBlue -= (ushort)rand.Next(0, 10);}
                            
                            if(f2) {bub->AddRed += (ushort)rand.Next(0, 10);}
                            else {bub->AddRed -= (ushort)rand.Next(0, 10);}
                            
                            if(f3) {bub->AddGreen += (ushort)rand.Next(0, 10);}
                            else {bub->AddGreen -= (ushort)rand.Next(0, 10);}
                            
                            if(bub->AddBlue>=100){bub->AddBlue = 100; f1=!f1;}
                            if(bub->AddRed>=100){bub->AddRed = 100; f2=!f2;}
                            if(bub->AddGreen>=100){bub->AddGreen = 100; f3=!f3;}
                            
                            if(bub->AddBlue<=10) f1=!f1;
                            if(bub->AddRed<=10) f2=!f2;
                            if(bub->AddGreen<=10) f3=!f3;
*/