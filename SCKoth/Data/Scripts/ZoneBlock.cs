using KingOfTheHill.Descriptions;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Game.Entities;
using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI;
using Sandbox.ModAPI.Interfaces.Terminal;
using SENetworkAPI;
using System;
using System.Collections.Generic;
using System.Text;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.ModAPI;
using VRage.Game.ObjectBuilders.ComponentSystem;
using VRage.ModAPI;
using VRage.ObjectBuilders;
using VRage.Utils;
using VRageMath;

namespace KingOfTheHill
{
	public enum ZoneStates { Active, Idle, Contested }
	public enum Days { EveryDay, Sunday, Monday, Tuesday, Wednesday, Thursday, Friday, Saturday }


	[MyEntityComponentDescriptor(typeof(MyObjectBuilder_Beacon), false, "ZoneBlock")]
	public class ZoneBlock : MyGameLogicComponent
	{
		public static event Action<ZoneBlock, IMyFaction, int, bool> OnAwardPoints = delegate { };
		public static event Action<ZoneBlock, IMyPlayer, IMyFaction> OnPlayerDied = delegate { };

		public static readonly Color[] ZoneStateColorLookup = new Color[] { Color.White, Color.Gray, Color.Orange };
		private static readonly MyDefinitionId Electricity = MyResourceDistributorComponent.ElectricityId;

		public ZoneStates State { get; private set; } = ZoneStates.Idle;
		public IMyBeacon ModBlock { get; private set; }

		private IMyFaction controlledByFaction = null;
		public IMyFaction ControlledByFaction
		{
			get { return controlledByFaction; }
			set
			{
				controlledByFaction = value;
				ControlledBy.Value = (value == null) ? 0 : value.FactionId;
			}

		}

		public NetSync<float> Progress;
		public NetSync<float> ProgressWhenComplete;
		public NetSync<long> ControlledBy;
		public NetSync<float> Radius;

		public NetSync<int> ActivateOnPlayerCount;
		public NetSync<bool> ActivateOnCharacter;
		public NetSync<bool> EncampmentMode;
		public NetSync<bool> ActivateOnSmallGrid;
		public NetSync<bool> ActivateOnLargeGrid;
		public NetSync<bool> ActivateOnUnpoweredGrid;
		public NetSync<bool> IgnoreCopilot;

		public NetSync<int> PointsOnCap;
		public NetSync<bool> AwardPointsToAllActiveFactions;
		public NetSync<bool> AwardPointsAsCredits;
		public NetSync<int> CreditsPerPoint;
		public NetSync<int> PointsRemovedOnDeath;

		public NetSync<int> MinSmallGridBlockCount;
		public NetSync<int> MinLargeGridBlockCount;

		public NetSync<float> IdleDrainRate;
		public NetSync<float> ContestedDrainRate;
		public NetSync<bool> FlatProgressRate;
		public NetSync<float> ActiveProgressRate;

		public NetSync<bool> AutomaticActivation;
		public NetSync<int> ActivationDay;
		public NetSync<int> ActivationStartTime;
		public NetSync<int> ActivationEndTime;

		public NetSync<float> Opacity;

		public NetSync<bool> IsLocationNamed;
		public NetSync<int> PointsForPrize;

		public NetSync<bool> UseComponentReward;
		public NetSync<bool> UseOreReward;
		public NetSync<bool> UseIngotReward;

		public NetSync<int> PrizeAmountComponent;
		public NetSync<int> PrizeAmountOre;
		public NetSync<int> PrizeAmountIngot;

		public NetSync<bool> AdvancedComponentSelection;
		public NetSync<bool> AdvancedOreSelection;
		public NetSync<bool> AdvancedIngotSelection;

		public NetSync<string> PrizeComponentSubtypeId;
		public NetSync<string> PrizeOreSubtypeId;
		public NetSync<string> PrizeIngotSubtypeId;

		public NetSync<string> SelectedComponentString;
		public NetSync<string> SelectedOreString;
		public NetSync<string> SelectedIngotString;

		public NetSync<bool> SpawnIntoPrizeBox;

		public int PointsEarnedSincePrize;

		private Dictionary<long, int> ActiveEnemiesPerFaction = new Dictionary<long, int>();
		private bool IsInitialized = false;
		private ZoneStates lastState = ZoneStates.Idle;

		public override void Init(MyObjectBuilder_EntityBase objectBuilder)
		{
			NeedsUpdate = MyEntityUpdateEnum.EACH_FRAME;
			ModBlock = Entity as IMyBeacon;

			ZoneDescription desc = ZoneDescription.GetDefaultSettings();

			if (MyAPIGateway.Session.IsServer)
			{
				ZoneDescription temp = ZoneDescription.Load(Entity);

				if (desc == null)
				{
					Tools.Log(MyLogSeverity.Warning, $"The data saved for {Entity.EntityId} returned null. loading defaults");
				}
				else
				{
					desc = temp;
				}
			}


			Progress = new NetSync<float>(this, TransferType.ServerToClient, desc.Progress);
			ProgressWhenComplete = new NetSync<float>(this, TransferType.Both, desc.ProgressWhenComplete);
			ControlledBy = new NetSync<long>(this, TransferType.ServerToClient, desc.ControlledBy);
			Radius = new NetSync<float>(this, TransferType.Both, desc.Radius);
			ActivateOnPlayerCount = new NetSync<int>(this, TransferType.Both, desc.ActivateOnPlayerCount);
			ActivateOnCharacter = new NetSync<bool>(this, TransferType.Both, desc.ActivateOnCharacter);
			EncampmentMode = new NetSync<bool>(this, TransferType.Both, desc.EncampmentMode);
			ActivateOnSmallGrid = new NetSync<bool>(this, TransferType.Both, desc.ActivateOnSmallGrid);
			ActivateOnLargeGrid = new NetSync<bool>(this, TransferType.Both, desc.ActivateOnLargeGrid);
			ActivateOnUnpoweredGrid = new NetSync<bool>(this, TransferType.Both, desc.ActivateOnUnpoweredGrid);
			IgnoreCopilot = new NetSync<bool>(this, TransferType.Both, desc.IgnoreCopilot);
			PointsOnCap = new NetSync<int>(this, TransferType.Both, desc.PointsOnCap);
			AwardPointsToAllActiveFactions = new NetSync<bool>(this, TransferType.Both, desc.AwardPointsToAllActiveFactions);
			AwardPointsAsCredits = new NetSync<bool>(this, TransferType.Both, desc.AwardPointsAsCredits);
			CreditsPerPoint = new NetSync<int>(this, TransferType.Both, desc.CreditsPerPoint);
			PointsRemovedOnDeath = new NetSync<int>(this, TransferType.Both, desc.PointsRemovedOnDeath);
			MinSmallGridBlockCount = new NetSync<int>(this, TransferType.Both, desc.MinSmallGridBlockCount);
			MinLargeGridBlockCount = new NetSync<int>(this, TransferType.Both, desc.MinLargeGridBlockCount);
			IdleDrainRate = new NetSync<float>(this, TransferType.Both, desc.IdleDrainRate);
			ContestedDrainRate = new NetSync<float>(this, TransferType.Both, desc.ContestedDrainRate);
			FlatProgressRate = new NetSync<bool>(this, TransferType.Both, desc.FlatProgressRate);
			ActiveProgressRate = new NetSync<float>(this, TransferType.Both, desc.ActiveProgressRate);
			AutomaticActivation = new NetSync<bool>(this, TransferType.Both, desc.AutomaticActivation);
			ActivationDay = new NetSync<int>(this, TransferType.Both, desc.ActivationDay);
			ActivationStartTime = new NetSync<int>(this, TransferType.Both, desc.ActivationStartTime);
			ActivationEndTime = new NetSync<int>(this, TransferType.Both, desc.ActivationEndTime);
			Opacity = new NetSync<float>(this, TransferType.Both, desc.ActiveProgressRate);
			IsLocationNamed = new NetSync<bool>(this, TransferType.Both, desc.IsLocationNamed);
			PointsForPrize = new NetSync<int>(this, TransferType.Both, desc.PointsForPrize);
			UseComponentReward = new NetSync<bool>(this, TransferType.Both, desc.UseComponentReward);
			UseOreReward = new NetSync<bool>(this, TransferType.Both, desc.UseOreReward);
			UseIngotReward = new NetSync<bool>(this, TransferType.Both, desc.UseIngotReward);
			PrizeAmountComponent = new NetSync<int>(this, TransferType.Both, desc.PrizeAmountComponent);
			PrizeAmountOre = new NetSync<int>(this, TransferType.Both, desc.PrizeAmountOre);
			PrizeAmountIngot = new NetSync<int>(this, TransferType.Both, desc.PrizeAmountIngot);
			AdvancedComponentSelection = new NetSync<bool>(this, TransferType.Both, desc.AdvancedComponentSelection);
			AdvancedOreSelection = new NetSync<bool>(this, TransferType.Both, desc.AdvancedOreSelection);
			AdvancedIngotSelection = new NetSync<bool>(this, TransferType.Both, desc.AdvancedIngotSelection);
			PrizeComponentSubtypeId = new NetSync<string>(this, TransferType.Both, desc.PrizeComponentSubtypeId);
			PrizeOreSubtypeId = new NetSync<string>(this, TransferType.Both, desc.PrizeOreSubtypeId);
			PrizeIngotSubtypeId = new NetSync<string>(this, TransferType.Both, desc.PrizeIngotSubtypeId);
			SelectedComponentString = new NetSync<string>(this, TransferType.Both, desc.SelectedComponentString);
			SelectedOreString = new NetSync<string>(this, TransferType.Both, desc.SelectedOreString);
			SelectedIngotString = new NetSync<string>(this, TransferType.Both, desc.SelectedIngotString);
			SpawnIntoPrizeBox = new NetSync<bool>(this, TransferType.Both, desc.SpawnIntoPrizeBox);
			PointsEarnedSincePrize = desc.PointsEarnedSincePrize;


			Core.RegisterZone(this);
		}
		public override bool IsSerialized()
		{
			Save();
			return base.IsSerialized();
		}

