using System.Collections.Generic;
using Sandbox.Game;
using Sandbox.ModAPI;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.ModAPI;
using VRageMath;
using Draygo.API.SC;
using VRage.Game.Entity;
using System;
using VRage.ModAPI;
using Sandbox.Game.Entities;
using VRage.Utils;
using BlendTypeEnum = VRageRender.MyBillboard.BlendTypeEnum;
using System.Linq;
using VRage.ObjectBuilders;
using VRage.Game.ModAPI.Ingame.Utilities;
using System.Text;
using Jnick_SCModRepository.StarCoreCTF.Data.Scripts.CTF;
using Jnick_SCModRepository.StarCoreCTF.Data.Scripts.CTF.Packets;

namespace Klime.CTF
{
    [MySessionComponentDescriptor(MyUpdateOrder.AfterSimulation)]
    public class CTF : MySessionComponentBase
    {
        private static readonly StringBuilder GripBracketConst = new StringBuilder("[       ]");
        private static readonly StringBuilder DMGResistConst = new StringBuilder("RESIST+");


        List<Flag> allflags = new List<Flag>();
        GameState gamestate;
        MyStringId square;
        MyStringId laser;
        List<IMyPlayer> allplayers = new List<IMyPlayer>();
        Dictionary<long, IMyPlayer> allplayerdict = new Dictionary<long, IMyPlayer>();
        List<IMyPlayer> reuse_players = new List<IMyPlayer>();
        List<BackgroundUIElement> background_elements = new List<BackgroundUIElement>();
        MatrixD reuse_matrix = MatrixD.Identity;
        PacketBase packet;
        string ModelPath = "";
        int timer = 0;

        public static CTF instance;
        public MyStringHash deformationType;



        Type reuse_type = typeof(MyObjectBuilder_EntityBase);
        MyIni ini = new MyIni();
        bool rdy = false;
        ushort netid = 45049;
        ushort eventnetid = 45823;

        //Single Factions
        IMyFaction faction1global;
        IMyFaction faction2global;
        public enum PacketOp
        {
            InitFromServer,
            RequestFromClient,
            UpdateFlags,
            UpdateGameState
        }

        public enum FlagState
        {
            Home,
            Active,
            Dropped
        }

        public enum CurrentGameState
        {
            None,
            Combat,
            Victory
        }

        public enum DropType
        {
            Instant,
            Ground,
            Floating,
            Attached
        }

        public enum InfoType
        {
            None,
            FlagCaptured,
            FlagDropped,
            FlagReset,
            FlagTaken
        }

        public enum BackgroundType
        {
            Billboard,
            Text
        }

        public enum FlagType
        {
            Single,
            Double
        }

        MyEntity3DSoundEmitter emitter;
        MySoundPair ctf_ping;
        HudAPIv2 HUD_Base;
        HudAPIv2.HUDMessage score_message;
        StringBuilder score_sb = new StringBuilder();
        HudAPIv2.HUDMessage event_message;
        StringBuilder event_sb = new StringBuilder();
        HudAPIv2.HUDMessage grip_strength_message;
        StringBuilder grip_strength_sb = new StringBuilder();
        int event_clock = 0;
        MyPlanet reuse_planet;
        List<MyEntity> reuse_Entities = new List<MyEntity>();
        EventInfo reuse_event;

        //Config
        Vector3D gamecenter = Vector3D.Zero;
        bool use_game_radius = false;
        double radius = 5000;
        int max_caps = 3;
        bool pickup_in_cockpit = false;
        bool drop_in_cockpit = false;
        DropType drop_type = DropType.Ground;
        int drop_reset_time = 300;
        double flagPickupRadius = 500;

        public class BackgroundUIElement
        {
            public HudAPIv2.BillBoardHUDMessage billboard;
            public HudAPIv2.HUDMessage text;
            public BackgroundType background_type;

            public BackgroundUIElement(HudAPIv2.BillBoardHUDMessage billboard)
            {
                this.billboard = billboard;
                this.text = null;
                background_type = BackgroundType.Billboard;
            }

            public BackgroundUIElement(HudAPIv2.HUDMessage text)
            {
                this.billboard = null;
                this.text = text;
                background_type = BackgroundType.Text;
            }
        }

        public override void Init(MyObjectBuilder_SessionComponent sessionComponent)
        {
            MyAPIGateway.Multiplayer.RegisterMessageHandler(netid, Data_Handler);
            MyAPIGateway.Multiplayer.RegisterMessageHandler(eventnetid, Event_Handler);

            if (!MyAPIGateway.Session.IsServer) return;
            instance = this;

            MyAPIGateway.Session.DamageSystem.RegisterBeforeDamageHandler(0, DamageHandler);

        }

        private void Event_Handler(byte[] obj)
        {
            if (!MyAPIGateway.Utilities.IsDedicated)
            {
                //reuse_event = NetworkDebug.DeserializeLogged<EventInfo>("EventInfo", obj);
                reuse_event = MyAPIGateway.Utilities.SerializeFromBinary<EventInfo>(obj);
                if (reuse_event != null)
                {
                    if (HUD_Base != null && HUD_Base.Heartbeat)
                    {
                        event_sb.Clear();
                        event_sb.Append(reuse_event.info);
                        event_clock = timer + 120;
                    }

                    if (MyAPIGateway.Session.Player?.Character != null)
                    {
                        if (emitter == null)
                        {
                            emitter = new MyEntity3DSoundEmitter((MyEntity)MyAPIGateway.Session.Player.Character);
                            ctf_ping = new MySoundPair("ctf_ping");
                        }
                        if (emitter != null)
                        {
                            ctf_ping.Init(reuse_event.infotype.ToString());
                            if (ctf_ping != null)
                            {
                                emitter.Entity = (MyEntity)MyAPIGateway.Session.Player.Character;
                                emitter.SetPosition(MyAPIGateway.Session.Camera.WorldMatrix.Translation);
                                emitter.PlaySound(ctf_ping, force2D: true);
                            }
                        }
                    }
                }
            }
        }

        private void Data_Handler(byte[] obj)
        {
            if (!MyAPIGateway.Session.IsServer)
            {
                //packet = NetworkDebug.DeserializeLogged<PacketBase>("GenericUpdate", obj);
                packet = MyAPIGateway.Utilities.SerializeFromBinary<PacketBase>(obj);

                if (packet != null)
                {
                    //MyLog.Default.WriteLine($"[CTF] Received Packet: {packet.packet_op}");
                    if (packet.packet_op == PacketOp.UpdateFlags)
                    {
                        //LogPacketDetails("UpdateFlags", packet.all_flags_packet.Count);  // Log additional details
                        if (allflags.Count == 0)
                        {
                            foreach (var subflag in packet.all_flags_packet)
                            {
                                subflag.flag_entity = PrimeEntityActivator();
                                subflag.flag_entity.EntityId = subflag.entity_id;

                                subflag.owning_faction = MyAPIGateway.Session.Factions.TryGetFactionById(subflag.owning_faction_id);
                                allflags.Add(subflag);
                            }
                        }

                        for (int i = 0; i < packet.all_flags_packet.Count; i++)
                        {
                            allflags[i].UpdateFromNetwork(packet.all_flags_packet[i]);
                        }
                    }

                    if (packet.packet_op == PacketOp.UpdateGameState)
                    {
                        gamestate = packet.gamestate_packet;
                        //LogPacketDetails("UpdateGameState", gamestate.ToString());  // Log additional details
                    }

                }
            }
        }

