using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MoA_Fusion_Systems.Data.Scripts.ModularAssemblies.Communication;
using RichHudFramework.Client;
using RichHudFramework.UI;
using RichHudFramework.UI.Client;
using RichHudFramework.UI.Rendering;
using Sandbox.ModAPI;
using VRage.Game.ModAPI;
using VRage.Utils;
using VRageMath;
using VRageRender;

namespace MoA_Fusion_Systems.Data.Scripts.ModularAssemblies.HudHelpers
{
    internal class ConsumptionBar : WindowBase
    {
        private TexturedBox _storageBackground; 
        private TexturedBox _storageForeground;
        private static ModularDefinitionAPI ModularAPI => ModularDefinition.ModularAPI;


        public ConsumptionBar(HudParentBase parent) : base(parent)
        {
            _storageForeground = new TexturedBox(body)
            {
                Material = new Material("ctf_score_background", Vector2.One * 100),
                ParentAlignment = ParentAlignments.Bottom | ParentAlignments.InnerV,
                DimAlignment = DimAlignments.Width,
            };
            
            _storageBackground = new TexturedBox(body)
            {
                Material = new Material("fusionBarBackground", Vector2.One * 100),
                ParentAlignment = ParentAlignments.Center,
                DimAlignment = DimAlignments.Both,
            };

            BodyColor = new Color(41, 54, 62, 150);
            BorderColor = new Color(58, 68, 77);

            //header.Format = new GlyphFormat(GlyphFormat.Blueish.Color, TextAlignment.Center, 1.08f);
            //header.Height = 30f;

            //HeaderText = "Fusion Systems";
            Size = new Vector2(250f, 500f);
            Offset = new Vector2(835, 290); // Top-right corner; relative to 1920x1080
        }

        protected override void Layout()
        {
            base.Layout();

            MinimumSize = new Vector2(Math.Max(1, MinimumSize.X), MinimumSize.Y);
        }

        public void Update()
        {
            var playerCockpit = MyAPIGateway.Session?.Player?.Controller?.ControlledEntity?.Entity as IMyCockpit;

            //if (playerCockpit == null)
            //{
            //    if (_storageBackground.Visible)
            //    {
            //        _storageBackground.Visible = false;
            //        _storageForeground.Visible = false;
            //    }
            //    return;
            //}
            //if (!_storageBackground.Visible)
            //{
            //    _storageBackground.Visible = true;
            //    _storageForeground.Visible = true;
            //}

            var playerGrid = playerCockpit.CubeGrid;

            float totalFusionCapacity = 0;
            float totalFusionGeneration = 0;
            float totalFusionStored = 0;
            foreach (var system in S_FusionManager.I.FusionSystems)
            {
                if (playerGrid != ModularAPI.GetAssemblyGrid(system.Key))
                    continue;

                totalFusionCapacity += system.Value.PowerCapacity;
                totalFusionGeneration += system.Value.PowerGeneration;
                totalFusionStored += system.Value.PowerStored;
            }

            _storageForeground.Height = totalFusionStored / totalFusionCapacity * _storageBackground.Height;
            //_storageForeground.Origin = new Vector2D(_storageForeground.Origin.X, _storageForeground.Width * 0.75 - _storageBackground.Width*0.35); // THIS SHOULD BE RICHHUD!
            MyAPIGateway.Utilities.ShowNotification(Math.Round(totalFusionStored/totalFusionCapacity * 100, 1) + "%", 1000/60);
        }
    }
}
