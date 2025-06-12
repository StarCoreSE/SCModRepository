using ProtoBuf;
using Sandbox.ModAPI;
using System;
using VRage.Game.ModAPI;
using VRage.ModAPI;
using System.Collections.Generic;
using VRageMath;
using Sandbox.Game.Entities;

namespace StealthSystem
{

    [ProtoContract]
    public class DriveRepo
    {
        [ProtoMember(1)] public bool StealthActive;
        [ProtoMember(2)] public bool CoolingDown;
        [ProtoMember(3)] public int RemainingDuration; //TimeElapsed
        [ProtoMember(4)] public int TotalTime;


        public void Sync(DriveComp comp)
        {
            StealthActive = comp.StealthActive;
            CoolingDown = comp.CoolingDown;
            //RemainingDuration = comp.RemainingDuration;
            RemainingDuration = comp.TimeElapsed;
            TotalTime = comp.TotalTime;
        }

    }

    [ProtoContract]
    public class SinkRepo
    {
        [ProtoMember(1)] public bool Accumulating;
        [ProtoMember(2)] public bool Radiating;
        [ProtoMember(3)] public byte HeatPercent;


        public void Sync(SinkComp comp)
        {
            Accumulating = comp.Accumulating;
            Radiating = comp.Radiating;
            HeatPercent = comp.HeatPercent;
        }

    }

    internal class WaterData
    {
        public WaterData(MyPlanet planet)
        {
            Planet = planet;
            WaterId = planet.EntityId;
        }

        internal MyPlanet Planet;
        internal long WaterId;
        internal Vector3D Centre;
        internal float Radius;
    }

    internal class GridComp
    {
        internal List<DriveComp> StealthComps = new List<DriveComp>();
        internal List<SinkComp> HeatComps = new List<SinkComp>();

        internal List<IMyFunctionalBlock> ShieldBlocks = new List<IMyFunctionalBlock>();
        internal List<IMyUserControllableGun> Turrets;

        internal DriveComp MasterComp;
        internal GroupMap GroupMap;
        internal IMyCubeGrid Grid;
        internal MyPlanet Planet;
        internal BoundingSphereD Water;

        internal bool GroupsDirty;
        internal bool Revealed;
        internal bool Underwater;
        internal bool WaterValid = true;
        internal bool DisableShields;
        internal bool DisableWeapons;

        internal int DamageTaken;
        internal int SinkBonus;

        private StealthSession _session;

        internal void Init(IMyCubeGrid grid, StealthSession session)
        {
            _session = session;

            Grid = grid;

            Grid.OnBlockAdded += BlockAdded;
            Grid.OnBlockRemoved += BlockRemoved;

            DisableShields = _session.DisableShields;
            DisableWeapons = _session.DisableWeapons && !_session.WcActive;

            if (DisableWeapons) Turrets = new List<IMyUserControllableGun>();

            var group = MyAPIGateway.GridGroups.GetGridGroup(GridLinkTypeEnum.Physical, grid);
            if (group != null)
            {
                GroupMap map;
                if (_session.GridGroupMap.TryGetValue(group, out map))
                    GroupMap = map;
            }
            else Logs.WriteLine("group null at GridComp.Init()");

            GroupsDirty = true;

            if (!DisableShields && !DisableWeapons) return;

            var blocks = grid.GetFatBlocks<IMyCubeBlock>();
            foreach (var block in blocks)
            {
                if (block?.BlockDefinition == null) continue;

                if (DisableShields && _session.ShieldBlocks.Contains(block.BlockDefinition.SubtypeName))
                    ShieldBlocks.Add(block as IMyFunctionalBlock);

                if (DisableWeapons && block is IMyUserControllableGun)
                    Turrets.Add(block as IMyUserControllableGun);
            }

            if (_session.TrackWater)
            {
                Planet = MyGamePruningStructure.GetClosestPlanet(Grid.PositionComp.WorldAABB.Center);

                WaterData waterData;
                if (Planet != null && _session.WaterMap.TryGetValue(Planet.EntityId, out waterData))
                {
                    Water = new BoundingSphereD(waterData.Centre, waterData.Radius + _session.WaterTransitionDepth);

                    var planetVector = Grid.PositionComp.WorldAABB.Center - waterData.Centre;
                    var radius = waterData.Radius + _session.WaterTransitionDepth;
                    var radiusSqr = radius * radius;
                    if (planetVector.LengthSquared() < radiusSqr)
                    {
                        Underwater = true;

                        var obb = new MyOrientedBoundingBoxD(Grid.PositionComp.LocalAABB, Grid.PositionComp.WorldMatrixRef);
                        obb.GetCorners(_session.ObbCorners, 0);
                        for (int j = 0; j < 8; j++)
                        {
                            var corner = _session.ObbCorners[j];
                            planetVector = corner = waterData.Centre;
                            if (planetVector.LengthSquared() > radiusSqr)
                            {
                                Underwater = false;
                                break;
                            }
                        }
                    }
                    WaterValid = Underwater == _session.WorkInWater;
                }
            }
        }

