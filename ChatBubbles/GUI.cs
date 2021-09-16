using Dalamud.Game.Text;
using Dalamud.Plugin;
using ImGuiNET;
using System;
using Num = System.Numerics;

namespace ChatBubbles
{
    public unsafe partial class ChatBubbles : IDalamudPlugin
    {
        private void BubbleConfigUi()
        {
            if (_config)
            {
                ImGui.SetNextWindowSizeConstraints(new Num.Vector2(620, 650), new Num.Vector2(1920, 1080));
                ImGui.Begin("Chat Bubbles Config", ref _config);
                ImGui.InputInt("Bubble Timer", ref _timer);
                ImGui.SameLine();
                ImGui.Text("(?)"); if (ImGui.IsItemHovered()) { ImGui.SetTooltip("How long the bubble will last on screen."); }

                ImGui.Checkbox("Debug Logging", ref _debug);
                ImGui.SameLine();
                ImGui.Text("(?)"); if (ImGui.IsItemHovered()) { ImGui.SetTooltip("Enable logging for debug purposes.\nOnly enable if you are going to share the `dalamud.txt` file in discord."); }
                ImGui.RadioButton("Queue", ref _bubbleFunctionality, 0);
                ImGui.SameLine();
                ImGui.RadioButton("Stack", ref _bubbleFunctionality, 1);
                ImGui.SameLine();
                ImGui.RadioButton("Replace", ref _bubbleFunctionality, 2);
                ImGui.SameLine();
                ImGui.Text("(?)"); if (ImGui.IsItemHovered()) { ImGui.SetTooltip("How do you want bubbles to function?\nQueue messages, stack messages, or insta-replace messages?"); }

                if (_bubbleFunctionality == 0)
                {
                    ImGui.InputInt("Maximum Bubble Queue", ref _queue);
                    ImGui.SameLine();
                    ImGui.Text("(?)");
                    if (ImGui.IsItemHovered())
                    {
                        ImGui.SetTooltip("How many bubbles can be queued to be seen per person.");
                    }
                }

                ImGui.Checkbox("Hide Your Chat", ref _hide);
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

                ImGui.Text("Shoutout to Lanselotto and Pastah for some additional ideas and code.");
                
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
                if (!((DateTime.Now - _charDatas[i].MessageDateTime).TotalMilliseconds > (_timer * 950))) continue;
                _charDatas.RemoveAt(i);
                i--;
            }
        }
    }
}