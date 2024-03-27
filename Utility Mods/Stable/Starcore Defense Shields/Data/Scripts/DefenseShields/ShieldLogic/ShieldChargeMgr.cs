using System;
using System.Collections.Generic;
using DefenseShields.Support;
using VRage.Utils;
using VRageMath;

namespace DefenseShields
{
    public partial class DefenseShields
    {
        internal class ShieldSideInfo
        {
            internal float Absorb;
            internal float Charge;
            internal float ChargeNorm;
            internal bool Online;
            internal uint NextOnline;
        }

        internal class ShieldChargeMgr
        {
            internal const float ConvToWatts = 0.01f;

            internal readonly RunningAverageCalculator NormalAverage = new RunningAverageCalculator(1800);

            internal readonly Dictionary<Session.ShieldSides, ShieldSideInfo> AbsorbSides = new Dictionary<Session.ShieldSides, ShieldSideInfo>();
            internal DefenseShields Controller;
            internal float Absorb;
            internal float AbsorbHeat;
            internal float ImpactSize = 9f;
            internal float EnergyDamage;
            internal float KineticDamage;
            internal float ModEnergyDamage;
            internal float ModKineticDamage;
            internal float RawEnergyDamage;
            internal float RawKineticDamage;
            internal float AverageNormDamage;
            internal uint LastDamageTick;
            internal uint LastDamageResetTick;

            internal HitType HitType;
            internal bool HitWave;
            internal bool WebDamage;
            internal Vector3D WorldImpactPosition = Vector3D.NegativeInfinity;

            public enum ChargeMode
            {
                Set,
                Charge,
                Discharge,
                Overload,
                Zero,
            }

            internal ShieldChargeMgr()
            {
                for (int i = 0; i < 6; i++)
                    AbsorbSides[(Session.ShieldSides) i] = new ShieldSideInfo();
            }

            internal void DoDamage(float damage, float impactSize, bool energy, Vector3D position, bool hitWave, bool webDamage, float heatScaler = 1, bool setRender = true)
            {
                Session.ShieldSides face;
                GetFace(position, out face);

                if (setRender) {
                    HitWave = hitWave;
                    WorldImpactPosition = position;
                    ImpactSize = impactSize;
                    HitType = energy ? HitType.Energy : HitType.Kinetic;
                }

                WebDamage = webDamage;
                Absorb += damage;
                AbsorbHeat += (damage * heatScaler);
                if (Session.Instance.Tick - LastDamageResetTick > 600)
                {
                    LastDamageResetTick = Session.Instance.Tick;
                    ModKineticDamage = 0;
                    RawKineticDamage = 0;
                    ModEnergyDamage = 0;
                    RawEnergyDamage = 0;
                }
                if (energy)
                {
                    EnergyDamage += damage;
                    ModEnergyDamage += damage;
                    RawEnergyDamage += (damage * Controller.DsState.State.ModulateKinetic);
                }
                else
                {
                    KineticDamage += damage;
                    ModKineticDamage += damage;
                    RawKineticDamage += (damage * Controller.DsState.State.ModulateEnergy);
                }

                var sideInfo = AbsorbSides[face];
                sideInfo.Absorb += damage;
            }

            internal void SetCharge(float amount, ChargeMode type)
            {

                if (type == ChargeMode.Overload)
                {
                    Controller.DsState.State.Charge = -(Controller.ShieldMaxCharge * 2);
                }
                else if (type == ChargeMode.Zero)
                {
                    Controller.DsState.State.Charge = 0;
                }
                else if (type == ChargeMode.Set)
                {
                    Controller.DsState.State.Charge = amount;
                }
                else if (type == ChargeMode.Charge)
                {
                    Controller.DsState.State.Charge += amount;
                }
                else
                {
                    Controller.DsState.State.Charge -= amount;
                    //ChargeSide(type);
                }
            }

            internal void ChargeSide(ChargeMode type)
            {
                switch (type)
                {
                    case ChargeMode.Set:
                        break;
                    case ChargeMode.Charge:
                        for (int i = 0; i < 6; i++)
                        {
                            var side = AbsorbSides[(Session.ShieldSides)i];
                            side.Charge += Controller.ShieldChargeRate;
                            var maxSideCharge = Controller.ShieldChargeBase / 6;
                            if (side.Charge > maxSideCharge)
                                side.Charge = maxSideCharge;
                        }
                        break;
                    case ChargeMode.Discharge:
                        for (int i = 0; i < 6; i++)
                        {
                            var side = AbsorbSides[(Session.ShieldSides)i];
                            side.Charge -= side.Absorb;
                        }
                        break;
                    case ChargeMode.Overload:
                        break;
                    case ChargeMode.Zero:
                        break;
                    default:
                        break;
                }
            }

