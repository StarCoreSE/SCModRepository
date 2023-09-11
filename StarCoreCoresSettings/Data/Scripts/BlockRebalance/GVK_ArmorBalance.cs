using Sandbox.Definitions;
using Sandbox.ModAPI;
using VRage.Game.Components;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using Sandbox.Game.AI.Pathfinding.Obsolete;
using Sandbox.Game.Entities;
using VRage.Game;
using VRage.ObjectBuilders;
using VRage.Game.ObjectBuilders.ComponentSystem;
using VRage.Utils;
using VRageMath;


// Code is based on Gauge's Balanced Deformation code, but heavily modified for more control. 
namespace MikeDude.ArmorBalance
{
    [MySessionComponentDescriptor(MyUpdateOrder.NoUpdate)]
    public class ArmorBalance : MySessionComponentBase
    {
        public const float lightArmorLargeDamageMod = 1f; //1.0 Vanilla
        public const float lightArmorLargeDeformationMod = 0.4f; //varies for every block
        public const float lightArmorSmallDamageMod = 1f; //1.0 Vanilla
        public const float lightArmorSmallDeformationMod = 0.4f; //varies for every block

        public const float heavyArmorLargeDamageMod = 1f; //0.5 Vanilla ONLY for full cube, 1.0 all else
        public const float heavyArmorLargeDeformationMod = 0.2f; //varies for every block
        public const float heavyArmorSmallDamageMod = 1f; //0.5 Vanilla ONLY for full cube, 1.0 all else
        public const float heavyArmorSmallDeformationMod = 0.2f; //varies for every block

        public const float blockExplosionResistanceMod = 1f; //DamageMultiplierExplosion

        public const float realWheelDamageMod = 0.75f; //1.0 Vanilla
        public const float realWheel5x5DamageMod = 0.75f; //1.0 Vanilla
        public const float suspensionDamageMod = 0.75f; //1.0 Vanilla
        public const float rotorDamageMod = 0.5f; //1.0 Vanilla
        public const float hingeDamageMod = 0.5f; //1.0 Vanilla
        public const float gyroDamageMod = 2; //1.0 Vanilla
        public const float thrusterDamageMod = 0.5f; //1.0 Vanilla
        public const float cockpitDamageMod = 0.5f; //1.0 Vanilla

        public const int drillPCU = 20000;
        public const int welderPCU = 10000;
        public const int pistonBasePCU = 20000;
        public const float beaconMaxRadius = 100000;

        public const double hydroTankH2Density = 15000000 / (2.5 * 2.5 * 2.5 * 27); // LG Large hydro tank capacity divided by its volume in meters

        private readonly MyPhysicalItemDefinition genericScrap = MyDefinitionManager.Static.GetPhysicalItemDefinition(new MyDefinitionId(typeof(MyObjectBuilder_Ore), "Scrap"));

        private readonly MyComponentDefinition unobtainiumComponent = MyDefinitionManager.Static.GetComponentDefinition(new MyDefinitionId(typeof(MyObjectBuilder_Component), "GVK_Unobtanium"));

        private readonly MyComponentDefinition steelPlateComponent = MyDefinitionManager.Static.GetComponentDefinition(new MyDefinitionId(typeof(MyObjectBuilder_Component), "SteelPlate"));

