using Sandbox.Common.ObjectBuilders;
using Sandbox.Game.Entities;
using Sandbox.ModAPI;
using VRage.Game.Components;
using VRage.ModAPI;
using VRageMath;
using VRage.Utils;
using VRageRender;
using System;
using System.Collections.Generic;
using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI.Interfaces.Terminal;
using VRage.Game;
using VRage.ObjectBuilders;
using VRage.Game.ModAPI;

namespace Invalid.ShieldProjector
{
    [MyEntityComponentDescriptor(typeof(MyObjectBuilder_UpgradeModule), false, "ShieldProjector_Large")]
    public class ShieldProjector : MyGameLogicComponent
    {
        private IMyCubeBlock block;
        private ShieldProjectorSettings Settings;
        public readonly Guid FoamSettingsGUID = new Guid("6200280a-be69-48ed-afa9-cf376ea62b8b");  // Preserved GUID

        private Vector3D offset = new Vector3D(0, 0, 10); // 10m in front of the block
        private float squareSize = 5.0f; // Default square size of 5 meters
        static bool m_controlsCreated = false;

        public override void Init(MyObjectBuilder_EntityBase objectBuilder)
        {
            base.Init(objectBuilder);
            NeedsUpdate |= MyEntityUpdateEnum.BEFORE_NEXT_FRAME | MyEntityUpdateEnum.EACH_FRAME;
            block = (IMyCubeBlock)Entity;
        }

        public override void UpdateOnceBeforeFrame()
        {
            if (block == null || block.CubeGrid?.Physics == null)
                return;

            CreateTerminalControls();
            Settings = new ShieldProjectorSettings(this);
            LoadSettings();
            SaveSettings();
        }

        public override void UpdateBeforeSimulation()
        {
            DrawDebugSquare();
        }

        private void DrawDebugSquare()
        {
            Vector3D blockPosition = block.WorldMatrix.Translation;
            Vector3D front = blockPosition + block.WorldMatrix.Forward * offset.Z;
            Vector3D up = block.WorldMatrix.Up * squareSize;
            Vector3D left = block.WorldMatrix.Left * squareSize;

            // Drawing a square in space
            MyTransparentGeometry.AddLineBillboard(MyStringId.GetOrCompute("Square"), Color.White, front - up - left, block.WorldMatrix.Forward, squareSize * 2, 0.1f);
            MyTransparentGeometry.AddLineBillboard(MyStringId.GetOrCompute("Square"), Color.White, front - up + left, block.WorldMatrix.Forward, squareSize * 2, 0.1f);
            MyTransparentGeometry.AddLineBillboard(MyStringId.GetOrCompute("Square"), Color.White, front + up - left, block.WorldMatrix.Forward, squareSize * 2, 0.1f);
            MyTransparentGeometry.AddLineBillboard(MyStringId.GetOrCompute("Square"), Color.White, front + up + left, block.WorldMatrix.Forward, squareSize * 2, 0.1f);
        }

        static void CreateTerminalControls()
        {
            if (m_controlsCreated)
                return;

            m_controlsCreated = true;

            // Control to adjust the square size
            var sizeSlider = MyAPIGateway.TerminalControls.CreateControl<IMyTerminalControlSlider, IMyUpgradeModule>("SquareSize");
            sizeSlider.Title = MyStringId.GetOrCompute("Square Size");
            sizeSlider.SetLimits(1, 10); // Min 1m, Max 10m
            sizeSlider.Getter = block => ((ShieldProjector)block.GameLogic).squareSize;
            sizeSlider.Setter = (block, value) => ((ShieldProjector)block.GameLogic).squareSize = value;
            sizeSlider.Writer = (block, text) => text.AppendFormat("{0:0.0} m", ((ShieldProjector)block.GameLogic).squareSize);
            MyAPIGateway.TerminalControls.AddControl<IMyUpgradeModule>(sizeSlider);
        }

        internal bool LoadSettings()
        {
            string rawData;
            if (block.Storage != null && block.Storage.TryGetValue(FoamSettingsGUID, out rawData))
            {
                try
                {
                    var loadedSettings = MyAPIGateway.Utilities.SerializeFromBinary<ShieldProjectorSettings>(Convert.FromBase64String(rawData));
                    if (loadedSettings != null)
                    {
                        Settings.FoamRadius = loadedSettings.FoamRadius;  // Adjust according to your actual settings fields
                        Settings.IsFoaming = loadedSettings.IsFoaming;
                        squareSize = loadedSettings.SquareSize;  // Example additional field
                        return true;
                    }
                }
                catch (Exception e)
                {
                    MyAPIGateway.Utilities.ShowNotification("Failed to load settings! Check the logs for more info.");
                    MyLog.Default.WriteLineAndConsole("Failed to load settings! Exception: " + e);
                }
            }
            return false;
        }

        internal void SaveSettings()
        {
            if (block == null || Settings == null)
                return;

            try
            {
                if (block.Storage == null)
                    block.Storage = new MyModStorageComponent();

                Settings.SquareSize = squareSize;  // Save the current square size
                string rawData = Convert.ToBase64String(MyAPIGateway.Utilities.SerializeToBinary(Settings));
                block.Storage.Add(FoamSettingsGUID, rawData);
            }
            catch (Exception e)
            {
                MyAPIGateway.Utilities.ShowNotification("Failed to save settings! Check the logs for more info.");
                MyLog.Default.WriteLineAndConsole("Failed to save settings! Exception: " + e);
            }
        }
    }
}
