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

        public float PowerGeneration = 0;
        public float PowerCapacity = 0;
        public float StoredPower = 0;

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
                    S_FusionArm newArm = new S_FusionArm((MyEntity) newPart, "Caster_Feeder");
                    if (newArm.IsValid)
                        Arms.Add(newArm);
                    break;
            }

            if (newPart is IMyThrust)
                Thrusters.Add((IMyThrust) newPart);

            if (newPart is IMyReactor)
                Reactors.Add((IMyReactor)newPart);

            UpdatePower();
        }

        public void RemovePart(IMyCubeBlock part)
        {
            if (part is IMyThrust)
                Thrusters.Remove((IMyThrust) part);

            foreach (var arm in Arms)
            {
                if (arm.Parts.Contains(part))
                {
                    Arms.Remove(arm);
                    UpdatePower();
                    break;
                }
            }
        }

        private void UpdatePower()
        {
            PowerGeneration = 0;
            PowerCapacity = 0;

            foreach (var arm in Arms)
            {
                PowerGeneration += arm.PowerGeneration;
                PowerCapacity += arm.PowerStorage;
            }

            //IMyReactor basePart = (IMyReactor) ModularAPI.GetBasePart(PhysicalAssemblyId);
            //
            //float desiredPower = Arms.Count * GetTotalArmBlocks();
            //
            //float actualPower = desiredPower;
            //
            //foreach (var thrust in Thrusters)
            //{
            //    SyncMultipliers.ThrusterOutput(thrust, desiredPower * 80000);
            //    actualPower -= desiredPower / 4;
            //}
            //
            //SyncMultipliers.ReactorOutput(basePart, actualPower);
            //
            //MyAPIGateway.Utilities.ShowMessage("Fusion Systems", basePart.PowerOutputMultiplier + " | " + actualPower);
        }

        public void UpdateTick()
        {
            StoredPower += PowerGeneration;

            if (StoredPower > PowerCapacity)
                StoredPower = PowerCapacity;
        }
    }
}