        private void DoWork()
        {
            foreach (var blockDef in MyDefinitionManager.Static.GetDefinitionsOfType<MyCubeBlockDefinition>())
            {
                var turretDef = blockDef as MyLargeTurretBaseDefinition;
                var weaponDef = blockDef as MyWeaponBlockDefinition;
                var sorterDef = blockDef as MyConveyorSorterDefinition;
                var drillDef = blockDef as MyShipDrillDefinition;
                var pistonBaseDef = blockDef as MyPistonBaseDefinition;
                var beaconDef = blockDef as MyBeaconDefinition;
                var suspensionDef = blockDef as MyMotorSuspensionDefinition;
                var statorDef = blockDef as MyMotorStatorDefinition; //Motor stator is the base
                var advStatorDef = blockDef as MyMotorAdvancedStatorDefinition; //Motor stator is the base
                var thrustDef = blockDef as MyThrustDefinition;
                var gyroDef = blockDef as MyGyroDefinition;
                var upgradeModuleDef = blockDef as MyUpgradeModuleDefinition;
                var cockpitDef = blockDef as MyCockpitDefinition;
                var remoteControlDef = blockDef as MyRemoteControlDefinition;
                var timerBlockDef = blockDef as MyTimerBlockDefinition;
                var hydroTankDef = blockDef as MyGasTankDefinition;
                var welderDef = blockDef as MyShipWelderDefinition;
                var oxygenGeneratorDef = blockDef as MyOxygenGeneratorDefinition;
                var batteryDef = blockDef as MyBatteryBlockDefinition;
                var laserAntennaDef = blockDef as MyLaserAntennaDefinition;
                var cargoDef = blockDef as MyCargoContainerDefinition;
				var reactorDef = blockDef as MyReactorDefinition;
				var solarDef = blockDef as MySolarPanelDefinition;

                blockDef.DamageMultiplierExplosion = blockExplosionResistanceMod;

                if (turretDef != null || weaponDef != null || (sorterDef != null && !sorterDef.Id.SubtypeName.Contains("ConveyorSorter")))
                {
                    blockDef.GeneralDamageMultiplier = 0.5f;
                }
                else
                {
                    blockDef.PCU = 1;
                }

                //light armor
                if (blockDef.EdgeType == "Light" && blockDef.BlockTopology != MyBlockTopology.TriangleMesh)
                {
                    if (blockDef.CubeSize == MyCubeSize.Large)
                    {
                        blockDef.GeneralDamageMultiplier = lightArmorLargeDamageMod;
                        blockDef.DeformationRatio = lightArmorLargeDeformationMod;
                    }

                    if (blockDef.CubeSize == MyCubeSize.Small)
                    {
                        blockDef.GeneralDamageMultiplier = lightArmorSmallDamageMod;
                        blockDef.DeformationRatio = lightArmorSmallDeformationMod;
                    }
                    //blockDef.PCU = lightArmorPCU;
                }

                //heavy armor
                if (blockDef.EdgeType == "Heavy")
                {
                    if (blockDef.CubeSize == MyCubeSize.Large)
                    {
                        blockDef.GeneralDamageMultiplier = heavyArmorLargeDamageMod;
                        blockDef.DeformationRatio = heavyArmorLargeDeformationMod;
                    }

                    if (blockDef.CubeSize == MyCubeSize.Small)
                    {
                        blockDef.GeneralDamageMultiplier = heavyArmorSmallDamageMod;
                        blockDef.DeformationRatio = heavyArmorSmallDeformationMod;
                    }
                    //blockDef.PCU = blastDoorPCU;
                }

                // Beam blocks and heat vents
                if (blockDef.EdgeType == "Light" && (blockDef.Id.SubtypeName.Contains("BeamBlock") || blockDef.Id.SubtypeName.Contains("HeatVentBlock")))
                {
                    if (blockDef.CubeSize == MyCubeSize.Large)
                    {
                        blockDef.GeneralDamageMultiplier = lightArmorLargeDamageMod;
                    }

                    if (blockDef.CubeSize == MyCubeSize.Small)
                    {
                        blockDef.GeneralDamageMultiplier = lightArmorSmallDamageMod;
                    }
                }

                //suspension
                if (suspensionDef != null)
                {
                    suspensionDef.GeneralDamageMultiplier = suspensionDamageMod;
                }

                //rotors (includes hinges)
                if (statorDef != null)
                {
                    statorDef.GeneralDamageMultiplier = rotorDamageMod;
                }

                //adv rotors
                if (advStatorDef != null)
                {
                    advStatorDef.GeneralDamageMultiplier = rotorDamageMod;
                }

                //suspension wheels
                if (blockDef.Id.SubtypeName.Contains("Real"))
                {
                    blockDef.GeneralDamageMultiplier = realWheelDamageMod;

                    if (blockDef.Id.SubtypeName.Contains("5x5"))
                    {
                        blockDef.GeneralDamageMultiplier = realWheel5x5DamageMod;
                    }
                }

                //rotor and hinge top parts
                if (blockDef.Id.SubtypeName.Contains("Rotor") || blockDef.Id.SubtypeName.Contains("HingeHead"))
                {
                    blockDef.GeneralDamageMultiplier = rotorDamageMod;
                }

                //drills
                if (drillDef != null)
                {
                    drillDef.PCU = drillPCU;
                }

                //welders
                if (welderDef != null)
                {
                    welderDef.PCU = welderPCU;
                }

                //pistons
                if (pistonBaseDef != null)
                {
                    pistonBaseDef.PCU = pistonBasePCU;
                }

                //Drillblocker
                if (beaconDef != null)
                {
                    if (!beaconDef.Id.SubtypeName.Contains("DrillBlocker"))
                    {
                        beaconDef.MaxBroadcastRadius = beaconMaxRadius;
                    }

                    if (beaconDef.Id.SubtypeName.Contains("BlockBeacon"))
                    {
                        beaconDef.PCU = 1; //this is so TopGrid doesn't pick random numbers when parent grid has 0 PCU.
                    }
                }

                //Thrusters
                if (thrustDef != null)
                {
                    thrustDef.GeneralDamageMultiplier = thrusterDamageMod;

                    if (!thrustDef.Id.SubtypeName.Contains("NPC") && !thrustDef.Id.SubtypeName.Contains("Hover"))
                    {
                        if (thrustDef.FuelConverter != null &&
                            !thrustDef.FuelConverter.FuelId.IsNull() &&
                            thrustDef.FuelConverter.FuelId.SubtypeId.Contains("Hydrogen"))
                        {
                            thrustDef.MinPlanetaryInfluence = 0f;
                            thrustDef.MaxPlanetaryInfluence = 0.25f;
                            thrustDef.EffectivenessAtMaxInfluence = 1f;
                            thrustDef.EffectivenessAtMinInfluence = 0f;
                            //thrustDef.NeedsAtmosphereForInfluence = false; //partially useless because it always searches for atmosphere regardless
                            //thrustDef.InvDiffMinMaxPlanetaryInfluence = 1f; 
                            thrustDef.ConsumptionFactorPerG = 0f;
                            thrustDef.SlowdownFactor = 1f;
                            //thrustDef.FuelConverter.Efficiency = 1f; //not using this because it now varies for large and small
                        }
                        else
                        {
                            blockDef.Enabled = false;
                            blockDef.Public = false;
                            blockDef.GuiVisible = false;
                            if (unobtainiumComponent != null)
                            {
                                InsertComponent(blockDef, 0, unobtainiumComponent, 1, genericScrap);
                            }
                        }
                    }
                }

                //gyros
                if (gyroDef != null || (upgradeModuleDef != null && blockDef.Id.SubtypeName.Contains("Gyro")))
                {
                    blockDef.GeneralDamageMultiplier = gyroDamageMod;
                }

                //cockpits (but not desks, or chairs)
                if (cockpitDef != null && cockpitDef.Id.SubtypeName.Contains("Cockpit"))
                {
                    cockpitDef.GeneralDamageMultiplier = cockpitDamageMod;
                }

                //remote controls
                if (remoteControlDef != null)
                {
                    remoteControlDef.GeneralDamageMultiplier = cockpitDamageMod;
                }

                //timer blocks 
                if (timerBlockDef != null)
                {
                    timerBlockDef.GeneralDamageMultiplier = cockpitDamageMod;
                }

                //H2 tanks
                if (hydroTankDef != null && hydroTankDef.StoredGasId.SubtypeName == "Hydrogen")
                {
                    hydroTankDef.Capacity = (float)Math.Ceiling(hydroTankDef.Size.Volume() * Math.Pow(hydroTankDef.CubeSize == MyCubeSize.Large ? 2.5 : 0.5, 3) * hydroTankH2Density);
                }

                // Fix the upgradeable O2/H2 gen
                if (oxygenGeneratorDef != null)
                {
                    switch (oxygenGeneratorDef.Id.SubtypeId.String)
                    {
                        case "MA_O2":
                            oxygenGeneratorDef.IceConsumptionPerSecond = 150;
                            // Make the generator exactly as efficient as normal gens, otherwise it's even more OP
                            oxygenGeneratorDef.OperationalPowerConsumption = 3;
                            ChangeComponentCount(oxygenGeneratorDef, oxygenGeneratorDef.Components.Length - 1, 25);
                            break;
                        case "":
                            ChangeComponentCount(oxygenGeneratorDef, oxygenGeneratorDef.Components.Length - 1, 25);
                            break;
                    }
                }
				
				//reduce default battery pre-charge, and nerf max output to reduce battery spam
                if (batteryDef != null)
                {
                    batteryDef.InitialStoredPowerRatio = 0.05f;
					batteryDef.MaxPowerOutput *=0.8f;
					batteryDef.GeneralDamageMultiplier = 1.25f;
                    foreach (var component in batteryDef.Components)
                    {
                        component.DeconstructItem = component.Definition;
                    }
                }

				//buffing output of solar to compensate for banned solar tracking
                if (solarDef != null)
                {
                    solarDef.MaxPowerOutput *= 2f;
                }

				//remove LOS check for laser antenna
                if (laserAntennaDef != null)
                {
                    laserAntennaDef.RequireLineOfSight = false;
                }
				
				//buffing output of NPC Proprietary reactors
                if (reactorDef != null && reactorDef.Id.SubtypeName.Contains("Proprietary"))
                {
                    reactorDef.MaxPowerOutput *= 5f;
					//reactorDef.FuelInfos[0].Ratio = 100f; //this is readonly and doesnt work, same for H2 engines
                }

				//buffing output of small grid reactors
                if (reactorDef != null && blockDef.CubeSize == MyCubeSize.Small)
                {
                    reactorDef.MaxPowerOutput *= 1.5f;
                }
				
				//Adjust container components to be proportional to block volume
                if (cargoDef != null && cargoDef.CubeSize == MyCubeSize.Large && cargoDef.Id.SubtypeName.Contains("Container"))
                {
                    ReplaceComponent(cargoDef, cargoDef.Components.Length - 1, steelPlateComponent, cargoDef.Size.Volume() > 1 ? 120 : 40);
                }

				//I dont remember what this does either
                if (blockDef.CubeSize == MyCubeSize.Large && blockDef.Id.SubtypeName == "LargeBlockConveyor")
                {
                    InsertComponent(blockDef, blockDef.Components.Length, steelPlateComponent, 40);
                }
				
				//Make all 5x5 XL blocks have light edge type, and no deformation
                if (blockDef.CubeSize == MyCubeSize.Large && blockDef.Id.SubtypeName.Contains("XL_") && blockDef.BlockTopology == MyBlockTopology.TriangleMesh)
                {
					blockDef.GeneralDamageMultiplier = lightArmorLargeDamageMod;
					blockDef.UsesDeformation = false;
					blockDef.DeformationRatio = 0.45f; //this seems to be a sweet spot between completely immune to collision, and popping with more than a light bump.
					blockDef.EdgeType = "Light";
                }

				//Make all Buster blocks have heavy edge type, and no deformation
                if (blockDef.CubeSize == MyCubeSize.Large && blockDef.Id.SubtypeName.Contains("MA_Buster") && blockDef.BlockTopology == MyBlockTopology.TriangleMesh)
                {
					blockDef.GeneralDamageMultiplier = lightArmorLargeDamageMod;
					blockDef.UsesDeformation = false;
					blockDef.DeformationRatio = 0.45f; //this seems to be a sweet spot between completely immune to collision, and popping with more than a light bump.
					blockDef.EdgeType = "Heavy";
                }
				
            }
        }