        private void LogPacketDetails(string operation, object details)
        {
            // Customize this method based on what details you want to log
            MyLog.Default.WriteLine($"[CTF] Operation: {operation}, Details: {details}");
        }

        private void AddBillboard(Color color, Vector3D pos, Vector3D left, Vector3D up, float scale, BlendTypeEnum blendType)
        {
            MyTransparentGeometry.AddBillboardOriented(laser, color.ToVector4(), pos, left, up, scale, blendType);
        }

        public void GetConfig()
        {
            try
            {
                if (MyAPIGateway.Session.IsServer)
                {
                    string save_file_name = "CTFCONFIG.txt";

                    if (MyAPIGateway.Utilities.FileExistsInWorldStorage(save_file_name, reuse_type))
                    {
                        var file = MyAPIGateway.Utilities.ReadFileInWorldStorage(save_file_name, reuse_type).ReadToEnd();
                        ini = new MyIni();
                        MyIniParseResult res;
                        if (ini.TryParse(file, out res))
                        {
                            bool is_single_flag = ini.Get("Single Flag", "bool").ToBoolean();
                            if (is_single_flag)
                            {
                                string single_flag_data_string = ini.Get("Single Flag Data", "string").ToString();
                                List<string> data = single_flag_data_string.Split('@').ToList();
                                Vector3D single_flag_homepostemp = Vector3D.Zero;
                                Vector3D capture_pos_reuse = Vector3D.Zero;
                                IMyFaction faction1;
                                IMyFaction faction2;
                                SerializableMatrix capture_pos_faction1;
                                SerializableMatrix capture_pos_faction2;
                                Dictionary<long, SerializableMatrix> capture_positions = new Dictionary<long, SerializableMatrix>();
                                List<string> single_flag_color_string = new List<string>();

                                ParseVector3DFromGPS(data[0], out single_flag_homepostemp);

                                faction1 = MyAPIGateway.Session.Factions.TryGetFactionByTag(data[1]);
                                faction2 = MyAPIGateway.Session.Factions.TryGetFactionByTag(data[2]);
                                ParseVector3DFromGPS(data[3], out capture_pos_reuse);
                                capture_pos_faction1 = GetHomePosition(capture_pos_reuse);
                                ParseVector3DFromGPS(data[4], out capture_pos_reuse);
                                capture_pos_faction2 = GetHomePosition(capture_pos_reuse);
                                capture_positions.Add(faction1.FactionId, capture_pos_faction1);
                                capture_positions.Add(faction2.FactionId, capture_pos_faction2);
                                single_flag_color_string = data[5].Split(',').ToList();
                                Color single_flag_color = new Color(int.Parse(single_flag_color_string[0]), int.Parse(single_flag_color_string[1]), int.Parse(single_flag_color_string[2]),
                                    int.Parse(single_flag_color_string[3]));

                                Flag single_flag = new Flag(PrimeEntityActivator().EntityId, FlagState.Home, single_flag_homepostemp, capture_positions, 0, single_flag_color,
                                    FlagType.Single, 100f, 0.2f, 0);

                                single_flag.Init();
                                allflags.Add(single_flag);
                            }
                            else
                            {
                                Vector3D flag_1_temp_pos = Vector3D.Zero;
                                Vector3D flag_2_temp_pos = Vector3D.Zero;
                                IMyFaction faction1;
                                IMyFaction faction2;
                                List<string> flag_1_color_string = new List<string>();
                                List<string> flag_2_color_string = new List<string>();

                                //Flag settings
                                ParseVector3DFromGPS(ini.Get("Flag Position Faction 1", "GPS").ToString(), out flag_1_temp_pos);
                                ParseVector3DFromGPS(ini.Get("Flag Position Faction 2", "GPS").ToString(), out flag_2_temp_pos);
                                faction1 = MyAPIGateway.Session.Factions.TryGetFactionByTag(ini.Get("Faction 1 Tag", "string").ToString());
                                faction2 = MyAPIGateway.Session.Factions.TryGetFactionByTag(ini.Get("Faction 2 Tag", "string").ToString());

                                flag_1_color_string = ini.Get("Faction 1 Color", "string").ToString().Split(',').ToList();
                                flag_2_color_string = ini.Get("Faction 2 Color", "string").ToString().Split(',').ToList();

                                Color flag_1_color = new Color(int.Parse(flag_1_color_string[0]), int.Parse(flag_1_color_string[1]), int.Parse(flag_1_color_string[2]),
                                    int.Parse(flag_1_color_string[3]));
                                Color flag_2_color = new Color(int.Parse(flag_2_color_string[0]), int.Parse(flag_2_color_string[1]), int.Parse(flag_2_color_string[2]),
                                    int.Parse(flag_2_color_string[3]));

                                Flag flag1 = new Flag(PrimeEntityActivator().EntityId, FlagState.Home, flag_1_temp_pos, faction1.FactionId, flag_1_color, FlagType.Double, 100, 0.2f, 0);
                                Flag flag2 = new Flag(PrimeEntityActivator().EntityId, FlagState.Home, flag_2_temp_pos, faction2.FactionId, flag_2_color, FlagType.Double, 100, 0.2f, 0);

                                flag1.Init();
                                flag2.Init();

                                allflags.Add(flag1);
                                allflags.Add(flag2);
                            }
                            //Game settings
                            use_game_radius = ini.Get("Use Game Radius", "bool").ToBoolean();
                            ParseVector3DFromGPS(ini.Get("Game Center", "GPS").ToString(), out gamecenter);
                            radius = ini.Get("Game Radius", "double").ToDouble();
                            max_caps = ini.Get("Max Caps", "int").ToInt32();
                            pickup_in_cockpit = ini.Get("Pickup in Cockpit", "bool").ToBoolean();
                            drop_in_cockpit = ini.Get("Drop in Cockpit", "bool").ToBoolean();
                            drop_type = (DropType)Enum.Parse(typeof(DropType), ini.Get("Drop Type", "Instant/Ground/Floating").ToString(), true);
                            drop_reset_time = ini.Get("Drop Reset Time", "int").ToInt32();
                            rdy = true;
                        }
                        else
                        {
                            MyVisualScriptLogicProvider.SendChatMessageColored("Incorrect or missing CTF config. Blank config generated", Color.Orange, "Server");
                            CreateBlankFile();
                            rdy = false;
                        }
                    }
                    else
                    {
                        MyVisualScriptLogicProvider.SendChatMessageColored("Incorrect or missing CTF config. Blank config generated", Color.Orange, "Server");
                        CreateBlankFile();
                        rdy = false;
                    }
                }
            }
            catch (Exception e)
            {
                MyVisualScriptLogicProvider.SendChatMessageColored("Incorrect or missing CTF config. Blank config generated\n" + e.Message, Color.Orange, "Server");
                CreateBlankFile();
                rdy = false;
            }
        }