        private void BlockAdded(IMySlimBlock slim)
        {
            if (slim.FatBlock == null) return;

            var fat = slim.FatBlock;
            if (fat is IMyUpgradeModule)
            {
                var module = fat as IMyUpgradeModule;
                if (_session.DriveDefinitions.ContainsKey(module.BlockDefinition.SubtypeName))
                {
                    if (!_session.DriveMap.ContainsKey(module.EntityId))
                    {
                        Logs.WriteLine("BlockAdded() - Drive not in map!");
                        return;
                    }

                    var dComp = _session.DriveMap[module.EntityId];

                    if (!dComp.Inited)
                    {
                        Logs.WriteLine("BlockAdded() - Drive not yet Inited!");
                        return;
                    }

                    var gridComp = _session.GridMap[Grid];

                    if (gridComp.StealthComps.Contains(dComp))
                    {
                        Logs.WriteLine("BlockAdded() - Drive already in correct GridComp!");
                        return;
                    }

                    try
                    {
                        dComp.GridChange();
                    }
                    catch (Exception ex)
                    {
                        Logs.WriteLine($"Exception in GridChange() {ex}");
                    }
                }
            }

            if (DisableShields && _session.ShieldBlocks.Contains(fat.BlockDefinition.SubtypeName))
                ShieldBlocks.Add(fat as IMyFunctionalBlock);

            if (DisableWeapons && fat is IMyUserControllableGun)
                Turrets.Add(fat as IMyUserControllableGun);
        }

        private void BlockRemoved(IMySlimBlock slim)
        {
            if (slim.FatBlock == null) return;

            var func = slim.FatBlock as IMyFunctionalBlock;
            if (DisableShields && func != null && ShieldBlocks.Contains(func))
                ShieldBlocks.Remove(func);

            var wep = func as IMyUserControllableGun;
            if (DisableWeapons && wep != null && Turrets.Contains(wep))
                Turrets.Remove(wep);
        }

        internal void Clean()
        {
            Grid.OnBlockAdded -= BlockAdded;
            Grid.OnBlockRemoved -= BlockRemoved;

            StealthComps.Clear();
            HeatComps.Clear();
            ShieldBlocks.Clear();

            if (DisableWeapons) Turrets.Clear();

            MasterComp = null;
            GroupMap = null;
            Grid = null;

            GroupsDirty = false;
            Revealed = false;
            DamageTaken = 0;

            _session = null;
        }
    }

    internal class GroupMap
    {
        private StealthSession _session;

        public IMyGridGroupData GroupData;

        internal List<IMyCubeGrid> ConnectedGrids = new List<IMyCubeGrid>();

        internal List<IMySlimBlock> SlimBlocks = new List<IMySlimBlock>();
        internal HashSet<IMyEntity> Children = new HashSet<IMyEntity>();

        internal void Init(IMyGridGroupData data, StealthSession session)
        {
            GroupData = data;

            _session = session;
        }

        public void OnGridAdded(IMyGridGroupData newGroup, IMyCubeGrid grid, IMyGridGroupData oldGroup)
        {
            try
            {
                ConnectedGrids.Add(grid);

                GridComp gridComp;
                if (!_session.GridMap.TryGetValue(grid, out gridComp))
                    return;

                gridComp.GroupMap = this;
                gridComp.GroupsDirty = true;

                bool thisActive = false;
                var thisMaster = gridComp.MasterComp;
                if (thisMaster != null && thisMaster.StealthActive) //Added grid has active drive
                    thisActive = true;
                else if (((uint)grid.Flags & StealthSession.IsStealthedFlag) > 0) //Added grid is being stealthed by another grid
                    return;

                var newSubgrids = new List<IMyCubeGrid>();
                GridComp subgridComp;
                DriveComp subgridMaster = null;

                GroupData.GetGrids(newSubgrids);
                for (int i = 0; i < newSubgrids.Count; i++)
                {
                    var newSubgrid = newSubgrids[i];
                    if (newSubgrid == grid) continue;

                    if (thisActive)
                    {
                        if (((uint)newSubgrid.Flags & StealthSession.IsStealthedFlag) > 0) continue; //Other grid already stealthed

                        newSubgrid.Flags |= _session.StealthFlag;

                        if (!_session.IsDedicated && !thisMaster.VisibleToClient)
                            StealthConnectedGrid(newSubgrid, thisMaster, true);

                        //continue;
                    }

                    if (!_session.GridMap.TryGetValue(newSubgrid, out subgridComp))
                        continue;

                    if (thisActive) //Reenable shield emitters/vanilla turrets since previously connected grid is no longer stealthed
                    {
                        if (gridComp.DisableShields)
                            thisMaster.DisableShields(subgridComp);

                        if (gridComp.DisableWeapons)
                            thisMaster.DisableTurrets(subgridComp);

                        continue;
                    }

                    subgridMaster = subgridComp.MasterComp;
                    if (subgridMaster == null) continue;

                    if (subgridMaster.StealthActive) //Other grid has active drive so stealth this grid
                    {
                        grid.Flags |= _session.StealthFlag;

                        if (!_session.IsDedicated && !subgridMaster.VisibleToClient)
                            StealthConnectedGrid(grid, subgridMaster, true);

                        if (gridComp.DisableShields)
                            subgridMaster.DisableShields(gridComp);

                        if (gridComp.DisableWeapons)
                            subgridMaster.DisableTurrets(gridComp);

                        return;
                    }
                }
            }
            catch (Exception ex)
            {
                Logs.WriteLine($"Exception in OnGridAdded(): {ex}");
            }

        }

