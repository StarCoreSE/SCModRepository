using Sandbox.ModAPI;
using System;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.ModAPI;
using VRageMath;
using VRage.Utils;
using Sandbox.Game.Entities;
using VRage.Game.Entity;
using System.Collections.Generic;
using Sandbox.Game.EntityComponents;
using System.Text;
using Sandbox.ModAPI.Interfaces.Terminal;
using VRage.Game.ModAPI.Interfaces;
using static VRage.Game.ObjectBuilders.Definitions.MyObjectBuilder_GameDefinition;

namespace StealthSystem
{
    public class SinkComp : MyEntityComponentBase
    {
        internal IMyFunctionalBlock Block;
        internal IMyCubeGrid Grid;
        internal MyResourceSinkComponent Sink;
        internal MyResourceDistributorComponent Source;
        internal IMyTerminalControlOnOffSwitch ShowInToolbarSwitch;

        internal SinkRepo Repo;
        internal DriveComp Master;
        internal Definitions.SinkDefinition Definition;

        internal Color OldColour;
        internal MyOrientedBoundingBoxD DamageBox;
        internal MyOrientedBoundingBoxD BlockBox;

        internal bool Inited;
        internal bool PowerDirty;
        internal bool Working = true;
        internal bool SufficientPower;
        internal bool Accumulating;
        internal bool Radiating;
        internal bool WasAccumulating;
        internal bool WorkingChanged;

        internal long CompTick;
        internal byte HeatPercent;

        private StealthSession _session;

        internal SinkComp(IMyFunctionalBlock sinkBlock, Definitions.SinkDefinition def, StealthSession session)
        {
            _session = session;

            Block = sinkBlock;
            Definition = def;
        }

        public override void OnBeforeRemovedFromContainer()
        {
            base.OnBeforeRemovedFromContainer();

            Close();
        }

        public override bool IsSerialized()
        {
            if (Block.Storage == null || Repo == null) return false;

            Repo.Sync(this);

            Block.Storage[_session.CompDataGuid] = Convert.ToBase64String(MyAPIGateway.Utilities.SerializeToBinary(Repo));

            return false;
        }

        internal void Init()
        {
            Grid = Block.CubeGrid;

            //Block.IsWorkingChanged += IsWorkingChanged;

            Block.Components.Add(this);
            CompTick = Block.EntityId % 20;

            Block.SetEmissiveParts(StealthSession.RADIANT_EMISSIVE, Color.DarkSlateGray, 0.1f);

            SinkInit();
            StorageInit();

            Source.SystemChanged += SourceChanged;

            Inited = true;

            if (!_session.IsDedicated)
            {
                GetShowInToolbarSwitch();
                Block.AppendingCustomInfo += AppendingCustomData;
            }
        }

        internal void Close()
        {
            GridComp gridComp;
            if (_session.GridMap.TryGetValue(Grid, out gridComp))
            {
                gridComp.HeatComps.Remove(this);
            }

            //Block.IsWorkingChanged -= IsWorkingChanged;

            Source.SystemChanged -= SourceChanged;

            if (!_session.IsDedicated)
                Block.AppendingCustomInfo -= AppendingCustomData;

            Clean();
        }

        internal void Clean()
        {
            Block = null;
            Grid = null;
            Sink = null;
            Source = null;
            ShowInToolbarSwitch = null;

            Repo = null;
            Master = null;
        }

        private void IsWorkingChanged(IMyCubeBlock block)
        {
            Working = block.IsWorking;
        }

        private void AppendingCustomData(IMyTerminalBlock block, StringBuilder builder)
        {
            var status = !Working ? "Offline" : !SufficientPower ? "Insufficient Power" : Radiating ? "Venting ඞ" : Accumulating ? "Accumulating Heat" : "Ready";

            builder.Append("Heat Sink Status: ")
                .Append(status)
                .Append("\n")
                .Append("Stored Heat: ")
                .Append($"{HeatPercent}%");
        }

        private void SourceChanged()
        {
            PowerDirty = true;
        }

        internal void GridChange(GridComp gridComp)
        {
            gridComp.HeatComps.Remove(this);

            Grid = Block.CubeGrid;

            var newGridComp = _session.GridMap[Grid];
            newGridComp.HeatComps.Add(this);

            Source = Grid.ResourceDistributor as MyResourceDistributorComponent;
        }

        internal void UpdateStatus()
        {
            if (PowerDirty || Source == null)
            {
                Source = Grid.ResourceDistributor as MyResourceDistributorComponent;
                PowerDirty = false;
            }
            var available = Source.MaxAvailableResourceByType(MyResourceDistributorComponent.ElectricityId, (MyCubeGrid)Grid) - Source.TotalRequiredInputByType(MyResourceDistributorComponent.ElectricityId, (MyCubeGrid)Grid);
            SufficientPower = available > 0;

            var isWorking = Block.IsFunctional && Block.Enabled && SufficientPower;
            if (isWorking != Working)
            {
                Working = isWorking;
                WorkingChanged = true;
            }
            //SufficientPower = StealthActive ? available >= 0 : available >= RequiredPower;
            //Online = Block.IsFunctional && Block.Enabled && available > 0;

            if (!_session.IsDedicated)
                SetEmissiveColor();
        }

