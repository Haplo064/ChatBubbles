using Dalamud.Game.Text;
using Dalamud.Plugin;
using ImGuiNET;
using System;
using System.Diagnostics;
using Num = System.Numerics;


namespace ChatBubbles
{
    public unsafe partial class ChatBubbles : IDalamudPlugin
    {
        
        private void BubbleConfigUi()
        {
            if (_config)
            {
                ImGui.SetNextWindowSizeConstraints(new Num.Vector2(600, 850), new Num.Vector2(1920, 1080));
                ImGui.Begin("Chat Bubbles Config", ref _config);
                
                
                ImGui.Checkbox("Show Bubbles", ref _switch);
                if (ImGui.IsItemHovered())
                {
                    ImGui.SetTooltip(
                        "This is here in case you used '/bub toggle' and forgot about doing it.");
                }
                ImGui.SameLine();
                ImGui.Checkbox("Hide Your Chat", ref _hide);
                if (ImGui.IsItemHovered())
                {
                    ImGui.SetTooltip("Hides your own character's bubbles.");
                }
                //ImGui.SameLine();
                //ImGui.Checkbox("Ass Bubbles", ref _assBubbles);
                //if (ImGui.IsItemHovered())
                //{
                //    ImGui.SetTooltip("Oh no, ass bubbles are back!");
                //}
                ImGui.Checkbox("Display friends only", ref _friendsOnly);
                if (ImGui.IsItemHovered())
                {
                    ImGui.SetTooltip("Hides the bubbles from every character that isn't in your friend list. Disables when in an instance.");
				}
				ImGui.SameLine();
				ImGui.Checkbox("Display FC only", ref _fcOnly);
                if (ImGui.IsItemHovered())
                {
                    ImGui.SetTooltip("Hides the bubbles from every character that isn't in your Free Company. Disables when in an instance.");
				}
				ImGui.SameLine();
				ImGui.Checkbox("Display party only", ref _partyOnly);
                if (ImGui.IsItemHovered())
                {
                    ImGui.SetTooltip("Hides the bubbles from every character that isn't in your current party. Disables when in an instance.");
                }
                ImGui.Checkbox("Debug Logging", ref _debug);
                if (ImGui.IsItemHovered())
                {
                    ImGui.SetTooltip(
                        "Enable logging for debug purposes.\nOnly enable if you are going to share the dalamud log file in discord.");
                }

                if (_debug)
                {
                    ImGui.Text("DEBUG Info");
                    ImGui.Text($"Player's bubble's position: {_playerBubbleX}");
                    try
                    {
                        foreach (CharData cd in _charDatas)
                        {
                            ImGui.Text($"Time since last message: {(DateTime.Now - cd.MessageDateTime).TotalMilliseconds}");
                            ImGui.Text($"Bubbles displayed: {cd.Message}");
                            //TODO : unsure about what this is for now, need to check it out
                            //ImGui.Text($"KillMe status: {cd.KillMe}");
                        }

                        // Unsure about why it's there
                        //ImGui.Text($"{_timer * 500}");
                    }
                    catch (Exception e)
                    {
                        ImGui.Text($"Error while fetching config in debug: {e}");
                    }
                }

                // SPACING
                ImGui.NewLine();
                ImGui.Separator();
                // SPACING

                ImGui.InputInt("Bubble Timer", ref _timer);
                if (ImGui.IsItemHovered())
                {
                    ImGui.SetTooltip("How long the bubble will last on screen.");
                }
                //Introduced a lot of issues
                /*
                ImGui.SameLine();
                ImGui.Checkbox("Scale with Text Length", ref _textScale);
                if (ImGui.IsItemHovered())
                {
                    ImGui.SetTooltip(
                        "Base the bubble time on the text length.");
                }
                */
                //Jitter occurs more than once a draw frame?
                /*
                ImGui.Checkbox("Remove Jitter on self", ref _selfLock);
                if (ImGui.IsItemHovered())
                {
                    ImGui.SetTooltip(
                        "Locks the X value of your own bubbles to remove jitter.");
                }
                */
                ImGui.InputFloat("Players Bubble Scale", ref _bubbleSize);
                if (ImGui.IsItemHovered())
                {
                    ImGui.SetTooltip("Scales player chat bubbles. ONLY WORKS IF DEFAULT UI IS NOT SET TO 100%!");
                }
                ImGui.InputFloat("NPC Bubble scale", ref _defaultScale);
                if (ImGui.IsItemHovered())
                {
                    ImGui.SetTooltip("Scales NPC Chat bubbles. ONLY WORKS IF DEFAULT UI IS NOT SET TO 100%!");
                }
                ImGui.InputInt("Yalm distance", ref _yalmCap);
                if (ImGui.IsItemHovered())
                {
                    ImGui.SetTooltip("Anything over this distance in Yalms will not be shown");
                }

                // SPACING
                ImGui.NewLine();
                // SPACING

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
                
                ImGui.SameLine();
                if (!_pride)
                {
                    ImGui.PushStyleVar(ImGuiStyleVar.Alpha, 0);
                    ImGui.Checkbox("Pride", ref _pride);
                    ImGui.PopStyleVar();
                }
                else
                {
                    ImGui.Checkbox("Pride", ref _pride);
                }

                if (ImGui.IsItemHovered())
                {
                    ImGui.SetTooltip(":O");
                }


                // SPACING
                ImGui.NewLine();
                // SPACING

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
                        ImGui.ColorEdit4($"Bubble tint##{i}", ref temp2, ImGuiColorEditFlags.NoInputs|ImGuiColorEditFlags.NoLabel);
                        if (ImGui.IsItemHovered())
                        {
                            ImGui.SetTooltip(
                                "Set the colour of the bubble tint.");
                        }
                        ImGui.SameLine();
                        var temp = _bubbleColours[i];
                        ImGui.ColorEdit4($"Bubble background##{i}", ref temp, ImGuiColorEditFlags.NoInputs|ImGuiColorEditFlags.NoLabel);
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


                // SPACING
                ImGui.NewLine();
                // SPACING

                ImGui.Separator();

                // SPACING
                ImGui.NewLine();
                // SPACING

                if (ImGui.Button("Save and Close Config"))
                {
                    SaveConfig();
                    _config = false;
                }

                ImGui.SameLine();
                ImGui.PushStyleColor(ImGuiCol.Button, 0xFF000000 | 0x005E5BFF);
                ImGui.PushStyleColor(ImGuiCol.ButtonActive, 0xDD000000 | 0x005E5BFF);
                ImGui.PushStyleColor(ImGuiCol.ButtonHovered, 0xAA000000 | 0x005E5BFF);
                ImGui.Text(" ");
                ImGui.SameLine();


                if (ImGui.Button("Buy Khayle some good croissants"))
                {
                    Process.Start(new ProcessStartInfo {FileName = "https://ko-fi.com/khayle", UseShellExecute = true});
                }

                ImGui.PopStyleColor(3);
                ImGui.End();

                if (_dirtyHack > 60)
                {
                    SaveConfig();
                    _dirtyHack = 0;
                }

                _dirtyHack++;

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


        }
    }
}