using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using DefenseShields.Support;
using Sandbox.Game.Entities;
using VRage.Game;
using VRage.Game.Entity;
using VRage.Game.ModAPI;
using VRage.Utils;
using VRageMath;
using static VRage.Game.MyObjectBuilder_BehaviorTreeDecoratorNode;

namespace DefenseShields
{
    public partial class Session
    {
        #region DamageHandler
        private readonly long[] _nodes = new long[1000];
        private readonly Dictionary<long, MyEntity> _backingDict = new Dictionary<long, MyEntity>(1001);
        private readonly object _entLock = new object();
        private int _emptySpot;
        private MyEntity _previousEnt;
        private long _previousEntId = -1;

        public void CheckDamage(object target, ref MyDamageInformation info)
        {
            try
            {
                var damageType = info.Type;
                if (damageType == MpIgnoreDamage || damageType == MyDamageType.Drill || damageType == MyDamageType.Grind || ManagedAttackers.ContainsKey(info.AttackerId)) return;
                var block = target as IMySlimBlock;
                if (block != null)
                {
                    var myGrid = block.CubeGrid as MyCubeGrid;
                    if (myGrid == null) return;

                    MyProtectors protectors;
                    GlobalProtect.TryGetValue(myGrid, out protectors);
                    if (protectors == null) return;

                    MyEntity hostileEnt;
                    var attackerId = info.AttackerId;
                    if (attackerId == _previousEntId) hostileEnt = _previousEnt;
                    else UpdatedHostileEnt(attackerId, out hostileEnt);

                    if (!IsServer && attackerId != 0 && hostileEnt == null) ForceEntity(out hostileEnt);

                    MyEntity trueAttacker = null;
                    var isVoxelBase = false;

                    try
                    {
                        if (hostileEnt != null)
                        {
                            MyCubeGrid grid;
                            if (damageType != MyDamageType.Environment) grid = hostileEnt as MyCubeGrid;
                            else grid = hostileEnt.Parent as MyCubeGrid;
                            if (grid == null)
                            {
                                var hostileCube = hostileEnt.Parent as MyCubeBlock;
                                trueAttacker = (hostileCube ?? (hostileEnt as IMyGunBaseUser)?.Owner) ?? hostileEnt;
                                if (trueAttacker is MyVoxelBase) isVoxelBase = true;
                            }
                            else trueAttacker = grid;
                            protectors.LastAttackerWasInside = true;
                            Vector3D originHit;
                            block.ComputeWorldCenter(out originHit);

                            var line = new LineD(trueAttacker.PositionComp.WorldAABB.Center, originHit);
                            var lineLength = (float)line.Length;
                            var testDir = Vector3D.Normalize(line.From - line.To);
                            var ray = new RayD(line.From, -testDir);
                            var hitDist = double.MaxValue;
                            foreach (var shield in protectors.Shields.Keys)
                            {
                                shield.Asleep = false;
                                shield.LastWokenTick = Tick;

                                var shieldActive = shield.DsState.State.Online && !shield.DsState.State.Lowered;
                                if (!shieldActive) continue;
                                var intersectDist = CustomCollision.IntersectEllipsoid(ref shield.DetectMatrixOutsideInv, shield.DetectionMatrix, ref ray);
                                var ellipsoid = intersectDist ?? 0;

                                var notContained = isVoxelBase || intersectDist <= 0 && shield.GridIsMobile && !CustomCollision.PointInShield(trueAttacker.PositionComp.WorldAABB.Center, MatrixD.Invert(shield.ShieldShapeMatrix * shield.MyGrid.WorldMatrix));
                                if (notContained) ellipsoid = lineLength;

                                var intersect = ellipsoid > 0 && lineLength + 1 >= ellipsoid;

                                if (intersect && ellipsoid <= hitDist)
                                {
                                    protectors.LastAttackerWasInside = false;
                                    hitDist = ellipsoid;
                                    protectors.BlockingShield = shield;
                                    protectors.BlockingTick = Tick;
                                }
                            }
                        }
                        if (Tick - protectors.BlockingTick > 10 && protectors.LastAttackerWasInside) protectors.BlockingShield = null;
                    }
                    catch (Exception ex) { Log.Line($"Exception in DamageFindShield: {ex}"); }

                    try
                    {
                        var activeProtector = protectors.BlockingShield != null && protectors.BlockingShield.DsState.State.Online && !protectors.BlockingShield.DsState.State.Lowered;
                        if (activeProtector)
                        {
                            var shield = protectors.BlockingShield;
                            if (!IsServer && !shield.WarmedUp)
                            {
                                info.Amount = 0;
                                return;
                            }
                            var isExplosionDmg = damageType == MyDamageType.Explosion;
                            var isDeformationDmg = damageType == MyDamageType.Deformation;

                            if (isVoxelBase)
                            {
                                shield.DeformEnabled = true;
                                return;
                            }
                            if (damageType == Bypass)
                            {
                                shield.DeformEnabled = true;
                                return;
                            }

                            if (!isDeformationDmg && !isExplosionDmg)
                            {
                                shield.DeformEnabled = false;
                                protectors.IgnoreAttackerId = -1;
                            }
                            else if (shield.DeformEnabled && trueAttacker == null)
                            {
                                return;
                            }
                            else if (!shield.DeformEnabled && trueAttacker == null)
                            {
                                info.Amount = 0;
                                return;
                            }

                            var bullet = damageType == MyDamageType.Bullet;
                            if (bullet || isDeformationDmg) info.Amount *= shield.DsState.State.ModulateEnergy;
                            else info.Amount *= shield.DsState.State.ModulateKinetic;

                            var noHits = !DedicatedServer && shield.ChargeMgr.Absorb < 1;
                            var hitSlotAvailable = noHits & (bullet && shield.KineticCoolDown == -1) || (!bullet && shield.EnergyCoolDown == -1);
                            if (hitSlotAvailable)
                            {
                                lock (shield.HandlerImpact)
                                {
                                    if (trueAttacker != null && block != null)
                                    {
                                        shield.HandlerImpact.Attacker = trueAttacker;
                                        shield.HandlerImpact.HitBlock = block;
                                        shield.ChargeMgr.ImpactSize = info.Amount;
                                        shield.HandlerImpact.Active = true;
                                        if (!bullet) shield.ChargeMgr.HitType = DefenseShields.HitType.Energy;
                                    }
                                }
                            }
                            if (isDeformationDmg && trueAttacker != null) protectors.IgnoreAttackerId = attackerId;


                            if (isDeformationDmg)
                            {
                                shield.ChargeMgr.KineticDamage += info.Amount;
                                shield.ChargeMgr.Absorb += info.Amount;
                                shield.ChargeMgr.AbsorbHeat += info.Amount;
                            }

                            info.Amount = 0f;
                            return;
                        }
                    }
                    catch (Exception ex) { Log.Line($"Exception in DamageHandlerActive: {ex}"); }

                    var iShield = protectors.IntegrityShield;
                    if (iShield != null && iShield.DsState.State.Online && !iShield.DsState.State.Lowered)
                    {
                        var attackingVoxel = trueAttacker as MyVoxelBase;
                        if (attackingVoxel != null || (trueAttacker is MyCubeGrid) && !(damageType == MPEnergy || damageType == MPKinetic || damageType == MPExplosion)) 
                            iShield.DeformEnabled = true;
                        else if (trueAttacker != null) iShield.DeformEnabled = false;

                        if (damageType == MyDamageType.Deformation && iShield.DeformEnabled)
                        {
                            if (attackingVoxel != null)
                            {
                                if (iShield.ChargeMgr.Absorb < 1 && iShield.ChargeMgr.WorldImpactPosition == Vector3D.NegativeInfinity && iShield.KineticCoolDown == -1)
                                {
                                    attackingVoxel.RootVoxel.RequestVoxelOperationElipsoid(Vector3.One, iShield.DetectMatrixOutside, 0, MyVoxelBase.OperationType.Cut);
                                }
                            }
                            var dmgAmount = info.Amount;
                            if (IsServer)
                            {
                                iShield.AddShieldHit(attackerId, dmgAmount, damageType, block, false);
                                iShield.ChargeMgr.Absorb += dmgAmount;
                                iShield.ChargeMgr.KineticDamage += dmgAmount;
                                iShield.ChargeMgr.AbsorbHeat += dmgAmount;
                            }
                            info.Amount = 0;
                            return;
                        }
                    }

                    if (info.AttackerId == protectors.IgnoreAttackerId && damageType == MyDamageType.Deformation)
                    {
                        if (Enforced.Debug >= 2) Log.Line($"old Del/Mp Attacker, ignoring: {damageType} - {info.Amount} - attackerId:{attackerId}");
                        info.Amount = 0;
                        return;
                    }
                    protectors.IgnoreAttackerId = -1;
                    if (Enforced.Debug >= 2) Log.Line($"[Uncaught Damage] Type:{damageType} - Amount:{info.Amount} - nullTrue:{trueAttacker == null} - nullHostile:{hostileEnt == null} - nullShield:{protectors.BlockingShield == null} - iShell:{protectors.IntegrityShield != null} - protectorShields:{protectors.Shields.Count} - attackerId:{info.AttackerId}");
                }
            }
            catch (Exception ex) { Log.Line($"Exception in SessionDamageHandler {_previousEnt == null}: {ex}"); }
        }

