using System;
using System.Collections.Generic;
using Dalamud.Game.Chat;
using Dalamud.Game.Chat.SeStringHandling;
using Dalamud.Plugin;
using Dalamud.Game.Command;
using Dalamud.Hooking;
using ImGuiNET;
using Dalamud.Configuration;
using System.Runtime.InteropServices;
using Dalamud.Game.Chat.SeStringHandling.Payloads;
using Lumina.Excel.GeneratedSheets;
using Num = System.Numerics;

namespace ChatBubbles
{
    public class ChatBubbles : IDalamudPlugin
    {
        public string Name => "Chat Bubbles";
        DalamudPluginInterface pluginInterface;
        public Config Configuration;
        public bool enable = true;
        public bool picker;
        public List<charData> charDatas = new List<charData>();
        public int timer = 3;
        public UIColorPick chooser;
        public int queue;

#if DEBUG
        public bool config = true;
        public bool debug = true;
#else
        public bool config = false;
        public bool debug = false;
#endif

        public List<XivChatType> _channels = new List<XivChatType>();

        public List<XivChatType> order = new List<XivChatType>
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

        public bool[] yesno = {
            false, false, false, false, true,
            true, true, true, true, true,
            true, true, true, true, true,
            true, true, true, true, true,
            true, true, true, true, true,
            true, false, false, false, false,
            false, false, true, true, true,
            true, true, true, true
        };

        public UIColorPick[] textColour;

        [UnmanagedFunctionPointer(CallingConvention.ThisCall, CharSet = CharSet.Ansi)]
        public unsafe delegate IntPtr UpdateBubble(SeBubble* bubble, IntPtr actor, IntPtr dunnoA, IntPtr dunnoB);
        UpdateBubble UpdateBubbleFunc;
        Hook<UpdateBubble> UpdateBubbleFuncHook;
        public IntPtr UpdateBubblePtr;

        [UnmanagedFunctionPointer(CallingConvention.ThisCall, CharSet = CharSet.Ansi)]
        public delegate IntPtr OpenBubble(IntPtr self, IntPtr actor, IntPtr textPtr, bool notSure);
        OpenBubble OpenBubbleFunc;
        Hook<OpenBubble> OpenBubbleFuncHook;
        public IntPtr OpenBubblePtr;

        public Lumina.Excel.ExcelSheet<UIColor> uiColours;
        public unsafe void Initialize(DalamudPluginInterface pluginInterface)
        {
            this.pluginInterface = pluginInterface;
            uiColours = pluginInterface.Data.Excel.GetSheet<UIColor>();
            Configuration = pluginInterface.GetPluginConfig() as Config ?? new Config();
            timer = Configuration.Timer;
            _channels = Configuration.Channels;
            textColour = Configuration.TextColour;
            queue = Configuration.Queue;

            this.pluginInterface.Framework.Gui.Chat.OnChatMessage += Chat_OnChatMessage;
            this.pluginInterface.UiBuilder.OnBuildUi += BubbleConfigUI;
            this.pluginInterface.UiBuilder.OnOpenConfigUi += BubbleConfig;
            this.pluginInterface.CommandManager.AddHandler("/bub", new CommandInfo(Command)
            {
                HelpMessage = "Opens the Chat Bubble config menu"
            });

            UpdateBubblePtr = pluginInterface.TargetModuleScanner.ScanText("48 85 D2 0F 84 ?? ?? ?? ?? 48 89 5C 24 ?? 57 48 83 EC 20 8B 41 0C");
            UpdateBubbleFunc = new UpdateBubble(UpdateBubbleFuncFunc);
            try
            {
                UpdateBubbleFuncHook = new Hook<UpdateBubble>(UpdateBubblePtr + 0x9, UpdateBubbleFunc, this);
                UpdateBubbleFuncHook.Enable();
                if (debug) PluginLog.Log("GOOD");
            }
            catch (Exception e)
            { PluginLog.Log("BAD\n" + e.ToString()); }

            OpenBubblePtr = pluginInterface.TargetModuleScanner.ScanText("E8 ?? ?? ?? ?? 80 BF ?? ?? ?? ?? ?? C7 07 ?? ?? ?? ??");
            OpenBubbleFunc = new OpenBubble(OpenBubbleFuncFunc);
            try
            {
                OpenBubbleFuncHook = new Hook<OpenBubble>(OpenBubblePtr, OpenBubbleFunc, this);
                OpenBubbleFuncHook.Enable();
                if (debug) PluginLog.Log("GOOD2");
            }
            catch (Exception e)
            { PluginLog.Log("BAD\n" + e.ToString()); }
        }

