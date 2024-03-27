using System.Collections.Generic;
using ProtoBuf;
using Sandbox.ModAPI;
using VRage.Game.Components;
using VRage.Utils;

namespace MoA_Fusion_Systems.Data.Scripts.ModularAssemblies
{
    [MySessionComponentDescriptor(MyUpdateOrder.AfterSimulation)]
    public class SyncMultipliers : MySessionComponentBase
    {
        private const int Channel = 8775;
        private static SyncMultipliers Instance;
        private readonly Dictionary<IMyReactor, float> mReactorList = new Dictionary<IMyReactor, float>();
        private readonly Dictionary<IMyThrust, float> mThrustList = new Dictionary<IMyThrust, float>();

        private bool needsUpdate;

        public override void LoadData()
        {
            Instance = this;
            if (!MyAPIGateway.Multiplayer.MultiplayerActive)
                return;

            MyAPIGateway.Multiplayer.RegisterSecureMessageHandler(Channel, HandleMessage);

            if (!MyAPIGateway.Session.IsServer)
                needsUpdate = true;
        }

        public override void UpdateAfterSimulation()
        {
            if (needsUpdate && MyAPIGateway.Session != null && MyAPIGateway.Multiplayer != null &&
                MyAPIGateway.Session.Player != null)
            {
                MyAPIGateway.Multiplayer.SendMessageToServer(Channel,
                    MyAPIGateway.Utilities.SerializeToBinary(new SerializableMultiplier(-1, 0, 0,
                        MyAPIGateway.Session.Player.SteamUserId)));
                needsUpdate = false;
            }
        }

        private void HandleMessage(ushort handlerId, byte[] package, ulong senderId, bool fromServer)
        {
            var sm = MyAPIGateway.Utilities.SerializeFromBinary<SerializableMultiplier>(package);
            if (sm == null)
                return;
            switch (sm.type)
            {
                case 0:
                    if (MyAPIGateway.Session.IsServer)
                        break;
                    var react = MyAPIGateway.Entities.GetEntityById(sm.entityid) as IMyReactor;
                    if (react != null)
                        ReactorOutput(react, sm.value);
                    else
                        needsUpdate = true;
                    break;
                case 1:
                    if (MyAPIGateway.Session.IsServer)
                        break;
                    var thrust = MyAPIGateway.Entities.GetEntityById(sm.entityid) as IMyThrust;
                    if (thrust != null)
                        ThrusterOutput(thrust, sm.value);
                    else
                        needsUpdate = true;
                    break;
                case -1:
                    if (!MyAPIGateway.Session.IsServer)
                        break;
                    foreach (var reactor in mReactorList)
                        MyAPIGateway.Multiplayer.SendMessageTo(Channel,
                            MyAPIGateway.Utilities.SerializeToBinary(
                                new SerializableMultiplier(0, reactor.Value, reactor.Key.EntityId)), sm.playerid);
                    foreach (var thruster in mThrustList)
                        MyAPIGateway.Multiplayer.SendMessageTo(Channel,
                            MyAPIGateway.Utilities.SerializeToBinary(
                                new SerializableMultiplier(1, thruster.Value, thruster.Key.EntityId)), sm.playerid);
                    break;
            }
        }

        protected override void UnloadData()
        {
            Instance = null;
            if (!MyAPIGateway.Multiplayer.MultiplayerActive)
                return;
            MyAPIGateway.Multiplayer.UnregisterSecureMessageHandler(Channel, HandleMessage);
        }

        public static void ReactorOutput(IMyReactor reactor, float output)
        {
            if (reactor.MaxOutput == output)
            {
                MyLog.Default.WriteLineAndConsole("Reactor NoSync: " + output);
                return;
            }

            if (MyAPIGateway.Session.IsServer)
            {
                MyAPIGateway.Multiplayer.SendMessageToOthers(Channel,
                    MyAPIGateway.Utilities.SerializeToBinary(new SerializableMultiplier(0, output, reactor.EntityId)));
                if (Instance.mReactorList.ContainsKey(reactor))
                {
                    Instance.mReactorList[reactor] = output;
                }
                else
                {
                    Instance.mReactorList.Add(reactor, output);
                    reactor.OnClose += ent => { Instance.mReactorList.Remove(reactor); };
                }

                MyLog.Default.WriteLineAndConsole("Reactor Sync: " + reactor.PowerOutputMultiplier);
            }

            reactor.PowerOutputMultiplier = output / (reactor.MaxOutput / reactor.PowerOutputMultiplier);
        }

        public static void ThrusterOutput(IMyThrust thrust, float output)
        {
            if (MyAPIGateway.Session.IsServer)
            {
                MyAPIGateway.Multiplayer.SendMessageToOthers(Channel,
                    MyAPIGateway.Utilities.SerializeToBinary(new SerializableMultiplier(0, output, thrust.EntityId)));
                if (Instance.mThrustList.ContainsKey(thrust))
                {
                    Instance.mThrustList[thrust] = output;
                }
                else
                {
                    Instance.mThrustList.Add(thrust, output);
                    thrust.OnClose += ent => { Instance.mThrustList.Remove(thrust); };
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
                this.type = type;
                this.value = value;
                this.entityid = entityid;
            }

            public SerializableMultiplier(int type, float value, long entityid, ulong playerid)
            {
                this.type = type;
                this.value = value;
                this.entityid = entityid;
                this.playerid = playerid;
            }

            [ProtoMember(1)] public int type { get; }
            [ProtoMember(2)] public float value { get; }
            [ProtoMember(3)] public long entityid { get; }
            [ProtoMember(4)] public ulong playerid { get; }
        }
    }
}