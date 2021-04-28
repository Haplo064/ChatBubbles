using Dalamud.Configuration;
using Dalamud.Game.Text;
using Dalamud.Game.Text.SeStringHandling;
using Dalamud.Game.Text.SeStringHandling.Payloads;
using Dalamud.Game.Command;
using Dalamud.Hooking;
using Dalamud.Plugin;
using ImGuiNET;
using Lumina.Excel.GeneratedSheets;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.InteropServices;
using Dalamud.Game.ClientState.Actors.Types;
using Num = System.Numerics;

namespace ChatBubbles
{
    public class ChatBubbles : IDalamudPlugin
    {
        public string Name => "Chat Bubbles";
        private DalamudPluginInterface _pluginInterface;
        private Config _configuration;
        private bool _picker;
        private readonly List<CharData> _charDatas = new List<CharData>();
        private int _timer = 3;
        private UiColorPick _chooser;
        private int _queue;
        private bool _stack;
        private bool _hide;

#if DEBUG
        private bool _config = true;
        private bool _debug = true;
#else
        private bool _config = false;
        private bool _debug = false;
#endif

        private List<XivChatType> _channels = new List<XivChatType>();

        private readonly List<XivChatType> _order = new List<XivChatType>
        {
            XivChatType.None, XivChatType.None, XivChatType.None, XivChatType.None, XivChatType.Say,
            XivChatType.Shout, XivChatType.TellOutgoing, XivChatType.TellIncoming, XivChatType.Party, XivChatType.Alliance,
            XivChatType.Ls1, XivChatType.Ls2, XivChatType.Ls3, XivChatType.Ls4, XivChatType.Ls5,
            XivChatType.Ls6, XivChatType.Ls7, XivChatType.Ls8, XivChatType.FreeCompany, XivChatType.NoviceNetwork,
            XivChatType.CustomEmote, XivChatType.StandardEmote, XivChatType.Yell, XivChatType.CrossParty, XivChatType.PvPTeam,
            XivChatType.CrossLinkShell1, XivChatType.None, XivChatType.None, XivChatType.None, XivChatType.None,
            XivChatType.None, XivChatType.None, XivChatType.CrossLinkShell2, XivChatType.CrossLinkShell3, XivChatType.CrossLinkShell4,
            XivChatType.CrossLinkShell5, XivChatType.CrossLinkShell6, XivChatType.CrossLinkShell7, XivChatType.CrossLinkShell8
        };

        private readonly bool[] _yesno = {
            false, false, false, false, true,
            true, true, true, true, true,
            true, true, true, true, true,
            true, true, true, true, true,
            true, true, true, true, true,
            true, false, false, false, false,
            false, false, true, true, true,
            true, true, true, true
        };


        private UiColorPick[] _textColour;

        [UnmanagedFunctionPointer(CallingConvention.ThisCall, CharSet = CharSet.Ansi)]
        private unsafe delegate IntPtr UpdateBubble(SeBubble* bubble, IntPtr actor, IntPtr dunnoA, IntPtr dunnoB);

        private UpdateBubble _updateBubbleFunc;
        private Hook<UpdateBubble> _updateBubbleFuncHook;
        private IntPtr _updateBubblePtr;

        [UnmanagedFunctionPointer(CallingConvention.ThisCall, CharSet = CharSet.Ansi)]
        private delegate IntPtr OpenBubble(IntPtr self, IntPtr actor, IntPtr textPtr, bool notSure);

        private OpenBubble _openBubbleFunc;
        private Hook<OpenBubble> _openBubbleFuncHook;
        private IntPtr _openBubblePtr;

        private Lumina.Excel.ExcelSheet<UIColor> _uiColours;

