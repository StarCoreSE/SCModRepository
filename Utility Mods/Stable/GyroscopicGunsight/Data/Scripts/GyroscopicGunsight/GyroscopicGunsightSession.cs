using System;
using SC.GyroscopicGunsight.API.CoreSystems;
using VRage.Game.Components;
using VRage.Game.ModAPI;
using VRage.Utils;
using VRageMath;

namespace SC.GyroscopicGunsight
{
    [MySessionComponentDescriptor(MyUpdateOrder.NoUpdate)]
    internal class GyroscopicGunsightSession : MySessionComponentBase
    {
        public WcApi WcApi = new WcApi();

        public override void LoadData()
        {
            WcApi.Load();
        }

        public override void Draw()
        {
            if (!(WcApi?.IsReady ?? false))
                return;

            try
            {

            }
            catch (Exception ex)
            {
                MyLog.Default.WriteLine(ex);
            }
        }

        public Vector3D CalculateDeflection(IMyCubeBlock thisWeapon, IMyCubeGrid targetGrid)
        {
            // TODO
            return Vector3D.Zero;
        }
    }
}
