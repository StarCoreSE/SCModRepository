using RichHudFramework;
using Sandbox.Game;
using Sandbox.Game.Entities;
using SC.SUGMA.Utilities;
using VRage.Game.ModAPI;
using VRageMath;
using System;
using System.Linq;
using System.Collections.Generic;

namespace SC.SUGMA.GameState
{
    public class KOTHSphereZone : SphereZone
    {
        public IMyFaction _zoneOwner;
        private Color _baseColor = Color.White.SetAlphaPct(0.25f);

        public bool IsCaptured = false;
        public float CaptureTime;
        public float CaptureTimeCurrent;
        public MySoundPair CaptureSound = new MySoundPair("SUGMA_CaptureSound_TF2");

        public IMyFaction ActiveCapturingFaction;

        public KOTHSphereZone(Vector3D center, double radius, float captureTime, IMyFaction initialOwner = null)
            : base(center, radius)
        {
            _zoneOwner = initialOwner;
            CaptureTime = captureTime;

            SphereDrawColor = (_zoneOwner?.CustomColor.ColorMaskToRgb() ?? Color.White).SetAlphaPct(0.25f);
            _baseColor = SphereDrawColor;
        }

        public override void UpdateTick()
        {
            GridFilter = SUGMA_SessionComponent.I.ShareTrackApi.GetTrackedGrids();
            base.UpdateTick();

            var distinctFactions = new HashSet<IMyFaction>();
            foreach (var grid in ContainedGrids)
            {
                var faction = grid.GetFaction();
                if (faction != null)
                    distinctFactions.Add(faction);
            }

            if (distinctFactions.Count == 0)
            {
                CaptureTimeCurrent = MathHelper.Max(0f, CaptureTimeCurrent - (1f / 120f));
                if (CaptureTimeCurrent <= 0f)
                    ActiveCapturingFaction = null;
            }
            else if (distinctFactions.Count > 1)
            {
                CaptureTimeCurrent = MathHelper.Max(0f, CaptureTimeCurrent - (1f / 120f));
                if (CaptureTimeCurrent <= 0f)
                    ActiveCapturingFaction = null;
            }
            else
            {
                var occupant = distinctFactions.First();

                if (occupant == _zoneOwner)
                {
                    CaptureTimeCurrent = 0f;
                    ActiveCapturingFaction = null;
                }
                else
                {
                    if (ActiveCapturingFaction == null)
                    {
                        ActiveCapturingFaction = occupant;
                    }
                    else if (ActiveCapturingFaction != occupant)
                    {
                        CaptureTimeCurrent = MathHelper.Max(0f, CaptureTimeCurrent - (1f / 60f));

                        if (CaptureTimeCurrent <= 0f)
                        {
                            ActiveCapturingFaction = occupant;
                            CaptureTimeCurrent = 0f;
                        }
                    }

                    if (ActiveCapturingFaction == occupant)
                    {
                        CaptureTimeCurrent += (1f / 60f);

                        if (CaptureTimeCurrent >= CaptureTime)
                        {
                            _zoneOwner = ActiveCapturingFaction;
                            CaptureTimeCurrent = 0f;
                            ActiveCapturingFaction = null;
                            OnCapture();
                        }
                    }                    
                }
            }

            float lerpAmount = (CaptureTime <= 0f ? 0f : CaptureTimeCurrent / CaptureTime);
            Color capturingColor = ActiveCapturingFaction?.CustomColor.ColorMaskToRgb() ?? Color.White;
            SphereDrawColor = Color.Lerp(_baseColor, capturingColor, lerpAmount).SetAlphaPct(0.25f);
        }

        public virtual void OnCapture()
        {
            IsCaptured = true;
            SUtils.PlaySound(CaptureSound);          
        }
    }
}
