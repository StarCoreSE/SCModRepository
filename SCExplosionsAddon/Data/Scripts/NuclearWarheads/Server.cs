using ProtoBuf;
using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using System.Text;
using VRage.Game.ModAPI;
using VRageMath;

namespace NuclearWarheads
{
    public class Networking
    {
        public readonly ushort PacketId;

        public readonly List<IMyPlayer> TempPlayers = new List<IMyPlayer>();

        public Networking(ushort packetId)
        {
            PacketId = packetId;
        }

        public void Register()
        {
            MyAPIGateway.Multiplayer.RegisterSecureMessageHandler(PacketId, ReceivedPacket);
        }

        public void Unregister()
        {
            MyAPIGateway.Multiplayer.UnregisterSecureMessageHandler(PacketId, ReceivedPacket);
        }

        private void ReceivedPacket(ushort id, byte[] rawData, ulong recipient, bool reliable)
        {
            try
            {
                if (!MyAPIGateway.Session.IsServer)
                {
                    if (rawData.Length <= 2)
                        return; // invalid packet

                    var packet = MyAPIGateway.Utilities.SerializeFromBinary<PacketGameState>(rawData);
                    packet.Received();
                }
            }
            catch (Exception e)
            {

            }
        }

        public void SendToPlayers(PacketGameState packet)
        {
            TempPlayers.Clear();
            MyAPIGateway.Players.GetPlayers(TempPlayers);

            var bytes = MyAPIGateway.Utilities.SerializeToBinary(packet);

            foreach (var player in TempPlayers)
            {
                if (player.SteamUserId == MyAPIGateway.Multiplayer.ServerId)
                    continue;

                MyAPIGateway.Multiplayer.SendMessageTo(PacketId, bytes, player.SteamUserId);
            }
        }

        public void SendAllMissileInfo(List<NuclearWarheadsCore.NuclearMissileInfo> missileData)
        {
            List<long> entityId = new List<long>();
            List<Vector3D> position = new List<Vector3D>();
            List<Vector3D> velocity = new List<Vector3D>();
            List<Vector3D> gravity = new List<Vector3D>();
            foreach (var missile in missileData)
            {
                entityId.Add(missile.entity.EntityId);
                position.Add(missile.position);
                velocity.Add(missile.velocity);
                gravity.Add(missile.gravity);
            }

            SendToPlayers(new PacketGameState(entityId, position, velocity, gravity));

            //MyAPIGateway.Utilities.ShowNotification($"SEND!!!");
        }

        public void SendNewMissileInfo(NuclearWarheadsCore.NuclearMissileInfo missileInfo)
        {
            SendToPlayers(new PacketGameState(missileInfo.entity.EntityId, missileInfo.position, missileInfo.velocity, missileInfo.gravity));
        }
    }

    public enum PacketType
    {
        RefreshAll,
        AddOne
    }

    [ProtoContract(UseProtoMembersOnly = true)]
    public class PacketGameState
    {
        [ProtoMember(1)]
        public PacketType packetType;
        [ProtoMember(2)]
        public List<long> entityId;
        [ProtoMember(3)]
        public List<Vector3D> position;
        [ProtoMember(4)]
        public List<Vector3D> velocity;
        [ProtoMember(5)]
        public List<Vector3D> gravity;

        public PacketGameState() { }

        public PacketGameState(List<long> entityId, List<Vector3D> position, List<Vector3D> velocity, List<Vector3D> gravity)
        {
            this.packetType = PacketType.RefreshAll;
            this.entityId = entityId;
            this.position = position;
            this.velocity = velocity;
            this.gravity = gravity;
        }

        public PacketGameState(long entityId, Vector3D position, Vector3D velocity, Vector3D gravity)
        {
            this.packetType = PacketType.AddOne;
            this.entityId = new List<long>() { entityId };
            this.position = new List<Vector3D>() { position };
            this.velocity = new List<Vector3D>() { velocity };
            this.gravity = new List<Vector3D>() { gravity };
        }

        public void Received()
        {
            //MyAPIGateway.Utilities.ShowNotification($"RECEIVED PACKET: {packetType}", 6000);

            if (packetType == PacketType.RefreshAll)
            {
                NuclearWarheadsCore.Instance.missileData.Clear();
                for (int i = 0; i < entityId.Count; ++i)
                {
                    var ent = MyAPIGateway.Entities.GetEntityById(entityId[i]);
                    if (ent != null)
                    {
                        //MyAPIGateway.Utilities.ShowNotification($"RECEIVED MISSILE: {entityId[i]}", 2000);
                        NuclearWarheadsCore.Instance.missileData.Add(new NuclearWarheadsCore.NuclearMissileInfo(ent, position[i], velocity[i], gravity[i]));
                    }
                }
            }
            else if (entityId.Count > 0)
            {
                var ent = MyAPIGateway.Entities.GetEntityById(entityId[0]);
                if (ent != null)
                {
                    NuclearWarheadsCore.Instance.missileData.Add(new NuclearWarheadsCore.NuclearMissileInfo(ent, position[0], velocity[0], gravity[0]));
                }
            }
        }
    }
}