        private void CreateBlankFile()
        {
            ini = new MyIni();
            ini.Set("Flag Position Faction 1", "GPS", "GPSHERE");
            ini.Set("Flag Position Faction 2", "GPS", "GPSHERE");
            ini.Set("Faction 1 Tag", "string", "TAG1");
            ini.Set("Faction 2 Tag", "string", "TAG2");
            ini.Set("Faction 1 Color", "string", "255,0,0,255");
            ini.Set("Faction 2 Color", "string", "0,0,255,255");
            ini.Set("Use Game Radius", "bool", false);
            ini.Set("Game Center", "GPS", "GPSHERE");
            ini.Set("Game Radius", "double", 5000);
            ini.Set("Max Caps", "int", 3);
            ini.Set("Pickup in Cockpit", "bool", false);
            ini.Set("Drop in Cockpit", "bool", false);
            ini.Set("Drop Type", "Instant/Ground/Floating", "Ground");
            ini.Set("Drop Reset Time", "int", 3000);
            ini.Set("Single Flag", "bool", false);
            ini.Set("Single Flag Data", "string", "ReplaceThis");
            ini.SetEndComment("DELETE THE _blank part of the filename to make it a valid config");
            var fullstring = ini.ToString();

            var writer = MyAPIGateway.Utilities.WriteFileInWorldStorage("CTFCONFIG_blank.txt", reuse_type);
            writer.Write(fullstring);
            writer.Close();
        }

        private SerializableMatrix GetHomePosition(Vector3D pos)
        {
            SerializableMatrix mat = MatrixD.Identity;

            // Set the flag's home position directly to the provided position
            mat = MatrixD.CreateWorld(pos, Vector3D.Forward, Vector3D.Up);

            return mat;
        }

        public override void BeforeStart()
        {
            ModelPath = ModContext.ModPath + @"\Models\flagpole.mwm";
            square = MyStringId.GetOrCompute("Square");
            laser = MyStringId.GetOrCompute("WeaponLaser");
            if (!MyAPIGateway.Utilities.IsDedicated)
            {
                if (HUD_Base == null)
                {
                    HUD_Base = new HudAPIv2(HUDLoaded);
                }
            }
        }

        private void HUDLoaded()
        {
            HudAPIv2.BillBoardHUDMessage score_billboard = new HudAPIv2.BillBoardHUDMessage();
            score_billboard.BillBoardColor = new Color(Color.White, 1);
            score_billboard.Material = MyStringId.GetOrCompute("ctf_score_background");
            score_billboard.Origin = new Vector2D(-0.33, 0.85);
            score_billboard.Scale *= 0.5f;
            score_billboard.Height *= 0.6f;
            score_billboard.Visible = true;
            score_billboard.Options |= HudAPIv2.Options.HideHud;
            score_billboard.Blend = BlendTypeEnum.AdditiveBottom;
            BackgroundUIElement element = new BackgroundUIElement(score_billboard);
            background_elements.Add(element);

            score_message = new HudAPIv2.HUDMessage();
            score_message.Blend = BlendTypeEnum.PostPP;
            score_message.InitialColor = Color.White;
            score_message.Message = score_sb;
            score_message.Visible = true;
            score_message.Origin = new Vector2D(-0.4, 0.97);
            score_message.Options |= HudAPIv2.Options.HideHud;
            score_message.Blend = BlendTypeEnum.PostPP;
            score_message.Scale = 1.5f;

            event_message = new HudAPIv2.HUDMessage();
            event_message.Blend = BlendTypeEnum.PostPP;
            event_message.Message = event_sb;
            event_message.Visible = true;
            event_message.Origin = new Vector2D(-0.18, 0.5);
            event_message.Options |= HudAPIv2.Options.HideHud;
            event_message.Scale = 2f;
            event_message.InitialColor = Color.DarkOrange;

            grip_strength_message = new HudAPIv2.HUDMessage();
            grip_strength_message.Blend = BlendTypeEnum.PostPP;
            grip_strength_message.Message = grip_strength_sb;
            grip_strength_message.Visible = true;
            grip_strength_message.Origin = new Vector2D(-0.18, -0.4);
            grip_strength_message.Options |= HudAPIv2.Options.HideHud;
            grip_strength_message.Scale = 2f;
            grip_strength_message.InitialColor = Color.DarkOrange;
        }

        public HashSet<long> damagedGrids = new HashSet<long>();
        Dictionary<long, int> playerDropTimes = new Dictionary<long, int>();


        private void DamageHandler(object target, ref MyDamageInformation info)
        {

            IMySlimBlock block = target as IMySlimBlock;

            if (block == null || block.CubeGrid == null || block.CubeGrid.WorldMatrix == null) return;


            var freshlydamaged_gridEntityId = block.CubeGrid.EntityId;

            foreach (var subflag in allflags)
            {
                if (subflag.state == FlagState.Active)
                {
                    IMyEntity controlledEntity = subflag.carrying_player.Controller != null ? subflag.carrying_player.Controller.ControlledEntity.Entity : null;
                    if (controlledEntity is IMyCockpit)
                    {
                        IMyCockpit cockpit = (IMyCockpit)controlledEntity;
                        long gridEntityId = cockpit.CubeGrid.EntityId;

                        if (gridEntityId == freshlydamaged_gridEntityId)
                        {
                            damagedGrids.Add(gridEntityId);
                            disableGripRegen = true;  // Set the flag to disable regeneration
                        }
                    }
                }
            }
        }

        float damageReductionCounter = 0.0f;
        bool disableGripRegen = false;  // Declare this member variable

        private string GenerateGripBar(float gripStrength, bool isActiveFlag)
        {
            int totalBars = 10;
            int filledBars = (int)Math.Round(gripStrength / 10f); // Assuming gripStrength is out of 100
            string gripBar = new string('|', filledBars);

            // If the flag is active, add brackets around the grip bar
            //if (isActiveFlag)
            //{
            //    gripBar = "[" + gripBar + "]";
            //}

            return gripBar;
        }


        private bool victoryTriggered = false;
        private int victoryTimer = 0;
        private const int SpawnInterval = 600; // 10 seconds * 60 updates per second
        private const int TotalVictoryDuration = 3600; // 60 seconds * 60 updates per second


