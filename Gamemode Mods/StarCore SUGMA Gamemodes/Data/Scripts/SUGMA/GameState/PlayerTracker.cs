using System.Collections.Generic;
using System.Linq;
using Sandbox.ModAPI;
using VRage.Game.ModAPI;

namespace SC.SUGMA.GameState
{
    internal class PlayerTracker : ComponentBase
    {
        public static PlayerTracker I;
        public readonly Dictionary<long, IMyPlayer> AllPlayers = new Dictionary<long, IMyPlayer>();

        private static readonly string[] ExcludedFactionTags = {
            "NEU",
            "SPRT",
            "OUT",
            "ADM",
            "UNKN",
            "FCTM",
        };

        #region Public Methods

        public IMyFaction GetGridFaction(IMyCubeGrid grid)
        {
            if (grid.BigOwners.Count == 0)
                return null;

            return MyAPIGateway.Session.Factions.TryGetPlayerFaction(grid.BigOwners[0]);
        }

        public IMyPlayer GetGridOwner(IMyCubeGrid grid)
        {
            if (grid.BigOwners.Count == 0)
                return null;

            return AllPlayers.GetValueOrDefault(grid.BigOwners[0], null);
        }

        public IEnumerable<IMyFaction> GetPlayerFactions()
        {
            return MyAPIGateway.Session.Factions.Factions.Values.Where(f => !ExcludedFactionTags.Contains(f.Tag));
        }

        #endregion

        private void UpdatePlayers()
        {
            var allPlayersList = new List<IMyPlayer>();
            MyAPIGateway.Players.GetPlayers(allPlayersList);

            AllPlayers.Clear();
            foreach (var player in allPlayersList) AllPlayers.Add(player.IdentityId, player);
        }

        #region Base Methods

        public override void Init(string id)
        {
            base.Init(id);
            I = this;

            UpdatePlayers();
        }

        public override void Close()
        {
            I = null;
        }

        private int _ticks;

        public override void UpdateTick()
        {
            _ticks++;

            if (_ticks % 59 == 0)
                UpdatePlayers();
        }

        #endregion
    }
}