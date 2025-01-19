using Medieval.ObjectBuilders;
using Sandbox.Game;
using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
    public class CleanDLCCategories : MySessionComponentBase
    {
        public override void LoadData()
        {
            if (MyAPIGateway.Utilities.IsDedicated)
                return;

            MyGuiBlockCategoryDefinition categoryDefinition = null;

            List<MyGuiBlockCategoryDefinition> dlcCategories = new List<MyGuiBlockCategoryDefinition>();
            foreach (var category in MyDefinitionManager.Static.GetCategories())
            {
                if (category.Value.Name == "DLCBlocks")
                {
                    //Locate my single dlc block category
                    categoryDefinition = category.Value;
                }
                else
                {
                    if (category.Value.ItemIds != null)
                    {
                        bool isDlc = true;
                        bool anyDlc = false;

                        foreach (var block in category.Value.ItemIds)
                        {
                            MyDefinitionId id;
                            MyDefinitionBase definition;
                            if (MyDefinitionId.TryParse(block, out id) && MyDefinitionManager.Static.TryGetDefinition(id, out definition))
                            {
                                if (definition.DLCs != null && definition.DLCs.Length > 0)
                                {
                                    anyDlc = true;
                                }

                                if ((definition.DLCs == null || definition.DLCs.Length == 0) && definition.Context.IsBaseGame)
                                {
                                    isDlc = false;
                                }
                            }
                        }

                        if (isDlc && anyDlc && category.Value.Name != "CharacterAnimations" && category.Value.Name != "ModdedBlocks")
                        {
                            //Disable dlc block categories
                            category.Value.Enabled = false;
                            category.Value.Public = false;
                            category.Value.ShowInCreative = false;
                            category.Value.AvailableInSurvival = false;

                            dlcCategories.Add(category.Value);
                        }
                    }
                }
            }

            if (categoryDefinition == null)
            {
                throw new Exception("The custom DLC Category definition was not found!");
            }

            foreach (var category in dlcCategories)
            {
                foreach (var block in category.ItemIds)
                {
                    categoryDefinition.ItemIds.Add(block);
                }

                category.ItemIds.Clear();
            }
        }
    }
}
