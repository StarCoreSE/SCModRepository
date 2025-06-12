using Epstein_Fusion_DS.HeatParts.ExtendableRadiators;
using ProtoBuf;
using Sandbox.ModAPI;
using VRage.Game.ModAPI;
using static Epstein_Fusion_DS.HeatParts.ExtendableRadiators.ExtendableRadiator;

namespace Epstein_Fusion_DS.Networking
{
    [ProtoContract]
    internal class BlockPacket : PacketBase
    {
        private static void LogInfo(string text) => ModularDefinition.ModularApi.Log(text);

        [ProtoMember(1)] public StoredRadiator[] Stored;
        [ProtoMember(2)] public long CubeGridId;
        [ProtoMember(3)] public long RadiatorBlockId;
        [ProtoMember(5)] public bool? IsExtending;

        private BlockPacket()
        {
        }

        public BlockPacket(StoredRadiator[] stored, IMyCubeGrid grid, IMyCubeBlock radiatorBlock, bool? isExtending)
        {
            Stored = stored;
            CubeGridId = grid.EntityId;
            RadiatorBlockId = radiatorBlock.EntityId;
            IsExtending = isExtending;

            //LogInfo("Sending " + ToString());
        }

        public override void Received(ulong SenderSteamId)
        {
            if (MyAPIGateway.Session.IsServer)
                return;

            //LogInfo("Received " + ToString());

            foreach (var radiator in Stored)
            {
                var block = (MyAPIGateway.Entities.GetEntityById(CubeGridId) as IMyCubeGrid)?.AddBlock(radiator.ObjectBuilder, true);
                if (block?.FatBlock != null)
                    block.FatBlock.Visible = false;
            }
            
            var logic = (MyAPIGateway.Entities.GetEntityById(RadiatorBlockId) as IMyCubeBlock)?.GameLogic
                ?.GetAs<ExtendableRadiator>();
            if (logic != null)
            {
                logic.StoredRadiators = Stored;

                if (IsExtending.HasValue)
                {
                    if (IsExtending.Value)
                        logic.Animation?.StartExtension();
                    else
                        logic.Animation?.StartRetraction();
                }
            }
        }

        public override string ToString()
        {
            return $"{CubeGridId}::{RadiatorBlockId}\n{Stored.Length}";
        }
    }
}
