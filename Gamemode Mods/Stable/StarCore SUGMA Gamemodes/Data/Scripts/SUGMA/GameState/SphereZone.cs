using System.Collections.Generic;
using Sandbox.Game.Entities;
using Sandbox.ModAPI;
using VRage.Game;
using VRage.Game.Entity;
using VRage.Game.ModAPI;
using VRageMath;

namespace SC.SUGMA.GameState
{
    public class SphereZone : ComponentBase
    {
        private readonly List<MyEntity> _containedEntities = new List<MyEntity>();

        public bool
            CheckOutside = false,
            IsVisible = true;

        public List<IMyCubeGrid>
            ContainedGrids = new List<IMyCubeGrid>(),
            OutsideGrids = new List<IMyCubeGrid>();

        /// <summary>
        ///     Assign this to limit the grid set. Leave null to check all grids.
        /// </summary>
        public ICollection<IMyCubeGrid> GridFilter = null;

        // display sphere on player hud
        // detects if any griven grid is whithn sphnere
        // has callbacks if grid is in or outside of sphere (idiot read: actions)
        // action that gets called every tick grid is in sphere
        // OR!!! every tick grid is outside the sphere
        // e.g. invisible bounce zone around the arena that bounces ships back into the arena in opposite directions
        public BoundingSphereD Sphere;

        public Color SphereDrawColor = Color.White;

        public SphereZone(Vector3D center, double radius)
        {
            Sphere = new BoundingSphereD(center, radius);
        }

        public SphereZone(BoundingSphereD sphere)
        {
            Sphere = sphere;
        }

        public override void Close()
        {
        }

        public override void UpdateTick()
        {
            _containedEntities.Clear();
            OutsideGrids.Clear();
            ContainedGrids.Clear();

            // Filtering
            if (GridFilter == null)
            {
                MyGamePruningStructure.GetAllTopMostEntitiesInSphere(ref Sphere, _containedEntities);

                for (var i = _containedEntities.Count - 1; i >= 0; i--)
                    if (_containedEntities[i] is IMyCubeGrid)
                        ContainedGrids.Add(_containedEntities[i] as IMyCubeGrid);

                if (CheckOutside)
                    MyAPIGateway.Entities.GetEntities(null, b =>
                    {
                        var grid = b as IMyCubeGrid;
                        if (grid != null && !_containedEntities.Contains((MyEntity)grid)) OutsideGrids.Add(grid);
                        return false;
                    });
            }
            else
            {
                var radiusSquare = Sphere.Radius * Sphere.Radius;
                foreach (var grid in GridFilter)
                    if (Vector3D.DistanceSquared(grid.GetPosition(), Sphere.Center) <= radiusSquare)
                        ContainedGrids.Add(grid);
                    else if (CheckOutside) OutsideGrids.Add(grid);
            }

            if (MyAPIGateway.Utilities.IsDedicated)
                return;
            // Visuals
            var stupidMatrix = MatrixD.CreateWorld(Sphere.Center, Vector3D.Up, Vector3D.Forward);
            if (IsVisible)
                MySimpleObjectDraw.DrawTransparentSphere(ref stupidMatrix, (float)Sphere.Radius, ref SphereDrawColor,
                    MySimpleObjectRasterizer.Solid, 20);
        }
    }
}