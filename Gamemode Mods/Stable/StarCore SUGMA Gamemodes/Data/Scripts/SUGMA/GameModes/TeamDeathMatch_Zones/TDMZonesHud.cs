using RichHudFramework.Client;
using RichHudFramework.UI.Client;
using SC.SUGMA.GameModes.TeamDeathMatch;
using System;
using System.Collections.Generic;
using RichHudFramework.UI;
using RichHudFramework.UI.Rendering;
using Sandbox.ModAPI;
using VRage.Game.Entity;
using VRage.Game.ModAPI;
using VRage.ModAPI;
using VRage.Utils;
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
        private static Material _chevronMaterial = new Material(MyStringId.GetOrCompute("SugmaChevron"), new Vector2(16, 16));
        private static Material _chevronMaterialFlip = new Material(MyStringId.GetOrCompute("SugmaChevronFlip"), new Vector2(16, 16));

        private Dictionary<IMyFaction, List<TexturedBox>> _factionChevrons =
            new Dictionary<IMyFaction, List<TexturedBox>>();
        private Dictionary<IMyFaction, bool> _didShiftChevrons = new Dictionary<IMyFaction, bool>();

        public TDMZonesHud_Window(HudParentBase parent, TDMZonesGamemode gamemode) : base(parent)
        {
            _gamemode = gamemode;
            _windowBase = SUGMA_SessionComponent.I.GetComponent<TeamDeathmatchHud>("tdmHud").Window;
            foreach (var faction in _gamemode.TrackedFactions.Keys)
            {
                _factionChevrons.Add(faction, new List<TexturedBox>());
                _didShiftChevrons.Add(faction, false);
            }
        }

        public void Update()
        {
            Dictionary<IMyFaction, int> neededChevrons = CalculateNeededChevrons();

            // I am so sorry
            foreach (var factionBanner in _windowBase.Banners)
            {
                if (factionBanner.TicketsBar.Width == 0)
                    continue;

                int needed = neededChevrons[factionBanner.Faction];

                RemoveExcessChevrons(factionBanner.Faction, needed);

                AddChevrons(factionBanner, needed);

                // Adjust chevrons if they're about to go off of the ticket bar
                if (!_didShiftChevrons[factionBanner.Faction] && factionBanner.TicketsBar.Width < needed * factionBanner.Height / 2)
                {
                    foreach (var chevron in _factionChevrons[factionBanner.Faction])
                    {
                        chevron.ParentAlignment = ParentAlignments.Inner |
                                                  (factionBanner.IsLeftAligned ? ParentAlignments.Left : ParentAlignments.Right);
                        chevron.Offset = -chevron.Offset;
                    }

                    _didShiftChevrons[factionBanner.Faction] = true;
                }
            }
        }

        private Dictionary<IMyFaction, int> CalculateNeededChevrons()
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

            return neededChevrons;
        }

        private void RemoveExcessChevrons(IMyFaction faction, int needed)
        {
            while (_factionChevrons[faction].Count > needed)
            {
                var toRemove =
                    _factionChevrons[faction][_factionChevrons[faction].Count - 1];
                toRemove.Parent.RemoveChild(toRemove);
                _factionChevrons[faction]
                    .RemoveAt(_factionChevrons[faction].Count - 1);
                Log.Info($"Removed chevron for {faction.Tag}.");
            }
        }

        private void AddChevrons(TDMHud_TeamBanner factionBanner, int needed)
        {
            while (_factionChevrons[factionBanner.Faction].Count < needed)
            {
                TexturedBox newChevron = new TexturedBox(factionBanner.TicketsBar)
                {
                    Material = factionBanner.IsLeftAligned ? _chevronMaterialFlip : _chevronMaterial,
                    ParentAlignment = ParentAlignments.Inner |
                                      (factionBanner.IsLeftAligned ? ParentAlignments.Right : ParentAlignments.Left),
                    Size = new Vector2(factionBanner.Height/2, factionBanner.Height/2),
                    Offset = (factionBanner.IsLeftAligned ? -1 : 1) * new Vector2(_factionChevrons[factionBanner.Faction].Count * factionBanner.Height / 2, 0),
                    Padding = Vector2.One*2,
                    ZOffset = sbyte.MaxValue,
                };
                _factionChevrons[factionBanner.Faction].Add(newChevron);
                Log.Info($"Created chevron for {factionBanner.Faction.Tag}.");
            }
        }
    }
}