		public override void Close()
		{
			Core.UnRegisterZone(this);
		}

		public void Save()
		{
			ZoneDescription desc = new ZoneDescription();

			desc.GridId = ModBlock.CubeGrid.EntityId;
			desc.BlockId = ModBlock.EntityId;

			desc.Progress = Progress.Value;
			desc.ProgressWhenComplete = ProgressWhenComplete.Value;
			desc.ControlledBy = ControlledBy.Value;
			desc.Radius = Radius.Value;
			desc.ActivateOnPlayerCount = ActivateOnPlayerCount.Value;
			desc.ActivateOnCharacter = ActivateOnCharacter.Value;
			desc.EncampmentMode = EncampmentMode.Value;
			desc.ActivateOnSmallGrid = ActivateOnSmallGrid.Value;
			desc.ActivateOnLargeGrid = ActivateOnLargeGrid.Value;
			desc.ActivateOnUnpoweredGrid = ActivateOnUnpoweredGrid.Value;
			desc.IgnoreCopilot = IgnoreCopilot.Value;
			desc.PointsOnCap = PointsOnCap.Value;
			desc.AwardPointsToAllActiveFactions = AwardPointsToAllActiveFactions.Value;
			desc.AwardPointsAsCredits = AwardPointsAsCredits.Value;
			desc.CreditsPerPoint = CreditsPerPoint.Value;
			desc.PointsRemovedOnDeath = PointsRemovedOnDeath.Value;
			desc.MinSmallGridBlockCount = MinSmallGridBlockCount.Value;
			desc.MinLargeGridBlockCount = MinLargeGridBlockCount.Value;
			desc.IdleDrainRate = IdleDrainRate.Value;
			desc.ContestedDrainRate = ContestedDrainRate.Value;
			desc.FlatProgressRate = FlatProgressRate.Value;
			desc.ActiveProgressRate = ActiveProgressRate.Value;
			desc.AutomaticActivation = AutomaticActivation.Value;
			desc.ActivationDay = ActivationDay.Value;
			desc.ActivationStartTime = ActivationStartTime.Value;
			desc.ActivationEndTime = ActivationEndTime.Value;
			desc.Opacity = Opacity.Value;
			desc.IsLocationNamed = IsLocationNamed.Value;
			desc.PointsForPrize = PointsForPrize.Value;
			desc.UseComponentReward = UseComponentReward.Value;
			desc.UseOreReward = UseOreReward.Value;
			desc.UseIngotReward = UseIngotReward.Value;
			desc.PrizeAmountComponent = PrizeAmountComponent.Value;
			desc.PrizeAmountOre = PrizeAmountOre.Value;
			desc.PrizeAmountIngot = PrizeAmountIngot.Value;
			desc.AdvancedComponentSelection = AdvancedComponentSelection.Value;
			desc.AdvancedOreSelection = AdvancedOreSelection.Value;
			desc.AdvancedIngotSelection = AdvancedIngotSelection.Value;
			desc.PrizeComponentSubtypeId = PrizeComponentSubtypeId.Value;
			desc.PrizeOreSubtypeId = PrizeOreSubtypeId.Value;
			desc.PrizeIngotSubtypeId = PrizeIngotSubtypeId.Value;
			desc.SelectedComponentString = SelectedComponentString.Value;
			desc.SelectedOreString = SelectedOreString.Value;
			desc.SelectedIngotString = SelectedIngotString.Value;
			desc.SpawnIntoPrizeBox = SpawnIntoPrizeBox.Value;
			desc.PointsEarnedSincePrize = PointsEarnedSincePrize;

			desc.Save(Entity);
		}

		public string GetClosestPlanet()
		{
			MyPlanet planet = MyGamePruningStructure.GetClosestPlanet(Entity.GetPosition());
			return (planet == null) ? "N/A" : planet.Name;
		}

		public override void UpdateBeforeSimulation()
		{
			if (!IsInitialized)
			{
				CreateControls();
				IsInitialized = true;
			}

			if (ModBlock.CubeGrid.Physics == null || !ModBlock.IsFunctional || !ModBlock.Enabled || !ModBlock.IsWorking)
				return; // if the block is incomplete, turned off or projected

			if (AutomaticActivation.Value)
			{
				string activationText = UpdateAutomaticActivation();

				if (activationText != string.Empty)
				{
					if (ModBlock.CustomName != activationText)
					{
						ModBlock.CustomName = activationText;
					}

					return;
				}
			}

			if (EncampmentMode.Value)
			{
				EncampmentMode_Update();
			}
			else
			{
				StandardMode_Update();
			}

			// display info

			if (MyAPIGateway.Multiplayer.IsServer)
			{
				int percent = (int)Math.Floor((Progress.Value / ProgressWhenComplete.Value) * 100f);


				string newText;

				if (!EncampmentMode.Value)
				{
					newText = $"{State.ToString().ToUpper()} - {percent}% {(State != ZoneStates.Idle ? $"[{ControlledByFaction.Tag}]" : "")}";
				}
				else
				{
					newText = $"{State.ToString().ToUpper()} - {percent}%";
				}

				if (ModBlock.CustomName != newText) // this makes sure that players are not spammed with network updates every frame
				{
					ModBlock.CustomName = newText;
				}

				//if (lastState != State)
				//{
				//	MyAPIGateway.Utilities.SendModMessage(Tools.ModMessageId, $"KotH: {ModBlock.CustomName}");
				//}
			}

			if (!MyAPIGateway.Utilities.IsDedicated)
			{
				Color color;
				switch (State)
				{
					case ZoneStates.Active:
						color = Color.White;
						break;
					case ZoneStates.Contested:
						color = Color.Orange;
						break;
					default:
						color = Color.Gray;
						break;
				}

				MatrixD matrix = Entity.WorldMatrix;
				color.A = (byte)Opacity.Value;
				MySimpleObjectDraw.DrawTransparentSphere(ref matrix, Radius.Value, ref color, MySimpleObjectRasterizer.Solid, 20, null, MyStringId.GetOrCompute("KothTransparency"), 0.12f, -1, null);
			}


		}

		/// <returns>true if progress is complete</returns>
		private bool UpdateProgress(ZoneStates state, float speedWhenActive)
		{
			lastState = State;
			State = state;

			float speed = 0;
			switch (State)
			{
				case ZoneStates.Idle:
					speed = -GetProgress(IdleDrainRate.Value);
					break;
				case ZoneStates.Contested:
					speed = -GetProgress(ContestedDrainRate.Value);
					break;
				case ZoneStates.Active:
					speed = speedWhenActive;
					break;
			}

			float newProgress = Progress.Value + speed;
			if (newProgress <= 0)
			{
				Progress.SetValue(0);
			}
			else
			{
				Progress.SetValue(Progress.Value + speed);
			}

			return (Progress.Value >= ProgressWhenComplete.Value);
		}

		/// <summary>
		/// Encampment mode awards points for all factions that have a powered static grid inside the zone
		/// </summary>
		private void EncampmentMode_Update()
		{

			List<IMyPlayer> players = new List<IMyPlayer>();
			MyAPIGateway.Players.GetPlayers(players);

			foreach (IMyPlayer p in players)
			{
				// make sure players that leave the zone dont count for zone deaths
				if (PointsRemovedOnDeath.Value > 0 && p.Character != null)
				{
					p.Character.CharacterDied -= Died;
				}

				if (Vector3D.Distance(p.GetPosition(), Entity.GetPosition()) > Radius.Value)
					continue;

				// add players in the zone to the death penalty
				if (PointsRemovedOnDeath.Value > 0 && p.Character != null)
				{
					p.Character.CharacterDied += Died;
				}
			}


			List<IMyFaction> activeFactions = new List<IMyFaction>();	
			foreach (IMyCubeGrid staticGrid in Core.StaticGrids)
			{
				if (!EncampmentMode_VerifyGrid(staticGrid as MyCubeGrid))
					continue;

				if (staticGrid != null && staticGrid.BigOwners.Count > 0)
				{
					IMyFaction f = MyAPIGateway.Session.Factions.TryGetPlayerFaction(staticGrid.BigOwners[0]);

					if (f != null)
					{
						if (!activeFactions.Contains(f))
						{
							activeFactions.Add(f);
						}
					}
				}
			}

			ZoneStates state = (activeFactions.Count > 0) ? ZoneStates.Active : ZoneStates.Idle;

			bool complete = UpdateProgress(state, 1f);
			if (complete)
			{
				bool header = true;
				foreach (IMyFaction faction in activeFactions)
				{
					OnAwardPoints.Invoke(this, faction, 2, header); // the active enemy value should be more than 1
					header = false;
				}

				ResetProgress();
			}

			EncampmentMode_Hud(activeFactions);
		}

		private bool EncampmentMode_VerifyGrid(MyCubeGrid grid)
		{
			if (grid.BlocksCount < MinLargeGridBlockCount.Value ||
				grid.BigOwners.Count == 0 ||
				grid == ModBlock.CubeGrid ||
				Vector3D.Distance(grid.WorldMatrix.Translation, Entity.GetPosition()) > Radius.Value)
			{
				return false;
			}

			bool flag = false;
			foreach (IMySlimBlock slim in grid.CubeBlocks)
			{
				if (slim.FatBlock is IMyPowerProducer && (slim.FatBlock as IMyPowerProducer).CurrentOutput > 0)
				{
					flag = true;
					break;
				}
			}

			return flag;
		}

