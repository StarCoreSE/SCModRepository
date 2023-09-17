using Sandbox.ModAPI;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.ModAPI;
using VRage.Utils;
using KingOfTheHill.Descriptions;
using SENetworkAPI;
using NexusAPIns;
using System;
using VRage.ModAPI;
using Sandbox.Game.Entities;
using VRage.ObjectBuilders;

namespace KingOfTheHill
{
	[MySessionComponentDescriptor(MyUpdateOrder.AfterSimulation)]
	public class Core : MySessionComponentBase
	{
		public const string Keyword = "/koth";
		public const string DisplayName = "KotH";
		public const ushort ComId = 42511;
		public const ushort CliComId = 42611;

		//NEXUS: init flags and nexus api instantiation, one is for messages second for scores
		public bool nexusCliInit = false;
		public bool nexusInit = false;
		public NexusAPI Nexus;
		public NexusAPI Nexus2;

		//private static Dictionary<long, ScoreDescription> Scores = new Dictionary<long, ScoreDescription>(); // faction, score
		private static List<ZoneBlock> Zones = new List<ZoneBlock>();
		private static List<PlanetDescription> Planets = new List<PlanetDescription>();

		public static List<IMyCubeGrid> StaticGrids = new List<IMyCubeGrid>();

		private NetworkAPI Network => NetworkAPI.Instance;

		public static void RegisterZone(ZoneBlock zone)
		{
			Zones.Add(zone);
		}

		public static void UnRegisterZone(ZoneBlock zone)
		{
			Zones.Remove(zone);
		}

		private void EntityAdded(IMyEntity e)
		{
			IMyCubeGrid g = e as IMyCubeGrid;
			if (g == null || g.Physics == null)
				return;

			g.OnIsStaticChanged += StaticChanged;

			if (!g.IsStatic)
				return;

			StaticGrids.Add(g);
		}

		private void EntityRemoved(IMyEntity e)
		{
			IMyCubeGrid g = e as IMyCubeGrid;
			if (g == null || g.Physics == null)
				return;

			g.OnIsStaticChanged -= StaticChanged;

			if (!g.IsStatic)
				return;

			if (StaticGrids.Contains(g))
			{
				StaticGrids.Remove(g);
			}
		}

		private void StaticChanged(IMyCubeGrid g, bool IsStatic)
		{
			if (g.IsStatic)
			{
				StaticGrids.Add(g);
			}
			else
			{
				if (StaticGrids.Contains(g))
				{
					StaticGrids.Remove(g);
				}
			}
		}

		public override void Init(MyObjectBuilder_SessionComponent sessionComponent)
		{
			Tools.Log(MyLogSeverity.Info, "Initializing");

			MyAPIGateway.Multiplayer.RegisterSecureMessageHandler(8008, PluginHandleIncomingPacket);
			if (!NetworkAPI.IsInitialized)
			{
				NetworkAPI.Init(ComId, DisplayName, Keyword);
			}

			Network.RegisterChatCommand(string.Empty, Chat_Help);
			Network.RegisterChatCommand("help", Chat_Help);

			if (!MyAPIGateway.Session.IsServer)
			{
				Network.RegisterChatCommand("score", (args) => Network.SendCommand("score"));
				Network.RegisterChatCommand("save", (args) => Network.SendCommand("save"));
				Network.RegisterChatCommand("force-load", (args) => Network.SendCommand("force-load"));

				Network.RegisterNetworkCommand("score", ClientCallback_Score);

				//NEXUS: client message handler, recieves and shows messages forwarded from the sector
				if (!nexusCliInit)
				{
					MyAPIGateway.Multiplayer.RegisterMessageHandler(CliComId, HandleClientCrossServerComm);
					nexusCliInit = true;
				}
			}
			else
			{
				ZoneBlock.OnAwardPoints += AwardPoints;
				ZoneBlock.OnPlayerDied += PlayerDied;

				Network.RegisterChatCommand("score", (args) => { MyAPIGateway.Utilities.ShowMissionScreen(Network.ModName, "King of the Hill", "", FormatScores()); });
				Network.RegisterChatCommand("save", (args) => { ServerCallback_Save(MyAPIGateway.Session.Player.SteamUserId, "save", null, DateTime.Now); });
				Network.RegisterChatCommand("force-load", (args) => { ServerCallback_ForceLoad(MyAPIGateway.Session.Player.SteamUserId, "force_load", null, DateTime.Now); });

				Network.RegisterNetworkCommand("score", ServerCallback_Score);
				Network.RegisterNetworkCommand("save", ServerCallback_Save);
				Network.RegisterNetworkCommand("force_load", ServerCallback_ForceLoad);

				if (MyAPIGateway.Multiplayer.IsServer && !nexusInit)
				{
					//NEXUS: registration of sector handlers and nexus instances for each
					Nexus2 = new NexusAPI(5431); // score message
					Nexus = new NexusAPI(5432); // text message
					MyAPIGateway.Multiplayer.RegisterMessageHandler(5431, HandleCrossServerScore);
					MyAPIGateway.Multiplayer.RegisterMessageHandler(5432, HandleCrossServerComm);

					nexusInit = true;
				}
			}

			MyAPIGateway.Entities.OnEntityAdd += EntityAdded;
			MyAPIGateway.Entities.OnEntityRemove += EntityRemoved;
		}

