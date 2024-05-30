using System;
using System.Collections.Generic;
using Sandbox.ModAPI;
using VRage.Game.ModAPI;

namespace SC.SUGMA.GameState
{
    internal class PointTracker : ComponentBase
    {
        public const int VictoryPoints = 3;

        private readonly Dictionary<IMyFaction, int> _factionPoints = new Dictionary<IMyFaction, int>();

        #region Base Methods

        public override void Init(string id)
        {
            base.Init(id);
            MyAPIGateway.Session.Factions.FactionCreated += OnFactionCreated;
            foreach (var faction in MyAPIGateway.Session.Factions.Factions)
                OnFactionCreated(faction.Key);
        }

        public override void Close()
        {
            
        }

        public override void UpdateTick()
        {
            foreach (var faction in _factionPoints)
            {
                MyAPIGateway.Utilities.ShowNotification($"{faction.Key.Tag}: {faction.Value}", 1000/60);
            }
        }

        #endregion

        #region Public Methods

        public Action<IMyFaction> OnFactionWin;

        public int GetFactionPoints(IMyFaction faction)
        {
            return _factionPoints.GetValueOrDefault(faction, int.MinValue);
        }

        public int GetFactionPoints(long factionId)
        {
            return GetFactionPoints(MyAPIGateway.Session.Factions.TryGetFactionById(factionId));
        }

        public void SetFactionPoints(IMyFaction faction, int value)
        {
            if (!_factionPoints.ContainsKey(faction))
                return;

            _factionPoints[faction] = value;

            if (value > VictoryPoints)
                OnFactionWin?.Invoke(faction);
        }

        public void SetFactionPoints(long factionId, int value)
        {
            SetFactionPoints(MyAPIGateway.Session.Factions.TryGetFactionById(factionId), value);
        }

        public void AddFactionPoints(IMyFaction faction, int value)
        {
            if (!_factionPoints.ContainsKey(faction))
                return;

            _factionPoints[faction] += value;

            if (_factionPoints[faction] > VictoryPoints)
                OnFactionWin?.Invoke(faction);
        }

        public void AddFactionPoints(long factionId, int value)
        {
            AddFactionPoints(MyAPIGateway.Session.Factions.TryGetFactionById(factionId), value);
        }

        #endregion

        private void OnFactionCreated(long factionId)
        {
            _factionPoints.Add(MyAPIGateway.Session.Factions.TryGetFactionById(factionId), 0);
        }
    }
}