		private void EncampmentMode_Hud(List<IMyFaction> factions)
		{
			IMyPlayer localPlayer = MyAPIGateway.Session.LocalHumanPlayer;
			if (localPlayer != null && Vector3D.Distance(localPlayer.GetPosition(), Entity.GetPosition()) < Radius.Value)
			{
				IMyFaction f = MyAPIGateway.Session.Factions.TryGetPlayerFaction(localPlayer.IdentityId);

				string specialColor = (factions.Count == 0) ? "Gray" : "White";

				MyAPIGateway.Utilities.ShowNotification($"Active Factions {factions.Count} - {(f != null && factions.Contains(f) ? "Encamped" : "NOT Encamped")} - {State.ToString().ToUpper()}: {((Progress.Value / ProgressWhenComplete.Value) * 100).ToString("n0")}%", 1, specialColor);
			}
		}


		/// <summary>
		/// Standard koth rules. players / controlled grids cap. if enemeies enter the zone is contested.
		/// </summary>
		private void StandardMode_Update()
		{
			bool isContested = false;
			IMyFaction nominatedFaction = null;
			List<IMyFaction> factionsInZone = new List<IMyFaction>();
			List<IMyCubeGrid> registeredGrids = new List<IMyCubeGrid>();
			List<IMyPlayer> playersInZone = new List<IMyPlayer>();

			List<IMyPlayer> players = new List<IMyPlayer>();
			MyAPIGateway.Players.GetPlayers(players);

			foreach (IMyPlayer p in players)
			{
				// make sure players that leave the zone dont count for zone deaths
				if (PointsRemovedOnDeath.Value > 0 && p.Character != null)
				{
					p.Character.CharacterDied -= Died;
				}

				if (!StandardMode_VerifyPlayer(p, ref registeredGrids))
					continue;

				playersInZone.Add(p);

				// add players in the zone to the death penalty
				if (PointsRemovedOnDeath.Value > 0 && p.Character != null)
				{
					p.Character.CharacterDied += Died;
				}

				// update faction status
				IMyFaction f = MyAPIGateway.Session.Factions.TryGetPlayerFaction(p.IdentityId);

				if (nominatedFaction == null)
				{
					nominatedFaction = f;
				}

				if (!factionsInZone.Contains(f))
				{
					if (!AwardPointsToAllActiveFactions.Value && !isContested) // only check for contested status if not contested
					{
						foreach (IMyFaction otherFaction in factionsInZone)
						{
							if (MyAPIGateway.Session.Factions.GetRelationBetweenFactions(otherFaction.FactionId, f.FactionId) == MyRelationsBetweenFactions.Enemies)
							{
								isContested = true;
							}
						}
					}

					factionsInZone.Add(f);
				}

				// update grid status
				IMyCubeBlock cube = p.Controller.ControlledEntity as IMyCubeBlock;
				if (cube != null)
				{
					List<IMyCubeGrid> grids = new List<IMyCubeGrid>();
					MyAPIGateway.GridGroups.GetGroup(cube.CubeGrid, GridLinkTypeEnum.Logical, grids);
					registeredGrids.AddRange(grids);
				}
			}

			if (!factionsInZone.Contains(ControlledByFaction))
			{
				ControlledByFaction = nominatedFaction;
			}

			foreach (IMyFaction zoneFaction in factionsInZone)
			{
				if (!ActiveEnemiesPerFaction.ContainsKey(zoneFaction.FactionId))
				{
					ActiveEnemiesPerFaction.Add(zoneFaction.FactionId, 0);
				}

				int enemyCount = 0;
				foreach (IMyPlayer p in players)
				{
					IMyFaction f = MyAPIGateway.Session.Factions.TryGetPlayerFaction(p.IdentityId);

					if (f == null || f.FactionId == zoneFaction.FactionId)
						continue;

					if (MyAPIGateway.Session.Factions.GetRelationBetweenFactions(f.FactionId, zoneFaction.FactionId) == MyRelationsBetweenFactions.Enemies)
					{
						enemyCount++;
					}
				}

				if (ActiveEnemiesPerFaction[zoneFaction.FactionId] < enemyCount)
				{
					ActiveEnemiesPerFaction[zoneFaction.FactionId] = enemyCount;
				}
			}

			ZoneStates state = ZoneStates.Idle;
			float speed = -GetProgress(IdleDrainRate.Value);

			if (isContested)
			{
				state = ZoneStates.Contested;
				speed = -GetProgress(ContestedDrainRate.Value);
			}
			else if (factionsInZone.Count > 0)
			{
				state = ZoneStates.Active;
				speed = GetProgress(playersInZone.Count);
			}

			bool complete = UpdateProgress(state, speed);
			if (complete)
			{
				if (AwardPointsToAllActiveFactions.Value)
				{
					bool header = true;
					foreach (IMyFaction fac in factionsInZone)
					{
						OnAwardPoints(this, fac, ActiveEnemiesPerFaction[fac.FactionId], header);
						header = false;
					}
				}
				else
				{
					OnAwardPoints.Invoke(this, ControlledByFaction, ActiveEnemiesPerFaction[ControlledByFaction.FactionId], true);
				}

				ResetProgress();
			}

			StandardMode_Hud(playersInZone, speed);
		}

		private bool StandardMode_VerifyPlayer(IMyPlayer player, ref List<IMyCubeGrid> registeredGrids)
		{
			if (Vector3D.Distance(player.GetPosition(), Entity.GetPosition()) > Radius.Value ||
				MyAPIGateway.Session.Factions.TryGetPlayerFaction(player.IdentityId) == null)
			{
				return false;
			}

			if (!ActivateOnCharacter.Value)
			{
				IMyCubeBlock controlledBlock = player.Controller.ControlledEntity as IMyCubeBlock;

				if (controlledBlock == null)
					return false;

				MyCubeGrid grid = controlledBlock.CubeGrid as MyCubeGrid;

				if (grid.IsStatic ||
					IgnoreCopilot.Value && registeredGrids.Contains(grid)) // if a grid is already registered this is a copilot
					return false;

				if (!ActivateOnUnpoweredGrid.Value)
				{
					MyResourceSinkComponent powerSink;
					if (!controlledBlock.Components.TryGet(out powerSink))
					{
						powerSink = new MyResourceSinkComponent();
						MyResourceSinkInfo resourceInfo = new MyResourceSinkInfo() {
							ResourceTypeId = Electricity
						};
						powerSink.AddType(ref resourceInfo);
						controlledBlock.Components.Add(powerSink);

						powerSink.SetRequiredInputByType(Electricity, 0);
						powerSink.SetMaxRequiredInputByType(Electricity, 0.000002f);
						powerSink.SetRequiredInputFuncByType(Electricity, () => { return 0; });
					}

					if (!powerSink.IsPowerAvailable(Electricity, 0.000001f))
						return false;
				}


				if (grid.GridSizeEnum == MyCubeSize.Large)
				{
					if (!ActivateOnLargeGrid.Value ||
						grid.BlocksCount < MinLargeGridBlockCount.Value)
						return false;
				}
				else if (grid.GridSizeEnum == MyCubeSize.Small)
				{
					if (!ActivateOnSmallGrid.Value ||
						grid.BlocksCount < MinSmallGridBlockCount.Value)
						return false;
				}
			}

			return true;
		}

		private void StandardMode_Hud(List<IMyPlayer> playersInZone, float speed)
		{
			IMyPlayer localPlayer = MyAPIGateway.Session.LocalHumanPlayer;
			if (localPlayer != null && Vector3D.Distance(localPlayer.GetPosition(), Entity.GetPosition()) <= Radius.Value)
			{
				int allies = 0;
				int enemies = 0;
				int neutral = 0;
				foreach (IMyPlayer p in playersInZone)
				{
					switch (localPlayer.GetRelationTo(p.IdentityId))
					{
						case MyRelationsBetweenPlayerAndBlock.Owner:
						case MyRelationsBetweenPlayerAndBlock.FactionShare:
							allies++;
							break;
						case MyRelationsBetweenPlayerAndBlock.Neutral:
							neutral++;
							break;
						case MyRelationsBetweenPlayerAndBlock.Enemies:
						case MyRelationsBetweenPlayerAndBlock.NoOwnership:
							enemies++;
							break;
					}
				}

				string specialColor = "White";
				switch (State)
				{
					case ZoneStates.Contested:
						specialColor = "Red";
						break;
					case ZoneStates.Active:
						specialColor = "Blue";
						break;
				}

				MyAPIGateway.Utilities.ShowNotification($"Allies: {allies}  Neutral: {neutral}  Enemies: {enemies} - {State.ToString().ToUpper()}: {((Progress.Value / ProgressWhenComplete.Value) * 100).ToString("n0")}% Speed: {(speed * 100).ToString("n0")}% {(!AwardPointsToAllActiveFactions.Value ? (ControlledByFaction != null ? $"Controlled By: {ControlledByFaction.Tag}" : "") : "")}", 1, specialColor);
			}
		}

		private string UpdateAutomaticActivation()
		{
			DateTime today = DateTime.UtcNow;
			if (ActivationDay.Value != 0 && (ActivationDay.Value - 1) != (int)today.DayOfWeek)
			{
				int hours = (int)Math.Floor(ActivationStartTime.Value / 60d);
				int minutes = ActivationStartTime.Value % 60;
				Progress.SetValue(0);

				return $"Activates on {(Days)ActivationDay.Value} at {(hours.ToString().Length == 1 ? "0" + hours.ToString() : hours.ToString())}:{(minutes.ToString().Length == 1 ? "0" + minutes.ToString() : minutes.ToString())} UTC";
			}

			int currentTime = today.Hour * 60 + today.Minute;
			if (currentTime < ActivationStartTime.Value)
			{
				int startTime = ActivationStartTime.Value - currentTime;
				int hours = (int)Math.Floor(startTime / 60d);
				int minutes = startTime % 60;
				Progress.SetValue(0);

				return $"Activates in {((hours == 0) ? "" : $"{hours}h ")}{minutes}m";
			}

			if (currentTime >= ActivationEndTime.Value)
			{
				if (ActivationDay.Value != 0)
				{
					int hours = (int)Math.Floor(ActivationStartTime.Value / 60d);
					int minutes = ActivationStartTime.Value % 60;
					Progress.SetValue(0);

					return $"Activates next {(Days)ActivationDay.Value} at {(hours.ToString().Length == 1 ? "0" + hours.ToString() : hours.ToString())}:{(minutes.ToString().Length == 1 ? "0" + minutes.ToString() : minutes.ToString())} UTC";
				}
				else
				{
					int nextStart = 1440 + ActivationStartTime.Value - currentTime;
					int hours = (int)Math.Floor(nextStart / 60d);
					int minutes = nextStart % 60;
					Progress.SetValue(0);

					return $"Activates in {((hours == 0) ? "" : $"{hours}h ")}{minutes}m";
				}
			}

			return string.Empty;
		}

