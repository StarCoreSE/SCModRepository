using Sandbox.ModAPI;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.Entity;
using VRage.Game.ModAPI;
using VRage.ModAPI;
using VRage.ObjectBuilders;
using VRageMath;

namespace Epstein_Fusion_DS.HeatParts.ExtendableRadiators
{
    [MyEntityComponentDescriptor(typeof(MyObjectBuilder_TerminalBlock), false, "ActiveRadiator")]
    internal class ActiveRadiatorAnimation : MyGameLogicComponent
    {
        private IMyCubeBlock Block;
        private MyEntitySubpart FanPart;
        private MyParticleEffect Particle;

        public override void Init(MyObjectBuilder_EntityBase objectBuilder)
        {
            base.Init(objectBuilder);

            Block = (IMyCubeBlock)Entity;

            NeedsUpdate |= MyEntityUpdateEnum.BEFORE_NEXT_FRAME;
        }

        public override void UpdateOnceBeforeFrame()
        {
            base.UpdateOnceBeforeFrame();

            if (Block?.CubeGrid?.Physics == null || MyAPIGateway.Utilities.IsDedicated)
                return;

            FanPart = Block.GetSubpart("blades");

            MyParticlesManager.TryCreateParticleEffect("ActiveRadiatorParticle", ref MatrixD.Identity, ref Vector3D.Zero, Block.Render.GetRenderObjectID(), out Particle);

            NeedsUpdate |= MyEntityUpdateEnum.EACH_FRAME;
        }

        public override void UpdateAfterSimulation()
        {
            float heatLevel = HeatManager.I.GetGridHeatLevel(Block.CubeGrid);

            Matrix refMatrix = MatrixD.CreateFromAxisAngle(Vector3D.Up, -0.1 * heatLevel) * FanPart.PositionComp.LocalMatrixRef;
            refMatrix.Translation = FanPart.PositionComp.LocalMatrixRef.Translation;
            FanPart.PositionComp.SetLocalMatrix(ref refMatrix);

            if (heatLevel < 0.15)
                heatLevel = 0;

            Particle.UserBirthMultiplier = heatLevel * heatLevel;
            Particle.UserVelocityMultiplier = heatLevel * heatLevel;
        }

        public override void Close()
        {
            Particle?.Close();
        }
    }
}
