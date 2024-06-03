using RichHudFramework.UI;
using Sandbox.ModAPI;
using VRage.Game;
using VRage.Game.ModAPI;
using VRageMath;

namespace SC.SUGMA.GameModes.TeamDeathMatch
{
    internal class TDMHud_TeamBanner : HudElementBase
    {
        public const int BaseWidth = 290;
        public const int BaseHeight = 25;

        public IMyFaction Faction;
        private int _startShipCount = 0;

        private TexturedBox _ticketsBar;
        private LabelBox _factionLabel;
        private LabelBox _ticketsLabel;
        private TexturedBox[] _ticketDividers;

        public TDMHud_TeamBanner(HudParentBase parent, IMyFaction faction, int shipCount, bool isLeftAligned) :
            base(parent)
        {
            Faction = faction;
            _startShipCount = shipCount;

            Size = new Vector2(BaseWidth, BaseHeight);

            _ticketsBar = new TexturedBox(this)
            {
                ParentAlignment = ParentAlignments.Inner |
                                  (isLeftAligned ? ParentAlignments.Left : ParentAlignments.Right),
                DimAlignment = DimAlignments.Height,
                Color = ColorMaskToRgb(faction.CustomColor),
                Size = new Vector2(BaseWidth / 3.5f * 2.4f, BaseHeight)
            };
            TexturedBox ticketsBarBackground = new TexturedBox(this)
            {
                ParentAlignment = ParentAlignments.Inner |
                                  (isLeftAligned ? ParentAlignments.Left : ParentAlignments.Right),
                DimAlignment = DimAlignments.Height,
                Color = new Color(255, 255, 255, 40),
                Size = new Vector2(BaseWidth / 3.5f * 2.4f, BaseHeight),
                ZOffset = -1
            };


            _factionLabel = new LabelBox(this)
            {
                Text = "MMMM888", // Text padding
                Color = new Color(255, 255, 255, 40),
                DimAlignment = DimAlignments.Height,
                ParentAlignment = ParentAlignments.Inner |
                                  (isLeftAligned ? ParentAlignments.Right : ParentAlignments.Left),
                Size = new Vector2(BaseWidth / 5f, BaseHeight),
                AutoResize = false,
                FitToTextElement = false,
                Format = new GlyphFormat(Color.White, TextAlignment.Center)
            };
            //_ticketsLabel = new LabelBox(this)
            //{
            //    Color = Color.DarkGray,
            //    ParentAlignment = ParentAlignments.Inner | ParentAlignments.Right,
            //    DimAlignment = DimAlignments.Height,
            //};

            _ticketDividers = new TexturedBox[shipCount - 1];
            for (int i = 0; i < _ticketDividers.Length; i++)
            {
                _ticketDividers[i] = new TexturedBox(this)
                {
                    Color = Color.Black,
                    Size = new Vector2(5, BaseHeight),
                    ParentAlignment = ParentAlignments.Inner |
                                      (isLeftAligned ? ParentAlignments.Left : ParentAlignments.Right),
                    Offset = isLeftAligned
                        ? new Vector2(_ticketsBar.Width / shipCount * (i + 1), 0)
                        : new Vector2(-_ticketsBar.Width / shipCount * (i + 1), 0)
                };
            }
        }

        public void Update(int remainingShips, int matchTimeSeconds, int startingPoints)
        {
            int factionPoints = (int)(startingPoints * (remainingShips / (float)_startShipCount) - matchTimeSeconds);
            if (factionPoints < 0)
                factionPoints = 0;

            string pointsString;
            if (factionPoints == 0)
                pointsString = "OUT";
            else
                pointsString = (factionPoints < 1000 ? "0" : "") + (factionPoints < 100 ? "0" : "") +
                               (factionPoints < 10 ? "0" : "") + factionPoints;

            _factionLabel.Text = $"{Faction.Tag}{(Faction.Tag.Length > 3 ? "" : "  ")} {pointsString}";
            _ticketsBar.Width = BaseWidth / 3.5f * 2.4f * factionPoints / startingPoints;
        }

        private static Vector3 ColorMaskToRgb(Vector3 colorMask)
        {
            return MyColorPickerConstants.HSVOffsetToHSV(colorMask).HSVtoColor();
        }
    }
}