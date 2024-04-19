using Sandbox.Common.ObjectBuilders;
using Sandbox.Game.Entities;
using Sandbox.ModAPI;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.ModAPI;
using VRage.ModAPI;
using VRage.ObjectBuilders;
using VRage.Utils;
using System;
using System.Collections.Generic;
using Sandbox.Game.EntityComponents;

namespace Invalid.ShieldProjector
{
    [MyEntityComponentDescriptor(typeof(MyObjectBuilder_UpgradeModule), false, "ShieldProjector_Large")]
    public class ShieldProjector : MyGameLogicComponent
    {
        private IMyCubeBlock block;
        private ShieldProjectorSettings Settings;

        public readonly Guid FoamSettingsGUID = new Guid("6200280a-be69-48ed-afa9-cf376ea62b8b");

        public override void Init(MyObjectBuilder_EntityBase objectBuilder)
        {
            base.Init(objectBuilder);
            NeedsUpdate |= MyEntityUpdateEnum.BEFORE_NEXT_FRAME;
            block = (IMyCubeBlock)Entity;
        }

        public override void UpdateOnceBeforeFrame()
        {
            base.UpdateOnceBeforeFrame();
            CreateTerminalControls();

            if (block == null || block.CubeGrid?.Physics == null)
                return;

            Settings = new ShieldProjectorSettings(this);

            LoadSettings();
            SaveSettings();
        }

        static void CreateTerminalControls()
        {
            // Add your terminal control creation logic here
        }

        internal bool LoadSettings()
        {
            string rawData;
            if (block.Storage == null || !block.Storage.TryGetValue(FoamSettingsGUID, out rawData))
            {
                return false;
            }

            try
            {
                var loadedSettings = MyAPIGateway.Utilities.SerializeFromBinary<ShieldProjectorSettings>(Convert.FromBase64String(rawData));
                if (loadedSettings == null)
                    return false;

                // Assign loaded settings to the current settings
                Settings.FoamRadius = loadedSettings.FoamRadius;
                Settings.IsFoaming = loadedSettings.IsFoaming;

                return true;
            }
            catch (Exception e)
            {
                MyAPIGateway.Utilities.ShowNotification("Failed to load foam settings! Check the logs for more info.");
                MyLog.Default.WriteLineAndConsole("Failed to load foam settings! Exception: " + e);
            }

            return false;
        }

        internal void SaveSettings()
        {
            if (block == null || Settings == null)
                return;

            if (block.Storage == null)
                block.Storage = new MyModStorageComponent();

            string rawData = Convert.ToBase64String(MyAPIGateway.Utilities.SerializeToBinary(Settings));
            block.Storage.Add(FoamSettingsGUID, rawData);
        }

        public override bool IsSerialized()
        {
            try
            {
                SaveSettings();
            }
            catch (Exception e)
            {
                // Log any exceptions that occur during serialization
            }

            return base.IsSerialized();
        }

        public override void Close()
        {
            base.Close();
            // Add any cleanup logic here
        }
    }
}