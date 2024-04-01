using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Draygo.API;
using MoA_Fusion_Systems.Data.Scripts.ModularAssemblies.Communication;
using Sandbox.ModAPI;
using VRage.Game.ModAPI;
using VRage.Utils;
using VRageMath;
using VRageRender;
using static Draygo.API.HudAPIv2;

namespace MoA_Fusion_Systems.Data.Scripts.ModularAssemblies.HudHelpers
{
    internal class ConsumptionBar
    {
        public HudAPIv2 TextApi;
        private BillBoardHUDMessage _storageBackground; 
        private BillBoardHUDMessage _storageForeground;
        private static ModularDefinitionAPI ModularAPI => ModularDefinition.ModularAPI;


        public void Init()
        {
            TextApi = new HudAPIv2(CreateHud);
        }

        void CreateHud()
        {
            _storageBackground = new BillBoardHUDMessage()
            {
                BillBoardColor = new Color(Color.Black, 1),
                Material = MyStringId.GetOrCompute("ctf_score_background"),
                Origin = new Vector2D(-0.91, 0),
                Rotation = (float)Math.PI/2,
                Scale = 0.5f,
                Height = 0.6f,
                Visible = true,
                Options = Options.HideHud,
                Blend = MyBillboard.BlendTypeEnum.Standard,
            };

            _storageForeground = new BillBoardHUDMessage()
            {
                BillBoardColor = new Color(Color.White, 1),
                Material = MyStringId.GetOrCompute("ctf_score_background"),
                Origin = new Vector2D(-0.91, 0),
                Rotation = (float)Math.PI / 2,
                Scale = 0.4f,
                Height = 0.5f,
                Visible = true,
                Options = Options.HideHud,
                Blend = MyBillboard.BlendTypeEnum.Standard,
            };
        }

        public void Update()
        {
            if (!TextApi.Heartbeat)
                return;

            var playerCockpit = MyAPIGateway.Session?.Player?.Controller?.ControlledEntity?.Entity as IMyCockpit;

            if (playerCockpit == null)
            {
                MyAPIGateway.Utilities.ShowNotification("it's fucnking NULL " + MyAPIGateway.Session?.Player?.Controller?.ControlledEntity?.Entity?.GetType(), 1000 / 60);
                if (_storageBackground.Visible)
                {
                    _storageBackground.Visible = false;
                    _storageForeground.Visible = false;
                }
                return;
            }
            if (!_storageBackground.Visible)
            {
                _storageBackground.Visible = true;
                _storageForeground.Visible = true;
            }

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

            _storageForeground.Height = totalFusionStored / totalFusionCapacity * 0.55f;
            MyAPIGateway.Utilities.ShowNotification(Math.Round(totalFusionStored/totalFusionCapacity * 100, 1) + "%", 1000/60);
        }
    }
}
