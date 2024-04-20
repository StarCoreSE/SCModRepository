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

        [ProtoContract]
        public class FunctionCall
        {
            public enum ActionType
            {
                OnPartAdd,
                OnPartRemove,
                OnPartDestroy,
                GetAllParts,
                GetAllAssemblies,
                GetMemberParts,
                GetConnectedBlocks
            }

            [ProtoMember(1)] public string DefinitionName { get; set; }
            [ProtoMember(2)] public int PhysicalAssemblyId { get; set; }
            [ProtoMember(3)] public ActionType ActionId { get; set; }
            [ProtoMember(4)] public SerializedObjectArray Values { get; set; }
        }

        [ProtoContract]
        public class SerializedObjectArray // This is awful so don't use it.
        {
            [ProtoMember(7)] internal bool[] BoolValues = Array.Empty<bool>();
            [ProtoMember(8)] internal double[] DoubleValues = Array.Empty<double>();
            [ProtoMember(6)] internal float[] FloatValues = Array.Empty<float>();

            [ProtoMember(1)] internal int[] IntValues = Array.Empty<int>();
            [ProtoMember(3)] internal long[] LongValues = Array.Empty<long>();
            [ProtoMember(2)] internal string[] StringValues = Array.Empty<string>();
            [ProtoMember(4)] internal ulong[] UlongValues = Array.Empty<ulong>();
            [ProtoMember(5)] internal Vector3D[] VectorValues = Array.Empty<Vector3D>();

            public SerializedObjectArray()
            {
            }

            public SerializedObjectArray(params object[] array)
            {
                var intValuesL = new List<int>();
                var stringValuesL = new List<string>();
                var longValuesL = new List<long>();
                var ulongValuesL = new List<ulong>();
                var vectorValuesL = new List<Vector3D>();
                var floatValuesL = new List<float>();
                var boolValuesL = new List<bool>();
                var doubleValuesL = new List<double>();

                foreach (var value in array)
                {
                    var type = value.GetType();
                    if (type == typeof(int))
                        intValuesL.Add((int)value);
                    else if (type == typeof(string))
                        stringValuesL.Add((string)value);
                    else if (type == typeof(long))
                        longValuesL.Add((long)value);
                    else if (type == typeof(ulong))
                        ulongValuesL.Add((ulong)value);
                    else if (type == typeof(Vector3D))
                        vectorValuesL.Add((Vector3D)value);
                    else if (type == typeof(float))
                        floatValuesL.Add((float)value);
                    else if (type == typeof(bool))
                        boolValuesL.Add((bool)value);
                    else if (type == typeof(double))
                        doubleValuesL.Add((double)value);
                }

                IntValues = intValuesL.ToArray();
                StringValues = stringValuesL.ToArray();
                LongValues = longValuesL.ToArray();
                UlongValues = ulongValuesL.ToArray();
                VectorValues = vectorValuesL.ToArray();
                FloatValues = floatValuesL.ToArray();
                BoolValues = boolValuesL.ToArray();
                DoubleValues = doubleValuesL.ToArray();

                //MyLog.Default.WriteLineAndConsole($"ModularDefinitions.DefinitionDefs: {array.Length} values packaged.");
            }

            public object[] Values()
            {
                var values = new List<object>();

                foreach (var value in IntValues)
                    values.Add(value);
                foreach (var value in StringValues)
                    values.Add(value);
                foreach (var value in LongValues)
                    values.Add(value);
                foreach (var value in UlongValues)
                    values.Add(value);
                foreach (var value in VectorValues)
                    values.Add(value);
                foreach (var value in FloatValues)
                    values.Add(value);
                foreach (var value in BoolValues)
                    values.Add(value);
                foreach (var value in DoubleValues)
                    values.Add(value);

                //MyLog.Default.WriteLineAndConsole($"ModularDefinitions.DefinitionDefs: {values.Count} values recieved.");
                return values.ToArray();
            }
        }
    }
}