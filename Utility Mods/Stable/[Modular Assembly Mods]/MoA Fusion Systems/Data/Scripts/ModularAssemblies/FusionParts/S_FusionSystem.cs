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
        const float MegawattsPerFusionPower = 50;

        static ModularDefinitionAPI ModularAPI => ModularDefinition.ModularAPI;

        public List<S_FusionArm> Arms = new List<S_FusionArm>();
        public List<IMyThrust> Thrusters = new List<IMyThrust>();
        public List<IMyReactor> Reactors = new List<IMyReactor>();
        public int PhysicalAssemblyId = -1;

        public float PowerGeneration = 0;
        public float PowerCapacity = 0;
        public float PowerStored = 0;

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
            if (part is IMyReactor)
                Reactors.Remove((IMyReactor) part);

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
            float totalPowerUsage = 0;

            foreach (var arm in Arms)
            {
                PowerGeneration += arm.PowerGeneration;
                PowerCapacity += arm.PowerStorage;
            }

            // Math for slider on reactor parts to allow for a power <-> efficiency tradeoff.
            foreach (var reactor in Reactors)
            {
                if (reactor.BlockDefinition.SubtypeName == "Caster_Controller")
                    continue;

                // Temporary percentage of fusion output to use. Should be slider.
                float temp_reactorConsumptionMultiplier = 0.5f;
                float reactorEfficiencyMultiplier = 1 / (0.5f + temp_reactorConsumptionMultiplier);

                // Power generation consumed (per second)
                float powerConsumption = PowerGeneration * 60 * temp_reactorConsumptionMultiplier;
                // Power generated (per second)
                float reactorOutput = reactorEfficiencyMultiplier * powerConsumption * MegawattsPerFusionPower;

                MyAPIGateway.Utilities.ShowNotification($"Output: {reactorOutput}/{PowerGeneration*60*MegawattsPerFusionPower}", 1000/60);
                MyAPIGateway.Utilities.ShowNotification($"Input: {powerConsumption}/{PowerGeneration*60}", 1000 / 60);
                MyAPIGateway.Utilities.ShowNotification($"Efficiency: {reactorEfficiencyMultiplier*100}%", 1000 / 60);

                totalPowerUsage += powerConsumption / 60;
                //SyncMultipliers.ReactorOutput(reactor, reactorOutput);
            }

            PowerGeneration -= totalPowerUsage;

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
            PowerStored += PowerGeneration;

            if (PowerStored > PowerCapacity)
                PowerStored = PowerCapacity;

            // TEMPORARY, TO BE REMOVED. SHOULD ONLY TRIGGER ON SYSTEM EDIT.
            UpdatePower();
        }
    }
}