        public unsafe void Initialize(DalamudPluginInterface pluginInterface)
        {
            _pluginInterface = pluginInterface;
            _uiColours = pluginInterface.Data.Excel.GetSheet<UIColor>();
            _configuration = pluginInterface.GetPluginConfig() as Config ?? new Config();
            _timer = _configuration.Timer;
            _channels = _configuration.Channels;
            _textColour = _configuration.TextColour;
            _queue = _configuration.Queue;
            _stack = _configuration.Stack;
            _hide = _configuration.Hide;

            _pluginInterface.Framework.Gui.Chat.OnChatMessage += Chat_OnChatMessage;
            _pluginInterface.UiBuilder.OnBuildUi += BubbleConfigUi;
            _pluginInterface.UiBuilder.OnOpenConfigUi += BubbleConfig;
            _pluginInterface.CommandManager.AddHandler("/bub", new CommandInfo(Command)
            {
                HelpMessage = "Opens the Chat Bubble config menu"
            });

            _updateBubblePtr = pluginInterface.TargetModuleScanner.ScanText("48 85 D2 0F 84 ?? ?? ?? ?? 48 89 5C 24 ?? 57 48 83 EC 20 8B 41 0C");
            _updateBubbleFunc = UpdateBubbleFuncFunc;
            try
            {
                _updateBubbleFuncHook = new Hook<UpdateBubble>(_updateBubblePtr + 0x9, _updateBubbleFunc, this);
                _updateBubbleFuncHook.Enable();
                if (_debug) PluginLog.Log("GOOD");
            }
            catch (Exception e)
            { PluginLog.Log("BAD\n" + e); }

            _openBubblePtr = pluginInterface.TargetModuleScanner.ScanText("E8 ?? ?? ?? ?? 80 BF ?? ?? ?? ?? ?? C7 07 ?? ?? ?? ??");
            _openBubbleFunc = OpenBubbleFuncFunc;
            try
            {
                _openBubbleFuncHook = new Hook<OpenBubble>(_openBubblePtr, _openBubbleFunc, this);
                _openBubbleFuncHook.Enable();
                if (_debug) PluginLog.Log("GOOD2");
            }
            catch (Exception e)
            { PluginLog.Log("BAD\n" + e); }
        }

        private void SaveConfig()
        {
            _configuration.Timer = _timer;
            _configuration.Channels = _channels;
            _configuration.TextColour = _textColour;
            _configuration.Queue = _queue;
            _configuration.Stack = _stack;
            _configuration.Hide = _hide;
            _pluginInterface.SavePluginConfig(_configuration);
        }

        private unsafe IntPtr UpdateBubbleFuncFunc(SeBubble* bubble, IntPtr actor, IntPtr dunnoA, IntPtr dunnoB)
        {
            const int idOffset = 116;
            var actorId = Marshal.ReadInt32(actor + idOffset);

            foreach (var cd in _charDatas.Where(cd => actorId == cd.actorId))
            {
                if (bubble->Status == SeBubbleStatus.Off)
                {
                    if (_debug)
                    {
                        PluginLog.Log("Switch On");
                        PluginLog.Log($"ActorID: {cd.actorId}");
                    }

                    bubble->Status = SeBubbleStatus.Init;
                    bubble->Timer = _timer;
                }

                if (bubble->Status == SeBubbleStatus.On && cd.Stack)
                {
                    bubble->Status = SeBubbleStatus.Off;
                    bubble->Timer = 0;
                    cd.Stack = false;
                }
                break;
            }

            return _updateBubbleFuncHook.Original(bubble, actor, dunnoA, dunnoB);
        }

        private IntPtr OpenBubbleFuncFunc(IntPtr self, IntPtr actor, IntPtr textPtr, bool notSure)
        {
            const int idOffset = 116;
            var actorId = Marshal.ReadInt32(actor, idOffset);

            foreach (var cd in _charDatas.Where(cd => actorId == cd.actorId))
            {
                if (_debug)
                {
                    PluginLog.Log("Update ballon text");
                    PluginLog.Log(cd.message.TextValue);
                }

                if (cd.message.TextValue.Length > 0)
                {
                    var bytes = cd.message.Encode();
                    var newPointer = Marshal.AllocHGlobal(bytes.Length + 1);
                    Marshal.Copy(bytes, 0, newPointer, bytes.Length);
                    Marshal.WriteByte(newPointer, bytes.Length, 0);
                    textPtr = newPointer;
                }

                break;
            }

            return _openBubbleFuncHook.Original(self, actor, textPtr, notSure);
        }

        public unsafe SeString GetSeStringFromPtr(byte* ptr)
        {
            var offset = 0;
            while (true)
            {
                var b = *(ptr + offset);
                if (b == 0) break;
                offset += 1;
            }

            var bytes = new byte[offset];
            Marshal.Copy(new IntPtr(ptr), bytes, 0, offset);
            return _pluginInterface.SeStringManager.Parse(bytes);
        }

        public class PluginConfiguration : IPluginConfiguration
        {
            public int Version { get; set; } = 0;
        }

