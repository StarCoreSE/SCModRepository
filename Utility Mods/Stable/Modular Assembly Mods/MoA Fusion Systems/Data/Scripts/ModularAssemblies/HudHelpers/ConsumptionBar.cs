using System;
using FusionSystems.Communication;
using FusionSystems.FusionParts;
using FusionSystems.HeatParts;
using RichHudFramework.UI;
using RichHudFramework.UI.Client;
using RichHudFramework.UI.Rendering;
using Sandbox.ModAPI;
using VRage.Input;
using VRageMath;

namespace FusionSystems.HudHelpers
{
    internal class ConsumptionBar : WindowBase
    {
        private readonly TexturedBox _storageBackground;
        private readonly TexturedBox _storageBar;
        private readonly TexturedBox _heatBar;
        private bool _shouldHide;


        public ConsumptionBar(HudParentBase parent) : base(parent)
        {
            _storageBar = new TexturedBox(body)
            {
                Material = new Material("fusionBarBackground", Vector2.One * 100),
                ParentAlignment = ParentAlignments.Bottom | ParentAlignments.Left | ParentAlignments.Inner,
                Color = new Color(1, 1, 1, 0.75f)
            };

            _heatBar = new TexturedBox(body)
            {
                Material = new Material("fusionBarBackground", Vector2.One * 100),
                ParentAlignment = ParentAlignments.Bottom | ParentAlignments.Right | ParentAlignments.Inner,
                Color = new Color(1, 0, 0, 0.75f)
            };

            _storageBackground = new TexturedBox(body)
            {
                Material = new Material("fusionBarForeground", Vector2.One * 100),
                ParentAlignment = ParentAlignments.Center,
                DimAlignment = DimAlignments.Both,
                Color = new Color(1, 1, 1, 1f)
            };

            BodyColor = new Color(0, 0, 0, 0);
            BorderColor = new Color(0, 0, 0, 0);

            header.Format = new GlyphFormat(GlyphFormat.Blueish.Color, TextAlignment.Center);
            header.Height = 30f;

            HeaderText = "Fusion | 0s";
            Size = new Vector2(100f, 300f);
            Offset = new Vector2(-HudMain.ScreenWidth / (2.01f * HudMain.ResScale) + Width / 2,
                0); // Relative to 1920x1080
        }

        private static ModularDefinitionApi ModularApi => ModularDefinition.ModularApi;

        protected override void Layout()
        {
            base.Layout();

            MinimumSize = new Vector2(Math.Max(1, MinimumSize.X), MinimumSize.Y);
        }

        public void Update()
        {
            var playerCockpit = MyAPIGateway.Session?.Player?.Controller?.ControlledEntity?.Entity as IMyShipController;

            // Pulling the current HudState is SLOOOOWWWW, so we only pull it when tab is just pressed.
            if (MyAPIGateway.Input.IsNewKeyPressed(MyKeys.Tab))
                _shouldHide = MyAPIGateway.Session?.Config?.HudState != 1;

            // Hide HUD element if the player isn't in a cockpit
            if (playerCockpit == null || _shouldHide)
            {
                if (Visible) Visible = false;
                return;
            }

            var playerGrid = playerCockpit.CubeGrid;

            float totalFusionCapacity = 0;
            float totalFusionGeneration = 0;
            float totalFusionStored = 0;
            foreach (var system in S_FusionManager.I.FusionSystems)
            {
                if (playerGrid != ModularApi.GetAssemblyGrid(system.Key))
                    continue;

                totalFusionCapacity += system.Value.MaxPowerStored;
                totalFusionGeneration += system.Value.PowerGeneration;
                totalFusionStored += system.Value.PowerStored;
            }

            // Hide HUD element if the grid has no fusion systems (capacity is always >0 for a fusion system)
            if (totalFusionCapacity == 0)
            {
                if (Visible) Visible = false;
                return;
            }

            // Show otherwise
            if (!Visible) Visible = true;

            var storagePct = totalFusionStored / totalFusionCapacity;
            float timeToCharge;

            if (totalFusionGeneration > 0)
                timeToCharge = (totalFusionCapacity - totalFusionStored) / totalFusionGeneration / 60;
            else if (totalFusionGeneration < 0)
                timeToCharge = totalFusionStored / -totalFusionGeneration / 60;
            else
                timeToCharge = 0;

            HeaderText = $"Fusion | {(totalFusionGeneration > 0 ? "+" : "-")}{Math.Round(timeToCharge)}s";
            _storageBar.Height = storagePct * _storageBackground.Height;

            float heatPct = HeatManager.I.GetGridHeatLevel(playerGrid);
            _heatBar.Height = heatPct * _storageBackground.Height;
            _heatBar.Color = new Color(heatPct, 1-heatPct, 0, 0.75f);

            _storageBar.Width = _storageBackground.Width/2;
            _heatBar.Width = _storageBackground.Width/2;
            //_storageForeground.Origin = new Vector2D(_storageForeground.Origin.X, _storageForeground.Width * 0.75 - _storageBackground.Width*0.35); // THIS SHOULD BE RICHHUD!
        }
    }
}