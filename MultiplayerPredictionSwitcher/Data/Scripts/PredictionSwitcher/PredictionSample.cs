using Sandbox.Game;
using Sandbox.Game.Entities;
using Sandbox.Game.World;
using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VRage.Game;
using VRage.Game.Components;
using VRage.Plugins;

namespace PredictionSample
{
   ////dont have to do anything in this class, but a class in the assembly has to implement iplugin for the game to recognize it
   internal class PredictionSample : IPlugin
   {
       public void Dispose()
       {

       }
   
       public void Init(object gameInstance)
       {
   
       }
   
       public void Update()
       {
   
       }
   }

    [MySessionComponentDescriptor(MyUpdateOrder.AfterSimulation)]
    public class SessionComp : MySessionComponentBase
    {
        public override void UpdateAfterSimulation()
        {
            MyCubeGrid controlled = MySession.Static?.ControlledGrid;
            if (controlled != null)
            {
                controlled.ForceDisablePrediction = true;
                MyAPIGateway.Utilities.ShowNotification("Prediction disabled for controlled grid");
            }
        }
    }
}

