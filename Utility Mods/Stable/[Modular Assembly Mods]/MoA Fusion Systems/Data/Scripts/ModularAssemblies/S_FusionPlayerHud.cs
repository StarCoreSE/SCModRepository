using Sandbox.Game;
using SCModRepository.Utility_Mods.Stable._Modular_Assembly_Mods_.MoA_Fusion_Systems.Data.Scripts.ModularAssemblies;
using Scripts.ModularAssemblies.Communication;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using VRage.Game.Components;

namespace Scripts.ModularAssemblies
{
    /// <summary>
    /// Semi-independent script for managing the player HUD.
    /// </summary>
    [MySessionComponentDescriptor(MyUpdateOrder.AfterSimulation)]
    public class S_FusionPlayerHud : MySessionComponentBase
    {
        public static S_FusionPlayerHud I;
        static ModularDefinitionAPI ModularAPI => ModularDefinition.ModularAPI;
        S_FusionManager FusionManager => S_FusionManager.I;
        
        #region Base Methods

        public override void LoadData()
        {
            I = this;
            FusionManager.Load();
        }

        protected override void UnloadData()
        {
            FusionManager.Unload();
            I = null;
        }

        public override void UpdateAfterSimulation()
        {
            FusionManager.UpdateTick();

            if (ModularAPI.IsDebug())
            {
                MyVisualScriptLogicProvider.SetQuestlog(true, $"Fusion Systems");

                foreach (var assemblyId in ModularAPI.GetAllAssemblies())
                {
                    if (!FusionManager.FusionSystems.ContainsKey(assemblyId))
                        continue;

                    S_FusionSystem system = FusionManager.FusionSystems[assemblyId];

                    MyVisualScriptLogicProvider.AddQuestlogDetail($"[{assemblyId}] Power: {Math.Round(system.StoredPower/system.PowerCapacity * 100f)}% ({Math.Round(system.PowerCapacity)} @ {Math.Round(system.PowerGeneration*60, 1)}/s) | Arms: {system.Arms.Count}", false, false);
                }
            }
            else
            {
                MyVisualScriptLogicProvider.SetQuestlog(false, $"Fusion Systems");
            }
        }

        #endregion
    }
}