        public void SaveConfig()
        {
            Configuration.Timer = timer;
            Configuration.Channels = _channels;
            Configuration.TextColour = textColour;
            Configuration.Queue = queue;
            pluginInterface.SavePluginConfig(Configuration);
        }

        public unsafe IntPtr UpdateBubbleFuncFunc(SeBubble* bubble, IntPtr actor, IntPtr dunnoA, IntPtr dunnoB)
        {
            var IdOffset = 116;
            int actorID = Marshal.ReadInt32(actor + IdOffset);

            foreach (charData cd in charDatas)
            {
                if (actorID == cd.actorID)
                {
                    if (bubble->Status == SeBubbleStatus.OFF)
                    {
                        if (debug) PluginLog.Log("Switch On");
                        bubble->Status = SeBubbleStatus.INIT;
                        bubble->Timer = timer;
                    }

                    break;
                }
            }

            return UpdateBubbleFuncHook.Original(bubble, actor, dunnoA, dunnoB);
        }

        public unsafe IntPtr OpenBubbleFuncFunc(IntPtr self, IntPtr actor, IntPtr textPtr, bool notSure)
        {
            var IdOffset = 116;
            int actorID = Marshal.ReadInt32(actor, IdOffset);
            IntPtr newPointer = textPtr;

            foreach (charData cd in charDatas)
            {
                if (actorID == cd.actorID)
                {
                    if (debug)
                    {
                        PluginLog.Log("Update ballon text");
                        PluginLog.Log(cd.message.TextValue);
                    }

                    if (cd.message.TextValue.Length > 0)
                    {
                        var bytes = cd.message.Encode();
                        newPointer = Marshal.AllocHGlobal(bytes.Length + 1);
                        Marshal.Copy(bytes, 0, newPointer, bytes.Length);
                        Marshal.WriteByte(newPointer, bytes.Length, 0);
                        // TODO: Maybe write to game here?
                        // Marshal.WriteByte(textPtr, bytes.Length, 0);
                        textPtr = newPointer;
                    }

                    break;
                }
            }

            return OpenBubbleFuncHook.Original(self, actor, textPtr, notSure);
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
            return pluginInterface.SeStringManager.Parse(bytes);
        }

        public class PluginConfiguration : IPluginConfiguration
        {
            public int Version { get; set; } = 0;
        }

        public void Dispose()
        {
            pluginInterface.Framework.Gui.Chat.OnChatMessage -= Chat_OnChatMessage;
            pluginInterface.UiBuilder.OnBuildUi -= BubbleConfigUI;
            pluginInterface.UiBuilder.OnOpenConfigUi -= BubbleConfig;
            pluginInterface.CommandManager.RemoveHandler("/bub");
            UpdateBubbleFuncHook.Disable();
            OpenBubbleFuncHook.Disable();
        }

        // What to do when command is called
        void Command(string command, string arguments) => config = true;

        // What to do when plugin install config button is pressed
        void BubbleConfig(object Sender, EventArgs args) => config = true;

