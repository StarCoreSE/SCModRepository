using RichHudFramework;
using Sandbox.Game;
using Sandbox.Game.Entities;
using SC.SUGMA.Utilities;
using VRage.Game.ModAPI;
using VRageMath;

namespace SC.SUGMA.GameState
{
    public class FactionSphereZone : SphereZone
    {
        private IMyFaction _faction;
        private Color _originalColor = Color.White.SetAlphaPct(0.25f);

        public float CaptureTime;
        public float CaptureTimeCurrent;
        public MySoundPair CaptureSound = new MySoundPair("SUGMA_CaptureSound_TF2");

        public IMyFaction CapturingFaction;

        public FactionSphereZone(Vector3D center, double radius, float captureTime, IMyFaction faction = null) : base(
            center, radius)
        {
            Faction = faction;
            CaptureTime = captureTime;
        }

        public IMyFaction Faction
        {
            get { return _faction; }
            set
            {
                _faction = value;
                SphereDrawColor = (_faction?.CustomColor.ColorMaskToRgb() ?? Color.White).SetAlphaPct(0.25f);
                _originalColor = SphereDrawColor;
            }
        }

        public override void UpdateTick()
        {
            GridFilter = SUGMA_SessionComponent.I.ShareTrackApi.GetTrackedGrids();
            base.UpdateTick();

            var capturingFactionGrids = 0;

            var zoneContested = false;
            foreach (var grid in ContainedGrids)
            {
                var gridFaction = grid.GetFaction();

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

            if (CapturingFaction == null || zoneContested || ContainedGrids.Count == 0)
            {
                CaptureTimeCurrent -= 1 / 60f;
                if (CaptureTimeCurrent < 0)
                {
                    CaptureTimeCurrent = 0;
                    CapturingFaction = null;
                }
            }
            else
            {
                CaptureTimeCurrent += 1 / 60f * capturingFactionGrids;
                if (CaptureTimeCurrent >= CaptureTime)
                {
                    CaptureTimeCurrent = 0;
                    Faction = CapturingFaction;
                    CapturingFaction = null;
                    OnCapture();
                }
            }

            SphereDrawColor = Color.Lerp(_originalColor, CapturingFaction?.CustomColor.ColorMaskToRgb() ?? Color.White,
                CaptureTimeCurrent / CaptureTime).SetAlphaPct(0.25f);
        }

        public virtual void OnCapture()
        {
            SUtils.PlaySound(CaptureSound);
        }
    }
}