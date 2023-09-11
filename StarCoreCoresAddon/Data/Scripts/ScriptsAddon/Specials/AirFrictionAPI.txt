using System;
using MIG.Shared.SE;
using Scripts.Specials.ShipClass;
using VRage.Game.Components;
using VRage.Game.ModAPI;

namespace Scripts.Specials
{
    [MySessionComponentDescriptor(MyUpdateOrder.NoUpdate)]
    public class SpecCoreSession : MySessionComponentBase
    {
        public override void LoadData()
        {
            AirFrictionAPI.Init();
        }
    }
    
    public class AirFrictionAPI
    {
        private static Func<IMyCubeGrid, double[]> getGridFriction;
        
        public static int MinSpeed = 0;
        public static int MaxSpeed = 1;
        public static int CurrentSpeed = 2;
        public static int AppliedForce = 3;
        
        public static void Init()
        {
            if (!ModConnection.IsInited)
            {
                ModConnection.Init();
            }
            ModConnection.Subscribe<Func<IMyCubeGrid, double[]>>("MIG.AirFriction.GetGridFriction", (x) =>
            {
                getGridFriction = x;
            });
        }

        public static double[] GetGridFriction(IMyCubeGrid grid)
        {
            return getGridFriction?.Invoke(grid);
        }
    }
}