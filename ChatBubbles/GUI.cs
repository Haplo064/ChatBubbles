using Dalamud.Game.Text;
using Dalamud.Plugin;
using ImGuiNET;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using Dalamud.Logging;
using Num = System.Numerics;
using FFXIVClientStructs;
using FFXIVClientStructs.FFXIV.Client.Game;
using FFXIVClientStructs.FFXIV.Client.Graphics;
using FFXIVClientStructs.FFXIV.Client.UI.Agent;
using Balloon = FFXIVClientStructs.FFXIV.Client.Game.Balloon;
using FFXIVClientStructs.FFXIV.Client.System.Framework;
using FFXIVClientStructs.FFXIV.Component.GUI;
using Framework = FFXIVClientStructs.FFXIV.Client.System.Framework.Framework;

namespace ChatBubbles
{
    public unsafe partial class ChatBubbles : IDalamudPlugin
    {
        
        private void BubbleConfigUi()
        {
            var log = (AgentScreenLog*)Framework.Instance()->GetUiModule()->GetAgentModule()->GetAgentByInternalId(AgentId.ScreenLog);
            if (bubbleNumber != (int)log->BalloonQueue.MySize)
            {
                //SO CLOSE
                //updateBubbleID(bubbleNumber - (int)log->BalloonQueue.MySize);
                bubbleNumber = (int)log->BalloonQueue.MySize;
            }
            
            if (_config)
            {
                ImGui.SetNextWindowSizeConstraints(new Num.Vector2(620, 650), new Num.Vector2(1920, 1080));
                ImGui.Begin("Chat Bubbles Config", ref _config);

                if (_debug)
                {

                    for (int y = 0; y < 10; y++)
                    {
                        try
                        {
                            
                            var temp = slotsArrayPos(_charDatas[y].BubbleNumber);
                            ImGui.Text(
                                $"i: {y} | A: {bubbleActive[9 - y]} | ID: {slots[9 - temp].ID} | BN: {_charDatas[y].BubbleNumber} | {_charDatas[y].Message}");
                            
 
                        }
                        catch (Exception e)
                        {
                            //lol
                        }
                    }

                    for (int z = 0; z < 10; z++)
                    {
                        try
                        {
                            ImGui.Text($"[{z}] | [{slots[z].ID}] |A: {bubbleActive[z]}");
                        }
                        catch (Exception e)
                        {
                            //lol
                        }
                    }

                    ImGui.Text($"{log->BalloonQueue.MySize}");




                }
                
                ImGui.InputInt("Bubble Timer", ref _timer);
                if (ImGui.IsItemHovered())
                {
                    ImGui.SetTooltip("How long the bubble will last on screen.");
                }
                ImGui.SameLine();
                ImGui.Checkbox("Scale with Text Length", ref _textScale);
                if (ImGui.IsItemHovered())
                {
                    ImGui.SetTooltip(
                        "Base the bubble time on the text length.");
                }
                ImGui.Checkbox("Remove Jitter on self", ref _selfLock);
                if (ImGui.IsItemHovered())
                {
                    ImGui.SetTooltip(
                        "Locks the X value of your own bubbles to remove jitter.");
                }
                ImGui.InputFloat("Bubble Scale", ref _bubbleSize);
                if (ImGui.IsItemHovered())
                {
                    ImGui.SetTooltip("Note that this looks a little janky.");
                }

                ImGui.Checkbox("Debug Logging", ref _debug);
                if (ImGui.IsItemHovered())
                {
                    ImGui.SetTooltip(
                        "Enable logging for debug purposes.\nOnly enable if you are going to share the dalamud log file in discord.");
                }

                ImGui.RadioButton("Queue", ref _bubbleFunctionality, 0);
                if (ImGui.IsItemHovered())
                {
                    ImGui.SetTooltip(
                        "Queue the messages to appear one after the other.");
                }
                ImGui.SameLine();
                ImGui.RadioButton("Stack", ref _bubbleFunctionality, 1);
                if (ImGui.IsItemHovered())
                {
                    ImGui.SetTooltip(
                        "Stack messages into one bubble.");
                }
                ImGui.SameLine();
                ImGui.RadioButton("Replace", ref _bubbleFunctionality, 2);
                if (ImGui.IsItemHovered())
                {
                    ImGui.SetTooltip(
                        "Instantly replace bubbles when new input occurs.");
                }
                ImGui.SameLine();
                ImGui.RadioButton("Sensible", ref _bubbleFunctionality, 3);
                if (ImGui.IsItemHovered())
                {
                    ImGui.SetTooltip(
                        "Stack for the same chat channel, but queue if a new chat channel occurs.");
                }


                if (_bubbleFunctionality == 0 || _bubbleFunctionality == 3)
                {
                    ImGui.InputInt("Maximum Bubble Queue", ref _queue);
                    if (ImGui.IsItemHovered())
                    {
                        ImGui.SetTooltip("How many bubbles can be queued to be seen per person.");
                    }
                }

                ImGui.Checkbox("Hide Your Chat", ref _hide);
                if (ImGui.IsItemHovered())
                {
                    ImGui.SetTooltip("Hides your character's bubble.");
                }

                var i = 0;
                ImGui.Text("Enabled channels:");
                if (ImGui.IsItemHovered())
                {
                    ImGui.SetTooltip("Which chat channels to show bubbles for.");
                }

                ImGui.Columns(2);
                foreach (var e in (XivChatType[]) Enum.GetValues(typeof(XivChatType)))
                {
                    if (_yesno[i])
                    {
                        var txtclr = BitConverter.GetBytes(_textColour[i].Choice);
                        if (ImGui.ColorButton($"Text Colour##{i}", new Num.Vector4(
                            (float) txtclr[3] / 255,
                            (float) txtclr[2] / 255,
                            (float) txtclr[1] / 255,
                            (float) txtclr[0] / 255)))
                        {
                            _chooser = _textColour[i];
                            _picker = true;
                        }
                        ImGui.SameLine();
                        var temp2 = _bubbleColours2[i];
                        ImGui.ColorEdit4($"Bubble Colour2##{i}", ref temp2, ImGuiColorEditFlags.NoInputs|ImGuiColorEditFlags.NoLabel);
                        if (ImGui.IsItemHovered())
                        {
                            ImGui.SetTooltip(
                                "Set the colour of the bubble tint.");
                        }
                        ImGui.SameLine();
                        var temp = _bubbleColours[i];
                        ImGui.ColorEdit4($"Bubble Colour##{i}", ref temp, ImGuiColorEditFlags.NoInputs|ImGuiColorEditFlags.NoLabel);
                        if (ImGui.IsItemHovered())
                        {
                            ImGui.SetTooltip(
                                "Set the colour of the bubble background.");
                        }
                        ImGui.SameLine();

                        var enabled = _channels.Contains(e);
                        if (ImGui.Checkbox($"{e}", ref enabled))
                        {
                            if (enabled) _channels.Add(e);
                            else _channels.Remove(e);
                        }

                        _bubbleColours[i] = temp;
                        _bubbleColours2[i] = temp2;
                        
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
                    Process.Start(new ProcessStartInfo {FileName = "https://ko-fi.com/haplo", UseShellExecute = true});
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
                        (float) temp[3] / 255,
                        (float) temp[2] / 255,
                        (float) temp[1] / 255,
                        (float) temp[0] / 255)))
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
                if (!((DateTime.Now - _charDatas[i].MessageDateTime).TotalMilliseconds > (_timer * 950))) continue;
                _charDatas.RemoveAt(i);
                i--;
            }
            
            var bubblesAtk = new AtkResNode*[10];
            var addonPtr = IntPtr.Zero;
            addonPtr =  Svc.gameGui.GetAddonByName("_MiniTalk",1);
            if (addonPtr != IntPtr.Zero)
            {
                AtkUnitBase* miniTalk2 = (AtkUnitBase*) addonPtr;
                _listOfBubbles = miniTalk2->RootNode;
                bubblesAtk[0] = _listOfBubbles->ChildNode;
                for (int k = 1; k < 10; k++)
                {
                    try
                    {
                        //CHECK IF BEING USED ATM
                        if (bubbleActive[k + 1])
                        {
                            //PluginLog.Log($"CHECKING {k + 1}: ACTIVE");
                            continue;
                        }

                        //PluginLog.Log($"CHECKING {k + 1}: IN-ACTIVE, RESETTING");
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
                    catch (Exception e)
                    {
                        //lol
                    }
                }
            }
            //PluginLog.Log("CLEANUP LOG");
            for (int u = 1; u < 11; u++)
            {
                bubbleActive[u] = false;
            }
        }
    }
}