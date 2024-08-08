using RichHudFramework.Client;
using RichHudFramework.UI;
using RichHudFramework.UI.Client;
using Sandbox.ModAPI;
using SC.SUGMA.API;
using SC.SUGMA.GameModes.TeamDeathMatch;
using SC.SUGMA.GameState;
using SC.SUGMA.Utilities;
using System;
using System.Collections.Generic;
using RichHudFramework.UI.Rendering;
using VRage.Game.ModAPI;
using VRageMath;

namespace SC.SUGMA.GameModes.Domination
{
    internal class DominationHud : ComponentBase
    {
        internal DominationGamemode Gamemode;
        internal DOMHud_Window Window;
        internal double RespawnTimeRemaining = -1;
        private int _closeTime = -1;

        public DominationHud(DominationGamemode gamemode)
        {
            Gamemode = gamemode;
        }

        public override void Init(string id)
        {
            base.Init(id);
            SUGMA_SessionComponent.I.ShareTrackApi.RegisterOnAliveChanged(OnAliveChanged);
            if (!RichHudClient.Registered)
                throw new Exception("RichHudAPI was not initialized in time!");

            Window = new DOMHud_Window(HudMain.HighDpiRoot, Gamemode);
        }

        public override void Close()
        {
            HudMain.HighDpiRoot.RemoveChild(Window);
            SUGMA_SessionComponent.I.ShareTrackApi.UnregisterOnAliveChanged(OnAliveChanged);
        }

        public override void UpdateTick()
        {
            if (RespawnTimeRemaining > 0)
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

    internal class DOMHud_Window : HudElementBase
    {
        private readonly DominationGamemode _gamemode;

        private bool _matchEnded;
        private readonly MatchTimer _timer;

        private readonly LabelBox _timerLabel;

        private readonly Dictionary<IMyFaction, LabelBox> _factionLabels = new Dictionary<IMyFaction, LabelBox>();

        public DOMHud_Window(HudParentBase parent, DominationGamemode gamemode) : base(parent)
        {
            _gamemode = gamemode;
            _timer = gamemode.MatchTimer;

            if (_gamemode == null)
                throw new Exception("Null DOM gamemode!");
            if (_timer == null)
                throw new Exception("Null match timer!");

            Size = new Vector2(320, 24);

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
                        ParentAlignments.Inner |
                        (idx % 2 == 0 ? ParentAlignments.Left : ParentAlignments.Right) |
                        ParentAlignments.Top,
                    Color = HudConstants.HudBackgroundColor,
                    Offset = new Vector2(0, (int)-Math.Floor(idx / 2f) * (24 + 5)),
                    Text = "0%",
                };
                idx++;
            }

            _gamemode.PointTracker.OnPointsUpdated += OnPointsUpdated;
        }

        private void OnPointsUpdated(IMyFaction faction, int points)
        {
            if (!_factionLabels.ContainsKey(faction))
                return;

            _factionLabels[faction].Text = $"{100f*((float)points/_gamemode.PointTracker.VictoryPoints):N0}%";
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

            if (_timerLabel == null)
                return;

            var winnerLabel = new LabelBox(_timerLabel)
            {
                Text = winner != null
                    ? $"A WINNER IS {winner.Name}. {winnerPoints} tickets remaining."
                    : "YOU ARE ALL LOSERS",
                ParentAlignment = ParentAlignments.Bottom,
                Height = TDMHud_TeamBanner.BaseHeight,
                TextPadding = new Vector2(2.5f, 0),
                Color = HudConstants.HudBackgroundColor
            };

            winnerLabel.TextBoard.SetFormatting(GlyphFormat.White.WithColor(Color.Red).WithSize(3)
                .WithAlignment(TextAlignment.Center));
        }
    }
}