        public void Dispose()
        {
            _pluginInterface.Framework.Gui.Chat.OnChatMessage -= Chat_OnChatMessage;
            _pluginInterface.UiBuilder.OnBuildUi -= BubbleConfigUi;
            _pluginInterface.UiBuilder.OnOpenConfigUi -= BubbleConfig;
            _pluginInterface.CommandManager.RemoveHandler("/bub");
            _updateBubbleFuncHook.Disable();
            _openBubbleFuncHook.Disable();
        }

        // What to do when command is called
        private void Command(string command, string arguments) => _config = true;

        // What to do when plugin install config button is pressed
        private void BubbleConfig(object sender, EventArgs args) => _config = true;

        // What to do with chat messages
        private void Chat_OnChatMessage(XivChatType type, uint senderId, ref SeString sender, ref SeString cmessage, ref bool isHandled)
        {
            if (isHandled) return;
            if (!_channels.Contains(type)) return;
            var fmessage = new SeString(new List<Payload>());
            var nline = new SeString(new List<Payload>());
            nline.Payloads.Add(new TextPayload("\n"));
            fmessage.Append(cmessage);
            
            //Checking for ChatTranslator things
            var skip = 0;
            if (cmessage.Payloads[0].Type == PayloadType.UIForeground && cmessage.Payloads[1].Type == PayloadType.UIForeground)
            {
                skip += 2;
            }
            var pName = _pluginInterface.ClientState.LocalPlayer.Name;
            if (sender.Payloads[0+skip].Type == PayloadType.Player)
            {
                var pPayload = (PlayerPayload) sender.Payloads[0+skip];
                pName = pPayload.PlayerName;
            }

            if (sender.Payloads[0+skip].Type == PayloadType.Icon && sender.Payloads[1].Type == PayloadType.Player)
            {
                var pPayload = (PlayerPayload) sender.Payloads[1];
                pName = pPayload.PlayerName;
            }

            if (type == XivChatType.StandardEmote || type == XivChatType.CustomEmote)
            {
                if (cmessage.Payloads[0+skip].Type == PayloadType.Player)
                {
                    var pPayload = (PlayerPayload) cmessage.Payloads[0+skip];
                    pName = pPayload.PlayerName;
                }

                fmessage.Payloads.Insert(0, new EmphasisItalicPayload(true));
                fmessage.Payloads.Add(new EmphasisItalicPayload(false));
            }

            var actr = GetActorId(pName);

            if (_debug)
            {
                PluginLog.Log($"Type={type}");
                PluginLog.Log($"Sender={pName}");
                PluginLog.Log($"Message Raw={cmessage.TextValue}");
                PluginLog.Log($"ActorID={actr}");
            }

            if (type == XivChatType.TellOutgoing)
            {
                actr = _pluginInterface.ClientState.LocalPlayer.ActorId;
                pName = _pluginInterface.ClientState.LocalPlayer.Name;
            }

            fmessage.Payloads.Insert(0,
                new UIForegroundPayload(_pluginInterface.Data, (ushort) _textColour[_order.IndexOf(type)].Option));
            fmessage.Payloads.Add(new UIForegroundPayload(_pluginInterface.Data, 0));

            if (actr == 0) return;
            var update = 0;
            var time = new TimeSpan(0, 0, 0);
            var add = 0;

            foreach (var cd in _charDatas)
            {
                if (_debug) PluginLog.Log($"Check: {actr}, Against: {cd.actorId}");

                if (cd.actorId != actr) continue;
                if (_debug) PluginLog.Log("Priors found");
                add += _timer;
                update++;

                if (!_stack) continue;
                update = 99999;
                cd.message.Append(nline);
                cd.message.Append(fmessage);
                cd.Stack = true;
                cd.dateTime = DateTime.Now;
            }


            if (update == 0)
            {
                if (_debug) PluginLog.Log("Adding new one");
                _charDatas.Add(new CharData
                {
                    actorId = actr,
                    dateTime = DateTime.Now,
                    message = fmessage,
                    name = pName,
                });
            }
            else
            {
                if (_debug)
                {
                    PluginLog.Log(DateTime.Now.Add(time).ToString(CultureInfo.CurrentCulture));
                }

                if (update >= _queue) return;
                time = new TimeSpan(0, 0, add);
                _charDatas.Add(new CharData
                {
                    actorId = actr,
                    dateTime = DateTime.Now.Add(time),
                    message = fmessage,
                    name = pName,
                });
            }
        }

