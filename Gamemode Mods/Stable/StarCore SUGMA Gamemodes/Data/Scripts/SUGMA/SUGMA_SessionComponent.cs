using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using DefenseShields;
using RichHudFramework.Client;
using Sandbox.ModAPI;
using SC.SUGMA.API;
using SC.SUGMA.Commands;
using SC.SUGMA.GameModes.Domination;
using SC.SUGMA.GameModes.Elimination;
using SC.SUGMA.GameModes.TeamDeathmatch;
using SC.SUGMA.GameState;
using SC.SUGMA.HeartNetworking;
using SC.SUGMA.HeartNetworking.Custom;
using SC.SUGMA.Utilities;
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
            ["DisconnectHandler"] = new DisconnectHandler(),
            ["BounceZone"] = new BounceZone(10000),
            ["elm"] = new EliminationGamemode(),
            ["dom"] = new DominationGamemode(),
            ["tdm"] = new TeamDeathmatchGamemode(),
        };

        /// <summary>
        ///     How many ticks to wait after joining before requesting sync.
        /// </summary>
        private int _pollTimer = 300;

        public GamemodeBase CurrentGamemode;

        public bool HasInited;

        public ShareTrackApi ShareTrackApi = new ShareTrackApi();
        public ShieldApi ShieldApi = new ShieldApi();

        public static SUGMA_SessionComponent I { get; private set; }

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
            var gamemodes = new List<string>();
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

            if (!CurrentGamemode.IsStarted)
            {
                CurrentGamemode = null;
                return false;
            }

            if (MyAPIGateway.Session.IsServer || notifyNetwork)
                GameStatePacket.UpdateGamestate(arguments);

            return true;
        }

        public bool StopGamemode(bool notifyNetwork = false)
        {
            Log.Info("Attempting to stop gamemode [" + CurrentGamemode?.ComponentId + "].");
            if (CurrentGamemode == null)
                return false;

            var currentGamemode = CurrentGamemode;
            CurrentGamemode = null;

            if (currentGamemode.IsStarted)
                currentGamemode.StopRound();

            string[] oldArgs = currentGamemode.Arguments;

            if (MyAPIGateway.Session.IsServer || notifyNetwork)
                GameStatePacket.UpdateGamestate(oldArgs);

            return true;
        }

        public static string ListGamemodes()
        {
            var availableGamemodes = new StringBuilder();

            foreach (var gamemode in I.GetGamemodes())
            {
                var gamemodeObject = I.GetComponent<GamemodeBase>(gamemode);
                availableGamemodes.Append(
                    $"\n-  {gamemode} ({gamemodeObject.ReadableName})\n  *  {gamemodeObject.ArgumentParser.HelpText.Replace("\n", "\n  *  ")}");
            }

            return availableGamemodes.ToString();
        }

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
                ShieldApi.Load();
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

            if (!HasInited && !MyAPIGateway.Session.IsServer)
            {
                // Clients should sync with a slight delay, in case a game is already running.
                // This gives time for ShareTrack to catch up.
                if (_pollTimer <= 0 && ShareTrackApi.AreTrackedGridsLoaded())
                {
                    SyncRequestPacket.RequestSync();
                    HasInited = true;
                }

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
                ShieldApi.Unload();
            }
            catch (Exception ex)
            {
                Log.Exception(ex, typeof(SUGMA_SessionComponent));
            }

            I = null;
            Log.Close();
        }

        #endregion
    }
}