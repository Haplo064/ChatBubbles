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

        private void OnceUponAFrame(object _)
        {
            var bubblesAtk2 = new AtkResNode*[10];
            var addonPtr2 = IntPtr.Zero;
            addonPtr2 =  Svc.gameGui.GetAddonByName("_MiniTalk",1);
            if (addonPtr2 != IntPtr.Zero)
            {
                AtkUnitBase* miniTalk2 = (AtkUnitBase*) addonPtr2;
                _listOfBubbles = miniTalk2->RootNode;
                bubblesAtk2[0] = _listOfBubbles->ChildNode;
                for (int k = 1; k < 10; k++)
                {
                    try
                    {
                        bubblesAtk2[k] = bubblesAtk2[k - 1]->PrevSiblingNode;
                        //CHECK IF BEING USED ATM
                        if (bubbleActive[k])
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
                            
                            PluginLog.Log($"{k}");
                            var colour = GetBubbleColour(bubbleActiveType[k]);
                            var colour2 = GetBubbleColour2(bubbleActiveType[k]);
                            
                            bubblesAtk2[k]->AddRed = (ushort) (colour2.X * 255);
                            bubblesAtk2[k]->AddGreen = (ushort) (colour2.Y * 255);
                            bubblesAtk2[k]->AddBlue = (ushort) (colour2.Z * 255);
                            bubblesAtk2[k]->ScaleX = _bubbleSize;
                            bubblesAtk2[k]->ScaleY = _bubbleSize;
                            
                            var resNodeNineGrid = ((AtkComponentNode*) bubblesAtk2[k])->Component->UldManager
                                .SearchNodeById(5);
                            var resNodeDangly = ((AtkComponentNode*) bubblesAtk2[k])->Component->UldManager
                                .SearchNodeById(4);

                            resNodeDangly->Color.R = (byte) (colour.X * 255);
                            resNodeDangly->Color.G = (byte) (colour.Y * 255);
                            resNodeDangly->Color.B = (byte) (colour.Z * 255);
                            resNodeNineGrid->Color.R = (byte) (colour.X * 255);
                            resNodeNineGrid->Color.G = (byte) (colour.Y * 255);
                            resNodeNineGrid->Color.B = (byte) (colour.Z * 255);
                        }
                        else
                        {
                            
                            bubblesAtk2[k]->AddRed = 0;
                            bubblesAtk2[k]->AddBlue = 0;
                            bubblesAtk2[k]->AddGreen = 0;
                            bubblesAtk2[k]->ScaleX = defaultScale;
                            bubblesAtk2[k]->ScaleY = defaultScale;
                            
                            var resNodeNineGrid = ((AtkComponentNode*) bubblesAtk2[k])->Component->UldManager
                                .SearchNodeById(5);
                            var resNodeDangly = ((AtkComponentNode*) bubblesAtk2[k])->Component->UldManager
                                .SearchNodeById(4);

                            resNodeDangly->Color.R = (byte) (255);
                            resNodeDangly->Color.G = (byte) (255);
                            resNodeDangly->Color.B = (byte) (255);
                            resNodeNineGrid->Color.R = (byte) (255);
                            resNodeNineGrid->Color.G = (byte) (255);
                            resNodeNineGrid->Color.B = (byte) (255);
                        }

                        //PluginLog.Log($"CHECKING {k + 1}: IN-ACTIVE, RESETTING");

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
                bubbleActiveType[u] = XivChatType.Debug;
            }
            
        }
    }
}