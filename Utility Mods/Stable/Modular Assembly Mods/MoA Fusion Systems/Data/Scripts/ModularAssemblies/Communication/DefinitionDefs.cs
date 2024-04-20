using System;
using System.Collections.Generic;
using ProtoBuf;
using VRage.Game.ModAPI;
using VRageMath;

namespace MoA_Fusion_Systems.Data.Scripts.ModularAssemblies.
    Communication
{
    public class DefinitionDefs
    {
        [ProtoContract]
        public class DefinitionContainer
        {
            [ProtoMember(1)] internal PhysicalDefinition[] PhysicalDefs;
        }

        [ProtoContract]
        public class PhysicalDefinition
        {
            /// <summary>
            ///     The name of this definition. Must be unique!
            /// </summary>
            [ProtoMember(1)]
            public string Name { get; set; }

            /// <summary>
            ///     Triggered whenever the definition is first loaded.
            /// </summary>
            public Action OnInit { get; set; }

            /// <summary>
            ///     Called when a valid part is placed.
            ///     <para>
            ///         Arg1 is PhysicalAssemblyId, Arg2 is BlockEntity, Arg3 is IsBaseBlock
            ///     </para>
            /// </summary>
            public Action<int, IMyCubeBlock, bool> OnPartAdd { get; set; }

            /// <summary>
            ///     Called when a valid part is removed.
            ///     <para>
            ///         Arg1 is PhysicalAssemblyId, Arg2 is BlockEntity, Arg3 is IsBaseBlock
            ///     </para>
            /// </summary>
            public Action<int, IMyCubeBlock, bool> OnPartRemove { get; set; }

            /// <summary>
            ///     Called when a component part is destroyed. Note - OnPartRemove is called simultaneously.
            ///     <para>
            ///         Arg1 is PhysicalAssemblyId, Arg2 is BlockEntity, Arg3 is IsBaseBlock
            ///     </para>
            /// </summary>
            public Action<int, IMyCubeBlock, bool> OnPartDestroy { get; set; }

            /// <summary>
            ///     All allowed SubtypeIds. The mod will likely misbehave if two mods allow the same blocks, so please be cautious.
            /// </summary>
            [ProtoMember(2)]
            public string[] AllowedBlocks { get; set; }

            /// <summary>
            ///     Allowed connection directions. Measured in blocks. If an allowed SubtypeId is not included here, connections are
            ///     allowed on all sides. If the connection type whitelist is empty, all allowed subtypes may connect on that side.
            /// </summary>
            [ProtoMember(3)]
            public Dictionary<string, Dictionary<Vector3I, string[]>> AllowedConnections { get; set; }

            /// <summary>
            ///     The primary block of a PhysicalAssembly. Make sure this is an AssemblyCore block OR null.
            /// </summary>
            [ProtoMember(4)]
            public string BaseBlock { get; set; }
        }
    }
}