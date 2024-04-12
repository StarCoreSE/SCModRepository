using System;
using System.Collections;
using System.Text;
using VRage.Game.Components;
using CoreSystems.Api;


namespace StarCore.SystemHighlight.APISession
{
    [MySessionComponentDescriptor(MyUpdateOrder.NoUpdate)]
    public class CoreSysAPI : MySessionComponentBase
    {
        public static WcApi _wcApi;

        public override void LoadData()
        {
            _wcApi = new WcApi();
            _wcApi.Load();
        }
        protected override void UnloadData()
        {
            _wcApi.Unload();
            _wcApi = null;
        }
    }
}
