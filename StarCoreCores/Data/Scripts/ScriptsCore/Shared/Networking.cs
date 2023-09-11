using ProtoBuf;
using Sandbox.ModAPI;
using System;
using Digi;
using MIG.SpecCores;

namespace MIG.Shared.SE
{
    [ProtoContract]
    public class EntitySync
    {

        [ProtoMember(1)] public long entityId = -1;
        [ProtoMember(2)] public byte type = 254; //254 - request, 255 - send
        [ProtoMember(3)] public byte[] data = null;
        public EntitySync() { }
        public EntitySync(long entityId, byte type, byte[] data)
        {
            this.entityId = entityId;
            this.type = type;
            this.data = data;
        }
    }

    [ProtoContract]
    public class NonEntitySync
    {
        [ProtoMember(1)] public byte type = 0; //0 - request, 1 - send
        [ProtoMember(2)] public byte[] data = null;
        public NonEntitySync() { }
        public NonEntitySync(byte type, byte[] data)
        {
            this.type = type;
            this.data = data;
        }
    }

    public class Connection<T>
    {
        private ushort port;
        private Action<T, ulong, bool> handler;
        public Connection(ushort port, Action<T, ulong, bool> handler)
        {
            this.port = port;
            this.handler = handler;
            MyAPIGateway.Multiplayer.RegisterSecureMessageHandler(port, Handler);
            GameBase.AddUnloadAction(() => { MyAPIGateway.Multiplayer.UnregisterSecureMessageHandler(port, Handler); });
        }

        public void SendMessageToServer(T data, bool reliable = true)
        {
            //Log.ChatError ("SendMessageToServer");
            var bdata = MyAPIGateway.Utilities.SerializeToBinary<T>(data);
            MyAPIGateway.Multiplayer.SendMessageToServer(port, bdata, reliable);
        }

        public void SendMessageToOthers(T data, bool reliable = true)
        {
            var bdata = MyAPIGateway.Utilities.SerializeToBinary<T>(data);
            MyAPIGateway.Multiplayer.SendMessageToOthers(port, bdata, reliable);
        }

        public void SendMessageTo(T data, ulong SteamID, bool reliable = true)
        {
            var bdata = MyAPIGateway.Utilities.SerializeToBinary<T>(data);
            MyAPIGateway.Multiplayer.SendMessageTo(port, bdata, SteamID, reliable);
        }

        public void Handler(ushort HandlerId, byte[] bdata, ulong PlayerSteamId, bool isFromServer)
        {
            try
            {
                var data = MyAPIGateway.Utilities.SerializeFromBinary<T>(bdata);
                handler(data, PlayerSteamId, isFromServer);
            } catch (Exception e)
            {
                Log.ChatError("Handle message: " + HandlerId + " " + PlayerSteamId + " " + e);
            }
            
        }
    }

    public class Sync<T, Z>
    {
        public const byte REQUEST = 254;
        public const byte SEND = 255;

        private ushort port;
        private Action<Z, T, byte, ulong, bool> handler;
        private Action<T, byte, ulong, bool> handler2;
        private Func<Z, T> getter;
        private Func<long, Z> entityLogicGetter = null;

        public Sync(ushort port, Func<Z, T> getter, Action<Z, T, ulong, bool> _handler, Action<T, ulong, bool> _handler2 = null, Func<long, Z> entityLogicGetter = null) 
        {
            this.port = port;
            this.entityLogicGetter = entityLogicGetter ?? this.entityLogicGetter;
            this.handler = (z, t, b, u, boo) => _handler(z, t, u, boo);
            if (_handler2 != null)
            {
                this.handler2 = (t, b, u, boo) => _handler2(t, u, boo);
            }
            this.getter = getter;
            MyAPIGateway.Multiplayer.RegisterSecureMessageHandler(port, Handler);
            GameBase.AddUnloadAction(() => { MyAPIGateway.Multiplayer.UnregisterSecureMessageHandler(port, Handler); });
        }

        public Sync(ushort port, Func<Z, T> getter, Action<Z, T, byte, ulong, bool> handler, Action<T, byte, ulong, bool> handler2 = null, Func<long, Z> entityLogicGetter = null)
        {
            this.entityLogicGetter = entityLogicGetter ?? this.entityLogicGetter;
            this.port = port;
            this.handler = handler;
            this.handler2 = handler2;
            this.getter = getter;
            MyAPIGateway.Multiplayer.RegisterSecureMessageHandler(port, Handler);
            GameBase.AddUnloadAction(() => { MyAPIGateway.Multiplayer.UnregisterSecureMessageHandler(port, Handler); });
        }

