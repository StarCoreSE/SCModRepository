using MoA_Fusion_Systems.Data.Scripts.ModularAssemblies.Communication;
using ProtoBuf;
using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using VRage.Game.Components;
using VRage.Game.ModAPI;
using VRage.Game.ModAPI.Network;
using VRage.ModAPI;
using VRage.Network;
using VRage.ObjectBuilders;
using VRage.Sync;
using VRage.Utils;

namespace MoA_Fusion_Systems.Data.Scripts.ModularAssemblies.FusionParts
{
    public abstract class FusionPart<T> : MyGameLogicComponent, IMyEventProxy
        where T : IMyCubeBlock
    {
        public static readonly Guid SettingsGUID = new Guid("36a45185-2e80-461c-9f1c-e2140a47a4df");
        internal FusionPartSettings Settings = new FusionPartSettings();

        internal T Block;

        public MySync<float, SyncDirection.BothWays> PowerUsageSync;
        public MySync<float, SyncDirection.BothWays> OverridePowerUsageSync;
        public MySync<bool, SyncDirection.BothWays> OverrideEnabled;
        private static ModularDefinitionAPI ModularAPI => ModularDefinition.ModularAPI;

        #region Base Methods

        public override void Init(MyObjectBuilder_EntityBase objectBuilder)
        {
            base.Init(objectBuilder);
            NeedsUpdate = MyEntityUpdateEnum.BEFORE_NEXT_FRAME;
        }
        public override void UpdateOnceBeforeFrame()
        {
            base.UpdateOnceBeforeFrame();
            Block = (T)Entity;

            if (Block.CubeGrid?.Physics == null)
                return; // ignore ghost/projected grids

            LoadSettings();
            // Trigger power update is only needed when OverrideEnabled is false
            PowerUsageSync.ValueChanged += value =>
                Settings.PowerUsage = value.Value;

            // Trigger power update is only needed when OverrideEnabled is true
            OverridePowerUsageSync.ValueChanged += value =>
                Settings.OverridePowerUsage = value.Value;

            // Trigger power update if boostEnabled is changed
            OverrideEnabled.ValueChanged += value =>
                Settings.OverrideEnabled = value.Value;
            SaveSettings();
        }

        #endregion

        internal void SaveSettings()
        {
            if (Block == null)
                return; // called too soon or after it was already closed, ignore

            if (Settings == null)
                throw new NullReferenceException($"Settings == null on entId={Entity?.EntityId}; Test log 1");

            if (MyAPIGateway.Utilities == null)
                throw new NullReferenceException($"MyAPIGateway.Utilities == null; entId={Entity?.EntityId}; Test log 2");

            if (Block.Storage == null)
                Block.Storage = new MyModStorageComponent();

            Block.Storage.SetValue(SettingsGUID, Convert.ToBase64String(MyAPIGateway.Utilities.SerializeToBinary(Settings)));
        }

        internal virtual void LoadDefaultSettings()
        {
            if (!MyAPIGateway.Session.IsServer)
                return;

            Settings.PowerUsage = 0.5f;
            Settings.OverridePowerUsage = 1.5f;

            PowerUsageSync.Value = Settings.PowerUsage;
            OverridePowerUsageSync.Value = Settings.OverridePowerUsage;
        }

        internal virtual bool LoadSettings()
        {
            if (Block.Storage == null)
            {
                LoadDefaultSettings();
                return false;
            }

            string rawData;
            if (!Block.Storage.TryGetValue(SettingsGUID, out rawData))
            {
                LoadDefaultSettings();
                return false;
            }

            try
            {
                var loadedSettings = MyAPIGateway.Utilities.SerializeFromBinary<FusionPartSettings>(Convert.FromBase64String(rawData));

                if (loadedSettings != null)
                {
                    Settings.PowerUsage = loadedSettings.PowerUsage;
                    Settings.OverridePowerUsage = loadedSettings.OverridePowerUsage;

                    PowerUsageSync.Value = loadedSettings.PowerUsage;
                    OverridePowerUsageSync.Value = loadedSettings.OverridePowerUsage;

                    return true;
                }
            }
            catch (Exception e)
            {
                MyLog.Default.WriteLineAndConsole("Exception in loading FusionPart settings: " + e.ToString());
                MyAPIGateway.Utilities.ShowMessage("Fusion Systems", "Exception in loading FusionPart settings: " + e.ToString());
            }
            return false;
        }

        public override bool IsSerialized()
        {
            try
            {
                SaveSettings();
            }
            catch (Exception e)
            {
                MyLog.Default.WriteLineAndConsole("Exception in loading FusionPart settings: " + e.ToString());
                MyAPIGateway.Utilities.ShowMessage("Fusion Systems", "Exception in loading FusionPart settings: " + e.ToString());
            }

            return base.IsSerialized();
        } 
    }

    [ProtoContract(UseProtoMembersOnly = true)]
    internal class FusionPartSettings
    {
        [ProtoMember(1)] public float PowerUsage;
        [ProtoMember(2)] public float OverridePowerUsage;
        // Don't need to save Override because it would be instantly reset.
    }
}