        // What to do with chat messages
        void Chat_OnChatMessage(XivChatType type, uint senderId, ref SeString sender, ref SeString cmessage, ref bool isHandled)
        {
            if (_channels.Contains(type))
            {
                var fmessage = new SeString(new List<Payload>());
                fmessage.Append(cmessage);

                var PName = "";
                if (sender.Payloads.Count == 1)
                {
                    PName = pluginInterface.ClientState.LocalPlayer.Name;
                }
                else
                {
                    foreach (Payload payload in sender.Payloads)
                    {
                        if (payload.Type == PayloadType.Player)
                        {
                            var pPayload = (PlayerPayload)payload;
                            PName = pPayload.PlayerName;
                        }
                    }
                }

                if (type == XivChatType.StandardEmote || type == XivChatType.CustomEmote)
                {
                    if (cmessage.Payloads[0].Type == PayloadType.Player)
                    {
                        var pPayload = (PlayerPayload)cmessage.Payloads[0];
                        PName = pPayload.PlayerName;
                    }

                    fmessage.Payloads.Insert(0, new EmphasisItalicPayload(true));
                    fmessage.Payloads.Add(new EmphasisItalicPayload(false));
                }

                int actr = GetActorID(PName);

                if (debug)
                {
                    PluginLog.Log($"Type={ type.ToString()}");
                    PluginLog.Log($"Sender={ PName }");
                    PluginLog.Log($"Message Raw={ cmessage.TextValue }");
                    PluginLog.Log($"ActorID={actr}");
                }

                if (type == XivChatType.TellOutgoing)
                {
                    actr = pluginInterface.ClientState.LocalPlayer.ActorId;
                    PName = pluginInterface.ClientState.LocalPlayer.Name;
                }

                fmessage.Payloads.Insert(0, new UIForegroundPayload(pluginInterface.Data, (ushort)textColour[order.IndexOf(type)].option));
                fmessage.Payloads.Add(new UIForegroundPayload(pluginInterface.Data, 0));

                if (actr != 0)
                {
                    int update = 0;
                    TimeSpan time = new TimeSpan(0, 0, 0);
                    int add = 0;

                    foreach (charData cd in charDatas)
                    {
                        if (cd.actorID == actr)
                        {
                            if (debug) PluginLog.Log("Priors found");
                            add += timer;
                            update++;
                        }
                    }

                    if (debug) PluginLog.Log("Adding new one");
                    if (update == 0)
                    {
                        charDatas.Add(new charData
                        {
                            actorID = actr,
                            DateTime = DateTime.Now,
                            message = fmessage,
                            name = PName,
                        });
                    }
                    else
                    {
                        if (debug)
                        {
                            PluginLog.Log(DateTime.Now.Add(time).ToString());
                        }

                        if (update < queue)
                        {
                            time = new TimeSpan(0, 0, add);
                            charDatas.Add(new charData
                            {
                                actorID = actr,
                                DateTime = DateTime.Now.Add(time),
                                message = fmessage,
                                name = PName,
                            });
                        }
                    }
                }
            }
        }

        public int GetActorID(string nameInput)
        {
            for (var k = 0; k < pluginInterface.ClientState.Actors.Length; k++)
            {
                if (pluginInterface.ClientState.Actors[k] is Dalamud.Game.ClientState.Actors.Types.PlayerCharacter pc)
                {
                    if (pc.Name == nameInput) return pc.ActorId;
                }
            }

            return 0;
        }

