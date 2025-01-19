using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Sandbox.Game.Multiplayer;
using Sandbox.ModAPI;
using SC.SUGMA.HeartNetworking;
using SC.SUGMA.HeartNetworking.Custom;
using SC.SUGMA.Utilities;
using VRage.Game.ModAPI;

namespace SC.SUGMA.GameState
{
    internal class PointTracker : ComponentBase
    {
        private bool _pointsUpdated;
        public int StartingPoints;
        public int VictoryPoints = 3;

        public Dictionary<IMyFaction, int> FactionPoints { get; internal set; } = new Dictionary<IMyFaction, int>();
        public Action<IMyFaction, int> OnPointsUpdated = null;

        private void OnFactionCreated(long factionId)
        {
            FactionPoints.Add(MyAPIGateway.Session.Factions.TryGetFactionById(factionId), StartingPoints);
        }

        #region Base Methods

        public PointTracker(int startingPoints, int victoryPoints)
        {
            StartingPoints = startingPoints;
            VictoryPoints = victoryPoints;
        }

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
            if (_pointsUpdated && MyAPIGateway.Session.IsServer)
            {
                HeartNetwork.I.SendToEveryone(new PointsPacket(this));
                _pointsUpdated = false;
            }
        }

        #endregion

        #region Public Methods

        public Action<IMyFaction> OnFactionWin;

        public int GetFactionPoints(IMyFaction faction)
        {
            return FactionPoints.GetValueOrDefault(faction, int.MinValue);
        }

        public int GetFactionPoints(long factionId)
        {
            return GetFactionPoints(MyAPIGateway.Session.Factions.TryGetFactionById(factionId));
        }

        public void SetFactionPoints(IMyFaction faction, int value)
        {
            if (!MyAPIGateway.Session.IsServer || !FactionPoints.ContainsKey(faction))
                return;

            FactionPoints[faction] = value;

            OnPointsUpdated?.Invoke(faction, FactionPoints[faction]);
            if (VictoryPoints > StartingPoints ? value >= VictoryPoints : value <= VictoryPoints)
                OnFactionWin?.Invoke(faction);
            _pointsUpdated = true;
        }

        public void SetFactionPoints(long factionId, int value)
        {
            SetFactionPoints(MyAPIGateway.Session.Factions.TryGetFactionById(factionId), value);
        }

        public void AddFactionPoints(IMyFaction faction, int value)
        {
            if (!MyAPIGateway.Session.IsServer || !FactionPoints.ContainsKey(faction))
                return;

            FactionPoints[faction] += value;

            OnPointsUpdated?.Invoke(faction, FactionPoints[faction]);
            if (VictoryPoints > StartingPoints
                    ? FactionPoints[faction] >= VictoryPoints
                    : FactionPoints[faction] <= VictoryPoints)
                OnFactionWin?.Invoke(faction);
            _pointsUpdated = true;
        }

        public void AddFactionPoints(long factionId, int value)
        {
            AddFactionPoints(MyAPIGateway.Session.Factions.TryGetFactionById(factionId), value);
        }

        public void UpdateFromPacket(PointsPacket packet)
        {
            int bufferPoints;
            foreach (var factionPts in packet.FactionPoints)
            {
                if (!FactionPoints.TryGetValue(factionPts.Key, out bufferPoints) || factionPts.Value != bufferPoints)
                    OnPointsUpdated?.Invoke(factionPts.Key, factionPts.Value);
            }
            FactionPoints = packet.FactionPoints;

            //string data = "";
            //foreach (var kvp in FactionPoints)
            //{
            //    data += $"\n-    {kvp.Key.Tag}: {kvp.Value}";
            //}
            //Log.Info("Updated from packet with:" + data);
        }

        #endregion

        public override string ToString()
        {
            IEnumerable<IMyFaction> playerFactions = PlayerTracker.I.GetPlayerFactions();

            StringBuilder sb = new StringBuilder();
            foreach (var faction in FactionPoints.Where(k => playerFactions.Contains(k.Key)))
                sb.Append($"{faction.Key.Name}: {faction.Value}pts\n");
            return sb.ToString();
        }
    }
}