		private void ResetProgress()
		{
			ResetActiveEnemies();
			Progress.Value = 0;
		}

		private void Died(IMyCharacter character)
		{
			List<IMyPlayer> players = new List<IMyPlayer>();
			MyAPIGateway.Players.GetPlayers(players);

			foreach (IMyPlayer p in players)
			{
				if (p.Character == character)
				{
					IMyFaction f = MyAPIGateway.Session.Factions.TryGetPlayerFaction(p.IdentityId);
					if (f != null)
					{
						OnPlayerDied.Invoke(this, p, f);
					}

					break;
				}
			}
		}

		private float GetProgress(float progressModifier)
		{
			return ((progressModifier * progressModifier - 1f) / (progressModifier * progressModifier + (3f * progressModifier) + 1f)) + 1f;
		}

		private void ResetActiveEnemies()
		{
			Dictionary<long, int> newDict = new Dictionary<long, int>();

			foreach (long key in ActiveEnemiesPerFaction.Keys)
			{
				newDict.Add(key, 0);
			}

			ActiveEnemiesPerFaction = newDict;
		}

		private void CreateControls()
		{
			if (!MyAPIGateway.Utilities.IsDedicated)
			{
				IMyTerminalControlSlider Slider = null;
				IMyTerminalControlCheckbox Checkbox = null;
				IMyTerminalControlOnOffSwitch onoff = null;
				IMyTerminalControlTextbox textbox = null;
				IMyTerminalControlSeparator separator = null;
				IMyTerminalControlLabel label = null;

				label = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlLabel, IMyBeacon>("zone_KothGeneralSettingsLabel");
				label.Enabled = (block) => { return block.EntityId == ModBlock.EntityId; };
				label.Visible = (block) => { return block.EntityId == ModBlock.EntityId; };

				label.Label = MyStringId.GetOrCompute("Koth General Settings");
				MyAPIGateway.TerminalControls.AddControl<Sandbox.ModAPI.Ingame.IMyBeacon>(label);

				separator = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlSeparator, IMyBeacon>("zone_KothGeneralSeparator");
				separator.Enabled = (block) => { return block.EntityId == ModBlock.EntityId; };
				separator.Visible = (block) => { return block.EntityId == ModBlock.EntityId; };
				MyAPIGateway.TerminalControls.AddControl<Sandbox.ModAPI.Ingame.IMyBeacon>(separator);

				onoff = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlOnOffSwitch, IMyBeacon>("Zone_EncampmentMode");
				onoff.Enabled = (block) => { return block.EntityId == ModBlock.EntityId; };
				onoff.Visible = (block) => { return block.EntityId == ModBlock.EntityId; };

				onoff.OnText = MyStringId.GetOrCompute("On");
				onoff.OffText = MyStringId.GetOrCompute("Off");

				onoff.Setter = (block, value) => {
					ActivateOnCharacter.Value = false;
					EncampmentMode.Value = value;
					UpdateControls();
				};

				onoff.Getter = (block) => EncampmentMode.Value;
				onoff.Title = MyStringId.GetOrCompute("Encampment Mode");
				onoff.Tooltip = MyStringId.GetOrCompute("All factions with powered static grids in the zone are awarded points");
				MyAPIGateway.TerminalControls.AddControl<Sandbox.ModAPI.Ingame.IMyBeacon>(onoff);

				Checkbox = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlCheckbox, IMyBeacon>("Zone_ActivateOnCharacter");
				Checkbox.Enabled = (block) => {return block.EntityId == ModBlock.EntityId && !EncampmentMode.Value; };
				Checkbox.Visible = (block) => { return block.EntityId == ModBlock.EntityId; };

				Checkbox.Setter = (block, value) => {
					ActivateOnCharacter.Value = value;
					UpdateControls();
				};
				Checkbox.Getter = (block) => ActivateOnCharacter.Value;
				Checkbox.Title = MyStringId.GetOrCompute("Activate On Character");
				Checkbox.Tooltip = MyStringId.GetOrCompute("Only requires a player to activate the zone");
				MyAPIGateway.TerminalControls.AddControl<Sandbox.ModAPI.Ingame.IMyBeacon>(Checkbox);

				Checkbox = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlCheckbox, IMyBeacon>("Zone_ActivateOnSmallGrid");
				Checkbox.Enabled = (block) => { return block.EntityId == ModBlock.EntityId && !ActivateOnCharacter.Value && !EncampmentMode.Value; };
				Checkbox.Visible = (block) => { return block.EntityId == ModBlock.EntityId; };

				Checkbox.Setter = (block, value) => { ActivateOnSmallGrid.Value = value; };
				Checkbox.Getter = (block) => ActivateOnSmallGrid.Value;
				Checkbox.Title = MyStringId.GetOrCompute("Activate On Small Grid");
				Checkbox.Tooltip = MyStringId.GetOrCompute("Will activate if a piloted small grid is in the zone");
				MyAPIGateway.TerminalControls.AddControl<Sandbox.ModAPI.Ingame.IMyBeacon>(Checkbox);

				Checkbox = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlCheckbox, IMyBeacon>("Zone_ActivateOnLargeGrid");
				Checkbox.Enabled = (block) => { return block.EntityId == ModBlock.EntityId && !ActivateOnCharacter.Value && !EncampmentMode.Value; };
				Checkbox.Visible = (block) => { return block.EntityId == ModBlock.EntityId; };

				Checkbox.Setter = (block, value) => { ActivateOnLargeGrid.Value = value; };
				Checkbox.Getter = (block) => ActivateOnLargeGrid.Value;
				Checkbox.Title = MyStringId.GetOrCompute("Activate On Large Grid");
				Checkbox.Tooltip = MyStringId.GetOrCompute("Will activate if a piloted large grid is in the zone");
				MyAPIGateway.TerminalControls.AddControl<Sandbox.ModAPI.Ingame.IMyBeacon>(Checkbox);


				Checkbox = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlCheckbox, IMyBeacon>("Zone_ActivateOnUnpoweredGrid");
				Checkbox.Enabled = (block) => { return block.EntityId == ModBlock.EntityId && !ActivateOnCharacter.Value && !EncampmentMode.Value; };
				Checkbox.Visible = (block) => { return block.EntityId == ModBlock.EntityId; };

				Checkbox.Setter = (block, value) => { ActivateOnUnpoweredGrid.Value = value; };
				Checkbox.Getter = (block) => ActivateOnUnpoweredGrid.Value;
				Checkbox.Title = MyStringId.GetOrCompute("Activate On Unpowered Grid");
				Checkbox.Tooltip = MyStringId.GetOrCompute("Will activate if piloted grid is unpowered");
				MyAPIGateway.TerminalControls.AddControl<Sandbox.ModAPI.Ingame.IMyBeacon>(Checkbox);

				Checkbox = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlCheckbox, IMyBeacon>("Zone_IgnoreCopilot");
				Checkbox.Enabled = (block) => { return block.EntityId == ModBlock.EntityId && !EncampmentMode.Value; };
				Checkbox.Visible = (block) => { return block.EntityId == ModBlock.EntityId; };

				Checkbox.Setter = (block, value) => { IgnoreCopilot.Value = value; };
				Checkbox.Getter = (block) => IgnoreCopilot.Value;
				Checkbox.Title = MyStringId.GetOrCompute("Ignore Copilot");
				Checkbox.Tooltip = MyStringId.GetOrCompute("Will count a copiloted grid as a single person");
				MyAPIGateway.TerminalControls.AddControl<Sandbox.ModAPI.Ingame.IMyBeacon>(Checkbox);

				Checkbox = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlCheckbox, IMyBeacon>("Zone_LocationName");
				Checkbox.Enabled = (block) => { return block.EntityId == ModBlock.EntityId; };
				Checkbox.Visible = (block) => { return block.EntityId == ModBlock.EntityId; };

				Checkbox.Setter = (block, value) => { IsLocationNamed.Value = value; };
				Checkbox.Getter = (block) => IsLocationNamed.Value;
				Checkbox.Title = MyStringId.GetOrCompute("Give Location & Name");
				Checkbox.Tooltip = MyStringId.GetOrCompute("gives location and name of koth when point is scored");
				MyAPIGateway.TerminalControls.AddControl<Sandbox.ModAPI.Ingame.IMyBeacon>(Checkbox);

				Slider = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlSlider, IMyBeacon>("Zone_Radius");
				Slider.Enabled = (block) => { return block.EntityId == ModBlock.EntityId; };
				Slider.Visible = (block) => { return block.EntityId == ModBlock.EntityId; };

				Slider.Setter = (block, value) => { Radius.Value = value; };
				Slider.Getter = (block) => Radius.Value;
				Slider.Writer = (block, value) => value.Append($"{Math.Round(Radius.Value, 0)}m");
				Slider.Title = MyStringId.GetOrCompute("Radius");
				Slider.Tooltip = MyStringId.GetOrCompute("Capture Zone Radius");
				Slider.SetLimits(Constants.MinRadius, Constants.MaxRadius);
				MyAPIGateway.TerminalControls.AddControl<Sandbox.ModAPI.Ingame.IMyBeacon>(Slider);

				Slider = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlSlider, IMyBeacon>("Zone_ProgressWhenComplete");
				Slider.Enabled = (block) => { return block.EntityId == ModBlock.EntityId; };
				Slider.Visible = (block) => { return block.EntityId == ModBlock.EntityId; };

				Slider.Setter = (block, value) => { ProgressWhenComplete.Value = value; };
				Slider.Getter = (block) => ProgressWhenComplete.Value;
				Slider.Writer = (block, value) => value.Append($"{TimeSpan.FromMilliseconds((ProgressWhenComplete.Value / 60) * 1000).ToString("g").Split('.')[0]}");
				Slider.Title = MyStringId.GetOrCompute("Capture Time");
				Slider.Tooltip = MyStringId.GetOrCompute("The base capture time");
				Slider.SetLimits(Constants.MinCaptureTime, Constants.MaxCaptureTime);
				MyAPIGateway.TerminalControls.AddControl<Sandbox.ModAPI.Ingame.IMyBeacon>(Slider);

				Slider = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlSlider, IMyBeacon>("Zone_IdleDrainRate");
				Slider.Enabled = (block) => { return block.EntityId == ModBlock.EntityId; };
				Slider.Visible = (block) => { return block.EntityId == ModBlock.EntityId; };

				Slider.Setter = (block, value) => { IdleDrainRate.Value = value; };
				Slider.Getter = (block) => IdleDrainRate.Value;
				Slider.Writer = (block, value) => value.Append($"{Math.Round(IdleDrainRate.Value * 100, 0)}%");
				Slider.Title = MyStringId.GetOrCompute("Idle Drain Rate");
				//Slider.Tooltip = MyStringId.GetOrCompute("How quickly the ");
				Slider.SetLimits(0, 5);
				MyAPIGateway.TerminalControls.AddControl<Sandbox.ModAPI.Ingame.IMyBeacon>(Slider);

				Slider = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlSlider, IMyBeacon>("Zone_ContestedDrainRate");
				Slider.Enabled = (block) => { return block.EntityId == ModBlock.EntityId && !EncampmentMode.Value; };
				Slider.Visible = (block) => { return block.EntityId == ModBlock.EntityId; };

				Slider.Setter = (block, value) => { ContestedDrainRate.Value = value; };
				Slider.Getter = (block) => ContestedDrainRate.Value;
				Slider.Writer = (block, value) => value.Append($"{Math.Round(ContestedDrainRate.Value * 100, 0)}%");
				Slider.Title = MyStringId.GetOrCompute("Contested Drain Rate");
				//Slider.Tooltip = MyStringId.GetOrCompute("The minimum blocks considered an activation grid");
				Slider.SetLimits(0, 5);
				MyAPIGateway.TerminalControls.AddControl<Sandbox.ModAPI.Ingame.IMyBeacon>(Slider);


				Slider = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlSlider, IMyBeacon>("Zone_MinSmallGridBlockCount");
				Slider.Enabled = (block) => { return block.EntityId == ModBlock.EntityId && !ActivateOnCharacter.Value && !EncampmentMode.Value; };
				Slider.Visible = (block) => { return block.EntityId == ModBlock.EntityId; };

				Slider.Setter = (block, value) => { MinSmallGridBlockCount.Value = (int)value; };
				Slider.Getter = (block) => MinSmallGridBlockCount.Value;
				Slider.Writer = (block, value) => value.Append($"{MinSmallGridBlockCount.Value} blocks");
				Slider.Title = MyStringId.GetOrCompute("SmallGrid min blocks");
				Slider.Tooltip = MyStringId.GetOrCompute("The minimum blocks considered an activation grid");
				Slider.SetLimits(Constants.MinBlockCount, Constants.MaxBlockCount);
				MyAPIGateway.TerminalControls.AddControl<Sandbox.ModAPI.Ingame.IMyBeacon>(Slider);


				Slider = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlSlider, IMyBeacon>("Zone_MinLargeGridBlockCount");
				Slider.Enabled = (block) => { return block.EntityId == ModBlock.EntityId && !ActivateOnCharacter.Value; };
				Slider.Visible = (block) => { return block.EntityId == ModBlock.EntityId; };

				Slider.Setter = (block, value) => { MinLargeGridBlockCount.Value = (int)value; };
				Slider.Getter = (block) => MinLargeGridBlockCount.Value;
				Slider.Writer = (block, value) => value.Append($"{MinLargeGridBlockCount.Value} blocks");
				Slider.Title = MyStringId.GetOrCompute("LargeGrid min blocks");
				Slider.Tooltip = MyStringId.GetOrCompute("The minimum blocks considered an activation grid");
				Slider.SetLimits(Constants.MinBlockCount, Constants.MaxBlockCount);
				MyAPIGateway.TerminalControls.AddControl<Sandbox.ModAPI.Ingame.IMyBeacon>(Slider);


				Slider = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlSlider, IMyBeacon>("Zone_Opacity");
				Slider.Enabled = (block) => { return block.EntityId == ModBlock.EntityId; };
				Slider.Visible = (block) => { return block.EntityId == ModBlock.EntityId; };

				Slider.Setter = (block, value) => { Opacity.Value = value; };
				Slider.Getter = (block) => Opacity.Value;
				Slider.Writer = (block, value) => value.Append($"{Opacity.Value} alpha");
				Slider.Title = MyStringId.GetOrCompute("Opacity");
				Slider.Tooltip = MyStringId.GetOrCompute("Sphere visiblility");
				Slider.SetLimits(0, 255);
				MyAPIGateway.TerminalControls.AddControl<Sandbox.ModAPI.Ingame.IMyBeacon>(Slider);


				label = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlLabel, IMyBeacon>("zone_TimedActivationLabel");
				label.Enabled = (block) => { return block.EntityId == ModBlock.EntityId; };
				label.Visible = (block) => { return block.EntityId == ModBlock.EntityId; };

				label.Label = MyStringId.GetOrCompute("Timed Activation Settings");
				MyAPIGateway.TerminalControls.AddControl<Sandbox.ModAPI.Ingame.IMyBeacon>(label);


				separator = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlSeparator, IMyBeacon>("zone_TimedActivationSeparator");
				separator.Enabled = (block) => { return block.EntityId == ModBlock.EntityId; };
				separator.Visible = (block) => { return block.EntityId == ModBlock.EntityId; };
				MyAPIGateway.TerminalControls.AddControl<Sandbox.ModAPI.Ingame.IMyBeacon>(separator);


				Checkbox = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlCheckbox, IMyBeacon>("Zone_AutomaticActivation");
				Checkbox.Enabled = (block) => { return block.EntityId == ModBlock.EntityId; };
				Checkbox.Visible = (block) => { return block.EntityId == ModBlock.EntityId; };

				Checkbox.Setter = (block, value) => { AutomaticActivation.Value = value; UpdateControls(); };
				Checkbox.Getter = (block) => AutomaticActivation.Value;
				Checkbox.Title = MyStringId.GetOrCompute("Auto Activate");
				Checkbox.Tooltip = MyStringId.GetOrCompute("Will allow activation durring a set time period");
				MyAPIGateway.TerminalControls.AddControl<Sandbox.ModAPI.Ingame.IMyBeacon>(Checkbox);


				Slider = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlSlider, IMyBeacon>("Zone_ActivationDay");
				Slider.Enabled = (block) => { return block.EntityId == ModBlock.EntityId && AutomaticActivation.Value; };
				Slider.Visible = (block) => { return block.EntityId == ModBlock.EntityId; };

				Slider.Setter = (block, value) => { ActivationDay.Value = (int)value; };
				Slider.Getter = (block) => ActivationDay.Value;
				Slider.Writer = (block, value) => value.Append($"{((Days)ActivationDay.Value).ToString()}");
				Slider.Title = MyStringId.GetOrCompute("Activation Day");
				Slider.Tooltip = MyStringId.GetOrCompute("The day or days that koth will activate on");
				Slider.SetLimits(0, 7);
				MyAPIGateway.TerminalControls.AddControl<Sandbox.ModAPI.Ingame.IMyBeacon>(Slider);


				Slider = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlSlider, IMyBeacon>("Zone_ActivationStartTime");
				Slider.Enabled = (block) => { return block.EntityId == ModBlock.EntityId && AutomaticActivation.Value; };
				Slider.Visible = (block) => { return block.EntityId == ModBlock.EntityId; };

				Slider.Setter = (block, value) => { ActivationStartTime.Value = (int)value; if (ActivationStartTime.Value > ActivationEndTime.Value) { ActivationEndTime.Value = ActivationStartTime.Value; UpdateControls(); } };
				Slider.Getter = (block) => ActivationStartTime.Value;
				Slider.Writer = (block, value) => value.Append($"{(Math.Floor(ActivationStartTime.Value / 60d).ToString("n0").Length == 1 ? "0" + Math.Floor(ActivationStartTime.Value / 60d).ToString("n0") : Math.Floor(ActivationStartTime.Value / 60d).ToString("n0"))}:{((ActivationStartTime.Value % 60).ToString().Length == 1 ? "0" + (ActivationStartTime.Value % 60).ToString() : (ActivationStartTime.Value % 60).ToString())} UTC");
				Slider.Title = MyStringId.GetOrCompute("Activation Start Time");
				//Slider.Tooltip = MyStringId.GetOrCompute("");
				Slider.SetLimits(0, 1440);
				MyAPIGateway.TerminalControls.AddControl<Sandbox.ModAPI.Ingame.IMyBeacon>(Slider);


				Slider = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlSlider, IMyBeacon>("Zone_ActivationEndTime");
				Slider.Enabled = (block) => { return block.EntityId == ModBlock.EntityId && AutomaticActivation.Value; };
				Slider.Visible = (block) => { return block.EntityId == ModBlock.EntityId; };

				Slider.Setter = (block, value) => { ActivationEndTime.Value = (int)value; if (ActivationStartTime.Value > ActivationEndTime.Value) { ActivationStartTime.Value = ActivationEndTime.Value; UpdateControls(); } };
				Slider.Getter = (block) => ActivationEndTime.Value;
				Slider.Writer = (block, value) => value.Append($"{(Math.Floor(ActivationEndTime.Value / 60d).ToString("n0").Length == 1 ? "0" + Math.Floor(ActivationEndTime.Value / 60d).ToString("n0") : Math.Floor(ActivationEndTime.Value / 60d).ToString("n0"))}:{((ActivationEndTime.Value % 60).ToString().Length == 1 ? "0" + (ActivationEndTime.Value % 60).ToString() : (ActivationEndTime.Value % 60).ToString())} UTC");
				Slider.Title = MyStringId.GetOrCompute("Activation End Time");
				//Slider.Tooltip = MyStringId.GetOrCompute("");
				Slider.SetLimits(0, 1440);
				MyAPIGateway.TerminalControls.AddControl<Sandbox.ModAPI.Ingame.IMyBeacon>(Slider);


				label = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlLabel, IMyBeacon>("zone_KothPointsSettingsLabel");
				label.Enabled = (block) => { return block.EntityId == ModBlock.EntityId; };
				label.Visible = (block) => { return block.EntityId == ModBlock.EntityId; };

				label.Label = MyStringId.GetOrCompute("Koth Point Settings");
				MyAPIGateway.TerminalControls.AddControl<Sandbox.ModAPI.Ingame.IMyBeacon>(label);

				separator = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlSeparator, IMyBeacon>("zone_KothPointsSeparator");
				separator.Enabled = (block) => { return block.EntityId == ModBlock.EntityId; };
				separator.Visible = (block) => { return block.EntityId == ModBlock.EntityId; };
				MyAPIGateway.TerminalControls.AddControl<Sandbox.ModAPI.Ingame.IMyBeacon>(separator);

				Checkbox = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlCheckbox, IMyBeacon>("Zone_AwardPointsToAllActiveFactions");
				Checkbox.Enabled = (block) => { return block.EntityId == ModBlock.EntityId && !EncampmentMode.Value; };
				Checkbox.Visible = (block) => { return block.EntityId == ModBlock.EntityId; };

				Checkbox.Setter = (block, value) => { AwardPointsToAllActiveFactions.Value = value; UpdateControls(); };
				Checkbox.Getter = (block) => AwardPointsToAllActiveFactions.Value;
				Checkbox.Title = MyStringId.GetOrCompute("Award Points to all Active Factions");
				Checkbox.Tooltip = MyStringId.GetOrCompute("All faction in zone get points on cap. No contesting zone. Zone caps at a set rate regardless of player count");
				MyAPIGateway.TerminalControls.AddControl<Sandbox.ModAPI.Ingame.IMyBeacon>(Checkbox);

				Checkbox = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlCheckbox, IMyBeacon>("Zone_AwardPointsAsCredits");
				Checkbox.Enabled = (block) => { return block.EntityId == ModBlock.EntityId; };
				Checkbox.Visible = (block) => { return block.EntityId == ModBlock.EntityId; };

				Checkbox.Setter = (block, value) => { AwardPointsAsCredits.Value = value; UpdateControls(); };
				Checkbox.Getter = (block) => AwardPointsAsCredits.Value;
				Checkbox.Title = MyStringId.GetOrCompute("Award Points As Credits");
				Checkbox.Tooltip = MyStringId.GetOrCompute("Will deposit credit into the capping faction");
				MyAPIGateway.TerminalControls.AddControl<Sandbox.ModAPI.Ingame.IMyBeacon>(Checkbox);

				Slider = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlSlider, IMyBeacon>("Zone_PointsOnCap");
				Slider.Enabled = (block) => { return block.EntityId == ModBlock.EntityId; };
				Slider.Visible = (block) => { return block.EntityId == ModBlock.EntityId; };

				Slider.Setter = (block, value) => { PointsOnCap.Value = (int)value; };
				Slider.Getter = (block) => PointsOnCap.Value;
				Slider.Writer = (block, value) => {
					if (PointsOnCap.Value == 0)
					{
						value.Append($"Standard");
					}
					else
					{
						value.Append($"{PointsOnCap.Value}");
					}
				};
				Slider.Title = MyStringId.GetOrCompute("Points On Cap");
				Slider.Tooltip = MyStringId.GetOrCompute("The number of points alotted on capture. Standard awards more points for more enemy players online. teams that are behind get a slight point boost.");
				Slider.SetLimits(0, Constants.MaxPoints);
				MyAPIGateway.TerminalControls.AddControl<Sandbox.ModAPI.Ingame.IMyBeacon>(Slider);


				Slider = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlSlider, IMyBeacon>("Zone_PointsRemovedOnDeath");
				Slider.Enabled = (block) => { return block.EntityId == ModBlock.EntityId; };
				Slider.Visible = (block) => { return block.EntityId == ModBlock.EntityId; };

				Slider.Setter = (block, value) => { PointsRemovedOnDeath.Value = (int)value; };
				Slider.Getter = (block) => PointsRemovedOnDeath.Value;
				Slider.Writer = (block, value) => value.Append($"{PointsRemovedOnDeath.Value}");
				Slider.Title = MyStringId.GetOrCompute("Points Removed On Death");
				Slider.SetLimits(0, Constants.MaxPoints);
				MyAPIGateway.TerminalControls.AddControl<Sandbox.ModAPI.Ingame.IMyBeacon>(Slider);


				Slider = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlSlider, IMyBeacon>("Zone_CreditPerPoint");
				Slider.Enabled = (block) => { return block.EntityId == ModBlock.EntityId && AwardPointsAsCredits.Value; };
				Slider.Visible = (block) => { return block.EntityId == ModBlock.EntityId; };

				Slider.Setter = (block, value) => { CreditsPerPoint.Value = (int)value; };
				Slider.Getter = (block) => CreditsPerPoint.Value;
				Slider.Writer = (block, value) => value.Append($"{CreditsPerPoint.Value}");
				Slider.Title = MyStringId.GetOrCompute("Credits per point");
				Slider.Tooltip = MyStringId.GetOrCompute("The number of credits per point that will be payed out to capping faction");
				Slider.SetLimits(Constants.MinCredit, Constants.MaxCredit);
				MyAPIGateway.TerminalControls.AddControl<Sandbox.ModAPI.Ingame.IMyBeacon>(Slider);


				label = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlLabel, IMyBeacon>("zone_PrizeLabel");
				label.Enabled = (block) => { return block.EntityId == ModBlock.EntityId; };
				label.Visible = (block) => { return block.EntityId == ModBlock.EntityId; };
				label.Label = MyStringId.GetOrCompute("Koth Prize Settings");
				MyAPIGateway.TerminalControls.AddControl<Sandbox.ModAPI.Ingame.IMyBeacon>(label);

				separator = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlSeparator, IMyBeacon>("zone_PrizeSeparator");
				separator.Enabled = (block) => { return block.EntityId == ModBlock.EntityId; };
				separator.Visible = (block) => { return block.EntityId == ModBlock.EntityId; };
				MyAPIGateway.TerminalControls.AddControl<Sandbox.ModAPI.Ingame.IMyBeacon>(separator);


				Checkbox = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlCheckbox, IMyBeacon>("Zone_SpawnIntoPrizeBox");
				Checkbox.Enabled = (block) => { return block.EntityId == ModBlock.EntityId; };
				Checkbox.Visible = (block) => { return block.EntityId == ModBlock.EntityId; };

				Checkbox.Setter = (block, value) => { SpawnIntoPrizeBox.Value = value; };
				Checkbox.Getter = (block) => SpawnIntoPrizeBox.Value;
				Checkbox.Title = MyStringId.GetOrCompute("Spawn Items Into PrizeBox");
				Checkbox.Tooltip = MyStringId.GetOrCompute("Items will be dropped into the prize box on this grid.");
				MyAPIGateway.TerminalControls.AddControl<Sandbox.ModAPI.Ingame.IMyBeacon>(Checkbox);


				Checkbox = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlCheckbox, IMyBeacon>("Zone_UseComponentReward");
				Checkbox.Enabled = (block) => { return block.EntityId == ModBlock.EntityId; };
				Checkbox.Visible = (block) => { return block.EntityId == ModBlock.EntityId; };

				Checkbox.Setter = (block, value) => {

					UseComponentReward.Value = value;
					UpdateControls();
				};
				Checkbox.Getter = (block) => UseComponentReward.Value;
				Checkbox.Title = MyStringId.GetOrCompute("Component Reward");
				Checkbox.Tooltip = MyStringId.GetOrCompute("Prize will be components");
				MyAPIGateway.TerminalControls.AddControl<Sandbox.ModAPI.Ingame.IMyBeacon>(Checkbox);

				Checkbox = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlCheckbox, IMyBeacon>("Zone_UseOreReward");
				Checkbox.Enabled = (block) => { return block.EntityId == ModBlock.EntityId; };
				Checkbox.Visible = (block) => { return block.EntityId == ModBlock.EntityId; };

				Checkbox.Setter = (block, value) => {
					UseOreReward.Value = value;
					UpdateControls();
				};

				Checkbox.Getter = (block) => UseOreReward.Value;
				Checkbox.Title = MyStringId.GetOrCompute("Ore Reward");
				Checkbox.Tooltip = MyStringId.GetOrCompute("Prize will be ore");
				MyAPIGateway.TerminalControls.AddControl<Sandbox.ModAPI.Ingame.IMyBeacon>(Checkbox);

				Checkbox = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlCheckbox, IMyBeacon>("Zone_UseIngotReward");
				Checkbox.Enabled = (block) => { return block.EntityId == ModBlock.EntityId; };
				Checkbox.Visible = (block) => { return block.EntityId == ModBlock.EntityId; };

				Checkbox.Setter = (block, value) => {
					UseIngotReward.Value = value;
					UpdateControls();
				};
				Checkbox.Getter = (block) => UseIngotReward.Value;
				Checkbox.Title = MyStringId.GetOrCompute("Ingot Reward");
				Checkbox.Tooltip = MyStringId.GetOrCompute("Prize will be Ingot");
				MyAPIGateway.TerminalControls.AddControl<Sandbox.ModAPI.Ingame.IMyBeacon>(Checkbox);


				Slider = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlSlider, IMyBeacon>("Zone_PointsForPrizes");
				Slider.Enabled = (block) => { return block.EntityId == ModBlock.EntityId; };
				Slider.Visible = (block) => { return block.EntityId == ModBlock.EntityId; };

				Slider.Setter = (block, value) => { PointsForPrize.Value = (int)value; };
				Slider.Getter = (block) => PointsForPrize.Value;
				Slider.Writer = (block, value) => value.Append($"{PointsForPrize.Value} Points");
				Slider.Title = MyStringId.GetOrCompute("Points Required for Prize");
				Slider.Tooltip = MyStringId.GetOrCompute("Points");
				Slider.SetLimits(Constants.MinPoints, Constants.MaxPoints);
				MyAPIGateway.TerminalControls.AddControl<Sandbox.ModAPI.Ingame.IMyBeacon>(Slider);


				Slider = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlSlider, IMyBeacon>("Zone_PrizeAmountComponents");
				Slider.Enabled = (block) => { return block.EntityId == ModBlock.EntityId && UseComponentReward.Value; };
				Slider.Visible = (block) => { return block.EntityId == ModBlock.EntityId; };
				
				Slider.Setter = (block, value) => { PrizeAmountComponent.Value = (int)value;};
				Slider.Getter = (block) => PrizeAmountComponent.Value;
				Slider.Writer = (block, value) => value.Append($"{PrizeAmountComponent.Value}#");
				Slider.Title = MyStringId.GetOrCompute("Prize Amount Components");
				Slider.Tooltip = MyStringId.GetOrCompute("Number of Prizes");
				Slider.SetLimits(Constants.MinAmount, Constants.MaxAmount);
				MyAPIGateway.TerminalControls.AddControl<Sandbox.ModAPI.Ingame.IMyBeacon>(Slider);

				Slider = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlSlider, IMyBeacon>("Zone_PrizeAmountOre");
				Slider.Enabled = (block) => { return block.EntityId == ModBlock.EntityId && UseOreReward.Value; };
				Slider.Visible = (block) => { return block.EntityId == ModBlock.EntityId; };

				Slider.Setter = (block, value) => { PrizeAmountOre.Value = (int)value;};
				Slider.Getter = (block) => PrizeAmountOre.Value;
				Slider.Writer = (block, value) => value.Append($"{PrizeAmountOre.Value}#");
				Slider.Title = MyStringId.GetOrCompute("Prize Amount ore");
				Slider.Tooltip = MyStringId.GetOrCompute("Number of Prizes");
				Slider.SetLimits(Constants.MinAmount, Constants.MaxAmount);
				MyAPIGateway.TerminalControls.AddControl<Sandbox.ModAPI.Ingame.IMyBeacon>(Slider);

				Slider = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlSlider, IMyBeacon>("Zone_PrizeAmountIngot");
				Slider.Enabled = (block) => { return block.EntityId == ModBlock.EntityId && UseIngotReward.Value; };
				Slider.Visible = (block) => { return block.EntityId == ModBlock.EntityId; };

				Slider.Setter = (block, value) => { PrizeAmountIngot.Value = (int)value; };
				Slider.Getter = (block) => PrizeAmountIngot.Value;
				Slider.Writer = (block, value) => value.Append($"{PrizeAmountIngot.Value}#");
				Slider.Title = MyStringId.GetOrCompute("Prize Amount Ingots");
				Slider.Tooltip = MyStringId.GetOrCompute("Number of Prizes");
				Slider.SetLimits(Constants.MinAmount, Constants.MaxAmount);
				MyAPIGateway.TerminalControls.AddControl<Sandbox.ModAPI.Ingame.IMyBeacon>(Slider);

				IMyTerminalControlListbox componentlist = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlListbox, IMyBeacon>("Zone_ComponentList");
				componentlist.Enabled = (block) => { return block.EntityId == ModBlock.EntityId && !AdvancedComponentSelection.Value && UseComponentReward.Value; };
				componentlist.Visible = (block) => { return block.EntityId == ModBlock.EntityId; };

				componentlist.Title = MyStringId.GetOrCompute("Select Vanilla Components");
				componentlist.SupportsMultipleBlocks = false;
				componentlist.ListContent = ComponentPrize;
				componentlist.VisibleRowsCount = 6;
				componentlist.ItemSelected = SelectedComponent;
				componentlist.Multiselect = false;
				MyAPIGateway.TerminalControls.AddControl<IMyBeacon>(componentlist);

				Checkbox = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlCheckbox, IMyBeacon>("Zone_AdvancedComponentOption");
				Checkbox.Enabled = (block) => { return block.EntityId == ModBlock.EntityId && UseComponentReward.Value; };
				Checkbox.Visible = (block) => { return block.EntityId == ModBlock.EntityId; };

				Checkbox.Setter = (block, value) => {
					AdvancedComponentSelection.Value = value;
					UpdateControls();
				};
				Checkbox.Getter = (block) => AdvancedComponentSelection.Value;
				Checkbox.Title = MyStringId.GetOrCompute("Advanced Component Selection");
				Checkbox.Tooltip = MyStringId.GetOrCompute("allows for manually adding modded Components");
				MyAPIGateway.TerminalControls.AddControl<Sandbox.ModAPI.Ingame.IMyBeacon>(Checkbox);

				IMyTerminalControlListbox Orelist = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlListbox, IMyBeacon>("Zone_OreList");
				Orelist.Enabled = (block) => { return block.EntityId == ModBlock.EntityId && !AdvancedOreSelection.Value && UseOreReward.Value; };
				Orelist.Visible = (block) => { return block.EntityId == ModBlock.EntityId; };

				Orelist.Title = MyStringId.GetOrCompute("Select Vanilla Ores");
				Orelist.SupportsMultipleBlocks = false;
				Orelist.ListContent = OrePrize;
				Orelist.VisibleRowsCount = 6;
				Orelist.ItemSelected = SelectedOre;
				Orelist.Multiselect = false;
				MyAPIGateway.TerminalControls.AddControl<IMyBeacon>(Orelist);

				Checkbox = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlCheckbox, IMyBeacon>("Zone_AdvancedOreOption");
				Checkbox.Enabled = (block) => { return block.EntityId == ModBlock.EntityId && UseOreReward.Value; };
				Checkbox.Visible = (block) => { return block.EntityId == ModBlock.EntityId; };

				Checkbox.Setter = (block, value) => {
					AdvancedOreSelection.Value = value;
					UpdateControls();
				};
				Checkbox.Getter = (block) => AdvancedOreSelection.Value;
				Checkbox.Title = MyStringId.GetOrCompute("Advanced Ore Selection");
				Checkbox.Tooltip = MyStringId.GetOrCompute("allows for manually adding modded Ores");
				MyAPIGateway.TerminalControls.AddControl<Sandbox.ModAPI.Ingame.IMyBeacon>(Checkbox);


				var Ingotlist = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlListbox, IMyBeacon>("Zone_IngotList");
				Ingotlist.Enabled = (block) => { return block.EntityId == ModBlock.EntityId && !AdvancedIngotSelection.Value && UseIngotReward.Value; };
				Ingotlist.Visible = (block) => { return block.EntityId == ModBlock.EntityId; };

				Ingotlist.Title = MyStringId.GetOrCompute("Select Vanilla Ingot");
				Ingotlist.SupportsMultipleBlocks = false;
				Ingotlist.ListContent = IngotPrize;
				Ingotlist.VisibleRowsCount = 6;
				Ingotlist.ItemSelected = SelectedIngot;
				Ingotlist.Multiselect = false;
				MyAPIGateway.TerminalControls.AddControl<IMyBeacon>(Ingotlist);

				Checkbox = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlCheckbox, IMyBeacon>("Zone_AdvancedIngotOption");
				Checkbox.Enabled = (block) => { return block.EntityId == ModBlock.EntityId && UseIngotReward.Value; };
				Checkbox.Visible = (block) => { return block.EntityId == ModBlock.EntityId; };

				Checkbox.Setter = (block, value) => {
					AdvancedIngotSelection.Value = value;
					UpdateControls();
				};
				Checkbox.Getter = (block) => AdvancedIngotSelection.Value;
				Checkbox.Title = MyStringId.GetOrCompute("Advanced Ingot Selection");
				Checkbox.Tooltip = MyStringId.GetOrCompute("allows for manually adding modded Ingot");
				MyAPIGateway.TerminalControls.AddControl<Sandbox.ModAPI.Ingame.IMyBeacon>(Checkbox);


				label = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlLabel, IMyBeacon>("zone_AdvanceLabel");
				label.Enabled = (block) => { return block.EntityId == ModBlock.EntityId; };
				label.Visible = (block) => { return block.EntityId == ModBlock.EntityId; };
				label.Label = MyStringId.GetOrCompute("Advanced Prize Settings");
				MyAPIGateway.TerminalControls.AddControl<Sandbox.ModAPI.Ingame.IMyBeacon>(label);

				separator = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlSeparator, IMyBeacon>("zone_PrizeSeparator");
				separator.Enabled = (block) => { return block.EntityId == ModBlock.EntityId; };
				separator.Visible = (block) => { return block.EntityId == ModBlock.EntityId; };
				MyAPIGateway.TerminalControls.AddControl<Sandbox.ModAPI.Ingame.IMyBeacon>(separator);

				textbox = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlTextbox, IMyBeacon>("zone_PrizeComponentSubtypeId");
				textbox.Enabled = (block) => { return block.EntityId == ModBlock.EntityId && AdvancedComponentSelection.Value && UseComponentReward.Value; };
				textbox.Visible = (block) => { return block.EntityId == ModBlock.EntityId; };

				textbox.Setter = (block, value) => { PrizeComponentSubtypeId.Value = (value.ToString()); };
				textbox.Getter = (block) => new StringBuilder(PrizeComponentSubtypeId.Value);
				textbox.Title = MyStringId.GetOrCompute("Component subtypeId");
				textbox.Tooltip = MyStringId.GetOrCompute("must use subtypeId");
				MyAPIGateway.TerminalControls.AddControl<Sandbox.ModAPI.Ingame.IMyBeacon>(textbox);

				textbox = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlTextbox, IMyBeacon>("zone_PrizeOreSubtypeId");
				textbox.Enabled = (block) => { return block.EntityId == ModBlock.EntityId && AdvancedOreSelection.Value && UseOreReward.Value; };
				textbox.Visible = (block) => { return block.EntityId == ModBlock.EntityId; };

				textbox.Setter = (block, value) => { PrizeOreSubtypeId.Value = (value.ToString()); };
				textbox.Getter = (block) => new StringBuilder(PrizeOreSubtypeId.Value);
				textbox.Title = MyStringId.GetOrCompute("Ore subtypeId");
				textbox.Tooltip = MyStringId.GetOrCompute("must use subtypeId");
				MyAPIGateway.TerminalControls.AddControl<Sandbox.ModAPI.Ingame.IMyBeacon>(textbox);

				textbox = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlTextbox, IMyBeacon>("zone_PrizeIngotSubtypeId");
				textbox.Enabled = (block) => { return block.EntityId == ModBlock.EntityId && AdvancedIngotSelection.Value && UseIngotReward.Value; };
				textbox.Visible = (block) => { return block.EntityId == ModBlock.EntityId; };

				textbox.Setter = (block, value) => { PrizeIngotSubtypeId.Value = (value.ToString()); };
				textbox.Getter = (block) => new StringBuilder(PrizeIngotSubtypeId.Value);
				textbox.Title = MyStringId.GetOrCompute("Ingot subtypeId");
				textbox.Tooltip = MyStringId.GetOrCompute("must use subtypeId");
				MyAPIGateway.TerminalControls.AddControl<Sandbox.ModAPI.Ingame.IMyBeacon>(textbox);
			}
		}

