using RichHudFramework;
using Sandbox.Game.Entities;
using Sandbox.ModAPI;
using System.Collections.Generic;
using VRage.Game;
using VRage.Game.Entity;
using VRage.Game.ModAPI;
using VRage.ModAPI;
using VRage.Scripting;
using VRageMath;

namespace SC.SUGMA.GameModes.TeamDeathMatch_Zones
{
    public class SphereZone : ComponentBase
    {
        // display sphere on player hud
        // detects if any griven grid is whithn sphnere
        // has callbacks if grid is in or outside of sphere (idiot read: actions)
        // action that gets called every tick grid is in sphere
        // OR!!! every tick grid is outside the sphere
        // e.g. invisible bounce zone around the arena that bounces ships back into the arena in opposite directions
        public BoundingSphereD Sphere;

        public Color SphereDrawColor = Color.White;
        public bool CheckOutside, IsVisible = false;
        public List<MyEntity> ContainedEntities = new List<MyEntity>();
        public List<IMyCubeGrid> 
            ContainedGrids = new List<IMyCubeGrid>(),
            OutsideGrids = new List<IMyCubeGrid>();
        public SphereZone(Vector3D center, double radius, bool Outside = false)
        {
            Sphere = new BoundingSphereD(center, radius);
            CheckOutside = Outside;
        }
        public override void Init(string id)
        {
            base.Init(id);
            
        }
        public override void Close()
        {
        }

        public override void UpdateTick()
        {
            ContainedEntities.Clear();
            OutsideGrids.Clear();
            ContainedGrids.Clear();

            MyGamePruningStructure.GetAllTopMostEntitiesInSphere(ref Sphere, ContainedEntities);
            for (int i = ContainedEntities.Count - 1; i >= 0; i--)
                if (ContainedEntities[i] is IMyCubeGrid)
                {
                    ContainedGrids.Add(ContainedEntities[i] as IMyCubeGrid);
                }
            if (CheckOutside)
                MyAPIGateway.Entities.GetEntities(null, b =>
                {
                    if (b is IMyCubeGrid && !ContainedEntities.Contains((MyEntity)b))
                    {
                        OutsideGrids.Add((IMyCubeGrid)b);
                    }
                    return false;
                });
            var stupidMatrix = MatrixD.CreateWorld(Sphere.Center);
            if (IsVisible)
            {
                MySimpleObjectDraw.DrawTransparentSphere(ref stupidMatrix, (float)Sphere.Radius, ref SphereDrawColor, MySimpleObjectRasterizer.Solid, 100);
            }
        }
    }
}