        internal void SetEmissiveColor()
        {
            if (Radiating)
                Block.SetEmissiveParts(StealthSession.RADIANT_EMISSIVE, Color.DarkRed, HeatPercent / 200);

            var emissiveColor = !Block.IsFunctional ? Color.Black : !Working ? EmissiveValues.RED : Accumulating ? Color.Cyan : Radiating ? Color.OrangeRed : EmissiveValues.GREEN;
            if (emissiveColor == OldColour)
                return;

            OldColour = emissiveColor;
            Block.SetEmissiveParts(StealthSession.STATUS_EMISSIVE, emissiveColor, 1f);
        }

        internal List<MyOrientedBoundingBoxD> BlockBoxes = new List<MyOrientedBoundingBoxD>();

        internal void DamageBlocks()
        {
            var large = Grid.GridSizeEnum == MyCubeSize.Large;
            var box = large ? _session.LargeBox : _session.SmallBox;
            var radius = large ? 7.25 : 6.45;
            var offset = large ? 7.75 : 7.25;
            var matrix = Block.WorldMatrix;
            matrix.Translation += Block.WorldMatrix.Up * offset;
            var obb = new MyOrientedBoundingBoxD(box, matrix);
            //DamageBox = obb;

            var hits = new List<MyEntity>();
            MyGamePruningStructure.GetAllEntitiesInOBB(ref obb, hits);

            //BlockBoxes.Clear();
            for (int i = 0; i < hits.Count; i++)
            {
                var ent = hits[i];

                var dest = ent as IMyDestroyableObject;
                if (dest != null)
                {
                    var entObb = new MyOrientedBoundingBoxD(ent.PositionComp.LocalAABB, ent.PositionComp.WorldMatrixRef);
                    if (entObb.Contains(ref obb) != ContainmentType.Disjoint)
                        dest.DoDamage(9f, MyDamageType.Temperature, true);

                    continue;
                }

                var grid = ent as IMyCubeGrid;
                if (grid != null)
                {
                    var sphere = new BoundingSphereD(matrix.Translation, radius);
                    var slims = grid.GetBlocksInsideSphere(ref sphere);

                    for (int j = 0; j < slims.Count; j++)
                    {
                        var slim = slims[j];
                        var fat = slim.FatBlock;
                        MyOrientedBoundingBoxD blockBox;
                        if (fat == null)
                        {
                            var gridSize = (double)Grid.GridSize;
                            var aabb = new BoundingBoxD(slim.Min * gridSize - gridSize / 2, slim.Max * gridSize + gridSize / 2);
                            blockBox = new MyOrientedBoundingBoxD(aabb, grid.PositionComp.WorldMatrixRef);
                        }
                        else
                        {
                            blockBox = new MyOrientedBoundingBoxD(fat.PositionComp.LocalAABB, fat.PositionComp.WorldMatrixRef);
                        }

                        if (obb.Contains(ref blockBox) != ContainmentType.Disjoint)
                        {
                            slim.DoDamage(500f, MyDamageType.Temperature, true);
                            //BlockBoxes.Add(blockBox);
                        }

                    }
                }
            }
        }

        internal void SinkInit()
        {
            var sinkInfo = new MyResourceSinkInfo()
            {
                MaxRequiredInput = 0,
                RequiredInputFunc = PowerFunc,
                ResourceTypeId = MyResourceDistributorComponent.ElectricityId
            };

            Sink = Block.Components?.Get<MyResourceSinkComponent>();
            if (Sink != null)
            {
                Sink.RemoveType(ref sinkInfo.ResourceTypeId);
                Sink.AddType(ref sinkInfo);
            }
            else
            {
                Sink = new MyResourceSinkComponent();
                Sink.Init(MyStringHash.GetOrCompute("Utility"), sinkInfo);
                (Block as MyCubeBlock).Components.Add(Sink);
            }

            Source = Grid.ResourceDistributor as MyResourceDistributorComponent;
            if (Source != null)
                Source.AddSink(Sink);
            else
                Logs.WriteLine($"SinkComp.SinkInit() - Distributor null");

            Sink.Update();
        }

        private float PowerFunc()
        {
            if (!Working)
                return 0f;
            if (Accumulating)
                return Definition.Power;
            return 0.001f;
        }

        private void GetShowInToolbarSwitch()
        {
            List<IMyTerminalControl> items;
            MyAPIGateway.TerminalControls.GetControls<IMyUpgradeModule>(out items);

            foreach (var item in items)
            {

                if (item.Id == "ShowInToolbarConfig")
                {
                    ShowInToolbarSwitch = (IMyTerminalControlOnOffSwitch)item;
                    break;
                }
            }
        }

        private void StorageInit()
        {
            string rawData;
            SinkRepo loadRepo = null;
            if (Block.Storage == null)
            {
                Block.Storage = new MyModStorageComponent();
            }
            else if (Block.Storage.TryGetValue(_session.CompDataGuid, out rawData))
            {
                try
                {
                    var base64 = Convert.FromBase64String(rawData);
                    loadRepo = MyAPIGateway.Utilities.SerializeFromBinary<SinkRepo>(base64);
                }
                catch (Exception ex)
                {
                    Logs.WriteLine($"SinkComp - Exception at StorageInit() - {ex}");
                }
            }

            if (loadRepo != null)
            {
                Sync(loadRepo);
            }
            else
            {
                Repo = new SinkRepo();
            }
        }

        private void Sync(SinkRepo repo)
        {
            Repo = repo;

            Accumulating = repo.Accumulating;
            Radiating = repo.Radiating;
            HeatPercent = repo.HeatPercent;
        }

        public override string ComponentTypeDebugString => "StealthMod";

    }
}
