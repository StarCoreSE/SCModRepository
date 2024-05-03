using VRage.Game.Components;

namespace SENetworkAPI
{
    [MySessionComponentDescriptor(MyUpdateOrder.NoUpdate)]
    public class SessionTools : MySessionComponentBase
    {
        protected override void UnloadData()
        {
            NetworkApi.Dispose();
        }
    }
}