        public override void LoadData()
        {
            DoWork();
        }

        private static void ReplaceComponent(MyCubeBlockDefinition blockDef, int index, MyComponentDefinition newComp, int newCount, MyPhysicalItemDefinition deconstructItem = null)
        {
            var comp = blockDef.Components[index];
            var oldCount = comp.Count;
            float intDiff;
            float massDiff;
            if (newCount > 0)
            {
                intDiff = newComp.MaxIntegrity * newCount - comp.Definition.MaxIntegrity * oldCount;
                massDiff = newComp.Mass * newCount - comp.Definition.Mass * oldCount;

                blockDef.Components[index].Count = newCount;
            }
            else
            {
                intDiff = (newComp.MaxIntegrity - comp.Definition.MaxIntegrity) * oldCount;
                massDiff = (newComp.Mass - comp.Definition.Mass) * oldCount;
            }

            comp.Definition = newComp;
            comp.DeconstructItem = deconstructItem ?? newComp;

            blockDef.MaxIntegrity += intDiff;
            blockDef.Mass += massDiff;

            SetRatios(blockDef, blockDef.CriticalGroup);
        }

        private static void InsertComponent(MyCubeBlockDefinition blockDef, int componentIndex, MyComponentDefinition comp, int count, MyPhysicalItemDefinition deconstructItem = null)
        {
            var intDiff = comp.MaxIntegrity * count;
            var massDiff = comp.Mass * count;

            if (componentIndex <= blockDef.CriticalGroup)
            {
                blockDef.CriticalGroup += 1;
            }

            blockDef.MaxIntegrity += intDiff;
            blockDef.Mass += massDiff;

            var newComps = new MyCubeBlockDefinition.Component[blockDef.Components.Length + 1];

            if (componentIndex == 0)
            {
                newComps[0] = new MyCubeBlockDefinition.Component
                {
                    Definition = comp,
                    DeconstructItem = deconstructItem ?? comp,
                    Count = count
                };
                blockDef.Components.CopyTo(newComps, 1);
            }
            else if (componentIndex == blockDef.Components.Length)
            {
                newComps[blockDef.Components.Length] = new MyCubeBlockDefinition.Component
                {
                    Definition = comp,
                    DeconstructItem = comp,
                    Count = count
                };
                blockDef.Components.CopyTo(newComps, 0);
            }
            else
            {
                for (var index = 0; index < newComps.Length; index++)
                {
                    if (index < componentIndex)
                    {
                        newComps[index] = blockDef.Components[index];
                    }
                    else if (index == componentIndex)
                    {
                        newComps[index] = new MyCubeBlockDefinition.Component
                        {
                            Definition = comp,
                            DeconstructItem = comp,
                            Count = count
                        };
                    }
                    else
                    {
                        newComps[index] = blockDef.Components[index - 1];
                    }
                }
            }

            blockDef.Components = newComps;

            SetRatios(blockDef, blockDef.CriticalGroup);
        }