        public override void UpdateAfterSimulation()
        {
            try
            {
                if (timer % 60 == 0)
                {
                    allplayers.Clear();
                    allplayerdict.Clear();
                    MyAPIGateway.Multiplayer.Players.GetPlayers(allplayers);

                    foreach (var player in allplayers)
                        allplayerdict.Add(player.IdentityId, player);

                    if (MyAPIGateway.Session.IsServer)
                        SendGameState();
                }
                if (MyAPIGateway.Session.IsServer)
                {
                    if (timer == 10)
                    {
                        GetConfig();
                        if (rdy)
                        {
                            gamestate = new GameState(CurrentGameState.Combat, allflags);
                        }
                    }

                    if (rdy)
                    {
                        if (gamestate.currentgamestate == CurrentGameState.Combat)
                        {
                            foreach (var val in gamestate.faction_scores.Keys)
                            {
                                if (gamestate.faction_scores[val] >= max_caps)
                                {
                                    gamestate.currentgamestate = CurrentGameState.Victory;
                                    gamestate.winning_tag = MyAPIGateway.Session.Factions.TryGetFactionById(val).Tag;
                                }
                            }

                            foreach (var subflag in allflags)
                            {

                                if (subflag.state == FlagState.Home)
                                {

                                    subflag.flag_entity.WorldMatrix = MatrixD.CreateWorld(subflag.homePos);


                                    foreach (var player in subflag.GetNearbyPlayers(ref allplayers, ref reuse_players, pickup_in_cockpit, flagPickupRadius))
                                    {
                                        IMyEntity controlledEntity = player.Controller != null ? player.Controller.ControlledEntity.Entity : null;
                                        if (pickup_in_cockpit && !(controlledEntity is IMyCockpit))
                                        {
                                            continue;
                                        }
                                        string faction_tag = MyVisualScriptLogicProvider.GetPlayersFactionTag(player.IdentityId);
                                        if (subflag.flag_type == FlagType.Single)
                                        {
                                            if (faction_tag != "")
                                            {
                                                IMyCockpit cockpit = (IMyCockpit)controlledEntity;
                                                long gridEntityId = cockpit.CubeGrid.EntityId;
                                                subflag.state = FlagState.Active;
                                                ShowANotificationPlease("flag set to active 1");
                                                subflag.carrying_player_id = player.IdentityId;
                                                subflag.carrying_player = player;
                                                SendEvent(player.DisplayName + " grabbed the flag!", InfoType.FlagTaken);
                                                MyVisualScriptLogicProvider.SetGridGeneralDamageModifier(cockpit.CubeGrid.Name, 0.5f);

                                            }
                                        }
                                        else
                                        {
                                            if (faction_tag != "" && faction_tag != subflag.owning_faction.Tag)
                                            {

                                                IMyCockpit cockpit = (IMyCockpit)controlledEntity;
                                                long gridEntityId = cockpit.CubeGrid.EntityId;
                                                subflag.state = FlagState.Active;
                                                ShowANotificationPlease("flag set to active 2");
                                                subflag.carrying_player_id = player.IdentityId;
                                                subflag.carrying_player = player;
                                                SendEvent(player.DisplayName + " stole " + subflag.owning_faction.Tag + " flag!", InfoType.FlagTaken);
                                                MyVisualScriptLogicProvider.SetGridGeneralDamageModifier(cockpit.CubeGrid.Name, 0.5f);

                                            }
                                        }
                                    }
                                }

                                if (subflag.state == FlagState.Active)
                                {

                                    //put any more IsActive logic here, if you put it last it wont get detected. this took forever to figure out.

                                    if (!allplayerdict.ContainsKey(subflag.carrying_player_id))
                                    {
                                        // Player has disconnected, drop the flag
                                        subflag.state = FlagState.Dropped;
                                        subflag.carrying_player_id = -1;
                                        subflag.carrying_player = null;
                                        subflag.grip_strength = 100; // Reset grip strength or any other necessary reset logic

                                        // Additional logic for dropping the flag, like sending notifications
                                        SendEvent("Flag dropped due to player disconnect!", InfoType.FlagDropped);

                                        //start the flag drop cooldown
                                        playerDropTimes[subflag.carrying_player.IdentityId] = timer;

                                    }


                                    IMyEntity controlledEntity = subflag.carrying_player.Controller != null ? subflag.carrying_player.Controller.ControlledEntity.Entity : null;
                                    if (pickup_in_cockpit && !(controlledEntity is IMyCockpit))
                                    {
                                        subflag.state = FlagState.Dropped;
                                        ShowANotificationPlease("flag state set to dropped 1");
                                        SendEvent(subflag.carrying_player.DisplayName + " dropped the flag due to leaving cockpit!", InfoType.FlagDropped);
                                        playerDropTimes[subflag.carrying_player.IdentityId] = timer;

                                        continue; // Skip the rest of the logic for this flag
                                    }
                                    if (controlledEntity is IMyCockpit)
                                    {
                                        IMyCockpit cockpit = (IMyCockpit)controlledEntity;
                                        long gridEntityId = cockpit.CubeGrid.EntityId;

                                        // Damage handling
                                        if (damagedGrids.Contains(gridEntityId))
                                        {
                                            if (damageReductionCounter < 10.0f)
                                            {
                                                float gripLoss = 1.0f;  // Loss per damage instance
                                                float newGripStrength = subflag.grip_strength - gripLoss;
                                                subflag.grip_strength = newGripStrength;

                                                damageReductionCounter += gripLoss;
                                            }

                                            damagedGrids.Remove(gridEntityId);
                                        }

                                        // Reset the damageReductionCounter every second (assuming this block runs every frame and there are 60 frames per second)
                                        if (timer % 60 == 0)
                                        {
                                            damageReductionCounter = 0.0f;
                                            disableGripRegen = false;  // Reset the flag
                                        }

                                        //var speenAcceleration = cockpit.CubeGrid.Physics.AngularAcceleration.Length();
                                        var linearAcceleration = cockpit.CubeGrid.Physics.LinearAcceleration.Length();
                                        var funpolice = cockpit.CubeGrid.Physics.LinearVelocity.Length();
                                        var totalAcceleration = /*speenAcceleration + */linearAcceleration;

                                        // Adjust grip strength regeneration based on acceleration
                                        float deltaV = totalAcceleration; //- subflag.lastTickAcceleration;
                                                                          //  subflag.lastTickAcceleration = totalAcceleration;

                                        float regenModifier = 0.2f - (deltaV / 100f); // 0.2 is the base regen rate, and we subtract a value based on acceleration

                                        if (deltaV >= 10 || funpolice >= 200) // If the deltaV is more than 1, adjust the regenModifier
                                        {
                                            subflag.regen_modifier = regenModifier;

                                        }
                                        else { subflag.regen_modifier = 0.25f; }


                                        var grip_temp = subflag.grip_strength;

                                        var regen_temp = subflag.regen_modifier;

                                        if (grip_temp >= 50)
                                        {
                                            MathHelper.Clamp(regen_temp, -49, 49);
                                        }

                                        if (!disableGripRegen)  // Halt grip regeneration if flag is set
                                        {
                                            subflag.grip_strength += regen_temp;
                                        }


                                        //give the flagbearer damage resistance to their grid!
                                        if (controlledEntity is IMyCockpit)
                                        {
                                            
                                            MyVisualScriptLogicProvider.SetGridGeneralDamageModifier(cockpit.CubeGrid.Name, 0.5f); // Applying 0.5x damage modifier
                                        }


                                        //    MathHelper.Smooth(regen_temp, subflag.grip_strength);

                                        if (subflag.grip_strength > 100) subflag.grip_strength = 100;
                                        // Cap grip strength to 100
                                        if (subflag.grip_strength < 0)
                                        {
                                            subflag.grip_strength = 0; //Cap grip strength to 0
                                            subflag.state = FlagState.Dropped;
                                            ShowANotificationPlease("flag dropped 2");
                                            SendEvent(subflag.carrying_player.DisplayName + " dropped " + subflag.owning_faction.Tag + " the flag!", InfoType.FlagDropped);
                                            MyVisualScriptLogicProvider.SetGridGeneralDamageModifier(cockpit.CubeGrid.Name, 1.0f); // Applying 0.5x damage modifier

                                            playerDropTimes[subflag.carrying_player.IdentityId] = timer;
                                        }
                                    }
                                    if (subflag.carrying_player != null && subflag.carrying_player.Character != null && !subflag.carrying_player.Character.IsDead)
                                    {
                                        // Existing logic for setting flag position
                                        reuse_matrix = subflag.carrying_player.Character.WorldMatrix;
                                        reuse_matrix.Translation += reuse_matrix.Backward * 0.4f + reuse_matrix.Up * 1.5f + reuse_matrix.Left * 0.25f;
                                        subflag.flag_entity.WorldMatrix = reuse_matrix;

                                        IMyCockpit cockpit = (IMyCockpit)controlledEntity;
                                        long gridEntityId = cockpit.CubeGrid.EntityId;

                                        // Check for cockpit and drop flag if conditions are met
                                        if (drop_in_cockpit && !pickup_in_cockpit)
                                        {
                                            if (controlledEntity is IMyCockpit)
                                            {
                                                subflag.state = FlagState.Dropped;
                                                ShowANotificationPlease("flag dropped 3");
                                                SendEvent(subflag.carrying_player.DisplayName + " dropped " + subflag.owning_faction.Tag + " flag!", InfoType.FlagDropped);
                                                MyVisualScriptLogicProvider.SetGridGeneralDamageModifier(cockpit.CubeGrid.Name, 1.0f); 

                                            }
                                        }


                                        if (use_game_radius)
                                        {
                                            if (Vector3D.Distance(gamecenter, subflag.flag_entity.WorldMatrix.Translation) >= radius)
                                            {
                                                subflag.carrying_player.Character.Kill();
                                                subflag.state = FlagState.Dropped;
                                                ShowANotificationPlease("flag dropped 4");
                                                SendEvent(subflag.carrying_player.DisplayName + " dropped " + subflag.owning_faction.Tag + " flag!", InfoType.FlagDropped);
                                                MyVisualScriptLogicProvider.SetGridGeneralDamageModifier(cockpit.CubeGrid.Name, 1.0f);

                                            }
                                        }

                                        if (subflag.flag_type == FlagType.Single)
                                        {
                                            IMyFaction carrying_faction = MyAPIGateway.Session.Factions.TryGetPlayerFaction(subflag.carrying_player.IdentityId);
                                            if (carrying_faction != null)
                                            {
                                                foreach (var faction in subflag.capture_positions.Keys)
                                                {
                                                    if (faction == carrying_faction.FactionId)
                                                    {
                                                        Vector3D capture_pos = ((MatrixD)subflag.capture_positions[faction]).Translation;

                                                        double distance = Vector3D.Distance(subflag.flag_entity.WorldMatrix.Translation, capture_pos);
                                                        bool valid_cap = false;

                                                        if (pickup_in_cockpit)
                                                        {
                                                            if (distance <= 250)
                                                            {
                                                                valid_cap = true;
                                                            }
                                                        }
                                                        else
                                                        {
                                                            if (distance <= 200)
                                                            {
                                                                valid_cap = true;
                                                            }
                                                        }

                                                        if (valid_cap)
                                                        {
                                                            subflag.state = FlagState.Home;
                                                            ShowANotificationPlease("flag home 1");
                                                            gamestate.UpdateScore(faction);
                                                            SendEvent(subflag.carrying_player.DisplayName + " captured the flag!", InfoType.FlagCaptured);
                                                        }
                                                    }
                                                }
                                            }
                                        }
                                        else
                                        {
                                            foreach (var otherflag in allflags)
                                            {
                                                if ((otherflag.flag_entity.EntityId != subflag.entity_id) && otherflag.state == FlagState.Home)
                                                {
                                                    double distance = Vector3D.Distance(subflag.flag_entity.WorldMatrix.Translation, otherflag.flag_entity.WorldMatrix.Translation);
                                                    bool valid_cap = false;

                                                    if (pickup_in_cockpit)
                                                    {
                                                        if (distance <= 250)
                                                        {
                                                            valid_cap = true;
                                                        }
                                                    }
                                                    else
                                                    {
                                                        if (distance <= 200)
                                                        {
                                                            valid_cap = true;
                                                        }
                                                    }

                                                    if (valid_cap)
                                                    {
                                                        subflag.state = FlagState.Home;
                                                        ShowANotificationPlease("flag home validcap");
                                                        otherflag.state = FlagState.Home;

                                                        gamestate.UpdateScore(otherflag.owning_faction.FactionId);
                                                        SendEvent(subflag.carrying_player.DisplayName + " captured " + subflag.owning_faction.Tag + " flag!", InfoType.FlagCaptured);
                                                    }
                                                }
                                            }
                                        }
                                    }
                                    else
                                    {
                                        IMyCockpit cockpit = (IMyCockpit)controlledEntity;
                                        long gridEntityId = cockpit.CubeGrid.EntityId;

                                        subflag.state = FlagState.Dropped;
                                        ShowANotificationPlease("flag dropped 4");
                                        if (subflag.flag_type == FlagType.Single)
                                        {
                                            SendEvent(subflag.carrying_player.DisplayName + " dropped the flag!", InfoType.FlagDropped);
                                            MyVisualScriptLogicProvider.SetGridGeneralDamageModifier(cockpit.CubeGrid.Name, 1.0f);

                                        }
                                        else
                                        {
                                            SendEvent(subflag.carrying_player.DisplayName + " dropped " + subflag.owning_faction.Tag + " flag!", InfoType.FlagDropped);
                                            MyVisualScriptLogicProvider.SetGridGeneralDamageModifier(cockpit.CubeGrid.Name, 1.0f);

                                        }
                                    }

                                    //if you enter a safezone, this is supposed to drop, but it also happens when damage is off. also it needs a cooldown. so ill just turn it off for now

                                    //if (subflag.state == FlagState.Active)
                                    //{
                                    //    if (!MySessionComponentSafeZones.IsActionAllowed(subflag.flag_entity.WorldMatrix.Translation, CastProhibit(MySessionComponentSafeZones.AllowedActions, 1)))
                                    //    {
                                    //        subflag.state = FlagState.Home;
                                    //        ShowANotificationPlease("flag home 3");
                                    //        if (subflag.flag_type == FlagType.Single)
                                    //        {
                                    //            SendEvent(subflag.carrying_player.DisplayName + " dropped the flag from entering a safezone!", InfoType.FlagDropped);
                                    //        }
                                    //        else
                                    //        {
                                    //            SendEvent(subflag.carrying_player.DisplayName + " dropped " + subflag.owning_faction.Tag + " flag!", InfoType.FlagDropped);
                                    //        }
                                    //    }
                                    //}



                                }

                                if (subflag.state == FlagState.Dropped)
                                {
                                    subflag.grip_strength = 100; //reset grip
                                    if (subflag.current_drop_life >= drop_reset_time)
                                    {
                                        subflag.current_drop_life = 0;
                                        subflag.state = FlagState.Home;
                                        ShowANotificationPlease("flag home 4");
                                        if (subflag.flag_type == FlagType.Single)
                                        {
                                            SendEvent("Flag reset", InfoType.FlagReset);
                                        }
                                        else
                                        {
                                            SendEvent(subflag.owning_faction.Tag + " flag reset", InfoType.FlagReset);
                                        }
                                    }
                                    else
                                    {
                                        if (drop_type == DropType.Instant)
                                        {
                                            subflag.state = FlagState.Home;
                                            ShowANotificationPlease("flag home 5");
                                            if (subflag.flag_type == FlagType.Single)
                                            {
                                                SendEvent("Flag reset", InfoType.FlagReset);
                                            }
                                            else
                                            {
                                                SendEvent(subflag.owning_faction.Tag + " flag reset", InfoType.FlagReset);
                                            }
                                        }

                                        if (drop_type == DropType.Ground)
                                        {
                                            reuse_planet = MyGamePruningStructure.GetClosestPlanet(subflag.flag_entity.WorldMatrix.Translation);
                                            if (reuse_planet != null)
                                            {
                                                reuse_matrix = subflag.flag_entity.WorldMatrix;
                                                reuse_matrix.Translation = reuse_planet.GetClosestSurfacePointGlobal(subflag.flag_entity.WorldMatrix.Translation);
                                                subflag.flag_entity.WorldMatrix = reuse_matrix;
                                            }
                                            else
                                            {
                                                drop_type = DropType.Floating;
                                            }
                                        }

                                        if (drop_type == DropType.Attached)
                                        {
                                            if (subflag.attachedGrid == null && subflag.current_drop_life == 0)
                                            {
                                                //Raycast down to find grid
                                                float interf = 0f;
                                                var gravityDir = Vector3D.Normalize(MyAPIGateway.Physics.CalculateNaturalGravityAt(subflag.flag_entity.WorldMatrix.Translation,
                                                    out interf));

                                                var start = subflag.flag_entity.WorldMatrix.Translation;
                                                var end = start + gravityDir * 5;

                                                List<IHitInfo> hits = new List<IHitInfo>();
                                                MyAPIGateway.Physics.CastRay(start, end, hits);

                                                foreach (var hit in hits)
                                                {
                                                    if (hit == null || hit.HitEntity == null) continue;

                                                    var testGrid = hit.HitEntity as MyCubeGrid;
                                                    if (testGrid != null && testGrid.Physics != null)
                                                    {
                                                        subflag.attachedGrid = testGrid;
                                                        break;
                                                    }
                                                }

                                                if (subflag.attachedGrid != null)
                                                {
                                                    subflag.attachedLocalMatrix = subflag.flag_entity.WorldMatrix * subflag.attachedGrid.PositionComp.WorldMatrixInvScaled;
                                                }
                                            }

                                            if (subflag.attachedGrid != null && !subflag.attachedGrid.MarkedForClose)
                                            {
                                                subflag.flag_entity.WorldMatrix = subflag.attachedLocalMatrix * subflag.attachedGrid.WorldMatrix;
                                            }
                                        }
                                    }

                                    subflag.current_drop_life += 1;

                                    foreach (var player in subflag.GetNearbyPlayers(ref allplayers, ref reuse_players, pickup_in_cockpit, flagPickupRadius))
                                    {
                                        int lastDropTime;
                                        if (playerDropTimes.TryGetValue(player.IdentityId, out lastDropTime))
                                        {
                                            if (timer - lastDropTime < 600)
                                            {
                                                continue;
                                            }
                                        }

                                        string faction_tag = MyVisualScriptLogicProvider.GetPlayersFactionTag(player.IdentityId);
                                        if (faction_tag == subflag.owning_faction.Tag) // Check if player belongs to the same faction as the flag
                                        {
                                            // Send the flag back home
                                            subflag.state = FlagState.Home;
                                            ShowANotificationPlease("flag sent back home");
                                            if (subflag.flag_type == FlagType.Single)
                                            {
                                                SendEvent("Flag sent back home", InfoType.FlagReset);
                                            }
                                            else
                                            {
                                                SendEvent(subflag.owning_faction.Tag + " flag sent back home", InfoType.FlagReset);
                                            }
                                            continue; // Skip further processing for this player
                                        }

                                        if (subflag.flag_type == FlagType.Single)
                                        {
                                            if (faction_tag != "")
                                            {
                                                subflag.state = FlagState.Active;
                                                ShowANotificationPlease("flag active 1");
                                                subflag.carrying_player_id = player.IdentityId;
                                                subflag.carrying_player = player;
                                                subflag.current_drop_life = 0;
                                                SendEvent(player.DisplayName + " grabbed the flag!", InfoType.FlagTaken);
                                                // MyVisualScriptLogicProvider.SetGridGeneralDamageModifier(cockpit.CubeGrid.Name, 0.5f);
                                            }
                                        }
                                        else
                                        {
                                            if (faction_tag != "" && faction_tag != subflag.owning_faction.Tag)
                                            {
                                                subflag.state = FlagState.Active;
                                                ShowANotificationPlease("flag active 2");
                                                subflag.carrying_player_id = player.IdentityId;
                                                subflag.carrying_player = player;
                                                subflag.current_drop_life = 0;
                                                SendEvent(player.DisplayName + " stole " + subflag.owning_faction.Tag + " flag!", InfoType.FlagTaken);
                                                // MyVisualScriptLogicProvider.SetGridGeneralDamageModifier(cockpit.CubeGrid.Name, 0.5f);
                                            }
                                        }
                                    }
                                }
                                subflag.current_pos = subflag.flag_entity.WorldMatrix.Translation;
                                Matrix3x3 rotationOnlyMatrix = subflag.flag_entity.WorldMatrix.Rotation;
                                Quaternion.CreateFromRotationMatrix(ref rotationOnlyMatrix, out subflag.current_rotation);
                                subflag.lifetime += 1;
                            }
                        }

                        if (gamestate.currentgamestate == CurrentGameState.Victory)
                        {
                            foreach (var subflag in allflags)
                            {
                                subflag.flag_entity.WorldMatrix = MatrixD.CreateWorld(subflag.homePos);
                            }
                        }

                        if (gamestate.currentgamestate == CurrentGameState.Victory)
                        {
                            if (!victoryTriggered)
                            {
                                victoryTriggered = true;
                                victoryTimer = 0;
                            }

                            if (victoryTimer % SpawnInterval == 0 && victoryTimer <= TotalVictoryDuration)
                            {
                                string winningFactionTag = gamestate.winning_tag;
                                IMyFaction winningFaction = MyAPIGateway.Session.Factions.TryGetFactionByTag(winningFactionTag);

                                // Determine number of prefabs to spawn (1 to 5)
                                int spawnCount = MyUtils.GetRandomInt(1, 6);

                                for (int i = 0; i < spawnCount; i++)
                                {
                                    // Generate a random offset
                                    Vector3D offset = new Vector3D(
                                        MyUtils.GetRandomDouble(-100, 100), // X-axis offset
                                        MyUtils.GetRandomDouble(-100, 100), // Y-axis offset
                                        MyUtils.GetRandomDouble(-100, 100)  // Z-axis offset
                                    );

                                    // Choose a prefab and calculate spawn location with offset
                                    string prefabName = "IT'S OVER"; // Replace with your desired prefab
                                    Vector3D baseSpawnPosition = new Vector3D(0, 7000, 0); // Base spawn position
                                    Vector3D spawnPosition = baseSpawnPosition + offset; // Apply offset to base position

                                    Vector3D direction = Vector3D.Forward;
                                    Vector3D up = Vector3D.Up;

                                    // Spawn the prefab
                                    List<IMyCubeGrid> resultList = new List<IMyCubeGrid>();
                                    IMyPrefabManager prefabManager = MyAPIGateway.PrefabManager;
                                    prefabManager.SpawnPrefab(resultList, prefabName, spawnPosition, direction, up, ownerId: winningFaction?.FounderId ?? 0, spawningOptions: SpawningOptions.None);

                                    // Set ownership of the spawned prefab to the winning faction
                                    foreach (IMyCubeGrid spawnedGrid in resultList)
                                    {
                                        spawnedGrid.ChangeGridOwnership(winningFaction?.FounderId ?? 0, MyOwnershipShareModeEnum.All);
                                    }
                                }
                            }

                            victoryTimer += 1;
                        }
                        else
                        {
                            victoryTriggered = false;
                            victoryTimer = 0;
                        }

                    }

                    if (packet == null)
                    {
                        packet = new PacketBase();
                    }

                    SendFlagState();
                }

            }
            catch (Exception e)
            {
                MyAPIGateway.Utilities.ShowMessage("", e.Message);
            }

            timer += 1;
        }

