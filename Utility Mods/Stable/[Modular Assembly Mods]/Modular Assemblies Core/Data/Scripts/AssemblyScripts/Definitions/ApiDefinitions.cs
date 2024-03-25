using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using VRage.Game.Entity;
using VRage.Game.ModAPI;
using VRage.ModAPI;
using VRage.Utils;

namespace Modular_Assemblies.Data.Scripts.AssemblyScripts.Definitions
{
    internal class ApiDefinitions
    {
        internal readonly Dictionary<string, Delegate> ModApiMethods;

        internal ApiDefinitions()
        {
            ModApiMethods = new Dictionary<string, Delegate>()
            {
                ["GetAllParts"] = new Func<MyEntity[]>(GetAllParts),
                ["GetAllAssemblies"] = new Func<int[]>(GetAllAssemblies),
                ["GetMemberParts"] = new Func<int, MyEntity[]>(GetMemberParts),
                ["GetConnectedBlocks"] = new Func<MyEntity, bool, MyEntity[]>(GetConnectedBlocks),
                ["GetBasePart"] = new Func<int, MyEntity>(GetBasePart),
                ["IsDebug"] = new Func<bool>(IsDebug),
            };
        }

        private bool IsDebug()
        {
            return Assemblies_SessionInit.I.DebugMode;
        }

        private MyEntity[] GetAllParts()
        {
            List<MyEntity> parts = new List<MyEntity>();
            foreach (var block in AssemblyPartManager.I.AllAssemblyParts.Keys)
                if (block.FatBlock != null)
                    parts.Add((MyEntity)block.FatBlock);
            return parts.ToArray();
        }

        private int[] GetAllAssemblies()
        {
            return AssemblyPartManager.I.AllPhysicalAssemblies.Keys.ToArray();
        }

        private MyEntity[] GetMemberParts(int assemblyId)
        {
            PhysicalAssembly wep;
            if (!AssemblyPartManager.I.AllPhysicalAssemblies.TryGetValue(assemblyId, out wep))
                return new MyEntity[0];

            List<MyEntity> parts = new List<MyEntity>();
            foreach (var part in wep.componentParts)
                if (part.block.FatBlock != null)
                    parts.Add((MyEntity)part.block.FatBlock);

            return parts.ToArray();
        }

        private MyEntity[] GetConnectedBlocks(MyEntity blockEntity, bool useCached)
        {
            if (!(blockEntity is IMyCubeBlock))
                return new MyEntity[0];

            AssemblyPart wep;
            if (!AssemblyPartManager.I.AllAssemblyParts.TryGetValue(((IMyCubeBlock)blockEntity).SlimBlock, out wep) || wep.connectedParts == null)
                return new MyEntity[0];

            List<MyEntity> parts = new List<MyEntity>();
            if (useCached)
            {
                foreach (var part in wep.connectedParts)
                    if (part.block.FatBlock != null)
                        parts.Add((MyEntity)part.block.FatBlock);
            }
            else
            {
                foreach (var part in wep.GetValidNeighbors(true))
                    if (part.FatBlock != null)
                        parts.Add((MyEntity)part.FatBlock);
            }

            return parts.ToArray();
        }

        private MyEntity GetBasePart(int assemblyId)
        {
            PhysicalAssembly wep;
            if (!AssemblyPartManager.I.AllPhysicalAssemblies.TryGetValue(assemblyId, out wep))
                return null;

            return (MyEntity) wep.basePart.block.FatBlock;
        }
    }
}
