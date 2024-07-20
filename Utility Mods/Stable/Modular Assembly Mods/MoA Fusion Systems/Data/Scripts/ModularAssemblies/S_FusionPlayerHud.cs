using System;
using System.Linq;
using RichHudFramework.Client;
using RichHudFramework.UI.Client;
using Sandbox.Game;
using StarCore.FusionSystems.Communication;
using StarCore.FusionSystems.FusionParts;
using StarCore.FusionSystems.HeatParts;
using StarCore.FusionSystems.HudHelpers;
using VRage.Game.Components;
using VRage.Utils;

namespace StarCore.FusionSystems
{
    /// <summary>
    ///     Semi-independent script for managing the player HUD.
    /// </summary>
    [MySessionComponentDescriptor(MyUpdateOrder.AfterSimulation)]
    public class SFusionPlayerHud : MySessionComponentBase
    {
        public static SFusionPlayerHud I;
        private int _ticks;

        private ConsumptionBar _consumptionBar;
        private static ModularDefinitionApi ModularApi => ModularDefinition.ModularApi;
        private static SFusionManager FusionManager => SFusionManager.I;
        private static HeatManager HeatManager => HeatManager.I;

        #region Base Methods

        public override void LoadData()
        {
            I = this;

            FusionManager.Load();
            HeatManager.Load();
            RichHudClient.Init("FusionSystems", () => { }, () => { });
        }

        protected override void UnloadData()
        {
            FusionManager.Unload();
            HeatManager.Unload();
            I = null;

            //RichHudClient.Reset();
        }

        private bool _questlogDisposed;

        public override void UpdateAfterSimulation()
        {
            _ticks++;
            try
            {
                if (_consumptionBar == null && RichHudClient.Registered)
                    _consumptionBar = new ConsumptionBar(HudMain.HighDpiRoot)
                    {
                        Visible = true
                    };

                HeatManager.UpdateTick();
                FusionManager.UpdateTick();
                _consumptionBar?.Update();

                if (ModularApi.IsDebug())
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
                            $"[{system.PhysicalAssemblyId}] Power: {Math.Round(system.PowerStored / system.MaxPowerStored * 100f)}% ({Math.Round(system.MaxPowerStored)} @ {Math.Round(system.PowerGeneration * 60, 1)}/s) | Loops: {system.Arms.Count} | Heat: -{HeatManager.I.GetGridHeatDissipation(system.Grid):N0} +{HeatManager.I.GetGridHeatGeneration(system.Grid):N0}",
                            false, false);
                        displayedCount++;
                    }

                    _questlogDisposed = false;
                }
                else if (!_questlogDisposed)
                {
                    MyVisualScriptLogicProvider.SetQuestlogLocal(false, "Fusion Systems");
                    _questlogDisposed = true;
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