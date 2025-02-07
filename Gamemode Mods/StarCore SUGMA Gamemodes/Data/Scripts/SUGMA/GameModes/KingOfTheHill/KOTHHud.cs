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

namespace SC.SUGMA.GameModes.KOTH
{
    internal class KOTHHud : ComponentBase
    {
        private readonly KOTHGamemode _gamemode;
        private KOTHHud_Window _window;

        public KOTHHud(KOTHGamemode gamemode)
        {
            _gamemode = gamemode;
        }

        public override void Init(string id)
        {
            base.Init(id);

            if (!RichHudClient.Registered)
                throw new Exception("RichHudAPI was not initialized in time!");

            _window = new KOTHHud_Window(HudMain.HighDpiRoot, _gamemode);
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

        public void MatchEnded(IMyFaction winner)
        {
            _window.MatchEnded(winner);
        }
    }

    internal class KOTHHud_Window : WindowBase
    {
        private static readonly Material _circleMaterial =
            new Material(MyStringId.GetOrCompute("SugmaCircle"), new Vector2(32, 32));

        private readonly KOTHGamemode _gamemode;
        private readonly elmHud_Window _windowBase;

        private TexturedBox _captureIndicator;

        private Label _captureLabel;

        public KOTHHud_Window(HudParentBase parent, KOTHGamemode gamemode) : base(parent)
        {
            _gamemode = gamemode;
            _windowBase = SUGMA_SessionComponent.I.GetComponent<EliminationHud>("elmHud").Window;

            _captureIndicator = new TexturedBox(_windowBase)
            {
                Material = _circleMaterial,
                ParentAlignment = ParentAlignments.Bottom | ParentAlignments.Center,
                Size = Vector2.One * 32,
                Offset = new Vector2(0, -10), 
                ZOffset = sbyte.MaxValue
            };

            _captureLabel = new Label(_captureIndicator)
            {
                ParentAlignment = ParentAlignments.Center,
                Offset = new Vector2(0, -35),
                Text = "Initializing KOTH..."
            };

            foreach (var banner in _windowBase.Banners)
            {
                banner.Visible = false;
            }
        }

        public void Update()
        {
            if (_gamemode.ControlPoint == null)
            {
                int timeLeft = Math.Max(0, _gamemode.ActivationTimeCounter);
                if (timeLeft > 0)
                {
                    _captureIndicator.Color = Color.White;
                    _captureLabel.Text = $"Zone Locked: {timeLeft}s";
                }
                else
                {
                    _captureIndicator.Color = Color.White;
                    _captureLabel.Text = "Waiting for zone creation...";
                }
                return;
            }

            var zone = _gamemode.ControlPoint;

            bool isActivelyCapturing = zone.ActiveCapturingFaction != null && zone.CaptureTimeCurrent > 0f;
            Color capturingColor = isActivelyCapturing
                ? zone.ActiveCapturingFaction.CustomColor.ColorMaskToRgb()
                : Color.White;

            _captureIndicator.Color = capturingColor.SetAlphaPct(0.5f);

            float current = zone.CaptureTimeCurrent;
            float total = zone.CaptureTime;

            if (!isActivelyCapturing && current > 0f)
            {
                _captureIndicator.Color = Color.White.SetAlphaPct(0.5f);
            }

            _captureLabel.Text = $"Capturing: {current:0.0}s / {total:0.0}s";
        }

        public void MatchEnded(IMyFaction winner)
        {
            _captureIndicator.Visible = false;
            _captureLabel.Visible = false;

            _windowBase._winnerLabel.Visible = false;
            _windowBase._winnerLabel = new LabelBox(_windowBase._timerLabel)
            {
                Text = winner != null
                    ? $"A WINNER IS {winner.Name}"
                    : "YOU ARE ALL LOSERS",
                ParentAlignment = ParentAlignments.Bottom,
                Height = EliminationHud_TeamBanner.BaseHeight,
                TextPadding = new Vector2(2.5f, 0),
                Color = HudConstants.HudBackgroundColor
            };
            _windowBase._winnerLabel.Visible = true;

            _windowBase._winnerLabel.TextBoard.SetFormatting(GlyphFormat.White.WithColor(Color.Red).WithSize(3)
                .WithAlignment(TextAlignment.Center));
        }
    }
}