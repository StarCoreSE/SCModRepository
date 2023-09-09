using Sandbox.Common.ObjectBuilders;
using Sandbox.Definitions;
using Sandbox.Game.Entities;
using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.Entity;
using VRage.Game.ModAPI;
using VRage.ModAPI;
using VRage.ObjectBuilders;
using VRage.Utils;
using VRageMath;







namespace ModelReplacer
{


    [MyEntityComponentDescriptor(typeof(MyObjectBuilder_Thrust), false,
        "LargeBlockLargeHydrogenThrustIndustrial"
        )]
    public class ModelReplacer : MyGameLogicComponent
    {
        
    
        
        public void LoadData() //replace model
        {
            IMyThrust temp = (IMyThrust)Entity;
            var id = temp.BlockDefinition.SubtypeId;
            var defId = new MyDefinitionId(typeof(MyObjectBuilder_Thrust), id);
            var blockDef = new MyCubeBlockDefinition();
            MyLog.Default.WriteLineAndConsole("CurrentModelPath: " + blockDef.Model);
            //string path = "";
            //if (MyDefinitionManager.Static.TryGetCubeBlockDefinition(defId, out blockDef) && path != "")
            //{
            //    blockDef.Model = path;
            //}


            // amogst the earliest execution points, but not everything is available at this point.

            // main entry point: MyAPIGateway
            // entry point for reading/editing definitions: MyDefinitionManager.Static
            // these can be used anywhere as they're types not fields.

            //Instance = this;
        }
    }
}