using Epstein_Fusion_DS.Communication;
using System.Collections.Generic;
using System.Linq;
using Epstein_Fusion_DS.HudHelpers;
using Sandbox.ModAPI;
using VRage.Game.ModAPI;
using VRageMath;

namespace Epstein_Fusion_DS.HeatParts.Definitions
{
    internal static class HeatPartDefinitions
    {
        private static ModularDefinitionApi ModularApi => Epstein_Fusion_DS.ModularDefinition.ModularApi;

        public static HeatPartDefinition GetDefinition(string subtypeId) =>
            Definitions.FirstOrDefault(d => d.SubtypeId == subtypeId);

        public static bool HasDefinition(string subtypeId) =>
            Definitions.Any(d => d.SubtypeId == subtypeId);

        /// <summary>
        /// Internal buffer list for cell casting
        /// </summary>
        private static readonly List<Vector3I> _cellPositions = new List<Vector3I>();

        /// <summary>
        /// Array of all heat part definitions
        /// </summary>
        public static readonly HeatPartDefinition[] Definitions =
        {
            new HeatPartDefinition
            {
                SubtypeId = "Heat_Heatsink",
                HeatCapacity = 60,
                HeatDissipation = 0,
                LoSCheck = null
            },
            new HeatPartDefinition
            {
                SubtypeId = "Heat_FlatRadiator",
                HeatCapacity = 0,
                HeatDissipation = 5,
                LoSCheck = radiatorBlock => CheckGridIntersect(radiatorBlock, Vector3I.Zero, Vector3I.Backward) ? 0 : 1
            },
            new HeatPartDefinition
            {
                SubtypeId = "MDA_Radiator_1x2",
                HeatCapacity = 0,
                HeatDissipation = 10,
                LoSCheck = radiatorBlock =>
                {
                    float occlusionModifier = 0;

                    if (!CheckGridIntersect(radiatorBlock, Vector3I.Up, Vector3I.Forward))
                        occlusionModifier += 1 / 6f;
                    if (!CheckGridIntersect(radiatorBlock, Vector3I.Up, Vector3I.Backward))
                        occlusionModifier += 1 / 6f;

                    if (!CheckGridIntersect(radiatorBlock, Vector3I.Zero, Vector3I.Forward))
                        occlusionModifier += 1 / 6f;
                    if (!CheckGridIntersect(radiatorBlock, Vector3I.Zero, Vector3I.Backward))
                        occlusionModifier += 1 / 6f;

                    if (!CheckGridIntersect(radiatorBlock, Vector3I.Down, Vector3I.Forward))
                        occlusionModifier += 1 / 6f;
                    if (!CheckGridIntersect(radiatorBlock, Vector3I.Down, Vector3I.Backward))
                        occlusionModifier += 1 / 6f;

                    return occlusionModifier;
                }
            },
            new HeatPartDefinition
            {
                SubtypeId = "RadiatorPanel",
                HeatCapacity = 0,
                HeatDissipation = 10f,
                LoSCheck = radiatorBlock =>
                {
                    float occlusionModifier = 0;

                    if (!CheckGridIntersect(radiatorBlock, Vector3I.Zero, Vector3I.Right))
                        occlusionModifier += 1 / 2f;
                    if (!CheckGridIntersect(radiatorBlock, Vector3I.Zero, Vector3I.Left))
                        occlusionModifier += 1 / 2f;

                    return occlusionModifier;
                }
            },
            new HeatPartDefinition
            {
                SubtypeId = "ActiveRadiator",
                HeatCapacity = 60,
                HeatDissipation = 150,
                LoSCheck = radiatorBlock =>
                {
                    float occlusionModifier = 0;

                    Vector3I[] checkPositions = 
                    {
                        new Vector3I(1, 0, -1),
                        new Vector3I(0, 0, -1),
                        new Vector3I(-1, 0, -1),

                        new Vector3I(1, 0, 0),
                        Vector3I.Zero,
                        new Vector3I(-1, 0, 0),

                        new Vector3I(1, 0, 1),
                        new Vector3I(0, 0, 1),
                        new Vector3I(-1, 0, 1),
                    };

                    foreach (var pos in checkPositions)
                        if (!CheckGridIntersect(radiatorBlock, pos, Vector3I.Up))
                            occlusionModifier += 1f / checkPositions.Length;

                    if (!CheckGridIntersect(radiatorBlock, Vector3I.Zero, Vector3I.Up))
                        occlusionModifier += 1 / 2f;
                    if (!CheckGridIntersect(radiatorBlock, Vector3I.Right, Vector3I.Up))
                        occlusionModifier += 1 / 2f;

                    return occlusionModifier;
                }
            }
        };

        private static bool CheckGridIntersect(IMyCubeBlock block, Vector3I checkOffset, Vector3I checkDirection)
        {
            _cellPositions.Clear();

            var grid = block.CubeGrid;
            var blockMatrix = block.WorldMatrix;
            var gridMaxExtents = Vector3.Distance(grid.Max, grid.Min) * grid.GridSize;
            var checkStartPosition = Vector3D.Transform(checkOffset * block.CubeGrid.GridSize, ref blockMatrix);

            if (ModularApi.IsDebug())
                DebugDraw.AddLine(checkStartPosition,
                    checkStartPosition + Vector3D.TransformNormal(checkDirection, ref blockMatrix) * gridMaxExtents, Color.Bisque, 2);

            block.CubeGrid.RayCastCells(checkStartPosition, 
                checkStartPosition + Vector3D.TransformNormal(checkDirection, ref blockMatrix) * gridMaxExtents, _cellPositions);

            foreach (var cellPosition in _cellPositions)
            {
                var testBlock = grid.GetCubeBlock(cellPosition);
                if (testBlock != null && testBlock != block.SlimBlock)
                    return true;
            }

            return false;
        }
    }
}
