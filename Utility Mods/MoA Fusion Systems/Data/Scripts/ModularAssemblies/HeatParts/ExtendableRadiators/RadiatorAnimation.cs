using System.Collections.Generic;
using System.Linq;
using Sandbox.Game.Entities;
using VRage.Game.Entity;
using VRage.ModAPI;
using VRageMath;

namespace Epstein_Fusion_DS.HeatParts.ExtendableRadiators
{
    internal class RadiatorAnimation
    {
        public readonly ExtendableRadiator Radiator;
        public bool IsActive = false;
        public bool IsExtending = false;

        private HashSet<AnimationPanel> _animationEntities = new HashSet<AnimationPanel>();

        public RadiatorAnimation(ExtendableRadiator radiator)
        {
            Radiator = radiator;
        }

        public void StartExtension()
        {
            IsActive = true;
            IsExtending = true;
        }

        public void StartRetraction()
        {
            IsActive = true;
            IsExtending = false;
        }

        private int _animationTick = 0;
        public void UpdateTick()
        {
            if (!IsActive)
                return;

            _animationTick++;

            if (IsExtending)
            {
                // Extension animation

                if (_animationTick == 1)
                {
                    MyEntity parentEntity = (MyEntity) Radiator.Block;
                    Matrix localMatrixOffset = Matrix.Invert(Radiator.Block.LocalMatrix);

                    for (int i = 0; i < Radiator.StoredRadiators.Length; i++)
                    {
                        _animationEntities.Add(new AnimationPanel(Radiator.StoredRadiators[i].Model, Radiator.StoredRadiators[i].LocalMatrix * localMatrixOffset, parentEntity));
                        parentEntity = _animationEntities.Last();
                        localMatrixOffset = Matrix.Invert(Radiator.StoredRadiators[i].LocalMatrix);
                    }

                    int idx = 0;
                    foreach (var entity in _animationEntities)
                    {
                        if (idx == 0)
                        {
                            entity.RotateAroundLocalAxis(1.1781);
                            entity.MoveLocalSpace(entity.RightVector * -0.75f);
                        }
                        else
                        {
                            entity.RotateAroundLocalAxis(1.1781*2);
                        }

                        idx++;
                    }
                }

                if (_animationTick <= 120)
                {
                    int idx = 0;
                    foreach (var entity in _animationEntities)
                    {
                        if (idx == 0)
                        {
                            entity.RotateAroundLocalAxis(-1.1781/120);
                            entity.MoveLocalSpace(entity.RightVector * 0.75f/120);
                        }
                        else
                        {
                            entity.RotateAroundLocalAxis(-1.1781/120*2);
                        }

                        idx++;
                    }

                    return;
                }

                Reset();
                Radiator.MakePanelsVisible();
            }
            else
            {
                // Retraction animation

                if (_animationTick == 1)
                {
                    MyEntity parentEntity = (MyEntity) Radiator.Block;
                    Matrix localMatrixOffset = Matrix.Invert(Radiator.Block.LocalMatrix);

                    for (int i = 0; i < Radiator.StoredRadiators.Length; i++)
                    {
                        _animationEntities.Add(new AnimationPanel(Radiator.StoredRadiators[i].Model, Radiator.StoredRadiators[i].LocalMatrix * localMatrixOffset, parentEntity));
                        parentEntity = _animationEntities.Last();
                        localMatrixOffset = Matrix.Invert(Radiator.StoredRadiators[i].LocalMatrix);
                    }
                }

                if (_animationTick <= 120)
                {
                    int idx = 0;
                    foreach (var entity in _animationEntities)
                    {
                        if (idx == 0)
                        {
                            entity.RotateAroundLocalAxis(1.1781/120);
                            entity.MoveLocalSpace(entity.RightVector * -0.75f/120);
                        }
                        else
                        {
                            entity.RotateAroundLocalAxis(1.1781/120*2);
                        }

                        idx++;
                    }

                    return;
                }

                Reset();
                return;
            }
        }

        private void Reset()
        {
            _animationTick = 0;
            IsActive = false;

            foreach (var entity in _animationEntities)
                entity.Close();
            _animationEntities.Clear();
        }

        private sealed class AnimationPanel : MyEntity
        {
            /// <summary>
            /// As the block rotates, its orientation changes. RightVector is used to translate the block relative to itself after rotation.
            /// </summary>
            public readonly Vector3D RightVector;
            private Vector3D _rotationPoint = new Vector3D(-0.5, 1.25, 0);
            private readonly bool _isUpsideDown;

            public AnimationPanel(string model, Matrix localMatrix, MyEntity parent)
            {
                Init(null, model, parent, 1);
                if (string.IsNullOrEmpty(model))
                    Flags &= ~EntityFlags.Visible;
                Save = false;
                NeedsWorldMatrix = true;

                PositionComp.SetLocalMatrix(ref localMatrix);
                MyEntities.Add(this);

                RightVector = PositionComp.LocalMatrixRef.Right;

                _isUpsideDown = PositionComp.LocalMatrixRef.Up.Y * PositionComp.LocalMatrixRef.Translation.Y > 0;
                if (_isUpsideDown)
                    _rotationPoint *= -1;
            }

            public void RotateAroundLocalAxis(double amount)
            {
                Matrix newMatrix = Utils.RotateMatrixAroundPoint(PositionComp.LocalMatrixRef,
                    Vector3D.Transform(_rotationPoint, PositionComp.LocalMatrixRef), PositionComp.LocalMatrixRef.Backward, amount);
                PositionComp.SetLocalMatrix(ref newMatrix);
            }

            public void MoveLocalSpace(Vector3 amount)
            {
                if (_isUpsideDown)
                    amount *= -1;

                Matrix newMatrix = PositionComp.LocalMatrixRef;
                newMatrix.Translation += amount;
                PositionComp.SetLocalMatrix(ref newMatrix);
            }
        }
    }
}
