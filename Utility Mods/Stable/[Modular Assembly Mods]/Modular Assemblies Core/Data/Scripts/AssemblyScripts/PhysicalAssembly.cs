using Modular_Assemblies.Data.Scripts.AssemblyScripts.DebugDraw;
using Modular_Assemblies.Data.Scripts.AssemblyScripts.Definitions;
using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using VRage.Game.ModAPI;
using VRageMath;

namespace Modular_Assemblies.Data.Scripts.AssemblyScripts
{
    /// <summary>
    /// The collection of AssemblyParts attached to a modular assembly base.
    /// </summary>
    public class PhysicalAssembly
    {
        public AssemblyPart basePart;
        public List<AssemblyPart> componentParts = new List<AssemblyPart>();
        public ModularDefinition AssemblyDefinition;
        public int id = -1;

        public int numReactors = 0;
        private Color color;

        public void Update()
        {
            if (Assemblies_SessionInit.I.DebugMode)
            {
                foreach (var part in componentParts)
                {
                    DebugDrawManager.AddGridPoint(part.block.Position, part.block.CubeGrid, color, 0f);
                    foreach (var conPart in part.connectedParts)
                        DebugDrawManager.AddLine(DebugDrawManager.GridToGlobal(part.block.Position, part.block.CubeGrid), DebugDrawManager.GridToGlobal(conPart.block.Position, part.block.CubeGrid), color, 0f);
                }
                MyAPIGateway.Utilities.ShowNotification($"Assembly {id} Parts: {componentParts.Count}", 1000 / 60);
            }
        }

        public PhysicalAssembly(int id, AssemblyPart basePart, ModularDefinition AssemblyDefinition)
        {
            this.basePart = basePart;
            this.AssemblyDefinition = AssemblyDefinition;
            this.id = id;
            AssemblyPartManager.I.CreatedPhysicalAssemblies++;

            if (AssemblyPartManager.I.AllPhysicalAssemblies.ContainsKey(id))
                throw new Exception("Duplicate assembly ID!");
            AssemblyPartManager.I.AllPhysicalAssemblies.Add(id, this);

            Random r = new Random();
            color = new Color(r.Next(255), r.Next(255), r.Next(255));

            AddPart(basePart);
            AssemblyPartManager.I.QueueAssemblyCheck(basePart, this);
        }

        public void AddPart(AssemblyPart part)
        {
            if (componentParts.Contains(part))
                componentParts.Remove(part);

            componentParts.Add(part);
            part.memberAssembly = this;
            if (part.prevAssemblyId != id)
                DefinitionHandler.I.SendOnPartAdd(AssemblyDefinition.Name, id, part.block.FatBlock.EntityId, part == basePart);
            part.prevAssemblyId = id;
        }

        /// <summary>
        /// Removes a part without running connection checks. Only use when the PhysicalAssembly will be removed.
        /// </summary>
        /// <param name="part"></param>
        public void RemoveFast(AssemblyPart part)
        {
            if (componentParts == null || part == null)
                return;

            if (!componentParts.Contains(part))
                return;
            componentParts.Remove(part);

            part.connectedParts.Clear();
            part.memberAssembly = null;

            DefinitionHandler.I.SendOnPartRemove(AssemblyDefinition.Name, id, part.block.FatBlock.EntityId, part == basePart);

            if (componentParts.Count == 0)
                Close();
        }

        public void Remove(AssemblyPart part)
        {
            if (componentParts == null || part == null)
                return;

            if (!componentParts.Contains(part))
                return;
            componentParts.Remove(part);

            DefinitionHandler.I.SendOnPartRemove(AssemblyDefinition.Name, id, part.block.FatBlock.EntityId, part == basePart);
            if (part.block.Integrity == 0)
                DefinitionHandler.I.SendOnPartDestroy(AssemblyDefinition.Name, id, part.block.FatBlock.EntityId, part == basePart);

            //MyAPIGateway.Utilities.ShowNotification("Subpart parts: " + part.connectedParts.Count);

            // Clear self if basepart was removed
            if (part == basePart)
            {
                foreach (var cPart in componentParts.ToList())
                    ResetPart(cPart);
                Close();
                return;
            }
            // Split apart if necessary. Recalculates every connection - suboptimal but neccessary, I believe.
            else if (part.connectedParts.Count > 1)
            {
                foreach (var cPart in componentParts.ToList())
                    ResetPart(cPart);
                componentParts.Clear();

                // Above loop removes all parts's memberAssemblies, but the base should always have one.
                basePart.memberAssembly = this;
                componentParts.Add(basePart);

                if (Assemblies_SessionInit.I.DebugMode)
                    MyAPIGateway.Utilities.ShowNotification("Recreating connections...");
                AssemblyPartManager.I.QueueConnectionCheck(basePart);
                AssemblyPartManager.I.QueueAssemblyCheck(basePart, this);

                return;
            }

            // Make doubly and triply sure that each part does not remember this one.
            foreach (var cPart in part.connectedParts)
            {
                int idx = cPart.connectedParts.IndexOf(part);
                if (idx >= 0)
                {
                    cPart.connectedParts.RemoveAt(idx);
                }
            }

            part.connectedParts.Clear();
            part.memberAssembly = null;

            if (componentParts.Count == 0)
                Close();
        }

        private void ResetPart(AssemblyPart part)
        {
            if (part == null)
                return;
            part.memberAssembly = null;
            part.connectedParts.Clear();
            AssemblyPartManager.I.QueueConnectionCheck(part);
        }

        public void Close()
        {
            if (componentParts == null)
                return;

            componentParts = null;
            basePart = null;
            AssemblyPartManager.I.AllPhysicalAssemblies.Remove(id);
        }

        public void RecursiveAssemblyChecker(AssemblyPart currentBlock)
        {
            // Safety check
            if (currentBlock == null || currentBlock.block == null || componentParts == null) return;

            // TODO split between threads/ticks
            currentBlock.memberAssembly = this;

            List<IMySlimBlock> slimNeighbors = new List<IMySlimBlock>();
            currentBlock.block.GetNeighbours(slimNeighbors);
        
            foreach (IMySlimBlock neighbor in slimNeighbors)
            {
                // Another safety check
                if (neighbor == null) continue;

                if (AssemblyDefinition.IsBlockAllowed(neighbor) && AssemblyDefinition.DoesBlockConnect(currentBlock.block, neighbor))
                {
                    AssemblyPart neighborPart;
                    
                    if (AssemblyPartManager.I.AllAssemblyParts.TryGetValue(neighbor, out neighborPart))
                    {
                        // Avoid double-including blocks
                        if (componentParts.Contains(neighborPart))
                        {
                            //MyLog.Default.WriteLineAndConsole("ModularAssemblies: Skip part " + neighbor.BlockDefinition.Id.SubtypeName + " @ " + neighbor.Position);
                            continue;
                        }

                        //MyLog.Default.WriteLineAndConsole("ModularAssemblies: Add part " + neighbor.BlockDefinition.Id.SubtypeName + " @ " + neighbor.Position);

                        componentParts.Add(neighborPart);
                        AssemblyPartManager.I.QueueConnectionCheck(neighborPart);
                        AssemblyPartManager.I.QueueAssemblyCheck(neighborPart, this);
                    }
                }
            }
        }
    }
}
