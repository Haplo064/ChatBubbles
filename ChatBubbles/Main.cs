using Dalamud.Configuration;
using Dalamud.Game.Text;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Game.Command;
using Dalamud.Hooking;
using Dalamud.Plugin;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.InteropServices;
using Dalamud.Data;
using Dalamud.Game.ClientState;
using Dalamud.Game.ClientState.Objects;
using Dalamud.Game.ClientState.Objects.SubKinds;
using Dalamud.Game.Gui;
using Dalamud.Logging;
using Lumina.Excel.GeneratedSheets;
using ImGuiNET;
using System.Numerics;
using System.Text;
using Dalamud.Game;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Game.Text.SeStringHandling.Payloads;

using FFXIVClientStructs;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using Balloon = FFXIVClientStructs.FFXIV.Client.Game.Balloon;
using FFXIVClientStructs.FFXIV.Component.GUI;
using FFXIVClientStructs.FFXIV.Client.System.Framework;
using FFXIVClientStructs.FFXIV.Client.System.String;
using FFXIVClientStructs.STD;
using Framework = FFXIVClientStructs.FFXIV.Client.System.Framework.Framework;
using Num = System.Numerics;

namespace ChatBubbles
{
    internal class UIColorComparer : IEqualityComparer<UIColor>
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
        private bool _textScale;
        public List<Num.Vector4> _bubbleColours;
        public List<Num.Vector4> _bubbleColours2;
        private float defaultScale = 0f;
        private float _bubbleSize;
        private bool _selfLock;
        private AtkResNode* _listOfBubbles;
        private int _playerBubble = 99;
        private int _playerBubbleX = 0;

#if DEBUG
        private bool _config = true;
        private bool _debug = true;
#else
        private bool _config = false;
        private bool _debug = false;
#endif

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
            XivChatType.CrossLinkShell1, XivChatType.None, XivChatType.None, XivChatType.None, XivChatType.None,
            XivChatType.None, XivChatType.None, XivChatType.CrossLinkShell2, XivChatType.CrossLinkShell3,
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
            true, false, false, false, false,
            false, false, true, true, true,
            true, true, true, true
        };

        private int bubbleNumber = 0;

        private bool[] bubbleActive = {false, false, false, false, false, false, false, false, false, false, false};

        private BalloonSlotState[] slots = new BalloonSlotState[10];
        private AtkResNode*[] bubblesAtk = new AtkResNode*[10];
        
        private readonly UiColorPick[] _textColour;


        [UnmanagedFunctionPointer(CallingConvention.ThisCall, CharSet = CharSet.Ansi)]
        //private delegate IntPtr UpdateBubble(SeBubble* bubble, IntPtr actor, IntPtr dunnoA, IntPtr dunnoB);
        private delegate IntPtr UpdateBubble(Balloon* bubble, IntPtr actor, IntPtr dunnoA, IntPtr dunnoB);

        //private UpdateBubble _updateBubbleFunc;
        private readonly Hook<UpdateBubble> _updateBubbleFuncHook;
        //private IntPtr _updateBubblePtr;

        [UnmanagedFunctionPointer(CallingConvention.ThisCall, CharSet = CharSet.Ansi)]
        private delegate IntPtr OpenBubble(IntPtr self, IntPtr actor, IntPtr textPtr, bool notSure);

        private readonly Hook<OpenBubble> _openBubbleFuncHook;
        

        public ChatBubbles(DalamudPluginInterface pluginInt)
        {
            pluginInt.Create<Svc>();
 
            _configuration = Svc.pluginInterface.GetPluginConfig() as Config ?? new Config();
            _timer = _configuration.Timer;
            _channels = _configuration.Channels;
            _textColour = _configuration.TextColour;
            _queue = _configuration.Queue;
            _bubbleFunctionality = _configuration.BubbleFunctionality;
            _hide = _configuration.Hide;
            _textScale = _configuration.TextScale;
            _bubbleColours = _configuration.BubbleColours;
            _bubbleColours2 = _configuration.BubbleColours2;
            _bubbleSize = _configuration.BubbleSize;
            _selfLock = _configuration.SelfLock;
            while (_bubbleColours.Count < 39) _bubbleColours.Add(new Vector4(1,1,1,0));
            while (_bubbleColours2.Count < 39) _bubbleColours2.Add(new Vector4(0,0,0,0));
            
            var list = new List<UIColor>(Svc.dataManager.Excel.GetSheet<UIColor>()!.Distinct(new UIColorComparer()));
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


            Svc.chatGui.ChatMessage += Chat_OnChatMessage;
            Svc.pluginInterface.UiBuilder.Draw += BubbleConfigUi;
            Svc.pluginInterface.UiBuilder.OpenConfigUi += BubbleConfig;
            Svc.commandManager.AddHandler("/bub", new CommandInfo(Command)
            {
                HelpMessage = "Opens the Chat Bubble config menu"
            });
            
            var updateBubblePtr = Svc.sigScannerD.ScanText("48 85 D2 0F 84 ?? ?? ?? ?? 48 89 5C 24 ?? 57 48 83 EC 20 8B 41 0C");
            UpdateBubble updateBubbleFunc = UpdateBubbleFuncFunc;
            try
            {
                _updateBubbleFuncHook = new Hook<UpdateBubble>(updateBubblePtr + 0x9, updateBubbleFunc);
                _updateBubbleFuncHook.Enable();
                if (_debug) PluginLog.Log("GOOD");
            }
            catch (Exception e)
            { PluginLog.Log("BAD\n" + e); }

            
            var openBubblePtr = Svc.sigScannerD.ScanText("E8 ?? ?? ?? ?? F6 86 ?? ?? ?? ?? ?? C7 46 ?? ?? ?? ?? ??");
            OpenBubble openBubbleFunc = OpenBubbleFuncFunc;
            try
            {
                _openBubbleFuncHook = new Hook<OpenBubble>(openBubblePtr, openBubbleFunc);
                _openBubbleFuncHook.Enable();
                if (_debug) PluginLog.Log("GOOD2");
            }
            catch (Exception e)
            { PluginLog.Log("BAD\n" + e); }
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
            _configuration.BubbleColours = _bubbleColours;
            _configuration.BubbleColours2 = _bubbleColours2;
            _configuration.TextScale = _textScale;
            _configuration.BubbleSize = _bubbleSize;
            _configuration.SelfLock = _selfLock;
            Svc.pluginInterface.SavePluginConfig(_configuration);
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
            var log = (AgentScreenLog*)Framework.Instance()->GetUiModule()->GetAgentModule()->GetAgentByInternalId(AgentId.ScreenLog);
            var slots = new BalloonSlotState[10];
            for (int k = 0; k < 10; k++)
            {
                slots[k] = new BalloonSlotState();
            }
            
            for (ulong j = 0; j < log->BalloonQueue.MySize; j++)
            {
                var balloonInfo = log->BalloonQueue.Get(j);
                slots[balloonInfo.Slot].ID = balloonInfo.BalloonId;
                slots[balloonInfo.Slot].Active = true;
            }
                    
                    
            var addonPtr = IntPtr.Zero;
            addonPtr =  Svc.gameGui.GetAddonByName("_MiniTalk",1);
            if (addonPtr != IntPtr.Zero)
            {
                if (defaultScale == 0f)
                {
                    AtkUnitBase* miniTalkTemp = (AtkUnitBase*) addonPtr;
                    defaultScale = miniTalkTemp->RootNode->ChildNode->ScaleX;
                }

                AtkUnitBase* miniTalk = (AtkUnitBase*) addonPtr;
                _listOfBubbles = miniTalk->RootNode;
                

                    bubblesAtk[0] = _listOfBubbles->ChildNode;
                    for (int k = 1; k < 10; k++)
                    {
                        bubblesAtk[k] = bubblesAtk[k - 1]->PrevSiblingNode;
                        bubblesAtk[k]->AddRed = 0;
                        bubblesAtk[k]->AddBlue = 0;
                        bubblesAtk[k]->AddGreen = 0;
                        bubblesAtk[k]->ScaleX = defaultScale;
                        bubblesAtk[k]->ScaleY = defaultScale;
                        var resNodeNineGrid = ((AtkComponentNode*) bubblesAtk[k])->Component->UldManager
                            .SearchNodeById(5);
                        var resNodeDangly = ((AtkComponentNode*) bubblesAtk[k])->Component->UldManager
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
        
            var log = (AgentScreenLog*)Framework.Instance()->GetUiModule()->GetAgentModule()->GetAgentByInternalId(AgentId.ScreenLog);
            
            for (int k = 0; k < 10; k++)
            {
                slots[k] = new BalloonSlotState();
            }


            for (ulong j = 0; j < log->BalloonQueue.MySize; j++)
            {
                var balloonInfo = log->BalloonQueue.Get(j);

                slots[9 - j].ID = balloonInfo.BalloonId - 1;
                slots[9 - j].Active = true;
            }

            var addonPtr = IntPtr.Zero;
            addonPtr =  Svc.gameGui.GetAddonByName("_MiniTalk",1);
            if (addonPtr != IntPtr.Zero)
            {
                if (defaultScale == 0f)
                {
                    AtkUnitBase* miniTalkTemp = (AtkUnitBase*) addonPtr;
                    defaultScale = miniTalkTemp->RootNode->ChildNode->ScaleX;
                }

                AtkUnitBase* miniTalk = (AtkUnitBase*) addonPtr;
                _listOfBubbles = miniTalk->RootNode;


                bubblesAtk[0] = _listOfBubbles->ChildNode;
                
            }

            const int idOffset = 116;
            var actorId = Marshal.ReadInt32(actor + idOffset);

            var counter = 0;
            foreach (var cd in _charDatas.Where(cd => actorId == cd.ActorId))
            {
                if (bubble->State == BalloonState.Inactive)
                {
                    
                    if (_debug)
                    {
                        PluginLog.Log("Switch On");
                        PluginLog.Log($"ActorID: {cd.ActorId}");
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
                            bubble->PlayTimer = (float)(_timer * val);
                        }
                        
                    }
                    else
                    {
                        bubble->PlayTimer = _timer;
                    }
                    
                    cd.BubbleNumber = log->BalloonCounter;
                }
                
                if (bubble->State == BalloonState.Active && cd.NewMessage)
                {
                    bubble->State = BalloonState.Inactive;
                    bubble->PlayTimer = 0;
                    cd.NewMessage = false;
                }

                if (bubble->State == BalloonState.Active)
                {
                    if (addonPtr != IntPtr.Zero)
                    {
                        bubblesAtk[0] = _listOfBubbles->ChildNode;
                        for (int k = 1; k < 10; k++)
                        {
                            bubblesAtk[k] = bubblesAtk[k - 1]->PrevSiblingNode;
                        }
                        for (int i = 0; i < 10; i++)
                        {
                            if (bubblesAtk[i]->IsVisible)
                            {
                                
                                var temp = slotsArrayPos(cd.BubbleNumber);
                                if (cd.BubbleNumber == slots[9-temp].ID && i + temp == 9)
                                {
                                    
                                    //PluginLog.Log($"BN: {cd.BubbleNumber} == {slots[9-temp].ID} for loop: {i}");
                                    //Trying to lock down the jiggle for own bubbles :|
                                    /*
                                    try
                                    {
                                        if (cd.Name==Svc.clientState.LocalPlayer?.Name.TextValue)
                                        {
                                            if (_playerBubble != i)
                                            {
                                                PluginLog.Log($"Locking at {i}, with X being {(int)bubblesAtk[i]->X}");
                                                _playerBubble = i;
                                                _playerBubbleX = (int)bubblesAtk[i]->X;
                                                PluginLog.Log($"Locked at {_playerBubble} | {_playerBubbleX}");
                                            }

                                            if (_selfLock)
                                            {
                                                PluginLog.Log($"X is currently: {bubblesAtk[i]->X}");
                                                bubblesAtk[i]->X=_playerBubbleX;
                                                PluginLog.Log($"X is now: {bubblesAtk[i]->X}");
                                            }
                                        }
                                    }
                                    catch (Exception e)
                                    {
                                        PluginLog.Verbose($"{e}");
                                        throw;
                                    }
                                    */
                                    
                                    var resNodeNineGrid = ((AtkComponentNode*) bubblesAtk[i])->Component->UldManager
                                        .SearchNodeById(5);
                                    var resNodeDangly = ((AtkComponentNode*) bubblesAtk[i])->Component->UldManager
                                        .SearchNodeById(4);
                                    var colour = GetBubbleColour(cd.Type);
                                    var colour2 = GetBubbleColour2(cd.Type);

                                    bubbleActive[i] = true;
                                    counter++;
                                    PluginLog.Log($"Changing BUBBLE AT: {i} | [{counter}]");
                                    resNodeDangly->Color.R = (byte) (colour.X * 255);
                                    resNodeDangly->Color.G = (byte) (colour.Y * 255);
                                    resNodeDangly->Color.B = (byte) (colour.Z * 255);
                                    resNodeNineGrid->Color.R = (byte) (colour.X * 255);
                                    resNodeNineGrid->Color.G = (byte) (colour.Y * 255);
                                    resNodeNineGrid->Color.B = (byte) (colour.Z * 255);
                                    bubblesAtk[i]->AddRed = (ushort) (colour2.X * 255);
                                    bubblesAtk[i]->AddGreen = (ushort) (colour2.Y * 255);
                                    bubblesAtk[i]->AddBlue = (ushort) (colour2.Z * 255);
                                    bubblesAtk[i]->ScaleX = _bubbleSize;
                                    bubblesAtk[i]->ScaleY = _bubbleSize;
                                    
                                }
                            }
                        }
                    }
                }
                break;
            }

            counter = 0;


            return _updateBubbleFuncHook.Original(bubble, actor, dunnoA, dunnoB);
        }

        private int slotsArrayPos(int number)
        {
            int value = 0;
            for (int i = 0; i < 10; i++)
            {
                
                if (number == slots[i].ID && number!=0)
                {
                    value= 9 - i;
                    //PluginLog.Log($"Found {number} at position {i}, returning: {value}");
                    break;
                }
            }
            return value;
        }
        
        private void updateBubbleID(int number)
        {
            foreach(var cd in _charDatas)
            {
                cd.BubbleNumber -= number;
            }
        }

        private IntPtr OpenBubbleFuncFunc(IntPtr self, IntPtr actor, IntPtr textPtr, bool notSure)
        {
            const int idOffset = 116;
            var actorId = Marshal.ReadInt32(actor, idOffset);

            foreach (var cd in _charDatas.Where(cd => actorId == cd.ActorId))
            {
                if (_debug)
                {
                    PluginLog.Log("--Update balloon text--");
                    PluginLog.Log(cd.Message.TextValue);
                }

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

            return _openBubbleFuncHook.Original(self, actor, textPtr, notSure);
        }

        void System.IDisposable.Dispose()
        {
            Svc.chatGui.ChatMessage -= Chat_OnChatMessage;
            Svc.pluginInterface.UiBuilder.Draw -= BubbleConfigUi;
            Svc.pluginInterface.UiBuilder.OpenConfigUi -= BubbleConfig;
            Svc.commandManager.RemoveHandler("/bub");
            _updateBubbleFuncHook.Disable();
            _openBubbleFuncHook.Disable();
            cleanBubbles();
        }

        private void BubbleConfig() => _config = true;


        // What to do when command is called
        private void Command(string command, string arguments) => _config = true;
        
 

        private uint GetActorId(string nameInput)
        {
            if (_hide && nameInput == Svc.clientState.LocalPlayer?.Name.TextValue) return 0;

            foreach (var t in Svc.objectTable)
            {
                if (!(t is PlayerCharacter pc)) continue;
                if (pc.Name.TextValue == nameInput) return pc.ObjectId;
            }
            return 0;
        }

        //Doesn't even do the check. If I need this I'll fix it.
        private bool isActorNPC(IntPtr actorPointer)
        {
            foreach (var t in Svc.objectTable)
            {
                PluginLog.Log($"{actorPointer}");
                if (t.Address == actorPointer)
                {
                    PluginLog.Log($"!!!!!!!!!Match!!!!!!!!!!!");
                    PluginLog.Log($"{t.ObjectKind}");
                }
                else PluginLog.Log($"Nope");
            }
            return false;
        }
        
        private class CharData
        {
            public SeString? Message;
            public XivChatType Type;
            public uint ActorId;
            public DateTime MessageDateTime;
            public string? Name;
            public bool NewMessage { get; set; }
            public int BubbleNumber;
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
        public bool TextScale { get; set; } = false;
        public bool SelfLock { get; set; } = false;
        public float BubbleSize { get; set; } = 1f;

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
            new() { Choice = 0, Option =0 }, new() { Choice = 0, Option =0 }, new () { Choice = 0, Option =0 }
        };

        public List<Vector4> BubbleColours { get; set; } = new List<Num.Vector4>();
        public List<Vector4> BubbleColours2 { get; set; } = new List<Num.Vector4>();

        public int Queue { get; set; } = 3;
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