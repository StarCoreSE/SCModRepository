using System;
using System.Collections.Generic;
using System.Linq;
using Sandbox.ModAPI;
using StarCore.FusionSystems.Communication;
using StarCore.FusionSystems.FusionParts.FusionReactor;
using StarCore.FusionSystems.FusionParts.FusionThruster;
using StarCore.FusionSystems.HeatParts;
using VRage.Game.ModAPI;

namespace StarCore.FusionSystems.
    FusionParts
{
    internal class S_FusionSystem
    {
        public const float MegawattsPerFusionPower = 32;
        public const float NewtonsPerFusionPower = 12800000;
        public readonly IMyCubeGrid Grid;

        public List<S_FusionArm> Arms = new List<S_FusionArm>();
        public int BlockCount;

        /// <summary>
        ///     Maximum power storage
        /// </summary>
        public float MaxPowerStored;

        public int PhysicalAssemblyId;

        /// <summary>
        ///     Total power consumed
        /// </summary>
        public float PowerConsumption;

        /// <summary>
        ///     Total power generated minus PowerConsumption
        /// </summary>
        public float PowerGeneration;

        public List<FusionReactorLogic> Reactors = new List<FusionReactorLogic>();
        public List<FusionThrusterLogic> Thrusters = new List<FusionThrusterLogic>();

        public S_FusionSystem(int physicalAssemblyId)
        {
            PhysicalAssemblyId = physicalAssemblyId;
            Grid = ModularApi.GetAssemblyGrid(physicalAssemblyId);
        }

        /// <summary>
        ///     Current power stored
        /// </summary>
        public float PowerStored
        {
            get { return ModularApi.GetAssemblyProperty<float>(PhysicalAssemblyId, "PowerStored"); }
            set { ModularApi.SetAssemblyProperty(PhysicalAssemblyId, "PowerStored", value); }
        }

        private static ModularDefinitionApi ModularApi => ModularDefinition.ModularApi;

        public void AddPart(IMyCubeBlock newPart)
        {
            if (newPart == null)
                return;

            BlockCount++;

            // Scan for 'arms' connected on both ends to the feeder block.
            switch (newPart.BlockDefinition.SubtypeName)
            {
                case "Caster_Accelerator_0":
                case "Caster_Accelerator_90":
                    var newArm = new S_FusionArm(newPart, "Caster_Feeder");
                    if (newArm.IsValid)
                    {
                        Arms.Add(newArm);
                        UpdatePower(true);
                    }

                    break;
                case "Caster_Feeder"
                    : // This is awful and I hate it. The idea is to generate new loops if a feeder is placed.
                    var connectedAccelerators = new List<IMyCubeBlock>();
                    foreach (var connectedBlock in ModularApi.GetConnectedBlocks(newPart, "Modular_Fusion"))
                    {
                        var subtype = connectedBlock?.BlockDefinition.SubtypeName;
                        if (subtype != "Caster_Accelerator_0" && subtype != "Caster_Accelerator_90")
                            continue;
                        connectedAccelerators.Add(connectedBlock);
                    }

                    foreach (var accelerator in connectedAccelerators)
                    {
                        if (Arms.Any(arm => arm.Parts.Contains(accelerator)))
                            continue;

                        var accelsShareArm = false;
                        var newArm2 = new S_FusionArm(accelerator, "Caster_Feeder");
                        if (newArm2.IsValid)
                        {
                            Arms.Add(newArm2);
                            UpdatePower(true);

                            foreach (var accelerator2 in connectedAccelerators)
                                if (accelerator2 != accelerator && newArm2.Parts.Contains(accelerator2))
                                    accelsShareArm = true;
                        }

                        if (accelsShareArm)
                            break;
                    }

                    break;
            }

            if (newPart is IMyThrust)
            {
                var logic = newPart.GameLogic.GetAs<FusionThrusterLogic>();
                if (logic != null)
                {
                    Thrusters.Add(logic);
                    logic.MemberSystem = this;
                    logic.UpdatePower(PowerGeneration, NewtonsPerFusionPower, Thrusters.Count);
                }
            }

            if (newPart is IMyReactor)
            {
                var logic = newPart.GameLogic.GetAs<FusionReactorLogic>();
                if (logic != null)
                {
                    Reactors.Add(logic);
                    logic.MemberSystem = this;
                    logic.UpdatePower(PowerGeneration, MegawattsPerFusionPower, Reactors.Count);
                }
            }

            UpdatePower();
        }

        public void RemovePart(IMyCubeBlock part)
        {
            if (part == null)
                return;

            BlockCount--;

            if (part is IMyThrust)
            {
                var logic = part.GameLogic.GetAs<FusionThrusterLogic>();
                logic.MemberSystem = null;
                Thrusters.Remove(logic);
            }

            if (part is IMyReactor)
            {
                var logic = part.GameLogic.GetAs<FusionReactorLogic>();
                logic.MemberSystem = null;
                Reactors.Remove(logic);
            }

            foreach (var arm in Arms.ToList())
                if (arm.Parts.Contains(part))
                {
                    Arms.Remove(arm);
                    UpdatePower(true);
                }

            if (BlockCount <= 0)
                S_FusionManager.I.FusionSystems.Remove(PhysicalAssemblyId);

            UpdatePower();
        }

        private void UpdatePower(bool updateReactors = false)
        {
            var generationModifier = 1 / (HeatManager.I.GetGridHeatLevel(Grid) + 0.5f);
            var powerGeneration = float.Epsilon;
            var powerCapacity = float.Epsilon;
            var totalPowerUsage = 0f;

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
                    reactor?.UpdatePower(powerGeneration, MegawattsPerFusionPower * generationModifier, Reactors.Count);
            }

            foreach (var thruster in Thrusters)
            {
                totalPowerUsage += thruster?.PowerConsumption ?? 0;

                if (updateReactors)
                    thruster?.UpdatePower(powerGeneration, NewtonsPerFusionPower * generationModifier, Thrusters.Count);
            }

            // Subtract power usage afterwards so that all reactors have the same stats.
            PowerGeneration = powerGeneration;
            MaxPowerStored = powerCapacity;
            PowerConsumption = totalPowerUsage;
            PowerGeneration -= totalPowerUsage;

            // Update PowerStored
            PowerStored += PowerGeneration;
            if (PowerStored > MaxPowerStored) PowerStored = MaxPowerStored;
            //PowerGeneration = 0;
            ModularApi.SetAssemblyProperty(PhysicalAssemblyId, "HeatGeneration",
                PowerConsumption * MegawattsPerFusionPower);
        }

        public void UpdateTick()
        {
            UpdatePower(true);
        }
    }
}