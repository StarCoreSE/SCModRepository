using Modular_Definitions.Data.Scripts.ModularAssemblies;
using Sandbox.ModAPI;
using Scripts.ModularAssemblies.Communication;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VRage.Game.Entity;
using VRage.Game.ModAPI;

namespace SCModRepository.Utility_Mods.Stable._Modular_Assembly_Mods_.MoA_Fusion_Systems.Data.Scripts.ModularAssemblies
{
    internal class S_FusionSystem
    {
        static ModularDefinitionAPI ModularAPI => ModularDefinition.ModularAPI;

        public List<S_FusionArm> Arms = new List<S_FusionArm>();
        public List<IMyThrust> Thrusters = new List<IMyThrust>();
        public List<IMyReactor> Reactors = new List<IMyReactor>();
        public int PhysicalAssemblyId = -1;

        public S_FusionSystem(int physicalAssemblyId)
        {
            PhysicalAssemblyId = physicalAssemblyId;
        }

        public void AddPart(IMyCubeBlock newPart)
        {
            // Scan for 'arms' connected on both ends to the feeder block.
            switch (newPart.BlockDefinition.SubtypeName)
            {
                case "Caster_Accelerator_0":
                case "Caster_Accelerator_90":
                    MyEntity basePart = ModularAPI.GetBasePart(PhysicalAssemblyId);
                    S_FusionArm newArm = new S_FusionArm();
                    if (newArm.PerformScan((MyEntity) newPart, null, "Caster_Feeder"))
                        Arms.Add(newArm);
                    break;
            }

            if (newPart is IMyThrust)
                Thrusters.Add((IMyThrust) newPart);

            UpdatePower();
        }

        public void RemovePart(IMyCubeBlock part)
        {
            if (part is IMyThrust)
                Thrusters.Remove((IMyThrust) part);

            foreach (var arm in Arms)
            {
                if (arm.Contains((MyEntity) part))
                {
                    Arms.Remove(arm);
                    UpdatePower();
                    break;
                }
            }
        }

        private int GetTotalArmBlocks(int PhysicalAssemblyId)
        {
            int total = 0;

            foreach (var arm in Arms)
            {
                total += arm.StraightParts.Count;
                total += arm.CornerParts.Count;
            }

            return total;
        }

        private void UpdatePower()
        {
            IMyReactor basePart = (IMyReactor)ModularAPI.GetBasePart(PhysicalAssemblyId);

            float desiredPower = Arms.Count * GetTotalArmBlocks(PhysicalAssemblyId);
            float actualPower = desiredPower;

            foreach (var thrust in Thrusters)
            {
                SyncMultipliers.ThrusterOutput(thrust, desiredPower * 80000);
                actualPower -= desiredPower / 4;
            }

            SyncMultipliers.ReactorOutput(basePart, actualPower);

            MyAPIGateway.Utilities.ShowMessage("Fusion Systems", basePart.PowerOutputMultiplier + " | " + actualPower);
        }
    }
}
