using System;
using System.Linq;
using MoA_Fusion_Systems.Data.Scripts.ModularAssemblies.Communication;
using MoA_Fusion_Systems.Data.Scripts.ModularAssemblies.HudHelpers;
using Sandbox.Game;
using VRage.Game.Components;
using VRage.Utils;

namespace MoA_Fusion_Systems.Data.Scripts.ModularAssemblies
{
    /// <summary>
    ///     Semi-independent script for managing the player HUD.
    /// </summary>
    [MySessionComponentDescriptor(MyUpdateOrder.AfterSimulation)]
    public class S_FusionPlayerHud : MySessionComponentBase
    {
        public static S_FusionPlayerHud I;
        private static ModularDefinitionAPI ModularAPI => ModularDefinition.ModularAPI;
        private static S_FusionManager FusionManager => S_FusionManager.I;

        private ConsumptionBar ConsumptionBar = new ConsumptionBar();

        #region Base Methods

        public override void LoadData()
        {
            I = this;
            FusionManager.Load();
            ConsumptionBar.Init();
        }

        protected override void UnloadData()
        {
            FusionManager.Unload();
            I = null;
        }

        public override void UpdateAfterSimulation()
        {
            try
            {
                FusionManager.UpdateTick();
                ConsumptionBar.Update();

                if (ModularAPI.IsDebug())
                {
                    MyVisualScriptLogicProvider.SetQuestlogLocal(true,
                        $"Fusion Systems ({FusionManager.FusionSystems.Count})");

                    // Limits the number of displayed systems to 6
                    var displayedCount = 0;
                    foreach (var system in FusionManager.FusionSystems.Values.ToList())
                    {
                        if (displayedCount > 6 || system.Arms.Count == 0)
                            continue;

                        MyVisualScriptLogicProvider.AddQuestlogDetailLocal(
                            $"[{system.PhysicalAssemblyId}] Power: {Math.Round(system.PowerStored / system.PowerCapacity * 100f)}% ({Math.Round(system.PowerCapacity)} @ {Math.Round(system.PowerGeneration * 60, 1)}/s) | Loops: {system.Arms.Count} | Blocks: {system.BlockCount}",
                            false, false);
                        displayedCount++;
                    }
                }
                else
                {
                    MyVisualScriptLogicProvider.SetQuestlogLocal(false, "Fusion Systems");
                }
            }
            catch (Exception ex)
            {
                MyLog.Default.WriteLineAndConsole(ex.ToString());
            }
        }

        #endregion
    }
}