		private void UpdateControls()
		{
			List<IMyTerminalControl> controls = new List<IMyTerminalControl>();
			MyAPIGateway.TerminalControls.GetControls<IMyBeacon>(out controls);

			foreach (IMyTerminalControl control in controls)
			{
				control.UpdateVisual();
			}
		}


		private void ComponentPrize(IMyTerminalBlock block, List<MyTerminalControlListBoxItem> items, List<MyTerminalControlListBoxItem> selectedItems)
		{
			try
			{
				List<string> ComponentPrizeList = new List<string>();
				ComponentPrizeList.Add("Construction");
				ComponentPrizeList.Add("MetalGrid");
				ComponentPrizeList.Add("InteriorPlate");
				ComponentPrizeList.Add("SteelPlate");
				ComponentPrizeList.Add("Girder");
				ComponentPrizeList.Add("SmallTube");
				ComponentPrizeList.Add("LargeTube");
				ComponentPrizeList.Add("Motor");
				ComponentPrizeList.Add("Display");
				ComponentPrizeList.Add("BulletproofGlass");
				ComponentPrizeList.Add("Superconductor");
				ComponentPrizeList.Add("Computer");
				ComponentPrizeList.Add("Reactor");
				ComponentPrizeList.Add("Thrust");
				ComponentPrizeList.Add("GravityGenerator");
				ComponentPrizeList.Add("Medical");
				ComponentPrizeList.Add("RadioCommunication");
				ComponentPrizeList.Add("Detector");
				ComponentPrizeList.Add("Explosives");
				ComponentPrizeList.Add("SolarCell");
				ComponentPrizeList.Add("PowerCell");
				ComponentPrizeList.Add("Canvas");

				MyTerminalControlListBoxItem dummy = new MyTerminalControlListBoxItem(MyStringId.GetOrCompute("-Select Component Below-"), MyStringId.GetOrCompute("-Select Component Below-"), "abc");
				items.Add(dummy);

				if (!string.IsNullOrEmpty(SelectedComponentString.Value))
				{
					var placeholder = new MyTerminalControlListBoxItem(MyStringId.GetOrCompute(SelectedComponentString.Value), MyStringId.GetOrCompute(SelectedComponentString.Value), SelectedComponentString.Value);
					items.Add(placeholder);
					selectedItems.Add(placeholder);
				}
				foreach (var name in ComponentPrizeList)
				{
					var toList = new MyTerminalControlListBoxItem(MyStringId.GetOrCompute(name), MyStringId.GetOrCompute(name), "abc");
					items.Add(toList);
				}


			}
			catch (Exception ex)
			{
				VRage.Utils.MyLog.Default.WriteLineAndConsole($"list error when setting up ComponentPrize {ex}");
			}

		}

