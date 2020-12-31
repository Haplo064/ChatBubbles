using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;
using Dalamud.Game.Chat;
using Dalamud.Game.Chat.SeStringHandling;
using Dalamud.Plugin;
using Dalamud.Game.Command;
using Dalamud.Hooking;
using ImGuiNET;
using System.IO;
using System.Runtime.CompilerServices;
using Dalamud.Configuration;
using Num = System.Numerics;
using System.Runtime.InteropServices;
using System.Collections.Concurrent;
using System.Diagnostics;

namespace ChatBubbles
{
    public class ChatBubbles : IDalamudPlugin
    {
        public string Name => "Chat Bubbles";
        private DalamudPluginInterface pluginInterface;
        public PluginConfiguration Configuration;
        public bool enable = true;
        public List<charData> charDatas = new List<charData>();

#if DEBUG
        public bool config = true;
        public bool debug = true;
#endif

        private static readonly XivChatType[] _channels = new[] { XivChatType.Alliance, XivChatType.Echo, XivChatType.Say };

        [UnmanagedFunctionPointer(CallingConvention.ThisCall, CharSet = CharSet.Ansi)]
        public unsafe delegate IntPtr UpdateBubble(SeBubble* bubble, IntPtr actor, IntPtr dunnoA, IntPtr dunnoB);
        private UpdateBubble UpdateBubbleFunc;
        private Hook<UpdateBubble> UpdateBubbleFuncHook;
        public IntPtr UpdateBubblePtr;

        [UnmanagedFunctionPointer(CallingConvention.ThisCall, CharSet = CharSet.Ansi)]
        public delegate IntPtr OpenBubble(IntPtr self, IntPtr actor, string balloonText, bool notSure);
        private OpenBubble OpenBubbleFunc;
        private Hook<OpenBubble> OpenBubbleFuncHook;
        public IntPtr OpenBubblePtr;


        public unsafe void Initialize(DalamudPluginInterface pluginInterface)
        {



            this.pluginInterface = pluginInterface;
            Configuration = pluginInterface.GetPluginConfig() as PluginConfiguration ?? new PluginConfiguration();

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
                PluginLog.Log("GOOD");
            }
            catch (Exception e)
            { PluginLog.Log("BAD\n" + e.ToString()); }

            OpenBubblePtr = pluginInterface.TargetModuleScanner.ScanText("E8 ?? ?? ?? ?? 80 BF ?? ?? ?? ?? ?? C7 07 ?? ?? ?? ??");
            OpenBubbleFunc = new OpenBubble(OpenBubbleFuncFunc);
            try
            {
                OpenBubbleFuncHook = new Hook<OpenBubble> (OpenBubblePtr, OpenBubbleFunc, this);
                OpenBubbleFuncHook.Enable();
                PluginLog.Log("GOOD2");
            }
            catch (Exception e)
            { PluginLog.Log("BAD\n" + e.ToString()); }
        }

        public unsafe IntPtr UpdateBubbleFuncFunc(SeBubble* bubble, IntPtr actor, IntPtr dunnoA, IntPtr dunnoB)
        {
            /*
            if (bubble->Status == SeBubbleStatus.ON)
            {
                PluginLog.Log($"ID={bubble->Id}");
                PluginLog.Log($"Status={bubble->Status}");
                PluginLog.Log($"Timer={bubble->Timer}");
            }
            */
            foreach(charData cd in charDatas)
            {
                if (actor == cd.actorPtr)
                {
                    //if(debug) PluginLog.Log("Actor found");
                    if (bubble->Status == SeBubbleStatus.OFF)
                    {
                        if (debug) PluginLog.Log("Switch On");
                        bubble->Status = SeBubbleStatus.INIT;
                        bubble->Timer = 3;
                    }

                }
            }

            return UpdateBubbleFuncHook.Original(bubble, actor, dunnoA, dunnoB);
        }

        public unsafe IntPtr OpenBubbleFuncFunc(IntPtr self, IntPtr actor, string balloonText, bool notSure)
        {
            foreach (charData cd in charDatas)
            {
                if (actor == cd.actorPtr)
                {
                    if (debug)
                    {
                        PluginLog.Log("Update ballon text");
                        PluginLog.Log(cd.message);
                    }

                    balloonText = cd.message;
                }
            }
            //PluginLog.Log(balloonText);
            return OpenBubbleFuncHook.Original(self, actor, balloonText, notSure);
        }

        public class PluginConfiguration : IPluginConfiguration
        {
            public int Version { get; set; } = 0;
        }

        public void Dispose()
        {
            this.pluginInterface.Framework.Gui.Chat.OnChatMessage -= Chat_OnChatMessage;
            this.pluginInterface.UiBuilder.OnBuildUi -= BubbleConfigUI;
            this.pluginInterface.UiBuilder.OnOpenConfigUi -= BubbleConfig;
            pluginInterface.CommandManager.RemoveHandler("/bub");
        }

        //What to do when command is called
        private void Command(string command, string arguments)
        {
            config = true;
        }

        //What to do when plugin install config button is pressed
        private void BubbleConfig(object Sender, EventArgs args)
        {
            config = true;
        }

        //What to do with chat messages
        private void Chat_OnChatMessage(XivChatType type, uint senderId, ref SeString sender, ref SeString message, ref bool isHandled)
        {
            /*
if (_channels.Contains(type))
{
    PluginLog.Log(type.ToString() + "|" + message.TextValue);
}
*/
            if (sender.TextValue != "")
            {
                IntPtr actr = GetActorPtr(sender.TextValue);
                if (debug)
                {
                    //PluginLog.Log($"Type={ type.ToString()}");
                    PluginLog.Log($"Sender={ sender.TextValue}");
                    PluginLog.Log($"Message={ message.TextValue}");
                    //PluginLog.Log($"ActorID={actr}");
                }

                if(actr != IntPtr.Zero)
                {
                    bool update = false;
                    foreach (charData cd in charDatas)
                    {
                        if (cd.actorPtr == actr)
                        {
                            if (debug) PluginLog.Log("Update message");
                            cd.message = message.TextValue;
                            cd.DateTime = DateTime.Now;
                            update = true;
                        }
                    }
                    if (!update)
                    {
                        if (debug) PluginLog.Log("Adding new one");

                        charDatas.Add(new charData
                        {
                            actorPtr = actr,
                            DateTime = DateTime.Now,
                            message = message.TextValue,
                            name = sender.TextValue
                        });
                    }
                }
            }
        }

        public IntPtr GetActorPtr(string nameInput)
        {
                for (var k = 0; k < pluginInterface.ClientState.Actors.Length; k++)
                {
                    if (pluginInterface.ClientState.Actors[k] is Dalamud.Game.ClientState.Actors.Types.PlayerCharacter pc)
                    {
                        if (pc.Name == nameInput)
                        {
                            return pc.Address;
                        }
                    }
                }

            return IntPtr.Zero;
        }

        //ConfigUI
        private void BubbleConfigUI()
        {
            if (config)
            {
                ImGui.Begin("Chat Bubbles Config", ref config);
                ImGui.Text("//TODO");
                ImGui.End();
            }

            for(int i = 0; i < charDatas.Count; i++)
            {
                if ((DateTime.Now - charDatas[i].DateTime).TotalSeconds > 2)
                {
                    if (debug) PluginLog.Log("Removing");
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
            public string message;
            public IntPtr actorPtr;
            public DateTime DateTime;
            public string name;
        }
    }
}