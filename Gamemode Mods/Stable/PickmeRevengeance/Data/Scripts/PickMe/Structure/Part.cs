using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Sandbox.ModAPI;
using VRage;
using VRage.Game.Components;
using VRage.Game.ModAPI;
using Sandbox.Common.ObjectBuilders;
using VRage.ModAPI;
using VRage.ObjectBuilders;
using Sandbox.Game.Entities;
using Sandbox.Game.EntityComponents;
using SpaceEngineers.Game.ModAPI;
using VRage.Game;
using VRage.Game.Entity;
using VRage.Utils;
using VRageMath;
using ProtoBuf;

namespace PickMe.Structure
{
    public class Part
    {
        public MyCubeBlock Block;
        public float Value = 0;
        public string Name = "";
        public string SubtypeID = "";
        public Part(MyCubeBlock block)
        {
            Block = block;
            Name = Block.BlockDefinition.Id.SubtypeName;
            SubtypeID = Block.BlockDefinition.Id.SubtypeId.String;
            if (Session.PointValues.ContainsKey(Name))
            {
                Value = Session.PointValues[Name];
            } 
        }

        public bool IsFunctional()
        {
            return Block.IsFunctional;
        }
    }
}
