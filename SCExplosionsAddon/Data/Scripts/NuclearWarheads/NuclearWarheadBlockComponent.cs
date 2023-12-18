using Sandbox.Common.ObjectBuilders;
using Sandbox.Game;
using Sandbox.Game.Entities;
using Sandbox.Game.Entities.Cube;
using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.Entity;
using VRage.Game.ModAPI;
using VRage.ModAPI;
using VRage.ObjectBuilders;
using VRage.Utils;
using VRageMath;

namespace NuclearWarheads
{
    [MyEntityComponentDescriptor(typeof(MyObjectBuilder_Warhead), true, "SmallWarhead", "LargeWarhead", "SmallWarhead_Nuclear", "LargeWarhead_Nuclear", "LargeWarhead_NuclearSmall", "LargeWarhead_NuclearLarge")]
    public class NuclearWarheadBlockComponent : MyGameLogicComponent
    {
        public static readonly MyDefinitionId warhead_small = MyVisualScriptLogicProvider.GetDefinitionId("Warhead", "SmallWarhead");
        public static readonly MyDefinitionId warhead_large = MyVisualScriptLogicProvider.GetDefinitionId("Warhead", "LargeWarhead");
        public static readonly MyDefinitionId warhead_mininuke = MyVisualScriptLogicProvider.GetDefinitionId("Warhead", "SmallWarhead_Nuclear");
        public static readonly MyDefinitionId warhead_nuclear = MyVisualScriptLogicProvider.GetDefinitionId("Warhead", "LargeWarhead_Nuclear");
        public static readonly MyDefinitionId warhead_nuclearsmall = MyVisualScriptLogicProvider.GetDefinitionId("Warhead", "LargeWarhead_NuclearSmall");
        public static readonly MyDefinitionId warhead_thermonuclear = MyVisualScriptLogicProvider.GetDefinitionId("Warhead", "LargeWarhead_NuclearLarge");

        private static Dictionary<MyDefinitionId, float> warheadTable = new Dictionary<MyDefinitionId, float>();
        private IMyWarhead warheadBlock;

        public static void Init()
        {
            if (NuclearWarheadsCore.ConfigData.VanillaWarheadBlockConfig.OverrideVanillaWarhead)
            {
                warheadTable.Add(warhead_small, NuclearWarheadsCore.ConfigData.VanillaWarheadBlockConfig.VanillaSmallWarheadExplosionRadius);
                warheadTable.Add(warhead_large, NuclearWarheadsCore.ConfigData.VanillaWarheadBlockConfig.VanillaLargeWarheadExplosionRadius);
            }

            warheadTable.Add(warhead_mininuke, NuclearWarheadsCore.ConfigData.MiniNukeBlockConfig.MiniNukeExplosionRadius);
            warheadTable.Add(warhead_nuclear, NuclearWarheadsCore.ConfigData.NuclearWarheadBlockConfig.NuclearExplosionRadius);
            warheadTable.Add(warhead_nuclearsmall, NuclearWarheadsCore.ConfigData.NuclearWarheadBlockConfig.NuclearExplosionRadius);
            warheadTable.Add(warhead_thermonuclear, NuclearWarheadsCore.ConfigData.ThermoNuclearWarheadBlockConfig.ThermoNuclearExplosionRadius);
        }

        public override void Init(MyObjectBuilder_EntityBase objectBuilder)
        {
            if (!MyAPIGateway.Session.IsServer) return;

            warheadBlock = Entity as IMyWarhead;
            if (warheadTable.ContainsKey(warheadBlock.SlimBlock.BlockDefinition.Id))
            {
                warheadBlock.OnMarkForClose += (IMyEntity ent) =>
                {
                    if (warheadBlock != null && warheadBlock.DetonationTime < 0.01f)
                    {
                        Vector3D position = warheadBlock.GetPosition();
                        Vector3D up = warheadBlock.WorldMatrix.Up;
                        Vector3D velocity = warheadBlock.CubeGrid.Physics != null ? warheadBlock.CubeGrid.Physics.LinearVelocity : Vector3.Zero;
                        MyAPIUtilities.Static.SendModMessage(NuclearWarheadsCore.EXPLOSION_MOD_MESSAGE_ID, string.Join(";", position.X, position.Y, position.Z, up.X, up.Y, up.Z, velocity.X, velocity.Y, velocity.Z, warheadTable[warheadBlock.SlimBlock.BlockDefinition.Id]));
                    }
                };
            }

            base.Init(objectBuilder);
        }

        public override void OnRemovedFromScene()
        {
            base.OnRemovedFromScene();

            if (!MyAPIGateway.Session.IsServer) return;
        }
    }
}
