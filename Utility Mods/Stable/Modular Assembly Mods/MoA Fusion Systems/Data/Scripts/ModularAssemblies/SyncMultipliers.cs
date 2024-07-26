using System;
using System.Collections.Generic;
using ProtoBuf;
using Sandbox.ModAPI;
using VRage.Game.Components;
using VRage.Game.ModAPI;

namespace StarCore.FusionSystems
{
    [MySessionComponentDescriptor(MyUpdateOrder.AfterSimulation)]
    public class SyncMultipliers : MySessionComponentBase
    {
        private const int Channel = 8775;
        private const int MaxUpdateRateTicks = 10;

        private static SyncMultipliers _i;
        private readonly Dictionary<IMyReactor, float> _mReactorList = new Dictionary<IMyReactor, float>();
        private readonly Dictionary<IMyThrust, float> _mThrustList = new Dictionary<IMyThrust, float>();

        private readonly int _ticks = 0;
        private readonly HashSet<IMyCubeBlock> _updateLimiter = new HashSet<IMyCubeBlock>();

        private bool _needsUpdate;

        public override void LoadData()
        {
            _i = this;
            if (!MyAPIGateway.Multiplayer.MultiplayerActive)
                return;

            MyAPIGateway.Multiplayer.RegisterSecureMessageHandler(Channel, HandleMessage);

            if (!MyAPIGateway.Session.IsServer)
                _needsUpdate = true;
        }

        public override void UpdateAfterSimulation()
        {
            if (_needsUpdate && MyAPIGateway.Session != null && MyAPIGateway.Multiplayer != null &&
                MyAPIGateway.Session.Player != null)
            {
                MyAPIGateway.Multiplayer.SendMessageToServer(Channel,
                    MyAPIGateway.Utilities.SerializeToBinary(new SerializableMultiplier(-1, 0, 0,
                        MyAPIGateway.Session.Player.SteamUserId)));
                _needsUpdate = false;
            }

            if (_ticks % MaxUpdateRateTicks == 0) _updateLimiter.Clear();
        }

        private void HandleMessage(ushort handlerId, byte[] package, ulong senderId, bool fromServer)
        {
            var sm = MyAPIGateway.Utilities.SerializeFromBinary<SerializableMultiplier>(package);
            if (sm == null)
                return;
            switch (sm.Type)
            {
                case 0:
                    if (MyAPIGateway.Session.IsServer)
                        break;
                    var react = MyAPIGateway.Entities.GetEntityById(sm.Entityid) as IMyReactor;
                    if (react != null)
                        ReactorOutput(react, sm.Value);
                    else
                        _needsUpdate = true;
                    break;
                case 1:
                    if (MyAPIGateway.Session.IsServer)
                        break;
                    var thrust = MyAPIGateway.Entities.GetEntityById(sm.Entityid) as IMyThrust;
                    if (thrust != null)
                        ThrusterOutput(thrust, sm.Value);
                    else
                        _needsUpdate = true;
                    break;
                case -1:
                    if (!MyAPIGateway.Session.IsServer)
                        break;
                    foreach (var reactor in _mReactorList)
                        MyAPIGateway.Multiplayer.SendMessageTo(Channel,
                            MyAPIGateway.Utilities.SerializeToBinary(
                                new SerializableMultiplier(0, reactor.Value, reactor.Key.EntityId)), sm.Playerid);
                    foreach (var thruster in _mThrustList)
                        MyAPIGateway.Multiplayer.SendMessageTo(Channel,
                            MyAPIGateway.Utilities.SerializeToBinary(
                                new SerializableMultiplier(1, thruster.Value, thruster.Key.EntityId)), sm.Playerid);
                    break;
            }
        }

        protected override void UnloadData()
        {
            _i = null;
            if (!MyAPIGateway.Multiplayer.MultiplayerActive)
                return;
            MyAPIGateway.Multiplayer.UnregisterSecureMessageHandler(Channel, HandleMessage);
        }

        public static void ReactorOutput(IMyReactor reactor, float output)
        {
            if (Math.Abs(reactor.MaxOutput - output) < 0.1f || !_i._updateLimiter.Add(reactor))
                return;

            if (MyAPIGateway.Session.IsServer)
            {
                MyAPIGateway.Multiplayer.SendMessageToOthers(Channel,
                    MyAPIGateway.Utilities.SerializeToBinary(new SerializableMultiplier(0, output, reactor.EntityId)));
                if (_i._mReactorList.ContainsKey(reactor))
                {
                    _i._mReactorList[reactor] = output;
                }
                else
                {
                    _i._mReactorList.Add(reactor, output);
                    reactor.OnClose += ent => { _i._mReactorList.Remove(reactor); };
                }
            }

            reactor.PowerOutputMultiplier = output / (reactor.MaxOutput / reactor.PowerOutputMultiplier);
        }

        public static void ThrusterOutput(IMyThrust thrust, float output)
        {
            if (Math.Abs(thrust.MaxThrust - output) < 1.0f || !_i._updateLimiter.Add(thrust))
                return;

            if (MyAPIGateway.Session.IsServer)
            {
                MyAPIGateway.Multiplayer.SendMessageToOthers(Channel,
                    MyAPIGateway.Utilities.SerializeToBinary(new SerializableMultiplier(0, output, thrust.EntityId)));
                if (_i._mThrustList.ContainsKey(thrust))
                {
                    _i._mThrustList[thrust] = output;
                }
                else
                {
                    _i._mThrustList.Add(thrust, output);
                    thrust.OnClose += ent => { _i._mThrustList.Remove(thrust); };
                }
            }

            thrust.ThrustMultiplier = output / (thrust.MaxThrust / thrust.ThrustMultiplier);
        }

        [ProtoContract]
        private class SerializableMultiplier
        {
            public SerializableMultiplier()
            {
            }

            public SerializableMultiplier(int type, float value, long entityid)
            {
                this.Type = type;
                this.Value = value;
                this.Entityid = entityid;
            }

            public SerializableMultiplier(int type, float value, long entityid, ulong playerid)
            {
                this.Type = type;
                this.Value = value;
                this.Entityid = entityid;
                this.Playerid = playerid;
            }

            [ProtoMember(1)] public int Type { get; }
            [ProtoMember(2)] public float Value { get; }
            [ProtoMember(3)] public long Entityid { get; }
            [ProtoMember(4)] public ulong Playerid { get; }
        }
    }
}