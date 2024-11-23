using System;
using System.Collections.Generic;
using System.Linq;
using Sandbox.Game;
using Sandbox.ModAPI;
using SC.SUGMA.API;
using SC.SUGMA.GameState;
using SC.SUGMA.HeartNetworking;
using SC.SUGMA.HeartNetworking.Custom;
using VRage.Game.ModAPI;
using VRageMath;

namespace SC.SUGMA.Utilities
{
    public static class TeamBalancer
    {
        public const float MassWeightModifier = 20 / 1000000f;
        private static ShareTrackApi ShareTrackApi => SUGMA_SessionComponent.I.ShareTrackApi;

        private const float GRID_COUNT_WEIGHT = 1.2f;
        private const float DIVERSITY_WEIGHT = 0.25f;
        private static float _currentGridCountWeight = GRID_COUNT_WEIGHT;
        private static float _currentDiversityWeight = DIVERSITY_WEIGHT;
        private const float WEIGHT_ADJUST_RATE = 0.05f;

        private class GridSignature
        {
            public float MassToWeaponRatio;
            public float ThrustToMassRatio;
            public float PowerToConsumptionRatio;
            public int WeightClass;
            public float GyroStrength;
            public float ArmorDensity;

            public string GetSignatureHash()
            {
                return $"{Math.Round(MassToWeaponRatio, 1)}_{WeightClass}_" +
                       $"{Math.Round(ThrustToMassRatio, 1)}_{Math.Round(ArmorDensity, 1)}";
            }
        }

        private class PerformanceMetrics
        {
            public int EncountersThisSession;
            public float AverageRelativePerformance;
            public DateTime LastSeen;
            public List<float> RecentMatchups = new List<float>();
        }

        private static Dictionary<string, PerformanceMetrics> _signatureMetrics = new Dictionary<string, PerformanceMetrics>();

        public static void PerformBalancing(float randomOffset = 0.1f)
        {
            if (!ShareTrackApi.AreTrackedGridsLoaded())
                throw new Exception("Not all tracked grids are loaded!");

            if (!MyAPIGateway.Session.IsServer)
            {
                HeartNetwork.I.SendToServer(new AutoBalancePacket());
                return;
            }

            Dictionary<IMyFaction, IMyCubeGrid> factionSpawns = SUtils.GetFactionSpawns();
            SUtils.SetDamageEnabled(false);

            foreach (var factionKvp in AssignTeams(ShareTrackApi.GetTrackedGrids(), randomOffset))
            {
                IMyCubeGrid spawnGrid;
                if (!factionSpawns.TryGetValue(factionKvp.Key, out spawnGrid))
                {
                    MyAPIGateway.Utilities.SendMessage("Failed to find faction spawn for " + factionKvp.Key);
                    continue;
                }

                Vector3D spawnPos = spawnGrid.Physics.CenterOfMassWorld;
                Vector3D spawnDirection = -spawnPos.Normalized();
                Vector3D spawnLineDirection = Vector3D.Cross(spawnDirection, Vector3D.Up);

                spawnPos += spawnDirection * 250;

                Vector3D initialSpawnPos = spawnPos - (spawnLineDirection * 250 * factionKvp.Value.Count / 2f);
                for (int i = 0; i < factionKvp.Value.Count; i++)
                    factionKvp.Value[i].Teleport(MatrixD.CreateWorld(
                        initialSpawnPos + spawnLineDirection * 250 * i,
                        spawnDirection,
                        Vector3D.Up));
            }

            MyAPIGateway.Utilities.SendMessage($"Autobalance completed.");
        }