        public void OnGridRemoved(IMyGridGroupData oldGroup, IMyCubeGrid grid, IMyGridGroupData newGroup)
        {
            try
            {
                ConnectedGrids.Remove(grid);

                GridComp gridComp;
                if (!_session.GridMap.TryGetValue(grid, out gridComp))
                    return;

                gridComp.GroupsDirty = true;

                bool thisActive = false;
                var thisMaster = gridComp.MasterComp;
                if (thisMaster != null && thisMaster.StealthActive) //Removed grid has active drive
                    thisActive = true;
                //else if (((uint)grid.Flags & IsStealthedFlag) > 0)
                //    return;

                var formerSubgrids = new List<IMyCubeGrid>();
                GridComp subgridComp;
                DriveComp subgridMaster = null;

                GroupData.GetGrids(formerSubgrids);
                for (int i = 0; i < formerSubgrids.Count; i++)
                {
                    var formerSubgrid = formerSubgrids[i];
                    if (formerSubgrid == grid) continue;

                    if (thisActive) //Unstealth previously connected grid since this grid was providing stealth
                    {
                        formerSubgrid.Flags ^= _session.StealthFlag;

                        if (!_session.IsDedicated)
                            StealthConnectedGrid(formerSubgrid, thisMaster, false);
                    }

                    if (!_session.GridMap.TryGetValue(formerSubgrid, out subgridComp))
                        continue;

                    if (thisActive) //Reenable shield emitters/vanilla turrets since previously connected grid is no longer stealthed
                    {
                        if (gridComp.DisableShields)
                            thisMaster.ReEnableShields(subgridComp);

                        if (gridComp.DisableWeapons)
                            thisMaster.ReEnableTurrets(subgridComp);

                        continue;
                    }

                    //We only keep going if the removed grid wasn't providing stealth
                    //We check if the grid it was connected to was stealthing it

                    subgridMaster = subgridComp.MasterComp;
                    if (subgridMaster == null) continue;

                    if (subgridMaster.StealthActive) //Connected grid was providing stealth so destealth this
                    {
                        grid.Flags ^= _session.StealthFlag;

                        if (!_session.IsDedicated)
                            StealthConnectedGrid(grid, subgridMaster, false);

                        if (gridComp.DisableShields)
                            subgridMaster.ReEnableShields(gridComp);

                        if (gridComp.DisableWeapons)
                            subgridMaster.ReEnableTurrets(gridComp);

                        return;
                    }
                }
            }
            catch (Exception ex)
            {
                Logs.WriteLine($"Exception in OnGridRemoved(): {ex}");
            }

        }

        internal void StealthConnectedGrid(IMyCubeGrid grid, DriveComp comp, bool stealth)
        {
            if (stealth) _session.StealthedGrids.Add(grid);
            else _session.StealthedGrids.Remove(grid);

            grid.GetBlocks(SlimBlocks);

            var dither = stealth ? _session.Transparency : 0f;
            foreach (var slim in SlimBlocks)
            {
                var fatBlock = slim.FatBlock;
                if (fatBlock == null)
                {
                    slim.Dithering = dither;
                    continue;
                }

                fatBlock.Render.Transparency = dither;
                fatBlock.Render.UpdateTransparency();

                fatBlock.Hierarchy.GetChildrenRecursive(Children);
                foreach (var child in Children)
                {
                    child.Render.Transparency = dither;
                    child.Render.UpdateTransparency();
                }
                Children.Clear();

                //var cockpit = fatBlock as IMyCockpit;
                //if (cockpit != null && cockpit.Pilot != null)
                //    cockpit.Pilot.Render.Visible = !add;

                var jump = fatBlock as IMyJumpDrive;
                if (jump != null)
                {
                    if (stealth) comp.JumpDrives.Add(jump, jump.CurrentStoredPower);
                    else comp.JumpDrives.Remove(jump);
                }
            }
            SlimBlocks.Clear();
        }

        internal void Clean()
        {
            GroupData = null;

            ConnectedGrids.Clear();

            SlimBlocks.Clear();
            Children.Clear();

            _session = null;
        }
    }

}
