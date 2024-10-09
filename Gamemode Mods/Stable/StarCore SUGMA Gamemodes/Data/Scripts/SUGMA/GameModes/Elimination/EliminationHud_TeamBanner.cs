using RichHudFramework.UI;
using SC.SUGMA.Utilities;
using VRage.Game.ModAPI;
using VRageMath;

namespace SC.SUGMA.GameModes.Elimination
{
    internal class EliminationHud_TeamBanner : HudElementBase
    {
        public const int BaseWidth = 290;
        public const int BaseHeight = 25;
        public readonly bool IsLeftAligned;
        private readonly LabelBox _factionLabel;
        private readonly TexturedBox[] _ticketDividers;
        private LabelBox _ticketsLabel;

        public IMyFaction Faction;
        public int StartShipCount;

        public TexturedBox TicketsBar;

        public EliminationHud_TeamBanner(HudParentBase parent, IMyFaction faction, int shipCount, bool isLeftAligned) :
            base(parent)
        {
            Faction = faction;
            StartShipCount = shipCount;
            IsLeftAligned = isLeftAligned;

            Size = new Vector2(BaseWidth, BaseHeight);

            TicketsBar = new TexturedBox(this)
            {
                ParentAlignment = ParentAlignments.Inner |
                                  (isLeftAligned ? ParentAlignments.Left : ParentAlignments.Right),
                DimAlignment = DimAlignments.Height,
                Color = faction.CustomColor.ColorMaskToRgb(),
                Size = new Vector2(BaseWidth / 3.5f * 2.4f, BaseHeight)
            };
            var ticketsBarBackground = new TexturedBox(this)
            {
                ParentAlignment = ParentAlignments.Inner |
                                  (isLeftAligned ? ParentAlignments.Left : ParentAlignments.Right),
                DimAlignment = DimAlignments.Height,
                Color = HudConstants.HudBackgroundColor,
                Size = new Vector2(BaseWidth / 3.5f * 2.4f, BaseHeight),
                ZOffset = -1
            };

            _factionLabel = new LabelBox(this)
            {
                Text = "MMMM888", // Text padding
                Color = HudConstants.HudBackgroundColor,
                DimAlignment = DimAlignments.Height,
                ParentAlignment = ParentAlignments.Inner |
                                  (isLeftAligned ? ParentAlignments.Right : ParentAlignments.Left),
                Size = new Vector2(BaseWidth / 5f, BaseHeight),
                //AutoResize = false,
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
            for (var i = 0; i < _ticketDividers.Length; i++)
                _ticketDividers[i] = new TexturedBox(this)
                {
                    Color = Color.Black,
                    Size = new Vector2(5, BaseHeight),
                    ParentAlignment = ParentAlignments.Inner |
                                      (isLeftAligned ? ParentAlignments.Left : ParentAlignments.Right),
                    Offset = isLeftAligned
                        ? new Vector2(TicketsBar.Width / shipCount * (i + 1), 0)
                        : new Vector2(-TicketsBar.Width / shipCount * (i + 1), 0)
                };
        }

        public void Update(int factionPoints, int startingPoints)
        {
            if (factionPoints < 0)
                factionPoints = 0;

            string pointsString;
            if (factionPoints == 0)
                pointsString = "OUT";
            else
                pointsString = (factionPoints < 1000 ? "0" : "") + (factionPoints < 100 ? "0" : "") +
                               (factionPoints < 10 ? "0" : "") + factionPoints;

            _factionLabel.Text = $"{Faction.Tag}{(Faction.Tag.Length > 3 ? "" : "  ")} {pointsString}";
            TicketsBar.Width = BaseWidth / 3.5f * 2.4f * factionPoints / startingPoints;
        }
    }
}