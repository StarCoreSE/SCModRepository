using System.Collections.Generic;
using Sandbox.Game.Entities;
using Sandbox.ModAPI;
using Scripts.Specials.ShipClass;
using VRage.Game.Components;
using VRage.Game.ModAPI;
using VRageMath;
using VRage.Utils;
using Sandbox.Game.Entities.Cube;
using System.Runtime.CompilerServices;
using static VRage.Game.ObjectBuilders.Definitions.MyObjectBuilder_GameDefinition;
using ProtoBuf;
using Sandbox.Engine.Utils;
using VRage;
using static VRage.Game.MyObjectBuilder_BehaviorTreeDecoratorNode;

namespace ServerMod
{
    [MySessionComponentDescriptor(MyUpdateOrder.NoUpdate)]
    public class UpgradeThrusters : MySessionComponentBase
    {
        private static bool isServer;
        private static bool allowOnLimitedBlockCreated;
        private static List<object> limitedBlocksToProcess = new List<object>();

        public override void LoadData()
        {
            isServer = MyAPIGateway.Session.IsServer;
        }

        public override void BeforeStart()
        {
            //MyAPIGateway.Multiplayer.RegisterMessageHandler(5561, MessageHandler);
        }

        static UpgradeThrusters()
        {
            SpecBlockHooks.OnReady += HooksOnOnReady;
        }

        private static void HooksOnOnReady()
        {
            if (isServer)
                SpecBlockHooks.OnSpecBlockChanged += OnSpecBlockChanged;

            SpecBlockHooks.OnLimitedBlockCreated += OnLimitedBlockCreated;
            SpecBlockHooks.OnSpecBlockDestroyed += OnSpecBlockDestroyed;
        }

        //This takes effect works after the grid is cut/pasted or spec block is deleted
        private static void OnSpecBlockChanged(object specBlock, List<IMyCubeGrid> grids)
        {
            if (isServer)
            {
                allowOnLimitedBlockCreated = true;
                OnLimitedBlockCreated(null);
                SpecBlockHooks.OnSpecBlockChanged -= OnSpecBlockChanged;

                //RunUpgrades(specBlock, grids);
            }  
            /*else
            {
                MyAPIGateway.Parallel.StartBackground(() =>
                {
                    MyLog.Default.WriteLineAndConsole($"Running Code on Client");
                    MyAPIGateway.Parallel.Sleep(5000);
                    RunUpgrades(specBlock, grids);
                });
            }*/    
        }

