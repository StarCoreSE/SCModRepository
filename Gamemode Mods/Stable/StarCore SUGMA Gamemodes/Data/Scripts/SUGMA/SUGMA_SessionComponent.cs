using System;
using System.Collections.Generic;
using System.Linq;
using RichHudFramework.Client;
using Sandbox.ModAPI;
using SC.SUGMA.API;
using SC.SUGMA.Commands;
using SC.SUGMA.GameModes.TeamDeathMatch;
using SC.SUGMA.GameModes.TeamDeathMatch_Zones;
using SC.SUGMA.GameState;
using SC.SUGMA.HeartNetworking;
using SC.SUGMA.HeartNetworking.Custom;
using SC.SUGMA.Textures;
using VRage.Game.Components;
using VRage.ModAPI;

namespace SC.SUGMA
{
    [MySessionComponentDescriptor(MyUpdateOrder.AfterSimulation)]
    internal class SUGMA_SessionComponent : MySessionComponentBase
    {
        public const float ModVersion = 0.0f;

        private readonly Dictionary<string, ComponentBase> _components = new Dictionary<string, ComponentBase>
        {
            ["MatchTimer"] = new MatchTimer(),
            ["HeartNetwork"] = new HeartNetwork(),
            ["PlayerTracker"] = new PlayerTracker(),
            ["tdm"] = new TeamDeathmatchGamemode(),
            ["tdmz"] = new TDMZonesGamemode(),
            // You would need to add in the zones gamemode too
        };

        public static SUGMA_SessionComponent I { get; private set; }

        public ShareTrackApi ShareTrackApi = new ShareTrackApi();
        public GamemodeBase CurrentGamemode = null;

        public bool HasInited = false;
        private int _pollTimer = 0;

        #region Base Methods

        public override void LoadData()
        {
            I = this;
            Log.Init();
            try
            {
                CommandHandler.Init();
                ShareTrackApi.Init(ModContext, FinishInit);
                RichHudClient.Init(DebugName, () => { Log.Info("RichHudClient registered."); }, null);
            }
            catch (Exception ex)
            {
                Log.Exception(ex, typeof(SUGMA_SessionComponent));
            }
        }

        private void FinishInit()
        {
            try
            {
                foreach (var component in _components.ToArray())
                    component.Value.Init(component.Key);
            }
            catch (Exception ex)
            {
                Log.Exception(ex, typeof(SUGMA_SessionComponent));
            }
        }

        public override void UpdateAfterSimulation()
        {
            if (!ShareTrackApi.IsReady)
                return;

            {
                // Clients should sync first-thing, in case a game is already running.
                if (!HasInited)
                {
                    _pollTimer = 600;
                    HasInited = true;
                }

                if (_pollTimer == 0)
                {
                    if (!MyAPIGateway.Session.IsServer)
                        SyncRequestPacket.RequestSync();
                    _pollTimer = -1;
                }
                else if (_pollTimer > 0)
                    _pollTimer--;
            }

            try
            {
                foreach (var component in _components.Values.ToArray())
                    component.UpdateTick();
            }
            catch (Exception ex)
            {
                Log.Exception(ex, typeof(SUGMA_SessionComponent));
            }
        }

        protected override void UnloadData()
        {
            try
            {
                foreach (var component in _components.Values.ToArray())
                    component.Close();
            }
            catch (Exception ex)
            {
                Log.Exception(ex, typeof(SUGMA_SessionComponent));
            }

            try
            {
                CommandHandler.Close();
                ShareTrackApi.UnloadData();
            }
            catch (Exception ex)
            {
                Log.Exception(ex, typeof(SUGMA_SessionComponent));
            }

            I = null;
            Log.Close();
        }

        #endregion

        public T GetComponent<T>(string id) where T : ComponentBase
        {
            ComponentBase component;
            _components.TryGetValue(id, out component);
            return (T)component;
        }

        public bool RegisterComponent<T>(string id, T component) where T : ComponentBase
        {
            if (_components.ContainsKey(id))
                return false;

            _components.Add(id, component);
            component.Init(id);

            return true;
        }

        public bool UnregisterComponent(string id)
        {
            ComponentBase component;
            if (_components.TryGetValue(id, out component))
                component.Close();
            return _components.Remove(id);
        }

        public string[] GetGamemodes()
        {
            List<string> gamemodes = new List<string>();
            foreach (var component in _components)
            {
                if (!(component.Value is GamemodeBase))
                    continue;
                gamemodes.Add(component.Key);
            }

            return gamemodes.ToArray();
        }

        public bool StartGamemode(string id, string[] arguments, bool notifyNetwork = false)
        {
            // TODO: Apex Legends-esque starting screen.

            Log.Info("Attempting to start gamemode ID: " + id);
            if (!GetGamemodes().Contains(id))
                return false;
            CurrentGamemode?.StopRound();

            CurrentGamemode = (GamemodeBase)_components[id];
            CurrentGamemode.StartRound(arguments);

            SUtils.SetWorldPermissionsForMatch(true);

            if (MyAPIGateway.Session.IsServer || notifyNetwork)
                GameStatePacket.UpdateGamestate();

            return true;
        }

        public bool StopGamemode(bool notifyNetwork = false)
        {
            Log.Info("Attempting to stop gamemode [" + CurrentGamemode?.ComponentId + "].");
            if (CurrentGamemode == null)
                return false;

            if (CurrentGamemode.IsStarted)
                CurrentGamemode.StopRound();

            CurrentGamemode = null;

            SUtils.SetWorldPermissionsForMatch(false);

            if (MyAPIGateway.Session.IsServer || notifyNetwork)
                GameStatePacket.UpdateGamestate();

            return true;
        }
    }
}