using System;
using System.Collections.Generic;
using ProtoBuf;
using Sandbox.Game.World;
using Sandbox.ModAPI;
using SC.SUGMA.GameState;
using VRage.Game.ModAPI;

namespace SC.SUGMA.HeartNetworking.Custom
{
    internal class PointsPacket : PacketBase
    {
        [ProtoMember(1)] private string _senderObjectId;
        [ProtoMember(2)] private Dictionary<long, int> _points;

        public Dictionary<IMyFaction, int> FactionPoints
        {
            get
            {
                Dictionary<IMyFaction, int> toReturn = new Dictionary<IMyFaction, int>();
                foreach (var factionKvp in _points)
                {
                    IMyFaction faction = MyAPIGateway.Session.Factions.TryGetFactionById(factionKvp.Key);
                    if (faction == null)
                        throw new Exception("Failed to locate faction " + factionKvp.Key);
                    toReturn.Add(faction, factionKvp.Value);
                }

                return toReturn;
            }
        }

        private PointsPacket()
        {
        }

        public PointsPacket(PointTracker pointTracker)
        {
            _senderObjectId = pointTracker.Id;
            _points = new Dictionary<long, int>();
            foreach (var factionKvp in pointTracker.FactionPoints)
                _points.Add(factionKvp.Key.FactionId, factionKvp.Value);
        }

        public override void Received(ulong SenderSteamId)
        {
            SUGMA_SessionComponent.I.GetComponent<PointTracker>(_senderObjectId)?.UpdateFromPacket(this);
        }
    }
}
