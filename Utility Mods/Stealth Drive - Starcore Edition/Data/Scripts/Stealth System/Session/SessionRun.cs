using Sandbox.ModAPI;
using VRage.Game.Components;
using VRageMath;
using Sandbox.Game.Entities;
using Sandbox.Game;
using VRage.Game;

namespace StealthSystem
{
    [MySessionComponentDescriptor(MyUpdateOrder.AfterSimulation)]
    public partial class StealthSession : MySessionComponentBase
    {
        internal static int Tick;
        internal int TickMod15;
        internal int TickMod20;
        internal int TickMod60;
        internal bool Tick10;
        internal bool Tick20;
        internal bool Tick60;
        internal bool Tick120;
        internal bool Tick600;
        internal bool Tick3600;
        internal bool IsServer;
        internal bool IsClient;
        internal bool IsDedicated;
        internal bool PlayersLoaded;

        public override void LoadData()
        {
            IsServer = MyAPIGateway.Multiplayer.MultiplayerActive && MyAPIGateway.Session.IsServer;
            IsClient = MyAPIGateway.Multiplayer.MultiplayerActive && !MyAPIGateway.Session.IsServer;
            IsDedicated = MyAPIGateway.Utilities.IsDedicated;

            LargeBox = new BoundingBoxD(-_large, _large);
            SmallBox = new BoundingBoxD(-_small, _small);

            Logs.InitLogs();

            ModPath = ModContext.ModPath;
            ModCheck();

            //RemoveEdges();
            CreateTerminalControls<IMyUpgradeModule>();

            MyEntities.OnEntityCreate += OnEntityCreate;
            //MyEntities.OnEntityDelete += OnEntityDelete;
            MyEntities.OnCloseAll += OnCloseAll;
            MyAPIGateway.TerminalControls.CustomControlGetter += CustomControlGetter;
            MyAPIGateway.TerminalControls.CustomActionGetter += CustomActionGetter;
        }

        public override void BeforeStart()
        {
            if (IsClient)
                MyAPIGateway.Multiplayer.RegisterSecureMessageHandler(ClientPacketId, ProcessPacket);
            else if (IsServer)
            {
                MyAPIGateway.Multiplayer.RegisterSecureMessageHandler(ServerPacketId, ProcessPacket);
                MyVisualScriptLogicProvider.PlayerRespawnRequest += PlayerConnected;
            }

            if (!IsClient)
                MyAPIGateway.Session.DamageSystem.RegisterAfterDamageHandler(0, AfterDamageApplied);

            ConfigSettings = new Settings(this);

            APIServer.Load();

            if (WaterMod)
                WaterAPI.Register();
        }

        public override void UpdateAfterSimulation()
        {
            Tick++;

            TickMod15 = Tick % 15;
            TickMod20 = Tick % 20;
            TickMod60 = Tick % 60;

            Tick10 = Tick % 10 == 0;
            Tick20 = TickMod20 == 0;
            Tick60 = TickMod60 == 0;
            Tick120 = Tick % 120 == 0;
            Tick600 = Tick % 600 == 0;
            Tick3600 = Tick % 3600 == 0;

            if (!PlayersLoaded && IsServer && PlayerInit())
                PlayersLoaded = true;

            if (TrackWater && (Tick3600 || Tick60 && WaterMap.IsEmpty))
                UpdateWaters();

            if (Enforced && (!_startBlocks.IsEmpty || !_startGrids.IsEmpty))
                StartComps();

            CompLoop();
        }

        protected override void UnloadData()
        {
            if (IsClient)
                MyAPIGateway.Multiplayer.UnregisterSecureMessageHandler(ClientPacketId, ProcessPacket);
            else if (IsServer)
            {
                MyAPIGateway.Multiplayer.UnregisterSecureMessageHandler(ServerPacketId, ProcessPacket);
                MyVisualScriptLogicProvider.PlayerRespawnRequest -= PlayerConnected;
            }

            MyEntities.OnEntityCreate -= OnEntityCreate;
            MyEntities.OnCloseAll -= OnCloseAll;
            
            MyAPIGateway.TerminalControls.CustomControlGetter -= CustomControlGetter;
            MyAPIGateway.TerminalControls.CustomActionGetter -= CustomActionGetter;

            Logs.Close();
            APIServer.Unload();
            if (WaterMod)
                WaterAPI.Unregister();

            Clean();
        }

        public override MyObjectBuilder_SessionComponent GetObjectBuilder()
        {
            return base.GetObjectBuilder();
        }

    }
}