		private void PluginHandleIncomingPacket(ushort comId, byte[] msg, ulong id, bool reliable)
		{
			try
			{
				string message = Encoding.ASCII.GetString(msg);
				if (message.Equals("clear"))
				{
					ClearScore();

					//NEXUS: if nexus is initialized, broadcast the message to nexus (and all other sectors in that way)
					//NEXUS: KotH should work normally if nexus is not initialized
					if (nexusInit)
					{
						//NEXUS: score is broadcasted to other sectors, this basically triggers the save not just on this sector but on all the others with the same score
						var serializedScore = MyAPIGateway.Utilities.SerializeToBinary<List<PlanetDescription>>(Planets);
						Nexus2.SendMessageToAllServers(serializedScore);
					}
				}

			}
			catch (Exception error)
			{
				Tools.Log(MyLogSeverity.Error, error.Message);
			}
		}

		//NEXUS: recieves a score object, overrides the current score and saves it
		private void HandleCrossServerScore(byte[] obj)
		{
			try
			{
				if (obj == null)
					return;

				Planets = MyAPIGateway.Utilities.SerializeFromBinary<List<PlanetDescription>>(obj);

				SaveData();

			}
			catch (Exception exc)
			{
				MyLog.Default.WriteLineAndConsole($"##MOD: Chillout error, ERROR: {exc}");
			}
		}

		//NEXUS: recieves a message from another sector and sends it to all connected clients
		private void HandleCrossServerComm(byte[] obj)
		{
			try
			{
				if (obj == null)
					return;

				MyAPIGateway.Multiplayer.SendMessageToOthers(CliComId, obj, true);

			}
			catch (Exception exc)
			{
				MyLog.Default.WriteLineAndConsole($"##MOD: Chillout error, ERROR: {exc}");
			}
		}

		//NEXUS: handles a message recieved from a server and shows it in the chat
		private void HandleClientCrossServerComm(byte[] obj)
		{
			try
			{
				if (obj == null)
					return;

				string message = Encoding.ASCII.GetString(obj);

				if (message == null)
					return;

				MyLog.Default.WriteLineAndConsole($"##MOD: Client recieved msg: {message}");

				MyAPIGateway.Utilities.ShowMessage(DisplayName, message);

			}
			catch (Exception exc)
			{
				MyLog.Default.WriteLineAndConsole($"##MOD: Chillout error, ERROR: {exc}");
			}
		}

		public override void LoadData()
		{
			if (!MyAPIGateway.Multiplayer.IsServer)
				return;

			Tools.Log(MyLogSeverity.Info, "Loading data");

			Session session = Descriptions.Session.Load();
			Planets = session.PlanetScores;
		}

		public override void SaveData()
		{
			if (!MyAPIGateway.Multiplayer.IsServer)
				return;

			Tools.Log(MyLogSeverity.Info, "Saving data");

			Session session = new Session() {
				PlanetScores = Planets
			};

			Descriptions.Session.Save(session);

			foreach (ZoneBlock b in Zones)
			{
				b.Save();
			}
		}

