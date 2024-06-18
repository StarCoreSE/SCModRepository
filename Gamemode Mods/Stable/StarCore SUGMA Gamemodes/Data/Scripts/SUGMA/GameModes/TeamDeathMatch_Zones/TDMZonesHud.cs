using RichHudFramework.Client;
using RichHudFramework.UI.Client;
using SC.SUGMA.GameModes.TeamDeathMatch;
using System;
using System.Collections.Generic;
using RichHudFramework.UI;
using Sandbox.ModAPI;
using VRage.Game.ModAPI;
using VRageMath;

namespace SC.SUGMA.GameModes.TeamDeathMatch_Zones
{
    internal class TDMZonesHud : ComponentBase
    {
        private TDMZonesGamemode _gamemode;
        private TDMZonesHud_Window _window;

        public TDMZonesHud(TDMZonesGamemode gamemode)
        {
            _gamemode = gamemode;
        }

        public override void Init(string id)
        {
            base.Init(id);

            if (!RichHudClient.Registered)
                throw new Exception("RichHudAPI was not initialized in time!");

            _window = new TDMZonesHud_Window(HudMain.HighDpiRoot, _gamemode);
        }

        public override void Close()
        {
            HudMain.HighDpiRoot.RemoveChild(_window);
        }

        public override void UpdateTick()
        {
            _window.Update();
        }
    }

    internal class TDMZonesHud_Window : WindowBase
    {
        private TDMZonesGamemode _gamemode;
        private TDMHud_Window _windowBase;

        private Dictionary<IMyFaction, List<TexturedBox>> _factionChevrons =
            new Dictionary<IMyFaction, List<TexturedBox>>();

        public TDMZonesHud_Window(HudParentBase parent, TDMZonesGamemode gamemode) : base(parent)
        {
            _gamemode = gamemode;
            _windowBase = SUGMA_SessionComponent.I.GetComponent<TeamDeathmatchHud>("tdmHud").Window;
            foreach (var faction in _gamemode.TrackedFactions.Keys)
                _factionChevrons.Add(faction, new List<TexturedBox>());
        }

        public void Update()
        {
            Dictionary<IMyFaction, int> neededChevrons = new Dictionary<IMyFaction, int>();

            foreach (var faction in _gamemode.TrackedFactions.Keys)
                neededChevrons.Add(faction, 0);

            foreach (var zone in _gamemode.FactionZones)
            {
                if (zone.Faction == null)
                    continue;

                foreach (var faction in _gamemode.TrackedFactions.Keys)
                    if (faction != zone.Faction)
                        neededChevrons[faction]++;
            }

            // I am so sorry
            foreach (var factionBanner in _windowBase.Banners)
            {
                int needed = neededChevrons[factionBanner.Faction];
                MyAPIGateway.Utilities.ShowNotification($"{factionBanner.Faction.Tag}: {needed}", 1000/60);

                while (_factionChevrons[factionBanner.Faction].Count > needed)
                {
                    factionBanner.RemoveChild(
                        _factionChevrons[factionBanner.Faction][_factionChevrons[factionBanner.Faction].Count - 1]);
                    _factionChevrons[factionBanner.Faction]
                        .RemoveAt(_factionChevrons[factionBanner.Faction].Count - 1);
                    Log.Info($"Removed chevron for {factionBanner.Faction.Tag}.");
                }

                while (_factionChevrons[factionBanner.Faction].Count < needed)
                {
                    TexturedBox newChevron = new TexturedBox(factionBanner)
                    {
                        Color = Color.White,
                        DimAlignment = DimAlignments.Height,
                        ParentAlignment = ParentAlignments.Inner | ParentAlignments.Top
                    };
                    _factionChevrons[factionBanner.Faction].Add(newChevron);
                    Log.Info($"Created chevron for {factionBanner.Faction.Tag}.");
                }
            }
        }
    }
}