        // ConfigUI
        void BubbleConfigUI()
        {
            if (config)
            {
                ImGui.SetNextWindowSizeConstraints(new Num.Vector2(620, 640), new Num.Vector2(1920, 1080));
                ImGui.Begin("Chat Bubbles Config", ref config);
                ImGui.InputInt("Bubble Timer", ref timer);
                ImGui.SameLine();
                ImGui.Text("(?)"); if (ImGui.IsItemHovered()) { ImGui.SetTooltip("How long the bubble will last on screen."); }
                ImGui.InputInt("Maximum Bubble Queue", ref queue);
                ImGui.SameLine();
                ImGui.Text("(?)"); if (ImGui.IsItemHovered()) { ImGui.SetTooltip("How many bubbles can be queued to be seen per person."); }
                ImGui.Checkbox("Debug Logging", ref debug);
                ImGui.SameLine();
                ImGui.Text("(?)"); if (ImGui.IsItemHovered()) { ImGui.SetTooltip("Enable logging for debug purposes.\nOnly enable if you are going to share the `dalamud.txt` file in discord."); }
                int i = 0;
                ImGui.Text("Enabled channels:");
                ImGui.SameLine();
                ImGui.Text("(?)"); if (ImGui.IsItemHovered()) { ImGui.SetTooltip("Which chat channels to show bubbles for."); }
                ImGui.Columns(2);
                foreach (var e in (XivChatType[])Enum.GetValues(typeof(XivChatType)))
                {
                    if (yesno[i])
                    {
                        var txtclr = BitConverter.GetBytes(textColour[i].choice);
                        if (ImGui.ColorButton($"Text Colour##{i}", new Num.Vector4(
                            (float)txtclr[3] / 255,
                            (float)txtclr[2] / 255,
                            (float)txtclr[1] / 255,
                            (float)txtclr[0] / 255)))
                        {
                            chooser = textColour[i];
                            picker = true;
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
                    config = false;
                }

                ImGui.End();
            }

            if (picker)
            {
                ImGui.SetNextWindowSizeConstraints(new Num.Vector2(320, 440), new Num.Vector2(640, 880));
                ImGui.Begin("UIColor Picker", ref picker);
                ImGui.Columns(10, "##columnsID", false);
                foreach (var z in uiColours)
                {
                    var temp = BitConverter.GetBytes(z.UIForeground);
                    if (ImGui.ColorButton(z.RowId.ToString(), new Num.Vector4(
                        (float)temp[3] / 255,
                        (float)temp[2] / 255,
                        (float)temp[1] / 255,
                        (float)temp[0] / 255)))
                    {
                        chooser.choice = z.UIForeground;
                        chooser.option = z.RowId;
                        picker = false;
                    }

                    ImGui.NextColumn();
                }

                ImGui.Columns(1);
                ImGui.End();
            }

            for (int i = 0; i < charDatas.Count; i++)
            {
                if ((DateTime.Now - charDatas[i].DateTime).TotalSeconds > (double)timer - (0.5 * (double)timer))
                {
                    // if (debug) PluginLog.Log("Removing");
                    charDatas.RemoveAt(i);
                    i--;
                }
            }
        }

        public enum SeBubbleStatus : uint
        {
            GET_DATA = 0,
            ON = 1,
            INIT = 2,
            OFF = 3
        }
        public struct SeBubble
        {
            public uint Id;
            public float Timer;
            public uint Dunno;
            public SeBubbleStatus Status;
            // Pretty sure there's more shit but I don't think it's relevant
        };

        public class charData
        {
            public SeString message;
            public int actorID;
            public DateTime DateTime;
            public string name;
        }
    }

    public class UIColorPick
    {
        public uint choice { get; set; }
        public uint option { get; set; }
    }

    public class Config : IPluginConfiguration
    {
        public int Version { get; set; } = 0;
        public List<XivChatType> Channels { get; set; } = new List<XivChatType>();
        public int Timer { get; set; } = 7;
        public UIColorPick[] TextColour { get; set; } =
        {
            new UIColorPick { choice = 0, option =0 }, new UIColorPick { choice = 0, option =0 }, new UIColorPick { choice = 0, option =0 },
            new UIColorPick { choice = 0, option =0 }, new UIColorPick { choice = 0, option =0 }, new UIColorPick { choice = 0, option =0 },
            new UIColorPick { choice = 0, option =0 }, new UIColorPick { choice = 0, option =0 }, new UIColorPick { choice = 0, option =0 },
            new UIColorPick { choice = 0, option =0 }, new UIColorPick { choice = 0, option =0 }, new UIColorPick { choice = 0, option =0 },
            new UIColorPick { choice = 0, option =0 }, new UIColorPick { choice = 0, option =0 }, new UIColorPick { choice = 0, option =0 },
            new UIColorPick { choice = 0, option =0 }, new UIColorPick { choice = 0, option =0 }, new UIColorPick { choice = 0, option =0 },
            new UIColorPick { choice = 0, option =0 }, new UIColorPick { choice = 0, option =0 }, new UIColorPick { choice = 0, option =0 },
            new UIColorPick { choice = 0, option =0 }, new UIColorPick { choice = 0, option =0 }, new UIColorPick { choice = 0, option =0 },
            new UIColorPick { choice = 0, option =0 }, new UIColorPick { choice = 0, option =0 }, new UIColorPick { choice = 0, option =0 },
            new UIColorPick { choice = 0, option =0 }, new UIColorPick { choice = 0, option =0 }, new UIColorPick { choice = 0, option =0 },
            new UIColorPick { choice = 0, option =0 }, new UIColorPick { choice = 0, option =0 }, new UIColorPick { choice = 0, option =0 },
            new UIColorPick { choice = 0, option =0 }, new UIColorPick { choice = 0, option =0 }, new UIColorPick { choice = 0, option =0 },
            new UIColorPick { choice = 0, option =0 }, new UIColorPick { choice = 0, option =0 }, new UIColorPick { choice = 0, option =0 }
        };
        public int Queue { get; set; } = 3;
    }
}