		// Fix the upgradeable O2/H2 gen
        private static void ChangeComponentCount(MyCubeBlockDefinition blockDef, int index, int newCount)
        {
            var comp = blockDef.Components[index];
            var oldCount = comp.Count;
            var intDiff = comp.Definition.MaxIntegrity * (newCount - oldCount);
            var massDiff = comp.Definition.Mass * (newCount - oldCount);

            comp.Count = newCount;

            blockDef.MaxIntegrity += intDiff;
            blockDef.Mass += massDiff;

            SetRatios(blockDef, blockDef.CriticalGroup);
        }

		// Fix the upgradeable O2/H2 gen
        private static void SetRatios(MyCubeBlockDefinition blockDef, int criticalIndex)
        {
            var criticalIntegrity = 0f;
            var ownershipIntegrity = 0f;
            for (var index = 0; index <= criticalIndex; index++)
            {
                var component = blockDef.Components[index];
                if (ownershipIntegrity == 0f && component.Definition.Id.SubtypeName == "Computer")
                {
                    ownershipIntegrity = criticalIntegrity + component.Definition.MaxIntegrity;
                }

                criticalIntegrity += component.Count * component.Definition.MaxIntegrity;
                if (index == criticalIndex)
                {
                    criticalIntegrity -= component.Definition.MaxIntegrity;
                }
            }

            blockDef.CriticalIntegrityRatio = criticalIntegrity / blockDef.MaxIntegrity;
            blockDef.OwnershipIntegrityRatio = ownershipIntegrity / blockDef.MaxIntegrity;

            var count = blockDef.BuildProgressModels.Length;
            for (var index = 0; index < count; index++)
            {
                var buildPercent = (index + 1f) / count;
                blockDef.BuildProgressModels[index].BuildRatioUpperBound = buildPercent * blockDef.CriticalIntegrityRatio;
            }
        }
    }
}