using klime.PointCheck;
using Math0424.ShipPoints;
using Sandbox.Game.Entities;
using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using VRage.Game.ModAPI;
using static Math0424.Networking.MyEasyNetworkManager;
using VRage.Utils;
using Sandbox.Game;

namespace Math0424.Networking
{
    class MyNetworkHandler : IDisposable
    {

        public MyEasyNetworkManager MyNetwork;
        public static MyNetworkHandler Static;
		private static List<ulong> all_players = new List<ulong>();
        private static List<IMyPlayer> listPlayers = new List<IMyPlayer>();

        public static void Init()
        {
            if (Static == null)
            {
                Static = new MyNetworkHandler();
            }
        }

        protected MyNetworkHandler()
        {
            MyNetwork = new MyEasyNetworkManager(45674);
            MyNetwork.Register();

            MyNetwork.OnRecievedPacket += PacketIn;
        }

        private void PacketIn(PacketIn e)
        {
            if (e.PacketId == 45) // SyncRequestPacket
            {
                if (MyAPIGateway.Multiplayer.IsServer)
                {
                    ulong playerId = e.SenderId;
                    foreach (var ship in PointCheck.Data.Values)
                    {
                        MyLog.Default.WriteLineAndConsole("Auto-syncing ship ID " + ship.GridID);
                        //PointCheck.Sending[ship.GridID].Add(e.SenderId);
                        PacketGridData packet = new PacketGridData { id = ship.GridID, tracked = ship, value = 1 };
                        Static.MyNetwork.TransmitToPlayer(packet, e.SenderId);
                    }
                }
            }

            if (e.PacketId == 1)
            {
			
				
			//inject for shared list

                    all_players.Clear();
                    listPlayers.Clear();
                    MyAPIGateway.Players.GetPlayers(listPlayers);
                    foreach (var p in listPlayers)
                    {
				    all_players.Add(p.SteamUserId);
                    }
			//end
			
			
			
			
                var packet = e.UnWrap<PacketGridData>();
                if (MyAPIGateway.Multiplayer.IsServer)
                {
                    var x = MyEntities.GetEntityById(packet.id);
                    if (x != null && x is IMyCubeGrid)
                    {
                        if (packet.value == 1) //add
                        { //if (packet.value == 1 && MyAPIGateway.Session.IsUserAdmin(e.SenderId))
                            if (PointCheck.Sending.ContainsKey(packet.id))
                            {
                                
								try
								{
								PointCheck.Sending[packet.id].Remove(e.SenderId);
								}
								catch{}
                            }
                            else
                            {
                                PointCheck.Sending.Add(packet.id, new List<ulong>());
                            }
                            
							//option 1
							//PointCheck.Sending[packet.id].Add(e.SenderId);
							
							foreach (var p in all_players)
							{
                            PointCheck.Sending[packet.id].Add(p);
							}
                        }
                        else if (packet.value == 2) //remove
                        {
                            if (PointCheck.Sending.ContainsKey(packet.id))
                            {
                                
								
								PointCheck.Sending[packet.id].Remove(e.SenderId);
								
                                foreach (var p in all_players)
								{
                                PointCheck.Sending[packet.id].Remove(p);
								}
								
								//end
								
								
								if (PointCheck.Sending[packet.id].Count == 0)
                                {
                                    PointCheck.Sending.Remove(packet.id);

                                    if (PointCheck.Sending.Count == 0)
                                    {
                                        PointCheck.Data[packet.id].DisposeHud();
                                        PointCheck.Data.Remove(packet.id);
                                    }

                                }
                            }
                        }
                    }
                }
                else
                {
					//Inject
					 if (packet.value == 1 && !PointCheck.Tracking.Contains(packet.id))
					{
					PointCheck.Tracking.Add(packet.id);
					PointCheck.Data[packet.id].CreateHud();	
					}
					else if (packet.value == 2 && PointCheck.Tracking.Contains(packet.id))
					{
					PointCheck.Tracking.Remove(packet.id);	
					}
					//end
					
                    packet.tracked.CreateHud();
                    if (PointCheck.Data.ContainsKey(packet.id))
                    {
                        PointCheck.Data[packet.id].DisposeHud();
                        PointCheck.Data[packet.id] = packet.tracked;
                    }
                    else
                    {
                        PointCheck.Data.Add(packet.id, packet.tracked);    
                    }
                }
            }

            if (e.PacketId == 5)
            {
                if (MyAPIGateway.Session.IsUserAdmin(e.SenderId))
                {
                    foreach (var g in MyEntities.GetEntities())
                    {
                        if (g != null && !g.MarkedForClose && g is MyCubeGrid)
                        {
                            var grid = g as MyCubeGrid;
                            var block = PointCheck.SH_api.GetShieldBlock(grid);
                            if (block != null)
                            {
                                PointCheck.SH_api.SetCharge(block, 99999999999);
                            }
                        }
                    }
                    MyAPIGateway.Utilities.ShowMessage("Shields", "Charged");
                }
            }

            if (e.PacketId == 6)
            {
                PointCheck.Begin();
            }

            if (e.PacketId == 7)
            {
               // PointCheck.TrackYourselfMyMan();
            }
            
            if (e.PacketId == 8)
            {
                PointCheck.EndMatch();
            }
            
            
            if (e.PacketId == 17)
            {
                PointCheck.There_Is_A_Problem();
            }
            if (e.PacketId == 18)
            {
                PointCheck.There_Is_A_Solution();
            }
        }

        public void Dispose()
        {
            MyNetwork.UnRegister();
            MyNetwork = null;
            Static = null;
        }
    }
}