		public static void ClearScore()
		{
			if (!MyAPIGateway.Multiplayer.IsServer)
				return;

			Planets.Clear();
			Session session = new Session();

			Descriptions.Session.Save(session);
		}

		protected override void UnloadData()
		{
			ZoneBlock.OnAwardPoints -= AwardPoints;
			ZoneBlock.OnPlayerDied -= PlayerDied;

			//NEXUS: unregister the handlers on unload
			if (!MyAPIGateway.Session.IsServer)
			{
				MyAPIGateway.Multiplayer.UnregisterMessageHandler(CliComId, HandleClientCrossServerComm);
			}
			if (nexusInit)
			{
				MyAPIGateway.Multiplayer.UnregisterMessageHandler(5431, HandleCrossServerScore);
				MyAPIGateway.Multiplayer.UnregisterMessageHandler(5432, HandleCrossServerComm);
			}
		}

		private void AwardPoints(ZoneBlock zone, IMyFaction faction, int enemies, bool displayHeader)
		{
			if (!MyAPIGateway.Multiplayer.IsServer)
				return;

			string planetName = zone.GetClosestPlanet();
			if (!Planets.Any(description => description.Name == planetName))
			{
				PlanetDescription p = new PlanetDescription() {
					Name = planetName,
					Scores = new List<ScoreDescription>()
				};

				Planets.Add(p);
			}

			long facId = faction.FactionId;
			PlanetDescription planet = Planets.Find(w => w.Name == planetName);
			IMyCubeGrid kothGrid = (zone.Entity as IMyCubeBlock).CubeGrid;

			if (!planet.Scores.Any(s => s.FactionId == facId))
			{
				planet.Scores.Add(new ScoreDescription() {
					FactionId = facId,
					FactionName = faction.Name,
					FactionTag = faction.Tag,
					Points = 1,
					PlanetId = planetName,
					GridName = kothGrid.DisplayName
				});
			}

			int total = GetTotalScore(planet);
			ScoreDescription score = planet.Scores.Find(s => s.FactionId == facId);
			int current = score.Points;

			int points;
			if (zone.PointsOnCap.Value == 0)
			{
				points = (int)(((float)(total - current) / (float)total) * 5f * enemies) + 1 + enemies;
			}
			else
			{
				points = zone.PointsOnCap.Value;
			}

			planet.Scores.Find(s => s.FactionId == facId).Points += points;
			zone.PointsEarnedSincePrize += points;


			if (zone.AwardPointsAsCredits.Value)
			{
				faction.RequestChangeBalance(points * zone.CreditsPerPoint.Value);
			}


			if (zone.PointsEarnedSincePrize >= zone.PointsForPrize.Value)
			{
				zone.PointsEarnedSincePrize -= zone.PointsForPrize.Value;

				IMyCargoContainer prizebox = null;
				List<IMySlimBlock> temp = new List<IMySlimBlock>();
				kothGrid.GetBlocks(temp, s => {
					if (prizebox == null &&
						s.FatBlock != null &&
						s.FatBlock is IMyCargoContainer &&
						s.FatBlock.BlockDefinition.SubtypeId == "Prizebox")
					{
						prizebox = s.FatBlock as IMyCargoContainer;
					}

					return false;
				});

				if (zone.UseComponentReward.Value)
				{
					string prizeType = (zone.AdvancedComponentSelection.Value) ? zone.PrizeComponentSubtypeId.Value : zone.SelectedComponentString.Value;
					int amount = zone.PrizeAmountComponent.Value;

					MyDefinitionId definitionId = new MyDefinitionId(typeof(MyObjectBuilder_Component), prizeType);
					MyObjectBuilder_Component content = (MyObjectBuilder_Component)MyObjectBuilderSerializer.CreateNewObject(definitionId);
					MyObjectBuilder_InventoryItem inventoryItem = new MyObjectBuilder_InventoryItem { Amount = amount, Content = content };

					if (zone.SpawnIntoPrizeBox.Value)
					{
						if (prizebox == null)
						{
							Tools.Log(MyLogSeverity.Error, $"Could not find prize box on grid: {kothGrid.DisplayName} - {kothGrid.EntityId}");
						}
						else if (prizebox.GetInventory().CanItemsBeAdded(amount, definitionId))
						{
							prizebox.GetInventory().AddItems(amount, inventoryItem.Content);
						}
					}
					else
					{
						if (zone.Entity.GetInventory().CanItemsBeAdded(amount, definitionId))
						{
							zone.Entity.GetInventory().AddItems(amount, inventoryItem.Content);
						}
					}
				}

				if (zone.UseIngotReward.Value)
				{
					string prizeType = (zone.AdvancedIngotSelection.Value) ? zone.PrizeIngotSubtypeId.Value : zone.SelectedIngotString.Value;
					int amount = zone.PrizeAmountIngot.Value;

					MyDefinitionId definitionId = new MyDefinitionId(typeof(MyObjectBuilder_Ingot), prizeType);
					MyObjectBuilder_Ingot content = (MyObjectBuilder_Ingot)MyObjectBuilderSerializer.CreateNewObject(definitionId);
					MyObjectBuilder_InventoryItem inventoryItem = new MyObjectBuilder_InventoryItem { Amount = amount, Content = content };

					if (zone.SpawnIntoPrizeBox.Value)
					{
						if (prizebox == null)
						{
							Tools.Log(MyLogSeverity.Error, $"Could not find prize box on grid: {kothGrid.DisplayName} - {kothGrid.EntityId}");
						}
						else if (prizebox.GetInventory().CanItemsBeAdded(amount, definitionId))
						{
							prizebox.GetInventory().AddItems(amount, inventoryItem.Content);
						}
					}
					else
					{
						if (zone.Entity.GetInventory().CanItemsBeAdded(amount, definitionId))
						{
							zone.Entity.GetInventory().AddItems(amount, inventoryItem.Content);
						}
					}
				}

				if (zone.UseOreReward.Value)
				{
					string prizeType = (zone.AdvancedOreSelection.Value) ? zone.PrizeOreSubtypeId.Value : zone.SelectedOreString.Value;
					int amount = zone.PrizeAmountOre.Value;

					MyDefinitionId definitionId = new MyDefinitionId(typeof(MyObjectBuilder_Ore), prizeType);
					MyObjectBuilder_Ore content = (MyObjectBuilder_Ore)MyObjectBuilderSerializer.CreateNewObject(definitionId);
					MyObjectBuilder_InventoryItem inventoryItem = new MyObjectBuilder_InventoryItem { Amount = amount, Content = content };

					if (zone.SpawnIntoPrizeBox.Value)
					{
						if (prizebox == null)
						{
							Tools.Log(MyLogSeverity.Error, $"Could not find prize box on grid: {kothGrid.DisplayName} - {kothGrid.EntityId}");
						}
						else if (prizebox.GetInventory().CanItemsBeAdded(amount, definitionId))
						{
							prizebox.GetInventory().AddItems(amount, inventoryItem.Content);
						}
					}
					else
					{
						if (zone.Entity.GetInventory().CanItemsBeAdded(amount, definitionId))
						{
							zone.Entity.GetInventory().AddItems(amount, inventoryItem.Content);
						}
					}
				}
			}

			StringBuilder message = new StringBuilder();
			if (displayHeader && zone.IsLocationNamed.Value)
			{
				if (zone.EncampmentMode.Value)
				{

					message.Append($"{kothGrid.DisplayName} on {planetName} Encampment Payout");
				}
				else
				{
					message.Append($"{kothGrid.DisplayName} on {planetName} under attack");

				}
			}

			byte[] bytes = Encoding.ASCII.GetBytes(message.ToString());
			MyAPIGateway.Multiplayer.SendMessageToServer(8008, bytes);
			Network.Say(message.ToString());

			message.Clear();
			if (zone.AwardPointsAsCredits.Value)
			{
				message.Append($"{faction.Name} Scored {points} Points! ({points * zone.CreditsPerPoint.Value} credits)");
			}
			else
			{
				message.Append($"{faction.Name} Scored {points} Points!");
			}


			SaveData();

			//NEXUS: if nexus is initialized, broadcast the message to nexus (and all other sectors in that way)
			//NEXUS: KotH should work normally if nexus is not initialized
			if (nexusInit)
			{
				//NEXUS: sends the message this sector displays in the chat to all the other sectors
				byte[] nexMessage = Encoding.ASCII.GetBytes(message.ToString());
				Nexus.SendMessageToAllServers(nexMessage);

				//NEXUS: score is broadcasted to other sectors, this basically triggers the save not just on this sector but on all the others with the same score
				var serializedScore = MyAPIGateway.Utilities.SerializeToBinary<List<PlanetDescription>>(Planets);
				Nexus2.SendMessageToAllServers(serializedScore);
			}

			bytes = Encoding.ASCII.GetBytes(message.ToString());
			MyAPIGateway.Multiplayer.SendMessageToServer(8008, bytes);
			Network.Say(message.ToString());
		}