        private void SendFlagState()
        {
            packet.gamestate_packet = null;
            packet.all_flags_packet = allflags;
            packet.packet_op = PacketOp.UpdateFlags;
            //byte[] serialized = NetworkDebug.SerializeLogged(packet.packet_op.ToString(), packet);
            byte[] serialized = MyAPIGateway.Utilities.SerializeToBinary(packet);

            foreach (var player in allplayers)
            {
                MyAPIGateway.Multiplayer.SendMessageTo(netid, serialized, player.SteamUserId);
            }
        }

        private void SendGameState()
        {
            packet.all_flags_packet = null;
            packet.gamestate_packet = gamestate;
            packet.packet_op = PacketOp.UpdateGameState;
            //byte[] serialized = NetworkDebug.SerializeLogged(packet.packet_op.ToString(), packet);
            byte[] serialized = MyAPIGateway.Utilities.SerializeToBinary(packet);

            foreach (var player in allplayers)
            {
                MyAPIGateway.Multiplayer.SendMessageTo(netid, serialized, player.SteamUserId);
            }
        }

        private void ShowANotificationPlease(string message)
        {
            MyAPIGateway.Utilities.ShowNotification(message);
        }

        public T CastProhibit<T>(T ptr, object val) => (T)val;
        public override void Draw()
        {
            try
            {
                if (!MyAPIGateway.Utilities.IsDedicated)
                {
                    foreach (var subflag in allflags)
                    {
                        if (subflag.flag_entity != null)
                        {
                            if (subflag.state == FlagState.Active)
                            {
                                if (allplayerdict.ContainsKey(subflag.carrying_player_id))
                                {
                                    if (allplayerdict[subflag.carrying_player_id].Character != null && !allplayerdict[subflag.carrying_player_id].Character.IsDead)
                                    {
                                        reuse_matrix = allplayerdict[subflag.carrying_player_id].Character.WorldMatrix;
                                        reuse_matrix.Translation += reuse_matrix.Backward * 0.4f + reuse_matrix.Up * 1.5f + reuse_matrix.Left * 0.25f;

                                        subflag.flag_entity.WorldMatrix = reuse_matrix;
                                    }
                                }
                            }

                            MyTransparentGeometry.AddBillboardOriented(square, subflag.flag_color, subflag.flag_entity.WorldMatrix.Translation +
                                subflag.flag_entity.WorldMatrix.Up * 70f + subflag.flag_entity.WorldMatrix.Backward * 50f,
                                subflag.flag_entity.WorldMatrix.Forward, subflag.flag_entity.WorldMatrix.Up, 50f, 20f, Vector2.Zero, BlendTypeEnum.PostPP);

                            Vector4 beam_col = subflag.flag_color;
                            beam_col.W *= 0.2f;
                            reuse_matrix = subflag.flag_entity.WorldMatrix;

                            float interference = 0f;
                            var gravP = MyAPIGateway.Physics.CalculateNaturalGravityAt(reuse_matrix.Translation, out interference);
                            if (gravP.Length() == 0f)
                            {
                                gravP = MyAPIGateway.Physics.CalculateArtificialGravityAt(reuse_matrix.Translation, 0);
                            }
                            Vector3D beam_up = -1 * Vector3.Normalize(gravP);
                            Vector3D beam_forward = MyUtils.GetRandomPerpendicularVector(ref beam_up);
                            reuse_matrix = MatrixD.CreateWorld(reuse_matrix.Translation, beam_forward, beam_up);
                            reuse_matrix.Translation += subflag.flag_entity.WorldMatrix.Backward * 0.5f;

                            //float beam_radius = 0.5f + 0.1f * (float)Vector3D.Distance(MyAPIGateway.Session.Camera.WorldMatrix.Translation,reuse_matrix.Translation);
                            float beam_radius = 0.5f;
                            MySimpleObjectDraw.DrawTransparentCylinder(ref reuse_matrix, beam_radius, beam_radius, 100000f, ref beam_col, true, 25, 0.9f, laser);
                        }
                    }


                    if (grip_strength_message != null && gamestate != null)
                    {
                        var redFlag = allflags[0];
                        var blueFlag = allflags[1];

                        var redGripStrength = redFlag.grip_strength;
                        var blueGripStrength = blueFlag.grip_strength;

                        var redGripBar = GenerateGripBar(redGripStrength, redFlag.state == FlagState.Active);
                        var blueGripBar = GenerateGripBar(blueGripStrength, blueFlag.state == FlagState.Active);

                        // You will need to adjust these coordinates based on where the "RED" and "BLU" text are.
                        // For example, if "RED" is centered at X = 0.30, you might use X = 0.28 for the grip bar.
                        Vector2D bluePosition = new Vector2D(-0.33, 0.85); // Position directly below the "RED" text
                        Vector2D redPosition = new Vector2D(-0.43, 0.85); // Position directly below the "BLU" text

                        StringBuilder redGripSb = new StringBuilder();
                        redGripSb.Append(redGripBar);
                        var redGripMessage = new HudAPIv2.HUDMessage(redGripSb, redPosition, TimeToLive: 2, Scale: 2f, Blend: BlendTypeEnum.PostPP);
                        redGripMessage.InitialColor = Color.Red;

                        StringBuilder blueGripSb = new StringBuilder();
                        blueGripSb.Append(blueGripBar);
                        var blueGripMessage = new HudAPIv2.HUDMessage(blueGripSb, bluePosition, TimeToLive: 2, Scale: 2f, Blend: BlendTypeEnum.PostPP);
                        blueGripMessage.InitialColor = Color.Blue;

                        const double VerticalOffsetForDamageResistText = +0.03; // Adjust this value as needed

                        if (redFlag.state == FlagState.Active)
                        {
                            var redGripBracket = new HudAPIv2.HUDMessage(GripBracketConst, redPosition, TimeToLive: 2, Scale: 2f, Blend: BlendTypeEnum.PostPP);
                            redGripBracket.Origin -= redGripBracket.GetTextLength() * Vector2D.UnitX / 10;
                            redGripBracket.InitialColor = Color.Red;

                            Vector2D redDmgResistPosition = new Vector2D(redPosition.X, redPosition.Y + VerticalOffsetForDamageResistText);
                            var redGripDmgResistWarning = new HudAPIv2.HUDMessage(DMGResistConst, redDmgResistPosition, TimeToLive: 2, Scale: 1f, Blend: BlendTypeEnum.PostPP);
                            redGripDmgResistWarning.InitialColor = Color.Red;

                        }

                        if (blueFlag.state == FlagState.Active)
                        {
                            var blueGripBracket = new HudAPIv2.HUDMessage(GripBracketConst, bluePosition, TimeToLive: 2, Scale: 2f, Blend: BlendTypeEnum.PostPP);
                            blueGripBracket.Origin -= blueGripBracket.GetTextLength() * Vector2D.UnitX / 10;
                            blueGripBracket.InitialColor = Color.Blue;

                            Vector2D blueDmgResistPosition = new Vector2D(bluePosition.X, bluePosition.Y + VerticalOffsetForDamageResistText);
                            var bluGripDmgResistWarning = new HudAPIv2.HUDMessage(DMGResistConst, blueDmgResistPosition, TimeToLive: 2, Scale: 1f, Blend: BlendTypeEnum.PostPP);
                            bluGripDmgResistWarning.InitialColor = Color.Blue;

                        }
                    }



                    if (score_message != null && gamestate != null)
                    {
                        score_sb.Clear();
                        if (allflags.Count == 1)
                        {
                            if (faction1global == null || faction2global == null)
                            {
                                faction1global = MyAPIGateway.Session.Factions.TryGetFactionByTag(gamestate.ordered_faction_tags[0]);
                                faction2global = MyAPIGateway.Session.Factions.TryGetFactionByTag(gamestate.ordered_faction_tags[1]);
                            }
                            score_sb.Append("<color=red>" + gamestate.ordered_faction_tags[0] + "  " + "<color=0,50,255,255>" + gamestate.ordered_faction_tags[1] + "\n");
                            score_sb.Append(" " + "<color=yellow>  " + gamestate.faction_scores[faction1global.FactionId] + "       " + gamestate.faction_scores[faction2global.FactionId]);
                        }
                        else if (allflags.Count == 2)
                        {
                            score_sb.Append("<color=red>" + allflags[0].owning_faction.Tag + "  " + "<color=0,50,255,255>" + allflags[1].owning_faction.Tag + "\n");
                            score_sb.Append(" " + "<color=yellow>  " + gamestate.faction_scores[allflags[0].owning_faction.FactionId] + "       " + gamestate.faction_scores[allflags[1].owning_faction.FactionId]);
                        }

                        if (gamestate != null && gamestate.currentgamestate == CurrentGameState.Victory)
                        {
                            score_sb.Append("\n\n\n\n\n\n\n" + "<color=white>" + gamestate.winning_tag + " VICTORY!");
                        }
                    }
                    if (event_message != null)
                    {
                        if (timer == event_clock)
                        {
                            event_sb.Clear();
                        }
                    }
                }
            }
            catch (Exception)
            {

            }
        }

