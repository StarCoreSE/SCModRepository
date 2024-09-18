using System;
using System.Collections.Generic;
using RichHudFramework.Client;
using RichHudFramework.UI;
using RichHudFramework.UI.Client;
using Sandbox.ModAPI;
using SC.SUGMA.GameModes.Elimination;
using SC.SUGMA.GameState;
using SC.SUGMA.Utilities;
using VRage.Game.ModAPI;
using VRageMath;

namespace SC.SUGMA.GameModes.TeamDeathmatch
{
    internal class TeamDeathmatchHud : ComponentBase
    {
        internal TeamDeathmatchGamemode Gamemode;
        internal tdmHud_Window Window;
        internal double RespawnTimeRemaining = -1;
        private int _closeTime = -1;

        public TeamDeathmatchHud(TeamDeathmatchGamemode gamemode)
        {
            Gamemode = gamemode;
        }

        public override void Init(string id)
        {
            base.Init(id);
            SUGMA_SessionComponent.I.ShareTrackApi.RegisterOnAliveChanged(OnAliveChanged);
            if (!RichHudClient.Registered)
                throw new Exception("RichHudAPI was not initialized in time!");

            Window = new tdmHud_Window(HudMain.HighDpiRoot, Gamemode);
        }

        public override void Close()
        {
            HudMain.HighDpiRoot.RemoveChild(Window);
            SUGMA_SessionComponent.I.ShareTrackApi.UnregisterOnAliveChanged(OnAliveChanged);
        }

        public override void UpdateTick()
        {
            if (RespawnTimeRemaining > 0 && Gamemode.IsStarted)
            {
                RespawnTimeRemaining -= 1 / 60d;
                MyAPIGateway.Utilities.ShowNotification($"Respawning in {RespawnTimeRemaining:F1}", 1000/60, "Red");
            }

            Window.Update();
            if (_closeTime > 0)
                _closeTime--;

            if (_closeTime == 0) SUGMA_SessionComponent.I.UnregisterComponent(ComponentId);
        }

        public void MatchEnded(IMyFaction winningFaction)
        {
            Window.MatchEnded(winningFaction);
            _closeTime = HudConstants.MatchResultsVisibleTicks;
        }

        private void OnAliveChanged(IMyCubeGrid grid, bool isAlive)
        {
            if (isAlive)
                return;

            if (grid.GetOwner() != MyAPIGateway.Session?.Player)
                return;

            RespawnTimeRemaining = Gamemode.RespawnManager.RespawnTimeSeconds;
        }
    }

    internal class tdmHud_Window : HudElementBase
    {
        private readonly TeamDeathmatchGamemode _gamemode;

        private bool _matchEnded;
        private readonly MatchTimer _timer;
        private readonly LabelBox _timerLabel;

        private readonly Dictionary<IMyFaction, LabelBox> _factionLabels = new Dictionary<IMyFaction, LabelBox>();

        public tdmHud_Window(HudParentBase parent, TeamDeathmatchGamemode gamemode) : base(parent)
        {
            _gamemode = gamemode;
            _timer = gamemode.MatchTimer;

            if (_gamemode == null)
                throw new Exception("Null TDM gamemode!");
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
                    Text = _gamemode.PointTracker.VictoryPoints == -1 ? "0" : "0%",
                };
                idx++;
            }

            _gamemode.PointTracker.OnPointsUpdated += OnPointsUpdated;
        }

        private void OnPointsUpdated(IMyFaction faction, int points)
        {
            if (!_factionLabels.ContainsKey(faction))
                return;

            _factionLabels[faction].Text = _gamemode.PointTracker.VictoryPoints == -1 ? points.ToString() : $"{100f*((float)points/_gamemode.PointTracker.VictoryPoints):N0}%";
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
