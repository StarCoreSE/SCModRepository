using System;
using System.Collections.Generic;
using RichHudFramework;
using RichHudFramework.Client;
using RichHudFramework.UI;
using RichHudFramework.UI.Client;
using RichHudFramework.UI.Rendering;
using SC.SUGMA.GameModes.Elimination;
using SC.SUGMA.GameState;
using SC.SUGMA.Utilities;
using VRage.Game.ModAPI;
using VRage.Utils;
using VRageMath;

namespace SC.SUGMA.GameModes.Domination
{
    internal class DominationHud : ComponentBase
    {
        private readonly DominationGamemode _gamemode;
        private DominationHud_Window _window;

        public DominationHud(DominationGamemode gamemode)
        {
            _gamemode = gamemode;
        }

        public override void Init(string id)
        {
            base.Init(id);

            if (!RichHudClient.Registered)
                throw new Exception("RichHudAPI was not initialized in time!");

            _window = new DominationHud_Window(HudMain.HighDpiRoot, _gamemode);
        }

        public override void Close()
        {
            HudMain.HighDpiRoot.RemoveChild(_window);
        }

        public override void UpdateTick()
        {
            if (SUGMA_SessionComponent.I.CurrentGamemode != null)
                _window.Update();
        }
    }

    internal class DominationHud_Window : WindowBase
    {
        private static readonly Material _chevronMaterial =
            new Material(MyStringId.GetOrCompute("SugmaChevron"), new Vector2(16, 16));

        private static readonly Material _chevronMaterialFlip =
            new Material(MyStringId.GetOrCompute("SugmaChevronFlip"), new Vector2(16, 16));

        private static readonly Material _circleMaterial =
            new Material(MyStringId.GetOrCompute("SugmaCircle"), new Vector2(32, 32));


        private readonly Dictionary<IMyFaction, bool> _didShiftChevrons = new Dictionary<IMyFaction, bool>();

        private readonly Dictionary<IMyFaction, List<TexturedBox>> _factionChevrons =
            new Dictionary<IMyFaction, List<TexturedBox>>();

        private readonly TexturedBox[] _captureIndicators;

        private readonly DominationGamemode _gamemode;
        private readonly elmHud_Window _windowBase;

        public DominationHud_Window(HudParentBase parent, DominationGamemode gamemode) : base(parent)
        {
            _gamemode = gamemode;
            _windowBase = SUGMA_SessionComponent.I.GetComponent<EliminationHud>("elmHud").Window;
            foreach (var faction in _gamemode.TrackedFactions.Keys)
            {
                _factionChevrons.Add(faction, new List<TexturedBox>());
                _didShiftChevrons.Add(faction, false);
            }

            _captureIndicators = new TexturedBox[_gamemode.FactionZones.Count];
            float indicatorsSectionWidth = _captureIndicators.Length * 48;
            for (int i = 0; i < _captureIndicators.Length; i++)
            {
                _captureIndicators[i] = new TexturedBox(_windowBase)
                {
                    Material = _circleMaterial,
                    ParentAlignment = ParentAlignments.Bottom,
                    Size = Vector2.One * 32,
                    Offset = new Vector2(-indicatorsSectionWidth/2f + (i * 48) + 24, -4),
                    ZOffset = sbyte.MaxValue
                };
            }
        }

        public void Update()
        {
            var neededChevrons = CalculateNeededChevrons();

            try
            {
                // I am so sorry
                foreach (var factionBanner in _windowBase.Banners)
                {
                    if (factionBanner.TicketsBar.Width == 0)
                        continue;

                    var needed = neededChevrons[factionBanner.Faction];

                    RemoveExcessChevrons(factionBanner.Faction, needed);

                    AddChevrons(factionBanner, needed);

                    // Adjust chevrons if they're about to go off of the ticket bar
                    if (!_didShiftChevrons[factionBanner.Faction] &&
                        factionBanner.TicketsBar.Width < needed * factionBanner.Height / 2)
                    {
                        foreach (var chevron in _factionChevrons[factionBanner.Faction])
                        {
                            chevron.ParentAlignment = ParentAlignments.Inner |
                                                      (factionBanner.IsLeftAligned
                                                          ? ParentAlignments.Left
                                                          : ParentAlignments.Right);
                            chevron.Offset = -chevron.Offset;
                        }

                        _didShiftChevrons[factionBanner.Faction] = true;
                    }
                }

                for (int i = 0; i < _captureIndicators.Length; i++)
                {
                    FactionSphereZone zone = _gamemode.FactionZones[i];
                    if (zone.CapturingFaction != null && (int)zone.CaptureTimeCurrent % 2 == 0)
                        _captureIndicators[i].Color = zone.CapturingFaction.CustomColor.ColorMaskToRgb().SetAlphaPct(0.5f);
                    else
                        _captureIndicators[i].Color = zone.Faction == null ? HudConstants.HudBackgroundColor : zone.SphereDrawColor;
                }
            }
            catch (Exception ex)
            {
                Log.Exception(ex, typeof(DominationHud_Window));
            }
        }

        private Dictionary<IMyFaction, int> CalculateNeededChevrons()
        {
            var neededChevrons = new Dictionary<IMyFaction, int>();

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

        private void AddChevrons(EliminationHud_TeamBanner factionBanner, int needed)
        {
            while (_factionChevrons[factionBanner.Faction].Count < needed)
            {
                var newChevron = new TexturedBox(factionBanner.TicketsBar)
                {
                    Material = factionBanner.IsLeftAligned ? _chevronMaterialFlip : _chevronMaterial,
                    ParentAlignment = ParentAlignments.Inner |
                                      (factionBanner.IsLeftAligned ? ParentAlignments.Right : ParentAlignments.Left),
                    Size = new Vector2(factionBanner.Height / 2, factionBanner.Height / 2),
                    Offset = (factionBanner.IsLeftAligned ? -1 : 1) *
                             new Vector2(_factionChevrons[factionBanner.Faction].Count * factionBanner.Height / 2, 0),
                    Padding = Vector2.One * 2,
                    ZOffset = sbyte.MaxValue
                };
                _factionChevrons[factionBanner.Faction].Add(newChevron);
                Log.Info($"Created chevron for {factionBanner.Faction.Tag}.");
            }
        }
    }
}