        private void UpdatedHostileEnt(long attackerId, out MyEntity ent)
        {
            lock (_entLock)
            {
                if (attackerId == 0)
                {
                    ent = null;
                    return;
                }
                MyEntity tmpPreviousEnt;
                if (_backingDict.TryGetValue(attackerId, out tmpPreviousEnt))
                {
                    if (!tmpPreviousEnt.MarkedForClose)
                    {
                        _previousEnt = tmpPreviousEnt;
                        _previousEntId = attackerId;
                        ent = tmpPreviousEnt;
                        return;
                    }
                    _backingDict.Remove(attackerId);
                }
                if (MyEntities.TryGetEntityById(attackerId, out _previousEnt))
                {
                    if (_emptySpot + 1 >= _nodes.Length) _backingDict.Remove(_nodes[0]);
                    _nodes[_emptySpot] = attackerId;
                    _backingDict.Add(attackerId, _previousEnt);

                    if (_emptySpot++ >= _nodes.Length) _emptySpot = 0;

                    _previousEntId = attackerId;
                    ent = _previousEnt;
                    return;
                }
                ent = null;
                _previousEntId = -1;
            }
        }

        private static void ForceEntity(out MyEntity hostileEnt)
        {
            hostileEnt = MyAPIGateway.Session.ControlledObject?.Entity as MyEntity;
            if (hostileEnt?.Parent != null) hostileEnt = hostileEnt.Parent;
            if (hostileEnt == null) hostileEnt = MyAPIGateway.Session.Player.Character as MyEntity;
        }
        #endregion
    }
}
