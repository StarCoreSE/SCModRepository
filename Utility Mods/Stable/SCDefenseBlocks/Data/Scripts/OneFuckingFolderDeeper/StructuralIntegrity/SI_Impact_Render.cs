using System;
using VRage.Game.ModAPI;
using Sandbox.Game.Entities;
using VRage.ModAPI;
using Sandbox.ModAPI;
using VRageMath;
using VRage.Game;
using VRage.Game.Entity;
using Sandbox.Definitions;
using Sandbox.ModAPI.Interfaces.Terminal;
using VRage.Utils;

namespace StarCore.StructuralIntegrity
{

	public class SI_Impact_Render
	{
		public int m_timeToLive = 16;

		public readonly SI_Core Core = new SI_Core();

		Color impact_Color = Color.White;

		IMySlimBlock m_block;

		MatrixD impact_Matrix;
		Vector3D impact_Scale;

		float m_shieldStatus;

		MyEntity3DSoundEmitter m_soundEmitter;

		public SI_Impact_Render (IMySlimBlock block)
		{
			m_block = block;

			MyCubeBlockDefinition blockDefinition = block.BlockDefinition as MyCubeBlockDefinition;

			impact_Scale.X = blockDefinition.Size.X + 0.1;
			impact_Scale.Y = blockDefinition.Size.Y + 0.1;
			impact_Scale.Z = blockDefinition.Size.Z + 0.1;

			Vector3D impact_Position;

			if(block.FatBlock == null) 
			{
				impact_Position = block.CubeGrid.GridIntegerToWorld(block.Position);

				impact_Matrix = MatrixD.CreateFromTransformScale(Quaternion.CreateFromRotationMatrix(block.CubeGrid.WorldMatrix.GetOrientation()), impact_Position, impact_Scale);

			}
			else
			{
				impact_Position = block.FatBlock.WorldMatrix.Translation;

				impact_Matrix = MatrixD.CreateFromTransformScale(Quaternion.CreateFromRotationMatrix(block.FatBlock.WorldMatrix.GetOrientation()), impact_Position, impact_Scale);


			}	
		}

		public void update()
		{
			m_timeToLive--;

			if (!m_block.CubeGrid.Closed)
			{			
				Color impact_Color = Color.Red;
				MyStringId impact_Material = MyStringId.GetOrCompute("particle_laser");

				float ttlPercent = m_timeToLive / 8f;

				if ((ttlPercent < 0.4) || (ttlPercent > 0.7))
				{

					BoundingBoxD renderBox = new BoundingBoxD(new Vector3D(-1.25d), new Vector3D(1.25d));


					MySimpleObjectDraw.DrawTransparentBox(ref impact_Matrix, ref renderBox, ref impact_Color, MySimpleObjectRasterizer.Solid, 0, 1f, impact_Material, null, true);

				}
			}
			else
			{
				m_timeToLive = 0;
			}
		}

		public void close()
		{
			
		}

		private IMyEntity generateShieldEffect(string name)
		{
			
			SI_Core.impactEffectObjectBuilder.CubeBlocks[0].SubtypeName = name;

			MyAPIGateway.Entities.RemapObjectBuilder(SI_Core.impactEffectObjectBuilder);
			var ent = MyAPIGateway.Entities.CreateFromObjectBuilder(SI_Core.impactEffectObjectBuilder);
			ent.Flags &= ~EntityFlags.Sync;
			ent.Flags &= ~EntityFlags.Save;

			MyAPIGateway.Entities.AddEntity(ent, true);
			return ent;
		}


	}
}

