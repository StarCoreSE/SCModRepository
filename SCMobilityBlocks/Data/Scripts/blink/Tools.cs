using Sandbox.Game.Entities;
using Sandbox.ModAPI;
using Sandbox.ModAPI.Interfaces.Terminal;
using System;
using System.Collections.Generic;
using System.Text;
using VRage.Game.ModAPI;
using VRage.ModAPI;
using VRage.Utils;
using VRageMath;

namespace BlinkDrive
{
	public struct SearchResults
	{
		public bool HasEntity;
		public bool HasGrid;
		public bool HasVoxel;
		public bool HasPlanet;
	}

	public static class Tools
	{
		public static SearchResults GetThingsAtLocation(Vector3D location, Vector3 gridLocalAABBCenter)
		{
			SearchResults results = new SearchResults();


			BoundingBoxD bounds = new BoundingBoxD(location - gridLocalAABBCenter, location + gridLocalAABBCenter);
			List<IMyEntity> entities = MyAPIGateway.Entities.GetEntitiesInAABB(ref bounds);

			results.HasEntity = entities.Count > 0;

			foreach (IMyEntity entity in entities)
			{
				if (entity is IMyCubeGrid)
				{
					results.HasGrid = true;
				}
				else if (entity is MyPlanet)
				{
					results.HasPlanet = true;
				}
				else if (entity is IMyVoxelMap)
				{
					results.HasVoxel = true;
				}
			}

			return results;
		}

		public static float MWToMWh(float MW)
		{
			return MW / 216000f;
		}

		public static float MWhToMW(float MWh)
		{
			return MWh * 216000f;
		}

		public static float WhToMWh(double watts)
		{
			return (float)(watts / 1000000d);
		}
	}
}