        private int GetActorId(string nameInput)
        {
            if (_hide)
            {
                if (nameInput == _pluginInterface.ClientState.LocalPlayer.Name) return 0;
            }
            foreach (var t in _pluginInterface.ClientState.Actors)
            {
                if (!(t is PlayerCharacter pc)) continue;
                if (pc.Name == nameInput) return pc.ActorId;
            }
            return 0;
        }

        // ConfigUI
        private void BubbleConfigUi()
        {
            if (_config)
            {
                ImGui.SetNextWindowSizeConstraints(new Num.Vector2(620, 640), new Num.Vector2(1920, 1080));
                ImGui.Begin("Chat Bubbles Config", ref _config);
                ImGui.InputInt("Bubble Timer", ref _timer);
                ImGui.SameLine();
                ImGui.Text("(?)"); if (ImGui.IsItemHovered()) { ImGui.SetTooltip("How long the bubble will last on screen."); }
                ImGui.InputInt("Maximum Bubble Queue", ref _queue);
                ImGui.SameLine();
                ImGui.Text("(?)"); if (ImGui.IsItemHovered()) { ImGui.SetTooltip("How many bubbles can be queued to be seen per person."); }
                ImGui.Checkbox("Debug Logging", ref _debug);
                ImGui.SameLine();
                ImGui.Text("(?)"); if (ImGui.IsItemHovered()) { ImGui.SetTooltip("Enable logging for debug purposes.\nOnly enable if you are going to share the `dalamud.txt` file in discord."); }
                ImGui.Checkbox("Stack Messages", ref _stack);
                ImGui.SameLine();
                ImGui.Text("(?)"); if (ImGui.IsItemHovered()) { ImGui.SetTooltip("Instead of queueing bubbles, this option instead 'stacks' them inside the one bubble."); }
                ImGui.Checkbox("Hide Yours", ref _hide);
                ImGui.SameLine();
                ImGui.Text("(?)"); if (ImGui.IsItemHovered()) { ImGui.SetTooltip("Hides your character's bubble."); }
                var i = 0;
                ImGui.Text("Enabled channels:");
                ImGui.SameLine();
                ImGui.Text("(?)"); if (ImGui.IsItemHovered()) { ImGui.SetTooltip("Which chat channels to show bubbles for."); }
                ImGui.Columns(2);
                foreach (var e in (XivChatType[])Enum.GetValues(typeof(XivChatType)))
                {
                    if (_yesno[i])
                    {
                        var txtclr = BitConverter.GetBytes(_textColour[i].Choice);
                        if (ImGui.ColorButton($"Text Colour##{i}", new Num.Vector4(
                            (float)txtclr[3] / 255,
                            (float)txtclr[2] / 255,
                            (float)txtclr[1] / 255,
                            (float)txtclr[0] / 255)))
                        {
                            _chooser = _textColour[i];
                            _picker = true;
                        }

                        ImGui.SameLine();

                        var enabled = _channels.Contains(e);
                        if (ImGui.Checkbox($"{e}", ref enabled))
                        {
                            if (enabled) _channels.Add(e);
                            else _channels.Remove(e);
                        }

                        ImGui.NextColumn();
                    }

                    i++;
                }

                ImGui.Columns(1);

                if (ImGui.Button("Save and Close Config"))
                {
                    SaveConfig();
                    _config = false;
                }
                ImGui.SameLine();
                ImGui.PushStyleColor(ImGuiCol.Button, 0xFF000000 | 0x005E5BFF);
                ImGui.PushStyleColor(ImGuiCol.ButtonActive, 0xDD000000 | 0x005E5BFF);
                ImGui.PushStyleColor(ImGuiCol.ButtonHovered, 0xAA000000 | 0x005E5BFF);

                if (ImGui.Button("Buy Haplo a Hot Chocolate"))
                {
                    System.Diagnostics.Process.Start("https://ko-fi.com/haplo");
                }
                ImGui.PopStyleColor(3);

                ImGui.End();
            }

            if (_picker)
            {
                ImGui.SetNextWindowSizeConstraints(new Num.Vector2(320, 440), new Num.Vector2(640, 880));
                ImGui.Begin("UIColor Picker", ref _picker);
                ImGui.Columns(10, "##columnsID", false);
                foreach (var z in _uiColours)
                {
                    var temp = BitConverter.GetBytes(z.UIForeground);
                    if (ImGui.ColorButton(z.RowId.ToString(), new Num.Vector4(
                        (float)temp[3] / 255,
                        (float)temp[2] / 255,
                        (float)temp[1] / 255,
                        (float)temp[0] / 255)))
                    {
                        _chooser.Choice = z.UIForeground;
                        _chooser.Option = z.RowId;
                        _picker = false;
                    }

                    ImGui.NextColumn();
                }

                ImGui.Columns(1);
                ImGui.End();
            }

            for (var i = 0; i < _charDatas.Count; i++)
            {
                if (!((DateTime.Now - _charDatas[i].dateTime).TotalMilliseconds > (_timer * 950))) continue;
                _charDatas.RemoveAt(i);
                i--;
            }
        }