        public void SendEvent(string message, InfoType infotype)
        {
            if (reuse_event == null)
            {
                reuse_event = new EventInfo();
            }
            reuse_event.info = message;
            reuse_event.infotype = infotype;
            //byte[] serialized = NetworkDebug.SerializeLogged(infotype.ToString(), reuse_event);
            byte[] serialized = MyAPIGateway.Utilities.SerializeToBinary(reuse_event);

            foreach (var player in allplayers)
            {
                if (player.Character != null)
                {
                    MyAPIGateway.Multiplayer.SendMessageTo(eventnetid, serialized, player.SteamUserId);
                }
            }
        }

        private MyEntity PrimeEntityActivator()
        {
            var ent = new MyEntity();
            ent.Init(null, ModelPath, null, null, null);
            ent.Render.CastShadows = false;
            ent.IsPreview = true;
            ent.Save = false;
            ent.SyncFlag = false;
            ent.NeedsWorldMatrix = false;
            ent.Flags |= EntityFlags.IsNotGamePrunningStructureObject;
            MyEntities.Add(ent, true);
            return ent;
        }

        bool ParseVector3DFromGPS(string gps, out Vector3D vec)
        {
            vec = Vector3D.Zero;

            if (!gps.StartsWith("GPS:"))
            {
                return false;
            }

            string[] segments = gps.Split(':');

            if (segments.Length < 6)
            {
                return false;
            }

            if (!double.TryParse(segments[2], out vec.X) || !double.TryParse(segments[3], out vec.Y) || !double.TryParse(segments[4], out vec.Z))
            {
                return false;
            }

            return true;
        }

        protected override void UnloadData()
        {
            MyAPIGateway.Multiplayer.UnregisterMessageHandler(netid, Data_Handler);
            MyAPIGateway.Multiplayer.UnregisterMessageHandler(eventnetid, Event_Handler);
            if (HUD_Base != null)
            {
                HUD_Base.Unload();
            }

            instance = null;

        }
    }
}