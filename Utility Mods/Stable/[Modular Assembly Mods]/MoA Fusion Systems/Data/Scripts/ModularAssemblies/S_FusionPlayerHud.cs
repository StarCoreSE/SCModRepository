using Sandbox.Game;
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
            if (ModularAPI.IsDebug())
            {
                MyVisualScriptLogicProvider.SetQuestlog(true, $"Fusion Systems");

                foreach (var assemblyId in ModularAPI.GetAllAssemblies())
                {
                    if (!FusionManager.Example_ValidArms.ContainsKey(assemblyId))
                        continue;

                    MyVisualScriptLogicProvider.AddQuestlogDetail($"[{assemblyId}] Arms: {FusionManager.Example_ValidArms[assemblyId].Count}", false, false);
                }
            }
        }

        #endregion
    }
}
