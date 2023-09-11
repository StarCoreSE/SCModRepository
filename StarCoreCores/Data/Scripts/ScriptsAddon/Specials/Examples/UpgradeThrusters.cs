using System.Collections.Generic;
using Sandbox.Game.Entities;
using Sandbox.ModAPI;
using Scripts.Specials.ShipClass;
using VRage.Game.Components;
using VRage.Game.ModAPI;
using VRageMath;

namespace ServerMod
{
    [MySessionComponentDescriptor(MyUpdateOrder.NoUpdate)]
    public class UpgradeThrusters : MySessionComponentBase
    {

        static UpgradeThrusters()
        {
            SpecBlockHooks.OnReady += HooksOnOnReady;
        }

        private static void HooksOnOnReady()
        {
            SpecBlockHooks.OnSpecBlockChanged += OnSpecBlockChanged;
			SpecBlockHooks.OnLimitedBlockCreated += OnLimitedBlockCreated;
            //SpecBlockHooks.OnSpecBlockDestroyed += OnSpecBlockDestroyed;
        }
		
		//This takes effect works after the grid is cut/pasted or spec block is deleted
        private static void OnSpecBlockChanged(object specBlock, List<IMyCubeGrid> grids)
        {
            foreach (var grid in grids)
            {				

				//npc checking stuff borrowed from Digi
				if(grid.BigOwners == null || grid.BigOwners.Count == 0)
				{
					continue;
				}

				long owner = grid.BigOwners[0]; // only check the first one, too edge case to check others 
				var faction = MyAPIGateway.Session.Factions.TryGetPlayerFaction(owner);

				if(faction == null || faction.IsEveryoneNpc())
				{
					continue;
				}

				List<IMySlimBlock> blocks = new List<IMySlimBlock>();
				var core = SpecBlockHooks.GetMainSpecCore(grid);
				var stats = new Dictionary<int, float>();
				SpecBlockHooks.GetSpecCoreLimits(core, stats, SpecBlockHooks.GetSpecCoreLimitsEnum.CurrentStaticOrDynamic);
				grid.GetBlocks(blocks, x=>x.FatBlock is IMyTerminalBlock);
				foreach(var block in blocks)
				{
					var thrustBlock = block.FatBlock as IMyThrust;
					var reactorBlock = block.FatBlock as IMyReactor;
					var generatorBlock = block.FatBlock as IMyGasGenerator;
					var drillBlock = block.FatBlock as IMyShipDrill;
					var gyroBlock = block.FatBlock as IMyGyro;

					if(thrustBlock != null)
					{
						if(stats.ContainsKey(46) && stats[46] != 0)
						{
							thrustBlock.ThrustMultiplier = stats[46];
						}
						else
						{	
							thrustBlock.ThrustMultiplier = 1.0f;
						}
						//Log.ChatError($"SetMultiplier to: {thrustBlock.ThrustMultiplier}");
						if(stats.ContainsKey(46) && stats[46] != 0)
						{
							thrustBlock.PowerConsumptionMultiplier = 1 / stats[46];
						}
						else
						{	
							thrustBlock.PowerConsumptionMultiplier = 1.0f;
						}
						//Log.ChatError($"SetMultiplier to: {thrustBlock.PowerConsumptionMultiplier}");
					}
					
					if(reactorBlock != null)
					{
						if(stats.ContainsKey(28) && stats[28] != 0)
						{
							reactorBlock.PowerOutputMultiplier = stats[28];
						}
						else
						{	
							reactorBlock.PowerOutputMultiplier = 1.0f;
						}
						//Log.ChatError($"SetMultiplier to: {reactorBlock.PowerOutputMultiplier}");
					}

					if(gyroBlock != null)
					{
						if(stats.ContainsKey(28) && stats[28] != 0)
						{
							gyroBlock.PowerConsumptionMultiplier = 1 / MathHelper.Max(1, stats[28]);
							//gyroBlock.GyroStrengthMultiplier = stats[28];
						}
						else
						{	
							gyroBlock.PowerConsumptionMultiplier = 1.0f;
							//gyroBlock.GyroStrengthMultiplier = 1.0f;
						}
						//Log.ChatError($"SetMultiplier to: {gyroBlock.PowerConsumptionMultiplier}");
					}

					
					if(generatorBlock != null)
					{
						if(stats.ContainsKey(29) && stats[29] != 0)
						{
							//This is the rate of ice consumption, NOT the rate of O2/H2 output
							generatorBlock.ProductionCapacityMultiplier = stats[29];
							generatorBlock.PowerConsumptionMultiplier = stats[29] / MathHelper.Max(1, stats[28]);
						}
						else
						{	
							//This is the rate of ice consumption, NOT the rate of O2/H2 output
							generatorBlock.ProductionCapacityMultiplier = 1.0f;
							generatorBlock.PowerConsumptionMultiplier = 1.0f;
						}
						//Log.ChatError($"SetMultiplier to: {generatorBlock.ProductionCapacityMultiplier}");
					}
					
					if(drillBlock != null)
					{
						if(stats.ContainsKey(27) && stats[27] != 0)
						{
							drillBlock.DrillHarvestMultiplier = stats[27];
							drillBlock.PowerConsumptionMultiplier = 1 / stats[27];
						}
						else
						{	
							drillBlock.DrillHarvestMultiplier = 1.0f;
							drillBlock.PowerConsumptionMultiplier = 1.0f;
						}
						//Log.ChatError($"SetMultiplier to: {drillBlock.DrillHarvestMultiplier}");
					}
				}
            }
        }
		
		//This doesnt seem to work when expected, not sure why, is this even needed?
		private static void OnLimitedBlockCreated(object limitedBlock)
        {
			//Log.ChatError($"OnLimitedBlockCreated");
			if (limitedBlock is IMyTerminalBlock)
			{
				var block = limitedBlock as IMyTerminalBlock;
				var core = SpecBlockHooks.GetSpecCoreBlock(block);
				var stats = new Dictionary<int, float>();
				SpecBlockHooks.GetSpecCoreLimits(core, stats, SpecBlockHooks.GetSpecCoreLimitsEnum.CurrentStaticOrDynamic);
				if(stats.ContainsKey(23) && stats[23]!=0)
				{
					if(((IMyTerminalBlock)(block)).DefinitionDisplayNameText.Contains("Thruster"))
					{
						(block as IMyThrust).ThrustMultiplier = stats[23];
						//Log.ChatError($"SetMultiplier to: {(block as IMyThrust).ThrustMultiplier}");
					}						
				}
			}
		}
		
	}
}