        private static void RunUpgrades(object specBlock, List<IMyCubeGrid> grids, bool reset = false)
        {
            foreach (var grid in grids)
            {

                //npc checking stuff borrowed from Digi
                if (grid.BigOwners == null || grid.BigOwners.Count == 0)
                    continue;

                long owner = grid.BigOwners[0]; // only check the first one, too edge case to check others 
                var faction = MyAPIGateway.Session.Factions.TryGetPlayerFaction(owner);

                if (faction != null && faction.IsEveryoneNpc())
                    continue;

                List<IMySlimBlock> blocks = new List<IMySlimBlock>();
                //var core = SpecBlockHooks.GetMainSpecCore(grid);
                var stats = new Dictionary<int, float>();
                List<int> upgrades = new List<int>();
                SpecBlockHooks.GetSpecCoreLimits(specBlock, stats, SpecBlockHooks.GetSpecCoreLimitsEnum.CurrentStaticOrDynamic);
                grid.GetBlocks(blocks, x => x.FatBlock is IMyTerminalBlock);

                foreach (var block in blocks)
                {
                    var thrustBlock = block.FatBlock as IMyThrust;
                    var reactorBlock = block.FatBlock as IMyReactor;
                    var generatorBlock = block.FatBlock as IMyGasGenerator;
                    var drillBlock = block.FatBlock as IMyShipDrill;
                    var gyroBlock = block.FatBlock as IMyGyro;

                    if (thrustBlock != null)
                    {
                        if (stats.ContainsKey(46) && stats[46] != 0 && !reset)
                        {
                            thrustBlock.ThrustMultiplier = stats[46];
                        }
                        else
                        {
                            thrustBlock.ThrustMultiplier = 1.0f;
                        }
                        //Log.ChatError($"SetMultiplier to: {thrustBlock.ThrustMultiplier}");
                        if (stats.ContainsKey(46) && stats[46] != 0 && !reset)
                        {
                            thrustBlock.PowerConsumptionMultiplier = 1 / stats[46];
                        }
                        else
                        {
                            thrustBlock.PowerConsumptionMultiplier = 1.0f;
                        }
                        //Log.ChatError($"SetMultiplier to: {thrustBlock.PowerConsumptionMultiplier}");
                    }


                    if (reactorBlock != null)
                    {
                        if (stats.ContainsKey(28) && stats[28] != 0 && !reset)
                        {
                            reactorBlock.PowerOutputMultiplier = stats[28];
                        }
                        else
                        {
                            reactorBlock.PowerOutputMultiplier = 1.0f;
                        }
                        //Log.ChatError($"SetMultiplier to: {reactorBlock.PowerOutputMultiplier}");
                    }

                    if (gyroBlock != null)
                    {
                        if (stats.ContainsKey(28) && stats[28] != 0 && !reset)
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


                    if (generatorBlock != null)
                    {
                        if (stats.ContainsKey(29) && stats[29] != 0 && !reset)
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

                    if (drillBlock != null)
                    {
                        if (stats.ContainsKey(27) && stats[27] != 0 && !reset)
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

        private static void UpgradeBlock(object limitedBlock)
        {
            var tBlock = SpecBlockHooks.GetLimitedBlockBlock(limitedBlock);
            if (tBlock == null)
                return;

            var grid = tBlock.CubeGrid;
            if (grid == null)
                return;

            //npc checking stuff borrowed from Digi
            if (grid.BigOwners == null || grid.BigOwners.Count == 0)
                return;

            long owner = grid.BigOwners[0]; // only check the first one, too edge case to check others 
            var faction = MyAPIGateway.Session.Factions.TryGetPlayerFaction(owner);

            if (faction != null && faction.IsEveryoneNpc())
                return;

            var thrustBlock = tBlock as IMyThrust;
            var reactorBlock = tBlock as IMyReactor;
            var generatorBlock = tBlock as IMyGasGenerator;
            var drillBlock = tBlock as IMyShipDrill;
            var gyroBlock = tBlock as IMyGyro;

            var core = SpecBlockHooks.GetMainSpecCore(grid);
            var stats = new Dictionary<int, float>();
            SpecBlockHooks.GetSpecCoreLimits(core, stats, SpecBlockHooks.GetSpecCoreLimitsEnum.CurrentStaticOrDynamic);

            if (thrustBlock != null)
            {
                if (stats.ContainsKey(46) && stats[46] != 0)
                {
                    thrustBlock.ThrustMultiplier = stats[46];
                }
                else
                {
                    thrustBlock.ThrustMultiplier = 1.0f;
                }
                if (stats.ContainsKey(46) && stats[46] != 0)
                {
                    thrustBlock.PowerConsumptionMultiplier = 1 / stats[46];
                }
                else
                {
                    thrustBlock.PowerConsumptionMultiplier = 1.0f;
                }
            }

            if (reactorBlock != null)
            {
                if (stats.ContainsKey(28) && stats[28] != 0)
                {
                    reactorBlock.PowerOutputMultiplier = stats[28];
                }
                else
                {
                    reactorBlock.PowerOutputMultiplier = 1.0f;
                }
                //MyLog.Default.WriteLineAndConsole($"Reactor upgraded - {reactorBlock.PowerOutputMultiplier}");
                //Log.ChatError($"SetMultiplier to: {reactorBlock.PowerOutputMultiplier}");
            }

            if (gyroBlock != null)
            {
                if (stats.ContainsKey(28) && stats[28] != 0)
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


            if (generatorBlock != null)
            {
                if (stats.ContainsKey(29) && stats[29] != 0)
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

            if (drillBlock != null)
            {
                if (stats.ContainsKey(27) && stats[27] != 0)
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
        //This doesnt seem to work when expected, not sure why, is this even needed?
        private static void OnLimitedBlockCreated(object limitedBlock)
        {
            /*MyAPIGateway.Parallel.StartBackground(() =>
            {
                MyAPIGateway.Parallel.Sleep(5000);
                if (!isServer)
                {
                    MyAPIGateway.Utilities.InvokeOnGameThread(() =>
                    {
                        var tBlock = SpecBlockHooks.GetLimitedBlockBlock(limitedBlock);
                        if (tBlock == null)
                        {
                            MyLog.Default.WriteLineAndConsole($"Limited Block is null!!");
                            return;
                        }

                        RequestBlockUpgrade(MyAPIGateway.Multiplayer.MyId);
                    });
                }
            });*/
            
            if (!allowOnLimitedBlockCreated && isServer)
            {
                limitedBlocksToProcess.Add(limitedBlock);
                return;
            }

            if (isServer)
            {
                foreach (var block in limitedBlocksToProcess)
                    UpgradeBlock(block);

                if (limitedBlock == null)
                {
                    limitedBlocksToProcess.Clear();
                    return;
                }
            }

            MyAPIGateway.Parallel.StartBackground(() =>
            {
                MyAPIGateway.Parallel.Sleep(5000);
                MyAPIGateway.Utilities.InvokeOnGameThread(() => { UpgradeBlock(limitedBlock); });
                
            });
        }

        private static void OnSpecBlockDestroyed(object specBlock)
        {
            var tBlock = SpecBlockHooks.GetBlockSpecCore(specBlock);
            if (tBlock == null)
                return;

            IMyCubeGrid grid = tBlock.CubeGrid;
            List<IMyCubeGrid> grids = new List<IMyCubeGrid>();
            MyAPIGateway.GridGroups.GetGroup(grid, GridLinkTypeEnum.Mechanical, grids);
            RunUpgrades(specBlock, grids, true);   
        }

        /*public static void MessageHandler(byte[] data)
        {
            try
            {
                var package = MyAPIGateway.Utilities.SerializeFromBinary<CommsPackage>(data);
                if (package == null) return;

                if (package.Type == DataType.RequestSettings)
                {
                    var packet = MyAPIGateway.Utilities.SerializeFromBinary<ObjectContainer>(package.Data);
                    if (packet == null) return;


                    return;
                }

                if (package.Type == DataType.SendSettings)
                {
                    var packet = MyAPIGateway.Utilities.SerializeFromBinary<ObjectContainer>(package.Data);
                    if (packet == null) return;

                    return;
                }
            }
            catch { };
        }

        public static void RequestBlockUpgrade(ulong steamId)
        {
            ObjectContainer objectContainer = new ObjectContainer()
            {
                steamId = steamId
            };

            CommsPackage package = new CommsPackage(DataType.RequestBlockUpgrade, objectContainer);
            var sendData = MyAPIGateway.Utilities.SerializeToBinary(package);
            MyAPIGateway.Multiplayer.SendMessageToServer(5561, sendData);
        }

        public enum DataType
        {

            RequestBlockUpgrade,
            SendBlockUpgrade,
        }

        [ProtoContract]
        public class ObjectContainer
        {
            [ProtoMember(1)] public ulong steamId;
            [ProtoMember(2)] public long blockId;
            [ProtoMember(3)] public float upgradeValue;



        }

        [ProtoContract]
        public class CommsPackage
        {
            [ProtoMember(1)]
            public DataType Type;

            [ProtoMember(2)]
            public byte[] Data;

            public CommsPackage()
            {
                Type = DataType.RequestBlockUpgrade;
                Data = new byte[0];
            }

            public CommsPackage(DataType type, ObjectContainer oc)
            {
                Type = type;
                Data = MyAPIGateway.Utilities.SerializeToBinary(oc);
            }
        }*/

        protected override void UnloadData()
        {
            //MyAPIGateway.Multiplayer.UnregisterMessageHandler(5561, MessageHandler);

        }
    }
}