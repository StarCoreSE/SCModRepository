using Data.Scripts.ModularAssemblies.FusionParts.FusionReactor;
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

namespace Scripts.ModularAssemblies.FusionParts
{
    internal class S_FusionSystem
    {
        public const float MegawattsPerFusionPower = 50;

        static ModularDefinitionAPI ModularAPI => ModularDefinition.ModularAPI;

        public List<S_FusionArm> Arms = new List<S_FusionArm>();
        public List<IMyThrust> Thrusters = new List<IMyThrust>();
        public List<FusionReactorLogic> Reactors = new List<FusionReactorLogic>();
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
            if (newPart == null)
                return;

            // Scan for 'arms' connected on both ends to the feeder block.
            switch (newPart.BlockDefinition.SubtypeName)
            {
                case "Caster_Accelerator_0":
                case "Caster_Accelerator_90":
                    S_FusionArm newArm = new S_FusionArm((MyEntity) newPart, "Caster_Feeder");
                    if (newArm.IsValid)
                    {
                        Arms.Add(newArm);
                        UpdatePower(true);
                    }
                    break;
            }

            if (newPart is IMyThrust)
                Thrusters.Add((IMyThrust) newPart);

            if (newPart is IMyReactor)
            {
                FusionReactorLogic logic = newPart.GameLogic.GetAs<FusionReactorLogic>();
                if (logic != null)
                {
                    Reactors.Add(logic);
                    logic.MemberSystem = this;
                    logic.UpdatePower(PowerGeneration, MegawattsPerFusionPower);
                }
            }

            UpdatePower();
        }

        public void RemovePart(IMyCubeBlock part)
        {
            if (part == null)
                return;

            if (part is IMyThrust)
                Thrusters.Remove((IMyThrust) part);
            if (part is IMyReactor)
            {
                FusionReactorLogic logic = part.GameLogic.GetAs<FusionReactorLogic>();
                logic.MemberSystem = null;
                Reactors.Remove(logic);
            }

            foreach (var arm in Arms)
            {
                if (arm.Parts.Contains(part))
                {
                    Arms.Remove(arm);
                    UpdatePower(true);
                    break;
                }
            }
        }

        private void UpdatePower(bool updateReactors = false)
        {
            float powerGeneration = 0;
            float powerCapacity = 0;
            float totalPowerUsage = 0;

            foreach (var arm in Arms)
            {
                powerGeneration += arm.PowerGeneration;
                powerCapacity += arm.PowerStorage;
            }

            // Math for slider on reactor parts to allow for a power <-> efficiency tradeoff.
            foreach (var reactor in Reactors)
            {
                totalPowerUsage += reactor?.PowerConsumption ?? 0;

                if (updateReactors)
                    reactor?.UpdatePower(powerGeneration, MegawattsPerFusionPower);
            }

            // Subtract power usage afterwards so that all reactors have the same stats.
            PowerGeneration = powerGeneration;
            PowerCapacity = powerCapacity;
            PowerGeneration -= totalPowerUsage;

            // Update PowerStored
            PowerStored += PowerGeneration;
            if (PowerStored > PowerCapacity)
                PowerStored = PowerCapacity;

            if (PowerStored <= 0)
            {
                PowerStored = 0;

                foreach (var reactor in Reactors)
                {
                    reactor?.UpdatePower(powerGeneration, 0);
                }
            }
        }

        public void UpdateTick()
        {
            UpdatePower();
        }
    }
}