        private static GridSignature AnalyzeGrid(IMyCubeGrid grid)
        {
            var blocks = new List<IMySlimBlock>();
            grid.GetBlocks(blocks);

            float totalMass = grid.Physics?.Mass ?? 0;
            float weaponMass = 0;
            float totalThrust = 0;
            float powerGeneration = 0;
            float powerConsumption = 0;
            float gyroStrength = 0;
            float armorBlocks = 0;

            foreach (var block in blocks)
            {
                var cubeBlock = block.FatBlock;
                if (cubeBlock == null) continue;

                if (cubeBlock.SlimBlock.BlockDefinition.Id.TypeId.ToString().Contains("Weapon"))
                {
                    weaponMass += block.Mass;
                }
                else
                {
                    IMyThrust thrust = cubeBlock as IMyThrust;
                    if (thrust != null)
                    {
                        totalThrust += thrust.MaxThrust;
                    }
                    else
                    {
                        IMyPowerProducer producer = cubeBlock as IMyPowerProducer;
                        if (producer != null)
                        {
                            powerGeneration += producer.CurrentOutput;
                        }
                        else
                        {
                            IMyGyro gyro = cubeBlock as IMyGyro;
                            if (gyro != null)
                            {
                                gyroStrength += gyro.GyroPower;
                            }
                            else if (block.BlockDefinition.Id.TypeId.ToString().Contains("Armor"))
                            {
                                armorBlocks++;
                            }
                        }
                    }
                }

                if (cubeBlock.Components != null)
                    powerConsumption += block.BuildIntegrity * 0.1f;
            }

            return new GridSignature
            {
                MassToWeaponRatio = weaponMass / (totalMass > 0 ? totalMass : 1),
                ThrustToMassRatio = totalThrust / (totalMass > 0 ? totalMass : 1),
                PowerToConsumptionRatio = powerGeneration / (powerConsumption > 0 ? powerConsumption : 1),
                WeightClass = DetermineWeightClass(totalMass),
                GyroStrength = gyroStrength / (totalMass > 0 ? totalMass : 1),
                ArmorDensity = armorBlocks / (float)blocks.Count
            };
        }

        private static int DetermineWeightClass(float mass)
        {
            if (mass < 100000) return 0;
            if (mass < 500000) return 1;
            if (mass < 1500000) return 2;
            if (mass < 5000000) return 3;
            return 4;
        }

        private static Dictionary<IMyFaction, List<IMyCubeGrid>> AssignTeams(IMyCubeGrid[] grids, float randomOffset)
        {
            MyAPIGateway.Utilities.SendMessage($"Autobalancing {grids.Length} grids...");

            IEnumerable<IMyFaction> factions = PlayerTracker.I.GetPlayerFactions();
            Dictionary<IMyFaction, int> factionPoints = new Dictionary<IMyFaction, int>();
            Dictionary<IMyFaction, List<IMyCubeGrid>> factionGrids = new Dictionary<IMyFaction, List<IMyCubeGrid>>();
            foreach (var faction in factions)
            {
                factionPoints.Add(faction, 0);
                factionGrids.Add(faction, new List<IMyCubeGrid>());
            }

            // Analyze all grids
            Dictionary<IMyCubeGrid, GridSignature> gridSignatures = new Dictionary<IMyCubeGrid, GridSignature>();
            Dictionary<IMyCubeGrid, int> gridPoints = new Dictionary<IMyCubeGrid, int>();

            foreach (var grid in grids)
            {
                gridSignatures[grid] = AnalyzeGrid(grid);
                gridPoints[grid] = ShareTrackApi.GetGridPoints(grid) +
                    (int)((grid.Physics?.Mass ?? 0) * MassWeightModifier);
                gridPoints[grid] += (int)(gridPoints[grid] * SUtils.Random.NextDouble() * randomOffset);
            }

            AdjustWeightsForMatchSize(grids.Length, gridPoints.Values.Sum());

            Array.Sort(grids, (a, b) => gridPoints[b] - gridPoints[a]);
            foreach (var grid in grids)
            {
                if (grid.BigOwners.Count < 1)
                {
                    MyAPIGateway.Utilities.SendMessage($"Grid {grid.DisplayName} has no owner!");
                    continue;
                }

                IMyFaction bestFaction = null;
                float bestScore = float.MinValue;

                foreach (var faction in factionPoints.Keys)
                {
                    float baseScore = -factionPoints[faction];
                    float counterScore = gridSignatures[grid]
                        .WeightClass;

                    factionPoints[faction] += gridPoints[grid];
                    factionGrids[faction].Add(grid);
                }
            }

            return factionGrids;
        }

        private static void AdjustWeightsForMatchSize(int gridCount, int totalPoints)
        {
            if (gridCount <= 4)
            {
                _currentDiversityWeight = DIVERSITY_WEIGHT * 0.8f;
                _currentGridCountWeight = GRID_COUNT_WEIGHT * 1.2f;
            }
            else if (gridCount >= 12)
            {
                _currentDiversityWeight = DIVERSITY_WEIGHT * 1.2f;
                _currentGridCountWeight = GRID_COUNT_WEIGHT * 0.8f;
            }

            if (totalPoints > 1000000)
            {
                _currentDiversityWeight *= 1.1f;
            }
        }
    }
}