        public void Handler(ushort HandlerId, byte[] bdata, ulong PlayerSteamId, bool isFromServer)
        {
            try
            {
                var data = MyAPIGateway.Utilities.SerializeFromBinary<EntitySync>(bdata);
                if (data == null) {
                    return;
                }
                if (data.type == REQUEST)
                {
                    var z = entityLogicGetter(data.entityId);
                    if (z != null)
                    {
                        var t = getter.Invoke(z);
                        SendMessageToOthers(data.entityId, t, true);
                    }

                    return;
                }

                if (data.type != REQUEST)
                {
                    var z = entityLogicGetter(data.entityId);
                    if (z != null)
                    {
                        var dataz = MyAPIGateway.Utilities.SerializeFromBinary<T>(data.data);
                        try
                        {
                            handler.Invoke(z, dataz, data.type, PlayerSteamId, isFromServer);
                        } 
                        catch (Exception e)
                        {
                            Log.ChatError($"Sync error! HandlerId:[{HandlerId}] isFromServer[{isFromServer}] PlayerSteamId:[{PlayerSteamId}] ex:{e}");
                        }
                    }
                    else
                    {
                        if (handler2 != null)
                        {
                            var dataz = MyAPIGateway.Utilities.SerializeFromBinary<T>(data.data);
                            handler2.Invoke(dataz, data.type, PlayerSteamId, isFromServer);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Error($"Sync Handler Error! HandlerId:[{HandlerId}] isFromServer[{isFromServer}] PlayerSteamId:[{PlayerSteamId}] ex:{ex}");
                throw;
            }
        }

        public void SendMessageToServer(long entityId, T data, bool reliable = true, byte type = SEND)
        {
            var bdata = MyAPIGateway.Utilities.SerializeToBinary(new EntitySync(entityId, type, MyAPIGateway.Utilities.SerializeToBinary<T>(data)));
            MyAPIGateway.Multiplayer.SendMessageToServer(port, bdata, reliable);
        }

        public void SendMessageToOthers(long entityId, T data, bool reliable = true, byte type = SEND)
        {
            var bdata = MyAPIGateway.Utilities.SerializeToBinary(new EntitySync(entityId, type, MyAPIGateway.Utilities.SerializeToBinary<T>(data)));
            MyAPIGateway.Multiplayer.SendMessageToOthers(port, bdata, reliable);
        }

        public void SendMessageTo(long entityId, T data, ulong SteamID, bool reliable = true, byte type = SEND)
        {
            var bdata = MyAPIGateway.Utilities.SerializeToBinary(new EntitySync(entityId, type, MyAPIGateway.Utilities.SerializeToBinary<T>(data)));
            MyAPIGateway.Multiplayer.SendMessageTo(port, bdata, SteamID, reliable);
        }

        public void RequestData(long entityId, bool reliable = true)
        {
            var bdata = MyAPIGateway.Utilities.SerializeToBinary(new EntitySync(entityId, REQUEST, null));
            MyAPIGateway.Multiplayer.SendMessageToServer(port, bdata, reliable);
        }
    }


    public class StaticSync<T>
    {
        public const byte REQUEST = 254;
        public const byte SEND = 255;

        private ushort port;
        private Action<T, byte, ulong, bool> handler;
        private Func<T> getter;

        public StaticSync(ushort port, Func<T> getter, Action<T, ulong, bool> _handler)
        {
            this.port = port;
            this.handler = (t, b, u, boo) => _handler(t, u, boo);
            this.getter = getter;
            MyAPIGateway.Multiplayer.RegisterSecureMessageHandler(port, Handler);
            GameBase.AddUnloadAction(() => { MyAPIGateway.Multiplayer.UnregisterSecureMessageHandler(port, Handler); });
        }

        public StaticSync(ushort port, Func<T> getter, Action<T, byte, ulong, bool> handler)
        {
            this.port = port;
            this.handler = handler;
            this.getter = getter;
            MyAPIGateway.Multiplayer.RegisterSecureMessageHandler(port, Handler);
            GameBase.AddUnloadAction(() => { MyAPIGateway.Multiplayer.UnregisterSecureMessageHandler(port, Handler); });
        }

        public void Handler(ushort HandlerId, byte[] bdata, ulong PlayerSteamId, bool isFromServer)
        {
            try
            {
                var data = MyAPIGateway.Utilities.SerializeFromBinary<NonEntitySync>(bdata);
                if (data.type == REQUEST)
                {
                    var t = getter.Invoke();
                    SendMessageToOthers(t, true);
                    return;
                }

                if (data.type != REQUEST)
                {
                    var dataz = MyAPIGateway.Utilities.SerializeFromBinary<T>(data.data);
                    handler.Invoke(dataz, data.type, PlayerSteamId, isFromServer);
                }
            }
            catch (Exception ex)
            {
                Log.Error($"Sync Handler Error! HandlerId:[{HandlerId}] isFromServer[{isFromServer}] ex:{ex} PlayerSteamId:[{PlayerSteamId}]");
                throw;
            }
        }

        public void SendMessageToServer(T data, bool reliable = true, byte type = SEND)
        {
            var bdata = MyAPIGateway.Utilities.SerializeToBinary(new NonEntitySync(type, MyAPIGateway.Utilities.SerializeToBinary<T>(data)));
            MyAPIGateway.Multiplayer.SendMessageToServer(port, bdata, reliable);
        }

        public void SendMessageToOthers(T data, bool reliable = true, byte type = SEND)
        {
            var bdata = MyAPIGateway.Utilities.SerializeToBinary(new NonEntitySync(type, MyAPIGateway.Utilities.SerializeToBinary<T>(data)));
            MyAPIGateway.Multiplayer.SendMessageToOthers(port, bdata, reliable);
        }

        public void SendMessageTo(T data, ulong SteamID, bool reliable = true, byte type = SEND)
        {
            var bdata = MyAPIGateway.Utilities.SerializeToBinary(new NonEntitySync(type, MyAPIGateway.Utilities.SerializeToBinary<T>(data)));
            MyAPIGateway.Multiplayer.SendMessageTo(port, bdata, SteamID, reliable);
        }

        public void RequestData(bool reliable = true)
        {
            var bdata = MyAPIGateway.Utilities.SerializeToBinary(new NonEntitySync(REQUEST, null));
            MyAPIGateway.Multiplayer.SendMessageToServer(port, bdata, reliable);
        }
    }
}