		private void PlayerDied(ZoneBlock zone, IMyPlayer player, IMyFaction faction)
		{
			if (zone.PointsRemovedOnDeath.Value == 0 || !MyAPIGateway.Multiplayer.IsServer)
				return;

			long facId = faction.FactionId;
			string planetName = zone.GetClosestPlanet();

			if (!Planets.Any(description => description.Name == planetName))
			{
				var world = new PlanetDescription() {
					Name = planetName,
					Scores = new List<ScoreDescription>()
				};
				Planets.Add(world);
			}

			PlanetDescription planet = Planets.Find(p => p.Name == planetName);
			if (!planet.Scores.Any(s => s.FactionId == facId))
			{
				planet.Scores.Add(new ScoreDescription() {
					FactionId = facId,
					FactionName = faction.Name,
					FactionTag = faction.Tag,
					Points = 1,
					PlanetId = planetName,
					GridName = (zone.Entity as IMyCubeBlock).CubeGrid.DisplayName,
				});
			}

			ScoreDescription score = planet.Scores.Find(s => s.FactionId == facId);
			int original = score.Points;
			if (original - zone.PointsRemovedOnDeath.Value < 1)
			{
				score.Points = 1;
			}
			else
			{
				score.Points = original - zone.PointsRemovedOnDeath.Value;
			}

			string message = $"[{faction.Tag}] {player.DisplayName} Died: {score.Points - original} Points";
			Network.Say(message);
		}

