using System;
using System.Collections.Generic;
using System.Linq;
using Sandbox.ModAPI;
using VRage;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.ModAPI;
using VRage.Utils;
using VRageMath;
using static VRageRender.MyBillboard;

namespace MoA_Fusion_Systems.Data.Scripts.ModularAssemblies.
    DebugDraw
{
    [MySessionComponentDescriptor(MyUpdateOrder.NoUpdate)]
    public class DebugDraw : MySessionComponentBase
    {
        protected const float OnTopColorMul = 0.5f;

        private const float DepthRatioF = 0.01f;
        // i'm gonna kiss digi on the 

        private static DebugDraw Instance;
        protected static readonly MyStringId MaterialDot = MyStringId.GetOrCompute("WhiteDot");
        protected static readonly MyStringId MaterialSquare = MyStringId.GetOrCompute("Square");

        private readonly Dictionary<Vector3I, MyTuple<long, Color, IMyCubeGrid>> QueuedGridPoints =
            new Dictionary<Vector3I, MyTuple<long, Color, IMyCubeGrid>>();

        private readonly Dictionary<MyTuple<Vector3D, Vector3D>, MyTuple<long, Color>> QueuedLinePoints =
            new Dictionary<MyTuple<Vector3D, Vector3D>, MyTuple<long, Color>>();

        private readonly Dictionary<Vector3D, MyTuple<long, Color>> QueuedPoints =
            new Dictionary<Vector3D, MyTuple<long, Color>>();

        public override void LoadData()
        {
            if (!MyAPIGateway.Utilities.IsDedicated)
                Instance = this;
        }

        protected override void UnloadData()
        {
            Instance = null;
        }

        public static void AddPoint(Vector3D globalPos, Color color, float duration)
        {
            if (Instance == null)
                return;


            if (Instance.QueuedPoints.ContainsKey(globalPos))
                Instance.QueuedPoints[globalPos] =
                    new MyTuple<long, Color>(DateTime.UtcNow.Ticks + (long)(duration * TimeSpan.TicksPerSecond), color);
            else
                Instance.QueuedPoints.Add(globalPos,
                    new MyTuple<long, Color>(DateTime.UtcNow.Ticks + (long)(duration * TimeSpan.TicksPerSecond),
                        color));
        }

        public static void AddGPS(string name, Vector3D position, float duration)
        {
            var gps = MyAPIGateway.Session.GPS.Create(name, string.Empty, position, true, true);
            gps.DiscardAt =
                MyAPIGateway.Session.ElapsedPlayTime.Add(new TimeSpan((long)(duration * TimeSpan.TicksPerSecond)));
            MyAPIGateway.Session.GPS.AddLocalGps(gps);
        }

        public static void AddGridGPS(string name, Vector3I gridPosition, IMyCubeGrid grid, float duration)
        {
            AddGPS(name, GridToGlobal(gridPosition, grid), duration);
        }

        public static void AddGridPoint(Vector3I blockPos, IMyCubeGrid grid, Color color, float duration)
        {
            if (Instance == null)
                return;

            if (Instance.QueuedGridPoints.ContainsKey(blockPos))
                Instance.QueuedGridPoints[blockPos] =
                    new MyTuple<long, Color, IMyCubeGrid>(
                        DateTime.UtcNow.Ticks + (long)(duration * TimeSpan.TicksPerSecond), color, grid);
            else
                Instance.QueuedGridPoints.Add(blockPos,
                    new MyTuple<long, Color, IMyCubeGrid>(
                        DateTime.UtcNow.Ticks + (long)(duration * TimeSpan.TicksPerSecond), color, grid));
        }

        public static void AddLine(Vector3D origin, Vector3D destination, Color color, float duration)
        {
            if (Instance == null)
                return;


            var key = new MyTuple<Vector3D, Vector3D>(origin, destination);
            if (Instance.QueuedLinePoints.ContainsKey(key))
                Instance.QueuedLinePoints[key] =
                    new MyTuple<long, Color>(DateTime.UtcNow.Ticks + (long)(duration * TimeSpan.TicksPerSecond), color);
            else
                Instance.QueuedLinePoints.Add(key,
                    new MyTuple<long, Color>(DateTime.UtcNow.Ticks + (long)(duration * TimeSpan.TicksPerSecond),
                        color));
        }

        public override void Draw()
        {
            try
            {
                foreach (var key in QueuedPoints.Keys.ToList())
                {
                    DrawPoint0(key, QueuedPoints[key].Item2);

                    if (DateTime.UtcNow.Ticks > QueuedPoints[key].Item1)
                        QueuedPoints.Remove(key);
                }

                foreach (var key in QueuedGridPoints.Keys.ToList())
                {
                    DrawGridPoint0(key, QueuedGridPoints[key].Item3, QueuedGridPoints[key].Item2);

                    if (DateTime.UtcNow.Ticks > QueuedGridPoints[key].Item1)
                        QueuedGridPoints.Remove(key);
                }

                foreach (var key in QueuedLinePoints.Keys.ToList())
                {
                    DrawLine0(key.Item1, key.Item2, QueuedLinePoints[key].Item2);

                    if (DateTime.UtcNow.Ticks > QueuedLinePoints[key].Item1)
                        QueuedLinePoints.Remove(key);
                }
            }
            catch
            {
            } // Icky no error logging
        }

        private void DrawPoint0(Vector3D globalPos, Color color)
        {
            //MyTransparentGeometry.AddPointBillboard(MaterialDot, color, globalPos, 1.25f, 0, blendType: BlendTypeEnum.PostPP);
            var depthScale = ToAlwaysOnTop(ref globalPos);
            MyTransparentGeometry.AddPointBillboard(MaterialDot, color * OnTopColorMul, globalPos, 0.35f * depthScale, 0,
                blendType: BlendTypeEnum.LDR);
        }

        private void DrawGridPoint0(Vector3I blockPos, IMyCubeGrid grid, Color color)
        {
            DrawPoint0(GridToGlobal(blockPos, grid), color);
        }

        private void DrawLine0(Vector3D origin, Vector3D destination, Color color)
        {
            var length = (float)(destination - origin).Length();
            var direction = (destination - origin) / length;

            MyTransparentGeometry.AddLineBillboard(MaterialSquare, color, origin, direction, length, 0.35f);

            var depthScale = ToAlwaysOnTop(ref origin);
            direction *= depthScale;

            MyTransparentGeometry.AddLineBillboard(MaterialSquare, color * OnTopColorMul, origin, direction, length,
                0.5f * depthScale);
        }

        public static Vector3D GridToGlobal(Vector3I position, IMyCubeGrid grid)
        {
            return Vector3D.Rotate((Vector3D)position * 2.5f, grid.WorldMatrix) + grid.GetPosition();
        }

        protected static float ToAlwaysOnTop(ref Vector3D position)
        {
            var camMatrix = MyAPIGateway.Session.Camera.WorldMatrix;
            position = camMatrix.Translation + (position - camMatrix.Translation) * DepthRatioF;

            return DepthRatioF;
        }
    }
}