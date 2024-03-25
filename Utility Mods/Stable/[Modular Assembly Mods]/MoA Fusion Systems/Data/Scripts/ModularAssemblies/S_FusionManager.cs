using Modular_Definitions.Data.Scripts.ModularAssemblies;
using Sandbox.ModAPI;
using Scripts.ModularAssemblies.Communication;
using Scripts.ModularAssemblies.Debug;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VRage.Game.Entity;
using VRage.Game.ModAPI;
using VRage.GameServices;
using VRageMath;

namespace Scripts.ModularAssemblies
{

    internal class S_FusionManager
    {
        static ModularDefinitionAPI ModularAPI => ModularDefinition.ModularAPI;

        public static S_FusionManager I = new S_FusionManager();
        public ModularDefinition Definition;

        public void Load()
        {
            I = this;
        }

        public void Unload()
        {
            I = null;
        }

        public Dictionary<int, List<MyEntity[]>> Example_ValidArms = new Dictionary<int, List<MyEntity[]>>();
        public List<MyEntity> Example_BufferArm = new List<MyEntity>();
        public Dictionary<int, List<IMyThrust>> Example_Thrusters = new Dictionary<int, List<IMyThrust>>();
        int StopHits = 0;

        private int GetNumBlocksInArm(int PhysicalAssemblyId)
        {
            int total = 0;

            foreach (var arm in Example_ValidArms[PhysicalAssemblyId])
                total += arm.Length;

            return total;
        }

        private bool Example_ScanArm(MyEntity blockEntity, MyEntity prevScan, string StopAt)
        {
            if (ModularAPI.IsDebug())
                DebugDraw.AddGridPoint(((IMyCubeBlock)blockEntity).Position, ((IMyCubeBlock)blockEntity).CubeGrid, Color.Blue, 2);
            Example_BufferArm.Add(blockEntity);

            MyEntity[] connectedBlocks = ModularAPI.GetConnectedBlocks(blockEntity, false);

            foreach (var connectedBlock in connectedBlocks)
            {
                string connectedSubtype = ((IMyCubeBlock)connectedBlock).BlockDefinition.SubtypeName;
                if (connectedSubtype == StopAt)
                    StopHits++;

                if (connectedBlock != prevScan && connectedSubtype != StopAt)
                {
                    Example_ScanArm(connectedBlock, blockEntity, StopAt);
                }
            }

            return StopHits == 2;
        }

        private void UpdatePower(int PhysicalAssemblyId)
        {
            IMyReactor basePart = (IMyReactor)ModularAPI.GetBasePart(PhysicalAssemblyId);

            float desiredPower = Example_ValidArms[PhysicalAssemblyId].Count * GetNumBlocksInArm(PhysicalAssemblyId);
            float actualPower = desiredPower;

            foreach (var thrust in Example_Thrusters[PhysicalAssemblyId])
            {
                SyncMultipliers.ThrusterOutput(thrust, desiredPower * 80000);
                actualPower -= desiredPower / 4;
            }

            SyncMultipliers.ReactorOutput(basePart, actualPower);

            MyAPIGateway.Utilities.SendMessage(basePart.PowerOutputMultiplier + " | " + actualPower);
        }

        public void OnPartAdd(int PhysicalAssemblyId, MyEntity NewBlockEntity, bool IsBaseBlock)
        {
            if (!Example_ValidArms.ContainsKey(PhysicalAssemblyId))
                Example_ValidArms.Add(PhysicalAssemblyId, new List<MyEntity[]>());
            if (!Example_Thrusters.ContainsKey(PhysicalAssemblyId))
                Example_Thrusters.Add(PhysicalAssemblyId, new List<IMyThrust>());

            // Scan for 'arms' connected on both ends to the feeder block.
            switch (((IMyCubeBlock)NewBlockEntity).BlockDefinition.SubtypeName)
            {
                case "Caster_Accelerator_0":
                case "Caster_Accelerator_90":
                    MyEntity basePart = ModularAPI.GetBasePart(PhysicalAssemblyId);
                    if (Example_ScanArm(NewBlockEntity, null, "Caster_Feeder"))
                        Example_ValidArms[PhysicalAssemblyId].Add(Example_BufferArm.ToArray());

                    Example_BufferArm.Clear();
                    StopHits = 0;
                    break;
            }

            if (NewBlockEntity is IMyThrust)
                Example_Thrusters[PhysicalAssemblyId].Add((IMyThrust)NewBlockEntity);

            UpdatePower(PhysicalAssemblyId);

            if (ModularAPI.IsDebug())
            {
                MyAPIGateway.Utilities.ShowNotification("Pass: Arms: " + Example_ValidArms[PhysicalAssemblyId].Count + " (Size " + Example_ValidArms[PhysicalAssemblyId][Example_ValidArms[PhysicalAssemblyId].Count - 1].Length + ")");
            }
        }

        public void OnPartRemove(int PhysicalAssemblyId, MyEntity BlockEntity, bool IsBaseBlock)
        {
            // Remove if the connection is broken.
            if (!IsBaseBlock)
            {
                if (BlockEntity is IMyThrust)
                    Example_Thrusters[PhysicalAssemblyId].Add((IMyThrust)BlockEntity);

                MyEntity[] armToRemove = null;
                foreach (var arm in Example_ValidArms[PhysicalAssemblyId])
                {
                    if (arm.Contains(BlockEntity))
                    {
                        armToRemove = arm;
                        break;
                    }
                }
                if (armToRemove != null)
                {
                    Example_ValidArms[PhysicalAssemblyId].Remove(armToRemove);

                    UpdatePower(PhysicalAssemblyId);
                }

                if (ModularAPI.IsDebug())
                    MyAPIGateway.Utilities.ShowNotification("Remove: Arms: " + Example_ValidArms[PhysicalAssemblyId].Count);
            }
            else
            {
                Example_ValidArms.Remove(PhysicalAssemblyId);
                Example_Thrusters.Remove(PhysicalAssemblyId);
            }
        }
    }
}
