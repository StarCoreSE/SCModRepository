using RichHudFramework.Client;
using RichHudFramework.UI;
using RichHudFramework.UI.Client;
using Sandbox.Game.Entities;
using SC.SUGMA.GameModes.Elimination;
using SC.SUGMA.GameState;
using SC.SUGMA.Utilities;
using System;
using System.Collections.Generic;
using Sandbox.ModAPI;
using VRage.Game.ModAPI;
using VRageMath;

namespace SC.SUGMA.GameModes.RocketCore
{
    internal class RocketCoreHud : ComponentBase
    {
        private int _closeTime = -1;
        private readonly RocketCoreGamemode _gamemode;
        public RocHud_Window Window;

        public MySoundPair CaptureSound = new MySoundPair("SUGMA_CaptureSound_TF2");
        private List<IMyGps> _zoneGpses = new List<IMyGps>();

        public RocketCoreHud(RocketCoreGamemode gamemode)
        {
            _gamemode = gamemode;
        }

        public override void Init(string id)
        {
            base.Init(id);

            if (!RichHudClient.Registered)
                throw new Exception("RichHudAPI was not initialized in time!");

            Window = new RocHud_Window(HudMain.HighDpiRoot, _gamemode);
            foreach (var zone in _gamemode.FactionGoals)
            {
                var gps = MyAPIGateway.Session.GPS.Create($"{zone.Key.Name} Goal", "", zone.Value.Sphere.Center, true,
                    false);
                MyAPIGateway.Session.GPS.AddLocalGps(gps);
                _zoneGpses.Add(gps);
            }
        }

        public override void Close()
        {
            HudMain.HighDpiRoot.RemoveChild(Window);
            foreach (var gps in _zoneGpses)
                MyAPIGateway.Session.GPS.RemoveLocalGps(gps);
        }

        public override void UpdateTick()
        {
            Window.Update();
            if (_closeTime > 0)
                _closeTime--;

            if (_closeTime == 0) SUGMA_SessionComponent.I.UnregisterComponent(ComponentId);
        }

        public void MatchEnded(IMyFaction winner)
        {
            Window.MatchEnded(winner);
            _closeTime = HudConstants.MatchResultsVisibleTicks;
        }

        public void GoalScored(IMyFaction faction)
        {
            MyAPIGateway.Utilities.ShowNotification($"Goal was scored against {faction.Name}!", 10000);
            SUtils.PlaySound(CaptureSound);
        }
    }

    internal class RocHud_Window : HudElementBase
    {
        private readonly RocketCoreGamemode _gamemode;

        private bool _matchEnded;
        private readonly MatchTimer _timer;
        private readonly LabelBox _timerLabel;

        private readonly Dictionary<IMyFaction, LabelBox> _factionLabels = new Dictionary<IMyFaction, LabelBox>();

        public RocHud_Window(HudParentBase parent, RocketCoreGamemode gamemode) : base(parent)
        {
            _gamemode = gamemode;
            _timer = gamemode.MatchTimer;

            if (_gamemode == null)
                throw new Exception("Null gamemode!");
            if (_timer == null)
                throw new Exception("Null match timer!");

            Size = new Vector2(100, 24);

            Offset = new Vector2(0, 515); // Regardless of screen size, this is out of 1920x1080

            _timerLabel = new LabelBox(this)
            {
                ParentAlignment = ParentAlignments.Inner | ParentAlignments.Top,
                Height = 24,
                DimAlignment = DimAlignments.Height,
                Text = "20:00",
                TextPadding = new Vector2(2.5f, 0),
                FitToTextElement = false,
                Color = HudConstants.HudBackgroundColor
            };

            var idx = 0;
            foreach (var faction in _gamemode.TrackedFactions)
            {
                _factionLabels[faction] = new LabelBox(this)
                {
                    Format = GlyphFormat.White.WithColor(faction.CustomColor.ColorMaskToRgb()).WithSize(2)
                        .WithAlignment(TextAlignment.Center),
                    ParentAlignment =
                        ParentAlignments.InnerV |
                        (idx % 2 == 0 ? ParentAlignments.Right : ParentAlignments.Left) |
                        ParentAlignments.Top,
                    Color = HudConstants.HudBackgroundColor,
                    Offset = new Vector2(0, (int)-Math.Floor(idx / 2f) * (24 + 5)),
                    Text = $"{_gamemode.PointTracker.StartingPoints} GOALS LEFT"
                };
                idx++;
            }

            _gamemode.PointTracker.OnPointsUpdated += OnPointsUpdated;
        }

        private void OnPointsUpdated(IMyFaction faction, int points)
        {
            if (!_factionLabels.ContainsKey(faction))
                return;

            _factionLabels[faction].Text = $"{points} GOAL{(points == 1 ? "" : "S")} LEFT";
        }

        public void Update()
        {
            if (_matchEnded)
                return;

            _timerLabel.Text = _timer.RemainingTimeString();
        }

        public void MatchEnded(IMyFaction winner)
        {
            _matchEnded = true;
            var winnerPoints = 0;

            _timerLabel?.Unregister();
            foreach (var label in _factionLabels)
                label.Value.Unregister();

            var winnerLabel = new LabelBox(_timerLabel)
            {
                Text = winner != null
                    ? $"A WINNER IS {winner.Name}."
                    : "YOU ARE ALL LOSERS",
                ParentAlignment = ParentAlignments.Bottom,
                Height = EliminationHud_TeamBanner.BaseHeight,
                TextPadding = new Vector2(2.5f, 0),
                Color = HudConstants.HudBackgroundColor
            };

            winnerLabel.TextBoard.SetFormatting(GlyphFormat.White.WithColor(Color.Red).WithSize(3)
                .WithAlignment(TextAlignment.Center));
        }
    }
}