        private enum SeBubbleStatus : uint
        {
            GetData = 0,
            On = 1,
            Init = 2,
            Off = 3
        }

        [StructLayout(LayoutKind.Explicit, Size = 0x80)]
        private struct SeBubble
        {
            [FieldOffset(0x0)] private readonly uint Id;
            [FieldOffset(0x4)] public float Timer;
            [FieldOffset(0x8)] private readonly uint Unk_8; // enum probably
            [FieldOffset(0xC)] public SeBubbleStatus Status; // state of the bubble
            [FieldOffset(0x10)] private readonly ulong Text;
            [FieldOffset(0x78)] private readonly ulong Unk_78; // check whats in memory here
        }

        private class CharData
        {
            public SeString message;
            public int actorId;
            public DateTime dateTime;
            public string name;
            public bool Stack { get; set; }
            public bool Hide { get; set; }
        }
    }

    public class UiColorPick
    {
        public uint Choice { get; set; }
        public uint Option { get; set; }
    }

    public class Config : IPluginConfiguration
    {
        public int Version { get; set; } = 0;
        public List<XivChatType> Channels { get; set; } = new List<XivChatType>();
        public int Timer { get; set; } = 7;
        public bool Stack { get; set; }

        public UiColorPick[] TextColour { get; set; } =
        {
            new UiColorPick { Choice = 0, Option =0 }, new UiColorPick { Choice = 0, Option =0 }, new UiColorPick { Choice = 0, Option =0 },
            new UiColorPick { Choice = 0, Option =0 }, new UiColorPick { Choice = 0, Option =0 }, new UiColorPick { Choice = 0, Option =0 },
            new UiColorPick { Choice = 0, Option =0 }, new UiColorPick { Choice = 0, Option =0 }, new UiColorPick { Choice = 0, Option =0 },
            new UiColorPick { Choice = 0, Option =0 }, new UiColorPick { Choice = 0, Option =0 }, new UiColorPick { Choice = 0, Option =0 },
            new UiColorPick { Choice = 0, Option =0 }, new UiColorPick { Choice = 0, Option =0 }, new UiColorPick { Choice = 0, Option =0 },
            new UiColorPick { Choice = 0, Option =0 }, new UiColorPick { Choice = 0, Option =0 }, new UiColorPick { Choice = 0, Option =0 },
            new UiColorPick { Choice = 0, Option =0 }, new UiColorPick { Choice = 0, Option =0 }, new UiColorPick { Choice = 0, Option =0 },
            new UiColorPick { Choice = 0, Option =0 }, new UiColorPick { Choice = 0, Option =0 }, new UiColorPick { Choice = 0, Option =0 },
            new UiColorPick { Choice = 0, Option =0 }, new UiColorPick { Choice = 0, Option =0 }, new UiColorPick { Choice = 0, Option =0 },
            new UiColorPick { Choice = 0, Option =0 }, new UiColorPick { Choice = 0, Option =0 }, new UiColorPick { Choice = 0, Option =0 },
            new UiColorPick { Choice = 0, Option =0 }, new UiColorPick { Choice = 0, Option =0 }, new UiColorPick { Choice = 0, Option =0 },
            new UiColorPick { Choice = 0, Option =0 }, new UiColorPick { Choice = 0, Option =0 }, new UiColorPick { Choice = 0, Option =0 },
            new UiColorPick { Choice = 0, Option =0 }, new UiColorPick { Choice = 0, Option =0 }, new UiColorPick { Choice = 0, Option =0 }
        };

        public int Queue { get; set; } = 3;
    }
}
