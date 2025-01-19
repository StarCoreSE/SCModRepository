using System;
using System.Collections.Generic;
using ProtoBuf;
using Sandbox.ModAPI;
using VRage.Game.Components;
using VRage.Game.ModAPI;

namespace Epstein_Fusion_DS
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

        private bool _needsUpdate = true;

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
                    MyAPIGateway.Utilities.SerializeToBinary(new SerializableMultiplier(float.MinValue, 0)));
                _needsUpdate = false;
            }

            if (_ticks % MaxUpdateRateTicks == 0) _updateLimiter.Clear();
        }

        private void HandleMessage(ushort handlerId, byte[] package, ulong senderId, bool fromServer)
        {
            var sm = MyAPIGateway.Utilities.SerializeFromBinary<SerializableMultiplier>(package);
            if (sm == null)
                return;

            if (sm.Value == float.MinValue)
            {
                if (!MyAPIGateway.Session.IsServer)
                    return;

                foreach (var reactor in _mReactorList)
                    MyAPIGateway.Multiplayer.SendMessageTo(Channel,
                        MyAPIGateway.Utilities.SerializeToBinary(
                            new SerializableMultiplier(reactor.Value, reactor.Key.EntityId)), senderId);

                foreach (var thruster in _mThrustList)
                    MyAPIGateway.Multiplayer.SendMessageTo(Channel,
                        MyAPIGateway.Utilities.SerializeToBinary(
                            new SerializableMultiplier(thruster.Value, thruster.Key.EntityId)), senderId);
            }

            if (MyAPIGateway.Session.IsServer)
                return;

            var ent = MyAPIGateway.Entities.GetEntityById(sm.Entityid);
            if (ent == null)
                return;

            if (ent is IMyReactor)
                ReactorOutput((IMyReactor) ent, sm.Value);
            else if (ent is IMyThrust)
                ThrusterOutput((IMyThrust) ent, sm.Value);
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
            if (reactor == null || _i == null)
                return;

            if (Math.Abs(reactor.MaxOutput - output) < 0.1f || !_i._updateLimiter.Add(reactor))
                return;

            if (MyAPIGateway.Session.IsServer)
            {
                MyAPIGateway.Multiplayer.SendMessageToOthers(Channel,
                    MyAPIGateway.Utilities.SerializeToBinary(new SerializableMultiplier(output, reactor.EntityId)));
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
            if (thrust == null || _i == null)
                return;

            if (Math.Abs(thrust.MaxThrust - output) < 1.0f || !_i._updateLimiter.Add(thrust))
                return;

            if (MyAPIGateway.Session.IsServer)
            {
                MyAPIGateway.Multiplayer.SendMessageToOthers(Channel,
                    MyAPIGateway.Utilities.SerializeToBinary(new SerializableMultiplier(output, thrust.EntityId)));
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

            float val = output / (thrust.MaxThrust / thrust.ThrustMultiplier);
            thrust.ThrustMultiplier = val < 0.01f ? 0.01f : val;
        }

        [ProtoContract]
        private class SerializableMultiplier
        {
            public SerializableMultiplier()
            {
            }

            public SerializableMultiplier(float value, long entityid)
            {
                this.Value = value;
                this.Entityid = entityid;
            }

            [ProtoMember(1)] public float Value { get; }
            [ProtoMember(2)] public long Entityid { get; }
        }
    }
}