using System;
using System.Collections.Generic;
using RichHudFramework.Client;
using RichHudFramework.UI;
using RichHudFramework.UI.Client;
using SC.SUGMA.GameState;
using VRageMath;

namespace SC.SUGMA.GameModes.TeamDeathMatch
{
    internal class TeamDeathmatchHud : ComponentBase
    {
        private TDMHud_Window _window;

        public override void Init(string id)
        {
            base.Init(id);

            if (!RichHudClient.Registered)
                throw new Exception("RichHudAPI was not initialized in time!");

            _window = new TDMHud_Window(HudMain.HighDpiRoot);
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

    internal class TDMHud_Window : HudElementBase
    {
        private TeamDeathmatchGamemode _gamemode;
        private MatchTimer _timer;

        private LabelBox _timerLabel;
        private TDMHud_TeamBanner[] _banners;

        public TDMHud_Window(HudParentBase parent) : base(parent)
        {
            _gamemode = SUGMA_SessionComponent.I.GetComponent<TeamDeathmatchGamemode>("tdm");
            _timer = SUGMA_SessionComponent.I.GetComponent<MatchTimer>("MatchTimer");

            if (_gamemode == null)
                throw new Exception("Null TDM gamemode!");
            if (_timer == null)
                throw new Exception("Null match timer!");

            Size = new Vector2(640, TDMHud_TeamBanner.BaseHeight);

            Offset = new Vector2(0, 515); // Regardless of screen size, this is out of 1920x1080

            _timerLabel = new LabelBox(this)
            {
                ParentAlignment = ParentAlignments.Inner | ParentAlignments.Top,
                Height = TDMHud_TeamBanner.BaseHeight,
                DimAlignment = DimAlignments.Height,
                Text = "20:00",
                TextPadding = new Vector2(2.5f, 0),
                FitToTextElement = false,
                Color = new Color(255, 255, 255, 40),
            };

            List<TDMHud_TeamBanner> banners = new List<TDMHud_TeamBanner>(_gamemode.TrackedFactions.Count);
            int idx = 0;
            foreach (var faction in _gamemode.TrackedFactions)
            {
                TDMHud_TeamBanner newBanner = new TDMHud_TeamBanner(this, faction.Key, faction.Value, idx % 2 == 0)
                {
                    ParentAlignment =
                        ParentAlignments.Inner |
                        (idx % 2 == 0 ? ParentAlignments.Left : ParentAlignments.Right) |
                        ParentAlignments.Top,

                    Offset = new Vector2(0, (int)-Math.Floor(idx / 2f) * (TDMHud_TeamBanner.BaseHeight + 5))
                };
                banners.Add(newBanner);
                idx++;
            }

            _banners = banners.ToArray();
        }

        public void Update()
        {
            TimeSpan matchTime = _timer.CurrentMatchTime;
            int matchSeconds = (int)matchTime.TotalSeconds;
            int basePoints = (int)(_timer.MatchDurationMinutes * 60);

            int remainingMinutes = (int)Math.Floor(_timer.MatchDurationMinutes - matchTime.TotalMinutes);
            int remainingSeconds =
                (int)((_timer.MatchDurationMinutes - matchTime.TotalMinutes - remainingMinutes) * 60);

            _timerLabel.Text =
                $"{(remainingMinutes < 10 ? "0" + remainingMinutes : remainingMinutes.ToString())}:{(remainingSeconds < 10 ? "0" + remainingSeconds : remainingSeconds.ToString())}";

            foreach (var banner in _banners)
            {
                banner.Update(_gamemode.PointTracker.GetFactionPoints(banner.Faction), matchSeconds, basePoints);
            }
        }
    }
}