		private void SelectedComponent(IMyTerminalBlock block, List<MyTerminalControlListBoxItem> selectedItems)
		{
			try
			{
				SelectedComponentString.Value = selectedItems[0].Text.ToString();

			}
			catch (Exception ex)
			{
				MyLog.Default.WriteLineAndConsole($"list error when setting up SelectedComponent {ex}");
			}
		}

		private void OrePrize(IMyTerminalBlock block, List<MyTerminalControlListBoxItem> items, List<MyTerminalControlListBoxItem> selectedItems)
		{
			try
			{
				List<string> OrePrizeList = new List<string>();
				OrePrizeList.Add("Stone");
				OrePrizeList.Add("Iron");
				OrePrizeList.Add("Nickel");
				OrePrizeList.Add("Cobalt");
				OrePrizeList.Add("Magnesium");
				OrePrizeList.Add("Silicon");
				OrePrizeList.Add("Silver");
				OrePrizeList.Add("Gold");
				OrePrizeList.Add("Platinum");
				OrePrizeList.Add("Uranium");

				MyTerminalControlListBoxItem dummy = new MyTerminalControlListBoxItem(MyStringId.GetOrCompute("-Select Ore Below-"), MyStringId.GetOrCompute("-Select Ore Below-"), "abc");
				items.Add(dummy);

				if (!string.IsNullOrEmpty(SelectedOreString.Value))
				{
					MyTerminalControlListBoxItem placeholder = new MyTerminalControlListBoxItem(MyStringId.GetOrCompute(SelectedOreString.Value), MyStringId.GetOrCompute(SelectedOreString.Value), SelectedOreString.Value);
					items.Add(placeholder);
					selectedItems.Add(placeholder);
				}

				foreach (var name in OrePrizeList)
				{
					MyTerminalControlListBoxItem toList = new MyTerminalControlListBoxItem(MyStringId.GetOrCompute(name), MyStringId.GetOrCompute(name), "abc");
					items.Add(toList);
				}
			}
			catch (Exception ex)
			{
				MyLog.Default.WriteLineAndConsole($"list error when setting up OrePrize {ex}");
			}

		}

