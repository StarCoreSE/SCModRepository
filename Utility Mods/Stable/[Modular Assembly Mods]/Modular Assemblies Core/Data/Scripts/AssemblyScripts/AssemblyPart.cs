using Sandbox.Common.ObjectBuilders;
using Sandbox.Game;
using Sandbox.Game.Entities.Cube;
using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.ModAPI;
using VRage.ModAPI;
using VRage.ObjectBuilders;

namespace Modular_Assemblies.Data.Scripts.AssemblyScripts
{
    /// <summary>
    /// Attached to every part in a AssemblyDefinition.
    /// </summary>
    public class AssemblyPart
    {
        public IMySlimBlock block;
        public PhysicalAssembly memberAssembly = null;
        public List<AssemblyPart> connectedParts = new List<AssemblyPart>();
        public ModularDefinition AssemblyDefinition;

        public AssemblyPart(IMySlimBlock block, ModularDefinition AssemblyDefinition)
        {
            this.block = block;
            this.AssemblyDefinition = AssemblyDefinition;

            //MyAPIGateway.Utilities.ShowNotification("Placed valid AssemblyPart");

            if (AssemblyPartManager.I.AllAssemblyParts.ContainsKey(block))
                return;

            AssemblyPartManager.I.AllAssemblyParts.Add(block, this);

            if (AssemblyDefinition.BaseBlockSubtype == block.BlockDefinition.Id.SubtypeName)
            {
                memberAssembly = new PhysicalAssembly(AssemblyPartManager.I.CreatedPhysicalAssemblies, this, AssemblyDefinition);
            }
            else
                AssemblyPartManager.I.QueueConnectionCheck(this);
        }

        public int prevAssemblyId = -1;
        public void CheckForExistingAssembly()
        {
            // You can't have two baseblocks per assembly
            //if (AssemblyDefinition.BaseBlockSubtype != block.BlockDefinition.Id.SubtypeName)
            //    memberAssembly = null;

            List<AssemblyPart> validNeighbors = GetValidNeighborParts();

            // Search for neighboring PhysicalAssemblies
            foreach (var nBlockPart in validNeighbors)
            {
                if (nBlockPart.memberAssembly == null)
                    continue;
                nBlockPart.memberAssembly.AddPart(this);
                break;
            }

            if (memberAssembly == null)
            {
                //MyAPIGateway.Utilities.ShowNotification("Null memberAssembly " + validNeighbors.Count);
                if (AssemblyDefinition.BaseBlockSubtype == block.BlockDefinition.Id.SubtypeName)
                    MyVisualScriptLogicProvider.SendChatMessage($"CRITICAL ERROR BaseBlock Null memberAssembly", "MW");
                return;
            }

            // Connect non-member blocks & populate connectedParts
            foreach (var nBlockPart in validNeighbors)
            {
                connectedParts.Add(nBlockPart);

                if (nBlockPart.memberAssembly == null)
                {
                    AssemblyPartManager.I.QueueConnectionCheck(nBlockPart);
                    //MyAPIGateway.Utilities.ShowNotification("Forced a assembly join");
                }
                //else if (nBlockPart.memberAssembly != memberAssembly)
                //    MyAPIGateway.Utilities.ShowNotification("Invalid memberAssembly");
                else if (!nBlockPart.connectedParts.Contains(this))
                    nBlockPart.connectedParts.Add(this);
            }

            if (Assemblies_SessionInit.I.DebugMode)
                MyAPIGateway.Utilities.ShowNotification("Connected: " + connectedParts.Count + " | Failed: " + (GetValidNeighbors().Count - connectedParts.Count));
        }

        /// <summary>
        /// Returns attached (as per AssemblyPart) neighbor blocks.
        /// </summary>
        /// <returns></returns>
        public List<IMySlimBlock> GetValidNeighbors(bool MustShareAssembly = false)
        {
            List<IMySlimBlock> neighbors = new List<IMySlimBlock>();
            block.GetNeighbours(neighbors);

            List<IMySlimBlock> validNeighbors = new List<IMySlimBlock>();
            foreach (var nBlock in neighbors)
            {
                if (AssemblyDefinition.DoesBlockConnect(block, nBlock, true))
                    validNeighbors.Add(nBlock);
            }

            if (MustShareAssembly)
                validNeighbors.RemoveAll(nBlock =>
                {
                    AssemblyPart part;
                    if (!AssemblyPartManager.I.AllAssemblyParts.TryGetValue(nBlock, out part))
                        return true;
                    return part.memberAssembly != this.memberAssembly;
                });

            return validNeighbors;
        }

        /// <summary>
        /// Returns attached (as per AssemblyPart) neighbor blocks's parts.
        /// </summary>
        /// <returns></returns>
        private List<AssemblyPart> GetValidNeighborParts()
        {
            List<AssemblyPart> validNeighbors = new List<AssemblyPart>();
            foreach (var nBlock in GetValidNeighbors())
            {
                AssemblyPart nBlockPart;
                if (AssemblyPartManager.I.AllAssemblyParts.TryGetValue(nBlock, out nBlockPart))
                {
                    validNeighbors.Add(nBlockPart);
                }
            }

            return validNeighbors;
        }
    }
}