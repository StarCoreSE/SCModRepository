using RichHudFramework;
using Sandbox.ModAPI;
using SC.SUGMA.API;
using SC.SUGMA.Textures;
using VRage.Game;
using VRage.Game.ModAPI;
using VRageMath;

namespace SC.SUGMA.GameState
{
    public class FactionSphereZone : SphereZone
    {
        private IMyFaction _faction;
        public IMyFaction Faction
        {
            get
            {
                return _faction;
            }
            set
            {
                _faction = value;
                SphereDrawColor = (_faction?.CustomColor.ColorMaskToRgb() ?? Color.White).SetAlphaPct(0.25f);
                _originalColor = SphereDrawColor;
            }
        }

        public IMyFaction CapturingFaction;

        public float CaptureTime;
        public float CaptureTimeCurrent = 0;
        private Color _originalColor = Color.White.SetAlphaPct(0.25f);

        public FactionSphereZone(Vector3D center, double radius, float captureTime, IMyFaction faction = null) : base(center, radius)
        {
            Faction = faction;
            CaptureTime = captureTime;
        }

        public override void UpdateTick()
        {
            GridFilter = SUGMA_SessionComponent.I.ShareTrackApi.GetTrackedGrids();
            base.UpdateTick();


            int capturingFactionGrids = 0;

            bool zoneContested = false;
            foreach (var grid in ContainedGrids)
            {
                IMyFaction gridFaction = grid.GetFaction();

                if (CapturingFaction == null && gridFaction != Faction)
                {
                    CapturingFaction = gridFaction;
                    capturingFactionGrids = 1;
                    continue;
                }

                if (gridFaction == Faction || CapturingFaction != gridFaction)
                {
                    zoneContested = true;
                    break;
                }

                capturingFactionGrids++;
            }

            if (CapturingFaction == null || zoneContested)
            {
                CaptureTimeCurrent -= CaptureTime * 0.05f * 1/60f;
                if (CaptureTimeCurrent < 0)
                {
                    CaptureTimeCurrent = 0;
                    CapturingFaction = null;
                }
            }
            else
            {
                CaptureTimeCurrent += 1/60f * capturingFactionGrids;
                if (CaptureTimeCurrent >= CaptureTime)
                {
                    CaptureTimeCurrent = 0;
                    Faction = CapturingFaction;
                    CapturingFaction = null;
                }
            }

            SphereDrawColor = Color.Lerp(_originalColor, (CapturingFaction?.CustomColor.ColorMaskToRgb() ?? Color.White), CaptureTimeCurrent/CaptureTime).SetAlphaPct(0.25f);
        }
    }
}
