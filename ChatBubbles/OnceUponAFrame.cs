using Dalamud.Game.Text;
using Dalamud.Plugin;
using System;
using Dalamud.Logging;
using Num = System.Numerics;

namespace ChatBubbles
{
    public unsafe partial class ChatBubbles : IDalamudPlugin
    {

        private void OnceUponAFrame(object _)
        {
            try
            {
                var addonPtr2 = IntPtr.Zero;
                addonPtr2 = Svc.gameGui.GetAddonByName("_MiniTalk", 1);
                if (addonPtr2 != IntPtr.Zero)
                {
                    AddonMiniTalk* miniTalk = (AddonMiniTalk*) addonPtr2;
                    bubblesAtk2[0] = miniTalk->ChatBubble0;
                    bubblesAtk2[1] = miniTalk->ChatBubble1;
                    bubblesAtk2[2] = miniTalk->ChatBubble2;
                    bubblesAtk2[3] = miniTalk->ChatBubble3;
                    bubblesAtk2[4] = miniTalk->ChatBubble4;
                    bubblesAtk2[5] = miniTalk->ChatBubble5;
                    bubblesAtk2[6] = miniTalk->ChatBubble6;
                    bubblesAtk2[7] = miniTalk->ChatBubble7;
                    bubblesAtk2[8] = miniTalk->ChatBubble8;
                    bubblesAtk2[9] = miniTalk->ChatBubble9;
                }
                else
                {
                    for (int k = 0; k < 10; k++)
                    {
                        bubblesAtk2[k] = null;
                    }
                }


                try
                {
                    for (int k = 0; k < 10; k++)
                    {
                        if (bubblesAtk2[k] == null)
                        {
                            break;
                        }
                        if (bubblesAtk2[k]->IsVisible && bubbleActive[k])
                        {
                            if (_playerBubble == k && _selfLock)
                            {
                                if (_playerBubbleX == 0)
                                {
                                    _playerBubbleX = bubblesAtk2[k]->X;
                                }

                                //bubblesAtk2[k]->SetPositionFloat(_playerBubbleX,bubblesAtk2[k]->Y);
                            }
                        
                            var colour = GetBubbleColour(bubbleActiveType[k]);
                            var colour2 = GetBubbleColour2(bubbleActiveType[k]);

                            if (!pride)
                            {
                                    bubblesAtk2[k]->AddRed = (ushort) (colour2.X * 255);
                                    bubblesAtk2[k]->AddGreen = (ushort) (colour2.Y * 255);
                                    bubblesAtk2[k]->AddBlue = (ushort) (colour2.Z * 255);
                                    var resNodeNineGrid = bubblesAtk2[k]->GetComponent()->UldManager.SearchNodeById(5);
                                    var resNodeDangly = bubblesAtk2[k]->GetComponent()->UldManager.SearchNodeById(4);
                                    resNodeDangly->Color.R = (byte) (colour.X * 255);
                                    resNodeDangly->Color.G = (byte) (colour.Y * 255);
                                    resNodeDangly->Color.B = (byte) (colour.Z * 255);

                                    resNodeNineGrid->Color.R = (byte) (colour.X * 255);
                                    resNodeNineGrid->Color.G = (byte) (colour.Y * 255);
                                    resNodeNineGrid->Color.B = (byte) (colour.Z * 255);
                            }
                            else
                            {
                                var rand = new Random();
                                
                                if (f1)
                                {
                                    bubblesAtk2[k]->AddBlue += (ushort)rand.Next(0, 2);
                                }
                                else
                                {
                                    bubblesAtk2[k]->AddBlue -= (ushort)rand.Next(0, 2);
                                }

                                if (f2)
                                {
                                    bubblesAtk2[k]->AddRed += (ushort)rand.Next(0, 2);
                                }
                                else
                                {
                                    bubblesAtk2[k]->AddRed -= (ushort)rand.Next(0, 2);
                                }

                                if (f3)
                                {
                                    bubblesAtk2[k]->AddGreen += (ushort)rand.Next(0, 2);
                                }
                                else
                                {
                                    bubblesAtk2[k]->AddGreen -= (ushort)rand.Next(0, 2);
                                }

                                if (bubblesAtk2[k]->AddBlue >= 100)
                                {
                                    bubblesAtk2[k]->AddBlue = 100;
                                    f1=!f1;
                                }

                                if (bubblesAtk2[k]->AddRed >= 100)
                                {
                                    bubblesAtk2[k]->AddRed = 100;
                                    f2=!f2;
                                }

                                if (bubblesAtk2[k]->AddGreen >= 100)
                                {
                                    bubblesAtk2[k]->AddGreen = 100;
                                    f3=!f3;
                                }
                            
                                if(bubblesAtk2[k]->AddBlue<=10) f1=!f1;
                                if(bubblesAtk2[k]->AddRed<=10) f2=!f2;
                                if(bubblesAtk2[k]->AddGreen<=10) f3=!f3;
                            }


                            if (_debug)
                            {
                                PluginLog.Log($"SxB: {bubblesAtk2[k]->ScaleX} | SyB: {bubblesAtk2[k]->ScaleY}");
                                PluginLog.Log($"dfs: {_bubbleSize}");
                            }
                            bubblesAtk2[k]->ScaleX = _bubbleSize;
                            bubblesAtk2[k]->ScaleY = _bubbleSize;
                            if (_debug)
                            {
                                PluginLog.Log($"SxA: {bubblesAtk2[k]->ScaleX} | SyA: {bubblesAtk2[k]->ScaleY}");
                            }



                        }
                        else if (bubblesAtk2[k]->IsVisible)
                        {
                            if (_debug)
                            {
                                PluginLog.Log($"SxB: {bubblesAtk2[k]->ScaleX} | SyB: {bubblesAtk2[k]->ScaleY}");
                                PluginLog.Log($"dfs: {_defaultScale}");
                            }
                            bubblesAtk2[k]->AddRed = 0;
                            bubblesAtk2[k]->AddBlue = 0;
                            bubblesAtk2[k]->AddGreen = 0;
                            bubblesAtk2[k]->ScaleX = _defaultScale;
                            bubblesAtk2[k]->ScaleY = _defaultScale;
                            if (_debug)
                            {
                                PluginLog.Log($"SxA: {bubblesAtk2[k]->ScaleX} | SyA: {bubblesAtk2[k]->ScaleY}");
                            }


                            var resNodeNineGrid = bubblesAtk2[k]->GetComponent()->UldManager.SearchNodeById(5);
                            var resNodeDangly = bubblesAtk2[k]->GetComponent()->UldManager.SearchNodeById(4);

                            resNodeDangly->Color.R = (byte) (255);
                            resNodeDangly->Color.G = (byte) (255);
                            resNodeDangly->Color.B = (byte) (255);
                            resNodeNineGrid->Color.R = (byte) (255);
                            resNodeNineGrid->Color.G = (byte) (255);
                            resNodeNineGrid->Color.B = (byte) (255);

                        }
                    }
                }
                catch (Exception e)
                {
                    PluginLog.Log($"Error while updating frame: {e}");
                }

                if (addonPtr2 != IntPtr.Zero && bubblesAtk2 is not null)
                {
                    for (int u = 0; u < 10; u++)
                    {
                        if (bubblesAtk2[u] is null)
                        {
                            break;
                        }

                        if (!bubblesAtk2[u]->IsVisible)
                        {
                            bubbleActive[u] = false;
                            bubbleActiveType[u] = XivChatType.Debug;
                        }

                        if (_playerBubble < 10)
                        {
                            if (!bubblesAtk2[_playerBubble]->IsVisible)
                            {
                                _playerBubble = 99;
                                _playerBubbleX = 0;
                            }
                        }

                    }
                }
            }
            catch (Exception e)
            {
                PluginLog.Log($"Error before updating frame: {e}");
            }
            
            //Cleaning charDatas
            for (var i = 0; i < _charDatas.Count; i++)
            {
                if ( (DateTime.Now - _charDatas[i].MessageDateTime).TotalMilliseconds > (_timer * 950) || _charDatas[i].KillMe)
                {
                    if (_debug)
                    {
                        PluginLog.Log(
                            $"{(DateTime.Now - _charDatas[i].MessageDateTime).TotalMilliseconds} > {_timer * 950}");
                        PluginLog.Log($"Removing [{i}]: {_charDatas[i].Message.TextValue}");
                    }

                    _charDatas.RemoveAt(i);
                    i--;
                }

            }
        }
    }
}