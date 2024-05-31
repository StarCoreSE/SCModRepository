using System;
using System.Collections.Generic;
using SC.SUGMA.API;
using SC.SUGMA.Commands;
using SC.SUGMA.GameModes.TeamDeathMatch;
using SC.SUGMA.GameState;
using SC.SUGMA.HeartNetworking;
using SC.SUGMA.HeartNetworking.Custom;
using VRage.Game.Components;

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
        };

        public static SUGMA_SessionComponent I { get; private set; }

        public ShareTrackApi ShareTrackApi = new ShareTrackApi();
        public GamemodeBase CurrentGamemode = null;

        #region Base Methods

        public override void LoadData()
        {
            I = this;
            Log.Init();
            try
            {
                CommandHandler.Init();
                ShareTrackApi.Init(ModContext, FinishInit);
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
                foreach (var component in _components)
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

            try
            {
                foreach (var component in _components.Values)
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
                foreach (var component in _components.Values)
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
            return (T) component;
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

        public bool StartGamemode(string id)
        {
            Log.Info("Attempting to start gamemode ID: " + id);
            if (!GetGamemodes().Contains(id))
                return false;
            CurrentGamemode?.StopRound();

            CurrentGamemode = (GamemodeBase) _components[id];
            CurrentGamemode.StartRound();

            SUtils.SetWorldPermissionsForMatch(true);

            GameStatePacket.UpdateGamestate();

            return true;
        }

        public bool StopGamemode()
        {
            Log.Info("Attempting to stop gamemode.");
            if (CurrentGamemode == null)
                return false;

            if (CurrentGamemode.IsStarted)
                CurrentGamemode.StopRound();

            CurrentGamemode = null;

            SUtils.SetWorldPermissionsForMatch(false);

            GameStatePacket.UpdateGamestate();

            return true;
        }
    }
}
