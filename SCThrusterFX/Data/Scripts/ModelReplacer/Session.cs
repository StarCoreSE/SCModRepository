using System;
using System.Collections.Generic;
using Sandbox.Common.ObjectBuilders;
using Sandbox.Definitions;
using Sandbox.Game.Entities;
using Sandbox.Game.Weapons;
using Sandbox.ModAPI;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.Entity;
using VRage.Game.ModAPI;
using VRage.ModAPI;
using VRage.ObjectBuilders;
using VRage.Utils;
using VRageMath;


using System.Linq;



namespace ModelReplacer
{
    // This object is always present, from the world load to world unload.
    // NOTE: all clients and server run mod scripts, keep that in mind.
    // The MyUpdateOrder arg determines what update overrides are actually called.
    // Remove any method that you don't need, none of them are required, they're only there to show what you can use.
    // Also remove all comments you've read to avoid the overload of comments that is this file.

    /*
     * Todo
     * Way to many things
     * 
     * 
     * 
     */


    [MySessionComponentDescriptor(MyUpdateOrder.BeforeSimulation | MyUpdateOrder.AfterSimulation)]
    public class ThrusterSession : MySessionComponentBase
    {
        //This place is kinda dirty and need some cleanup, grab some gloves and a face mask before going into these parts of town

        public static ThrusterSession Instance; // the only way to access session comp from other classes and the only accepted static.

        //public ThrusterEvents myevents;


        
        public ThrusterSession()
        {
            Instance = this;

        }

        



        public override void LoadData()
        {
            // amogst the earliest execution points, but not everything is available at this point.

            // main entry point: MyAPIGateway
            // entry point for reading/editing definitions: MyDefinitionManager.Static
            // these can be used anywhere as they're types not fields.

            //Instance = this;
        }

        

        
           









    }
}
