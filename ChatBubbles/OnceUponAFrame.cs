using Dalamud.Game.Text;
using Dalamud.Plugin;
using System;
using Dalamud.Logging;
using Num = System.Numerics;
using Dalamud.Plugin.Services;

namespace ChatBubbles
{
    public unsafe partial class ChatBubbles : IDalamudPlugin
    {

        private void OnceUponAFrame(IFramework framework)
        {
            try
            {
                // Populate the miniTalks if there are any bubbles 
                var addonPtr2 = IntPtr.Zero;
                addonPtr2 = Services.GameGui.GetAddonByName("_MiniTalk", 1);
                if (addonPtr2 != IntPtr.Zero)
                {
                    AddonMiniTalk* miniTalk = (AddonMiniTalk*) addonPtr2;
                    _bubblesAtk2[0] = miniTalk->ChatBubble0;
                    _bubblesAtk2[1] = miniTalk->ChatBubble1;
                    _bubblesAtk2[2] = miniTalk->ChatBubble2;
                    _bubblesAtk2[3] = miniTalk->ChatBubble3;
                    _bubblesAtk2[4] = miniTalk->ChatBubble4;
                    _bubblesAtk2[5] = miniTalk->ChatBubble5;
                    _bubblesAtk2[6] = miniTalk->ChatBubble6;
                    _bubblesAtk2[7] = miniTalk->ChatBubble7;
                    _bubblesAtk2[8] = miniTalk->ChatBubble8;
                    _bubblesAtk2[9] = miniTalk->ChatBubble9;
                }
                else
                {
                    for (int k = 0; k < 10; k++)
                    {
                        _bubblesAtk2[k] = null;
                    }
                }


                try
                {
                    for (int k = 0; k < 10; k++)
                    {
                        if (_bubblesAtk2[k] == null)
                        {
                            break;
                        }
                        if (_bubblesAtk2[k]->IsVisible() && _bubbleActive[k])
                        {
                            if (_playerBubble == k && _selfLock)
                            {
                                if (_playerBubbleX == 0)
                                {
                                    _playerBubbleX = _bubblesAtk2[k]->X;
                                }

                                //bubblesAtk2[k]->SetPositionFloat(_playerBubbleX,bubblesAtk2[k]->Y);
                            }
                        
                            var colour = GetBubbleColour(_bubbleActiveType[k]);
                            var colour2 = GetBubbleColour2(_bubbleActiveType[k]);

                            // Pride mode, relic haplo code
                            if (!_pride)
                            {
                                    _bubblesAtk2[k]->AddRed = (short) (colour2.X * 255);
                                    _bubblesAtk2[k]->AddGreen = (short) (colour2.Y * 255);
                                    _bubblesAtk2[k]->AddBlue = (short) (colour2.Z * 255);
                                    var resNodeNineGrid = _bubblesAtk2[k]->GetComponent()->UldManager.SearchNodeById(5);
                                    var resNodeDangly = _bubblesAtk2[k]->GetComponent()->UldManager.SearchNodeById(4);
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
                                
                                if (_f1)
                                {
                                    _bubblesAtk2[k]->AddBlue += (short)rand.Next(0, 2);
                                }
                                else
                                {
                                    _bubblesAtk2[k]->AddBlue -= (short)rand.Next(0, 2);
                                }

                                if (_f2)
                                {
                                    _bubblesAtk2[k]->AddRed += (short)rand.Next(0, 2);
                                }
                                else
                                {
                                    _bubblesAtk2[k]->AddRed -= (short)rand.Next(0, 2);
                                }

                                if (_f3)
                                {
                                    _bubblesAtk2[k]->AddGreen += (short)rand.Next(0, 2);
                                }
                                else
                                {
                                    _bubblesAtk2[k]->AddGreen -= (short)rand.Next(0, 2);
                                }

                                if (_bubblesAtk2[k]->AddBlue >= 100)
                                {
                                    _bubblesAtk2[k]->AddBlue = 100;
                                    _f1=!_f1;
                                }

                                if (_bubblesAtk2[k]->AddRed >= 100)
                                {
                                    _bubblesAtk2[k]->AddRed = 100;
                                    _f2=!_f2;
                                }

                                if (_bubblesAtk2[k]->AddGreen >= 100)
                                {
                                    _bubblesAtk2[k]->AddGreen = 100;
                                    _f3=!_f3;
                                }
                            
                                if(_bubblesAtk2[k]->AddBlue<=10) _f1=!_f1;
                                if(_bubblesAtk2[k]->AddRed<=10) _f2=!_f2;
                                if(_bubblesAtk2[k]->AddGreen<=10) _f3=!_f3;
                            }

                            _bubblesAtk2[k]->ScaleX = _bubbleSize;
                            _bubblesAtk2[k]->ScaleY = _bubbleSize;

                        }
                        else if (_bubblesAtk2[k]->IsVisible())
                        {
                            _bubblesAtk2[k]->AddRed = 0;
                            _bubblesAtk2[k]->AddBlue = 0;
                            _bubblesAtk2[k]->AddGreen = 0;
                            _bubblesAtk2[k]->ScaleX = _defaultScale;
                            _bubblesAtk2[k]->ScaleY = _defaultScale;

                            var component = _bubblesAtk2[k]->GetComponent();
                            ref var uldManager = ref component->UldManager;
                            var resNodeNineGrid = uldManager.SearchNodeById(5);
                            var resNodeDangly = uldManager.SearchNodeById(4);

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
                    Services.PluginLog.Error($"Error while updating frame: {e}");
                }

                if (addonPtr2 != IntPtr.Zero && _bubblesAtk2 is not null)
                {
                    for (int u = 0; u < 10; u++)
                    {
                        if (_bubblesAtk2[u] is null)
                        {
                            break;
                        }

                        if (!_bubblesAtk2[u]->IsVisible())
                        {
                            _bubbleActive[u] = false;
                            _bubbleActiveType[u] = XivChatType.Debug;
                        }

                        if (_playerBubble < 10)
                        {
                            if (!_bubblesAtk2[_playerBubble]->IsVisible())
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
                Services.PluginLog.Error($"Error while populating the bubbles: {e}");
            }
            
            //Cleaning charDatas
            for (var i = 0; i < _charDatas.Count; i++)
            {
                if ( (DateTime.Now - _charDatas[i].MessageDateTime).TotalMilliseconds > (_timer * 950) || _charDatas[i].KillMe)
                {
                    _charDatas.RemoveAt(i);
                    i--;
                }

            }
        }
    }
}