using System;
using Digi;
using MIG.Shared.SE;
using Sandbox.ModAPI;
using VRage.Game.ModAPI;

namespace MIG.SpecCores
{
    public class LimitedBlockNetworking
    {
        private static Guid Guid = new Guid("82f6e121-550f-4042-a2b8-185ed2a52abd");
        public static Sync<LimitedBlockSettings, ILimitedBlock> Sync;
        
        public static void Init()
        {
            Sync = new Sync<LimitedBlockSettings, ILimitedBlock>(17274, (x) => x.Component.Settings, Handler, entityLogicGetter: (id) =>
            {
                var block = id.As<IMyCubeBlock>();
                if (block == null) return null;
                var ship = block.CubeGrid.GetShip();
                if (ship == null) return null;
                ILimitedBlock shipSpecBlock;
                if (ship.LimitedBlocks.TryGetValue(block, out shipSpecBlock))
                {
                    return shipSpecBlock;
                }
                return null;
            });
        }
        
        public LimitedBlockSettings Settings;
        public IMyTerminalBlock Block;
        public ILimitedBlock LimitedBlock;
        public LimitedBlockNetworking(ILimitedBlock limitedBlock, IMyTerminalBlock block, LimitedBlockSettings defaultSettings)
        {
            Block = block;
            LimitedBlock = limitedBlock;
            
            if (MyAPIGateway.Session.IsServer)
            {
                if (!block.TryGetStorageData(Guid, out Settings, true))
                {
                    Settings = new LimitedBlockSettings(defaultSettings);
                }
            }
            else
            {
                Sync.RequestData(block.EntityId);
            }
        }
        
        public static void Handler (ILimitedBlock block, LimitedBlockSettings settings, byte type, ulong userSteamId, bool isFromServer)
        {
            if (isFromServer && !MyAPIGateway.Session.IsServer)
            {
                block.Component.Settings = settings;
                block.Component.OnSettingsChanged();
            }
            else
            {
                block.Component.ApplyDataFromClient(settings, userSteamId, type);
                block.Component.NotifyAndSave();
                block.Component.OnSettingsChanged();
            }
        }
        
        public void OnSettingsChanged() { }

        public void NotifyAndSave(byte type=255, bool forceSave = false)
        {
            try
            {
                if (MyAPIGateway.Session.IsServer)
                {
                    Sync.SendMessageToOthers(Block.EntityId, Settings, type: type);
                    SaveSettings(forceSave);
                }
                else
                {
                    if (Sync != null)
                    {
                        Sync.SendMessageToServer(Block.EntityId, Settings, type: type);
                    }
                }
            }
            catch (Exception ex)
            {
                Log.ChatError($"NotifyAndSave {type} Exception {ex} {ex.StackTrace}");
            }
        }
        
        public void SaveSettings(bool forceSave = false)
        {
            if (MyAPIGateway.Session.IsServer)
            {
                Block.SetStorageData(Guid, Settings, true);
            }
        }
        
        private void ApplyDataFromClient(LimitedBlockSettings blockSettings, ulong playerSteamId, byte command)
        {
            Settings.AutoEnable = blockSettings.AutoEnable;
            Settings.SmartTurnOn = blockSettings.SmartTurnOn;
        }
    }
}