            internal float SideHealthRatio(Session.ShieldSides shieldSide)
            {
                var maxHealth = Controller.ShieldChargeBase / 6;
                var side = AbsorbSides[shieldSide];
                var currentHealth = (float)MathHelperD.Clamp(side.Charge, 0, maxHealth);

                var ratioToFull = currentHealth / maxHealth;
                return ratioToFull;
            }

            private void GetFace(Vector3D pos, out Session.ShieldSides closestFaceHit)
            {
                var logic = Controller;
                var referenceLocalPosition = logic.MyGrid.PositionComp.LocalMatrixRef.Translation;
                var worldDirection = pos - referenceLocalPosition;
                var localPosition = Vector3D.TransformNormal(worldDirection, MatrixD.Transpose(logic.MyGrid.PositionComp.LocalMatrixRef));
                var impactTransNorm = localPosition - logic.ShieldShapeMatrix.Translation;

                var boxMax = logic.ShieldShapeMatrix.Backward + logic.ShieldShapeMatrix.Right + logic.ShieldShapeMatrix.Up;
                var boxMin = -boxMax;
                var box = new BoundingBoxD(boxMin, boxMax);

                var maxWidth = box.Max.LengthSquared();
                Vector3D norm;
                Vector3D.Normalize(ref impactTransNorm, out norm);
                var testLine = new LineD(Vector3D.Zero, norm * maxWidth); //This is to ensure we intersect the box
                LineD testIntersection;
                box.Intersect(ref testLine, out testIntersection);
                var intersection = testIntersection.To;
                var projForward = Vector3D.IsZero(logic.ShieldShapeMatrix.Forward) ? Vector3D.Zero : intersection.Dot(logic.ShieldShapeMatrix.Forward) / logic.ShieldShapeMatrix.Forward.LengthSquared() * logic.ShieldShapeMatrix.Forward;
                int closestFaceNum = -1;

                if (projForward.LengthSquared() >= 0.8 * logic.ShieldShapeMatrix.Forward.LengthSquared()) //if within the side thickness
                {
                    var dot = intersection.Dot(logic.ShieldShapeMatrix.Forward);
                    var face = dot > 0 ? Session.ShieldSides.Forward : Session.ShieldSides.Backward;
                    closestFaceNum = (int)face;
                }

                var projLeft = Vector3D.IsZero(logic.ShieldShapeMatrix.Left) ? Vector3D.Zero : intersection.Dot(logic.ShieldShapeMatrix.Left) / logic.ShieldShapeMatrix.Left.LengthSquared() * logic.ShieldShapeMatrix.Left;
                if (projLeft.LengthSquared() >= 0.8 * logic.ShieldShapeMatrix.Left.LengthSquared()) //if within the side thickness
                {
                    var dot = intersection.Dot(logic.ShieldShapeMatrix.Left);
                    var face = dot > 0 ? Session.ShieldSides.Left : Session.ShieldSides.Right;
                    var lengDiffSqr = projLeft.LengthSquared() - logic.ShieldShapeMatrix.Left.LengthSquared();
                    var validFace = closestFaceNum == -1 || !MyUtils.IsZero(lengDiffSqr);
                    if (validFace)
                        closestFaceNum = (int)face;
                }

                var projUp = Vector3D.IsZero(logic.ShieldShapeMatrix.Up) ? Vector3D.Zero : intersection.Dot(logic.ShieldShapeMatrix.Up) / logic.ShieldShapeMatrix.Up.LengthSquared() * logic.ShieldShapeMatrix.Up;
                if (projUp.LengthSquared() >= 0.8 * logic.ShieldShapeMatrix.Up.LengthSquared()) //if within the side thickness
                {
                    var dot = intersection.Dot(logic.ShieldShapeMatrix.Up);
                    var face = dot > 0 ? Session.ShieldSides.Up : Session.ShieldSides.Down;
                    var lengDiffSqr = projUp.LengthSquared() - logic.ShieldShapeMatrix.Up.LengthSquared();
                    var validFace = closestFaceNum == -1 || !MyUtils.IsZero(lengDiffSqr);

                    if (validFace)
                        closestFaceNum = (int)face;

                }

                if (closestFaceNum == -1)
                {
                    Log.Line($"no hit face wtf");
                    closestFaceNum = 0;
                }

                closestFaceHit = (Session.ShieldSides)closestFaceNum;
            }

            internal void ClearDamageTypeInfo()
            {
                NormalAverage.Clear();
                KineticDamage = 0;
                EnergyDamage = 0;
                RawKineticDamage = 0;
                RawEnergyDamage = 0;
                ModKineticDamage = 0;
                ModEnergyDamage = 0;
                AverageNormDamage = 0;
                Absorb = 0f;

            }
        }

    }
}
