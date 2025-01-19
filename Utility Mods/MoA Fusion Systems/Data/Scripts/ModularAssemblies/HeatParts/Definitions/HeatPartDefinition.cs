using System;
using VRage.Game.ModAPI;

namespace Epstein_Fusion_DS.HeatParts.Definitions
{
    internal class HeatPartDefinition
    {
        /// <summary>
        /// The subtype of the heat part block.
        /// </summary>
        public string SubtypeId;

        /// <summary>
        /// Amount of heat removed per second by one block.
        /// </summary>
        public float HeatDissipation;

        /// <summary>
        /// Amount of heat that can be stored by one block.
        /// </summary>
        public float HeatCapacity;

        /// <summary>
        /// Optional line of sight check. Returns the occlusion percentage (dissipation efficiency modifier) of the input radiator. Set to null if unneeded.
        /// </summary>
        public Func<IMyCubeBlock, float> LoSCheck;
    }
}