		private void SelectedOre(IMyTerminalBlock block, List<MyTerminalControlListBoxItem> selectedItems)
		{
			try
			{
				SelectedOreString.Value = selectedItems[0].Text.ToString();

			}
			catch (Exception ex)
			{
				MyLog.Default.WriteLineAndConsole($"list error when setting up SelectedComponent {ex}");
			}
		}

		private void IngotPrize(IMyTerminalBlock block, List<MyTerminalControlListBoxItem> items, List<MyTerminalControlListBoxItem> selectedItems)
		{
			try
			{
				List<string> IngotPrizeList = new List<string>();
				IngotPrizeList.Add("Stone");
				IngotPrizeList.Add("Iron");
				IngotPrizeList.Add("Nickel");
				IngotPrizeList.Add("Cobalt");
				IngotPrizeList.Add("Magnesium");
				IngotPrizeList.Add("Silicon");
				IngotPrizeList.Add("Silver");
				IngotPrizeList.Add("Gold");
				IngotPrizeList.Add("Platinum");
				IngotPrizeList.Add("Uranium");

				var dummy = new MyTerminalControlListBoxItem(MyStringId.GetOrCompute("-Select Ingot Below-"), MyStringId.GetOrCompute("-Select Ingot Below-"), "abc");
				items.Add(dummy);

				if (!string.IsNullOrEmpty(SelectedIngotString.Value))
				{
					MyTerminalControlListBoxItem placeholder = new MyTerminalControlListBoxItem(MyStringId.GetOrCompute(SelectedIngotString.Value), MyStringId.GetOrCompute(SelectedIngotString.Value), SelectedIngotString.Value);
					items.Add(placeholder);
					selectedItems.Add(placeholder);
				}

				foreach (var name in IngotPrizeList)
				{
					MyTerminalControlListBoxItem toList = new MyTerminalControlListBoxItem(MyStringId.GetOrCompute(name), MyStringId.GetOrCompute(name), "abc");
					items.Add(toList);
				}
			}
			catch (Exception ex)
			{
				MyLog.Default.WriteLineAndConsole($"list error when setting up IngotPrize {ex}");
			}

		}

		private void SelectedIngot(IMyTerminalBlock block, List<MyTerminalControlListBoxItem> selectedItems)
		{
			try
			{
				SelectedIngotString.Value = selectedItems[0].Text.ToString();

			}
			catch (Exception ex)
			{
				MyLog.Default.WriteLineAndConsole($"list error when setting up SelectedComponent {ex}");
			}
		}

	}
}
