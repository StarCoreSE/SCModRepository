using Sandbox.Common.ObjectBuilders;
using Sandbox.ModAPI;
using System.Collections.Generic;
using VRage.Game.ModAPI;

namespace StarCore.PowerControl.ModSystem
{
    public static class Overclock
    {

        //public static void Reactor(IMyTerminalBlock block, float value, bool find)
        //{
        //    if (find)
        //    {
        //        List<IMySlimBlock> Blocks = new List<IMySlimBlock>();
        //        block.CubeGrid.GetBlocks(Blocks, (s) =>
        //        {
        //            if (s.FatBlock is IMyReactor)
        //                return true;
        //            return false;
        //        });

        //        foreach (IMySlimBlock b in Blocks)
        //        {
        //            IMyReactor r = (IMyReactor)b.FatBlock;
        //            r.PowerOutputMultiplier = value;
        //        }
        //    }
        //    else
        //    {
        //        IMyReactor r = (IMyReactor)block;
        //        r.PowerOutputMultiplier = value;
        //    }
        //}

        //public static void GasGenerator(IMyTerminalBlock block, float value, bool find)
        //{
        //    if (find)
        //    {
        //        List<IMySlimBlock> Blocks = new List<IMySlimBlock>();
        //        block.CubeGrid.GetBlocks(Blocks, (s) =>
        //        {
        //            if (s.FatBlock is IMyGasGenerator)
        //                return true;
        //            return false;
        //        });

        //        foreach (IMySlimBlock b in Blocks)
        //        {
        //            IMyGasGenerator r = (IMyGasGenerator)b.FatBlock;
        //            r.PowerConsumptionMultiplier = value;
        //            r.ProductionCapacityMultiplier = 1 / value;
        //        }
        //    }
        //    else
        //    {
        //        IMyGasGenerator r = (IMyGasGenerator)block;
        //        r.PowerConsumptionMultiplier = value;
        //        r.ProductionCapacityMultiplier = 1 / value;
        //    }
        //}

        //public static void Gyro(IMyTerminalBlock block, float value, bool find)
        //{
        //    if (find)
        //    {
        //        List<IMySlimBlock> Blocks = new List<IMySlimBlock>();
        //        block.CubeGrid.GetBlocks(Blocks, (s) =>
        //        {
        //            if (s.FatBlock is IMyGyro)
        //                return true;
        //            return false;
        //        });

        //        foreach (IMySlimBlock b in Blocks)
        //        {
        //            IMyGyro r = (IMyGyro)b.FatBlock;
        //            r.PowerConsumptionMultiplier = value;
        //            r.GyroStrengthMultiplier = value;
        //        }
        //    }
        //    else
        //    {
        //        IMyGyro r = (IMyGyro)block;
        //        r.PowerConsumptionMultiplier = value;
        //        r.GyroStrengthMultiplier = value;
        //    }
        //}

        public static void Thrust(IMyTerminalBlock block, float value, bool find)
        {
            if (find)
            {
                List<IMySlimBlock> Blocks = new List<IMySlimBlock>();
                block.CubeGrid.GetBlocks(Blocks, (s) =>
                {
                    if (s.FatBlock is IMyThrust)
                        return true;
                    return false;
                });

                foreach (IMySlimBlock b in Blocks)
                {
                    IMyThrust r = (IMyThrust)b.FatBlock;
                    r.PowerConsumptionMultiplier = value;
                    r.ThrustMultiplier = value;
                }
            }
            else
            {
                IMyThrust r = (IMyThrust)block;
                r.PowerConsumptionMultiplier = value;
                r.ThrustMultiplier = value;
            }
        }

        //public static void Drill(IMyTerminalBlock block, float value, bool find)
        //{
        //    if (find)
        //    {
        //        List<IMySlimBlock> Blocks = new List<IMySlimBlock>();
        //        block.CubeGrid.GetBlocks(Blocks, (s) =>
        //        {
        //            if (s.FatBlock is IMyShipDrill)
        //                return true;
        //            return false;
        //        });

        //        foreach (IMySlimBlock b in Blocks)
        //        {
        //            IMyShipDrill r = (IMyShipDrill)b.FatBlock;
        //            r.PowerConsumptionMultiplier = value;
        //            r.DrillHarvestMultiplier = value;
        //        }
        //    }
        //    else
        //    {
        //        IMyShipDrill r = (IMyShipDrill)block;
        //        r.PowerConsumptionMultiplier = value;
        //        r.DrillHarvestMultiplier = value;
        //    }
        //}

        public static void Clear(IMyCubeGrid grid)
        {
            List<IMySlimBlock> Blocks = new List<IMySlimBlock>();
            grid.GetBlocks(Blocks, (b) =>
            {
                if (b.FatBlock != null)
                    return true;
                return false;
            });

            foreach (IMySlimBlock B in Blocks)
            {
                //if (B.FatBlock is IMyReactor)
                //    Overclock.Reactor((IMyTerminalBlock)B.FatBlock, 1, false);
                //if (B.FatBlock is IMyGasGenerator)
                //    Overclock.GasGenerator((IMyTerminalBlock)B.FatBlock, 1, false);
                //if (B.FatBlock is IMyGyro)
                //    Overclock.Gyro((IMyTerminalBlock)B.FatBlock, 1, false);
                if (B.FatBlock is IMyThrust)
                    Overclock.Thrust((IMyTerminalBlock)B.FatBlock, 1, false);
                //if (B.FatBlock is IMyShipDrill)
                //    Overclock.Drill((IMyTerminalBlock)B.FatBlock, 1, false);
            }
        }
    }
}