		private int GetTotalScore(PlanetDescription planet)
		{
			int total = 0;

			foreach (ScoreDescription s in planet.Scores)
			{
				total += s.Points;
			}

			return total;
		}

		private string FormatScores()
		{
			StringBuilder formatedScores = new StringBuilder();
			foreach (var planet in Planets)
			{
				formatedScores.AppendLine($"### {planet.Name} ###");
				foreach (ScoreDescription score in planet.Scores)
				{
					formatedScores.AppendLine($"    [{score.FactionTag}] {score.FactionName}: {score.Points.ToString()}");
				}
			}

			return formatedScores.ToString();
		}

		#region Network Communication

		private void Chat_Help(string args)
		{
			MyAPIGateway.Utilities.ShowMessage(Network.ModName, "\nSCORE: Displays the current score\nSAVE: saves the current state to disk\nFORCE-LOAD: reloads scores from file (admin only)");
		}

		private void ClientCallback_Score(ulong steamId, string commandString, byte[] data, DateTime timestamp)
		{
			MyAPIGateway.Utilities.ShowMissionScreen(DisplayName, "King of the Hill", "", ASCIIEncoding.ASCII.GetString(data));
		}

		private void ServerCallback_Score(ulong steamId, string commandString, byte[] data, DateTime timestamp)
		{
			Network.SendCommand("score", data: ASCIIEncoding.ASCII.GetBytes(FormatScores()), steamId: steamId);
		}

		private void ServerCallback_Save(ulong steamId, string commandString, byte[] data, DateTime timestamp)
		{
			if (Tools.IsAllowedSpecialOperations(steamId))
			{
				SaveData();
				Network.SendCommand("blank_message", "KotH Saved.", steamId: steamId);
			}
			else
			{
				Network.SendCommand("blank_message", "Requires admin rights", steamId: steamId);
			}
		}

		private void ServerCallback_ForceLoad(ulong steamId, string commandString, byte[] data, DateTime timestamp)
		{
			if (Tools.IsAllowedSpecialOperations(steamId))
			{
				LoadData();
				Network.SendCommand("blank_message", "Scores force loaded", steamId: steamId);
			}
			else
			{
				Network.SendCommand("blank_message", "Requires admin rights", steamId: steamId);
			}
		}

		#endregion
	}
}
