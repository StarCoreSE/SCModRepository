using System.Collections.Generic;
using VRageMath;
using static SC.SUGMA.GameModes.Domination.DominationGamemode;

namespace SC.SUGMA.GameModes.Domination
{
    internal partial class DominationGamemode
    {
        internal static readonly Dictionary<string, ZoneDef[]> ZonePositionDefs = new Dictionary<string, ZoneDef[]>
        {
            ["one"] = new []
            {
                new ZoneDef
                {
                    Position = Vector3D.Zero,
                    Radius = 1000,
                    CaptureTime = 20,
                },
            },
            ["two"] = new []
            {
                new ZoneDef
                {
                    Position = new Vector3D(0, 0, 4000),
                    Radius = 500,
                    CaptureTime = 15,
                },
                new ZoneDef
                {
                    Position = new Vector3D(0, 0, -4000),
                    Radius = 500,
                    CaptureTime = 15,
                },
            },
            ["three"] = new []
            {
                new ZoneDef
                {
                    Position = new Vector3D(0, 0, 4000),
                    Radius = 500,
                    CaptureTime = 15,
                },
                new ZoneDef
                {
                    Position = Vector3D.Zero,
                    Radius = 1000,
                    CaptureTime = 20,
                },
                new ZoneDef
                {
                    Position = new Vector3D(0, 0, -4000),
                    Radius = 500,
                    CaptureTime = 15,
                },
            },
            ["diagonal"] = new []
            {
                new ZoneDef
                {
                    Position = new Vector3D(2828.42712, 0, 2828.42712),
                    Radius = 500,
                    CaptureTime = 15,
                },
                new ZoneDef
                {
                    Position = Vector3D.Zero,
                    Radius = 1000,
                    CaptureTime = 20,
                },
                new ZoneDef
                {
                    Position = new Vector3D(-2828.42712, 0, -2828.42712), // weird number puts the zone at 4km from center
                    Radius = 500,
                    CaptureTime = 15,
                },
            },
        };
    }
}
