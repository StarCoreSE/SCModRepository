using Medieval.ObjectBuilders;
using Sandbox.Game;
using Sandbox.ModAPI;
using System;
using System.Linq;
using VRage.Game;
using VRage.Game.Components;
using VRage.Utils;
using VRageMath;
using VRage.Game.ModAPI;
using Sandbox.Game.Entities;
using VRage.Game.Entity;
using VRage.ModAPI;
using VRage.Game.Models;
using Sandbox.Definitions;
using VRage.ObjectBuilders;
using VRage.Serialization;
using VRageRender;

namespace Jakaria
{
    [MySessionComponentDescriptor(MyUpdateOrder.NoUpdate)]
    public class CleanModCategories : MySessionComponentBase
    {
        public override void LoadData()
        {
            if (MyAPIGateway.Utilities.IsDedicated)
                return;

            MyGuiBlockCategoryDefinition categoryDefinition = null;

            foreach (var definition in MyDefinitionManager.Static.GetCategories())
            {
                if (definition.Value.Name == "ModdedBlocks")
                {
                    // Locate my single modded block category
                    categoryDefinition = definition.Value;
                }
                else if (!definition.Value.Context.IsBaseGame &&
                         definition.Value.Name != "DLCBlocks" &&
                         /*definition.Value.Name != ".SC Tournament Weapons" && */
                         definition.Value.Name != ".Starcore Basic Greebles" &&
                         definition.Value.Name != "Fusion Systems" &&
                         !definition.Value.Name.StartsWith(".SC_")
                        )
                {
                    // Disable modded block categories
                    definition.Value.Enabled = false;
                    definition.Value.Public = false;
                    definition.Value.ShowInCreative = false;
                    definition.Value.AvailableInSurvival = false;
                    definition.Value.ItemIds.Clear();
                }
            }

            if (categoryDefinition == null)
            {
                MyLog.Default.WriteLine("The custom Mod Category definition was not found!");
            }

            foreach (var definition in MyDefinitionManager.Static.GetAllDefinitions())
            {
                MyCubeBlockDefinition blockDefinition = definition as MyCubeBlockDefinition;
                if (blockDefinition != null &&
                    blockDefinition.Context != null &&
                    !blockDefinition.Context.IsBaseGame)
                {
                    categoryDefinition.ItemIds.Add(blockDefinition.Id.ToString());
                }
            }
        }
    }
}
