using System.Collections.Generic;
using Sandbox.Game;
using Sandbox.ModAPI;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.ModAPI;
using VRageMath;
using Draygo.API;
using VRage.Game.Entity;
using System;
using VRage.ModAPI;
using Sandbox.Game.Entities;
using VRage.Utils;
using BlendTypeEnum = VRageRender.MyBillboard.BlendTypeEnum;
using VRage.Game.Models;
using System.Linq;
using ProtoBuf;
using VRage.ObjectBuilders;
using VRage.Game.ModAPI.Ingame.Utilities;
using System.Text;

namespace Klime.CTF
{
    [MySessionComponentDescriptor(MyUpdateOrder.AfterSimulation)]
    public class CTF : MySessionComponentBase
    {
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


        [ProtoContract]
        public class PacketBase
        {
            [ProtoMember(200)]
            public PacketOp packet_op;

            [ProtoMember(201)]
            public List<Flag> all_flags_packet = new List<Flag>();

            [ProtoMember(202)]
            public GameState gamestate_packet;

            public PacketBase()
            {

            }
        }

        [ProtoContract]
        public class Flag
        {
            public MyEntity flag_entity;
            public IMyPlayer carrying_player;
            public IMyFaction owning_faction;

            [ProtoMember(1)]
            public long entity_id;

            [ProtoMember(2)]
            public FlagState state;

            [ProtoMember(3)]
            public int lifetime;

            [ProtoMember(4)]
            public long carrying_player_id = -1;

            [ProtoMember(5)]
            public SerializableMatrix current_matrix;

            [ProtoMember(6)]
            public long owning_faction_id;

            [ProtoMember(7)]
            public int current_drop_life = 0;

            [ProtoMember(8)]
            public SerializableMatrix home_matrix;

            [ProtoMember(9)]
            public Dictionary<long,SerializableMatrix> capture_positions = new Dictionary<long, SerializableMatrix>();

            [ProtoMember(10)]
            public Color flag_color;

            [ProtoMember(11)]
            public FlagType flag_type;

            [ProtoIgnore]
            public MyCubeGrid attachedGrid = null;

            [ProtoIgnore]
            public MatrixD attachedLocalMatrix = MatrixD.Identity;

            public Flag()
            {

            }

            //Single
            public Flag(long entity_id, FlagState state, SerializableMatrix home_matrix, Dictionary<long,SerializableMatrix> capture_positions,
                long owning_faction_id, Color flag_color, FlagType flag_type)
            {
                this.entity_id = entity_id;
                this.state = state;
                this.home_matrix = home_matrix;
                this.current_matrix = home_matrix;
                this.owning_faction_id = owning_faction_id;
                this.flag_color = flag_color;
                this.capture_positions = capture_positions;
                this.flag_type = flag_type;
            }

            //Double
            public Flag(long entity_id, FlagState state, SerializableMatrix home_matrix, long owning_faction_id, Color flag_color, FlagType flag_type)
            {
                this.entity_id = entity_id;
                this.state = state;
                this.home_matrix = home_matrix;
                this.current_matrix = home_matrix;
                this.owning_faction_id = owning_faction_id;
                this.flag_color = flag_color;
                this.flag_type = flag_type;
            }

            public void Init()
            {
                flag_entity = MyAPIGateway.Entities.GetEntityById(entity_id) as MyEntity;
                if (owning_faction_id != 0)
                {
                    owning_faction = MyAPIGateway.Session.Factions.TryGetFactionById(owning_faction_id);
                }
            }

            public List<IMyPlayer> GetNearbyPlayers(ref List<IMyPlayer> all_players, ref List<IMyPlayer> return_list, bool cockpit_allowed)
            {
                return_list.Clear();
                foreach (var player in all_players)
                {
                    if (player.Character != null && !player.Character.IsDead)
                    {
                        double distance = Vector3D.Distance(player.Character.WorldMatrix.Translation, flag_entity.WorldMatrix.Translation);
                        if (cockpit_allowed && distance <= 50)
                        {
                            return_list.Add(player);
                        }
                        else
                        {
                            if (player.Controller?.ControlledEntity?.Entity is IMyCharacter && distance <= 40)
                            {
                                return_list.Add(player);
                            }
                        }
                    }
                }
                return return_list;
            }

            public void UpdateFromNetwork(Flag incoming_flag)
            {
                if (this.flag_entity != null)
                {
                    this.flag_entity.WorldMatrix = incoming_flag.current_matrix;
                }
                this.state = incoming_flag.state;
                this.lifetime = incoming_flag.lifetime;
                this.carrying_player_id = incoming_flag.carrying_player_id;
                this.current_matrix = incoming_flag.current_matrix;
                this.owning_faction_id = incoming_flag.owning_faction_id;
                this.current_drop_life = incoming_flag.current_drop_life;
                this.home_matrix = incoming_flag.home_matrix;
                this.flag_color = incoming_flag.flag_color;
                this.capture_positions = incoming_flag.capture_positions;
                this.flag_type = incoming_flag.flag_type;
            }
        }

        [ProtoContract]
        public class SerializableMatrix
        {
            [ProtoMember(100)]
            public double M11;

            [ProtoMember(102)]
            public double M12;

            [ProtoMember(103)]
            public double M13;

            [ProtoMember(104)]
            public double M14;

            [ProtoMember(105)]
            public double M21;

            [ProtoMember(106)]
            public double M22;

            [ProtoMember(107)]
            public double M23;

            [ProtoMember(108)]
            public double M24;

            [ProtoMember(109)]
            public double M31;

            [ProtoMember(110)]
            public double M32;

            [ProtoMember(111)]
            public double M33;

            [ProtoMember(112)]
            public double M34;

            [ProtoMember(113)]
            public double M41;

            [ProtoMember(114)]
            public double M42;

            [ProtoMember(115)]
            public double M43;

            [ProtoMember(116)]
            public double M44;

            public SerializableMatrix()
            {

            }

            public SerializableMatrix(MatrixD matrix)
            {
                M11 = matrix.M11;
                M12 = matrix.M12;
                M13 = matrix.M13;
                M14 = matrix.M14;
                M21 = matrix.M21;
                M22 = matrix.M22;
                M23 = matrix.M23;
                M24 = matrix.M24;
                M31 = matrix.M31;
                M32 = matrix.M32;
                M33 = matrix.M33;
                M34 = matrix.M34;
                M41 = matrix.M41;
                M42 = matrix.M42;
                M43 = matrix.M43;
                M44 = matrix.M44;
            }

            public static implicit operator SerializableMatrix(MatrixD matrix)
            {
                return new SerializableMatrix(matrix);
            }

            public static implicit operator MatrixD(SerializableMatrix v)
            {
                if (v == null)
                    return new MatrixD();
                return new MatrixD(v.M11, v.M12, v.M13, v.M14, v.M21, v.M22, v.M23, v.M24, v.M31, v.M32, v.M33, v.M34, v.M41, v.M42, v.M43, v.M44);
            }
        }

        [ProtoContract]
        public class GameState
        {
            [ProtoMember(50)]
            public CurrentGameState currentgamestate;

            [ProtoMember(51)]
            public Dictionary<long, int> faction_scores = new Dictionary<long, int>();

            [ProtoMember(52)]
            public string winning_tag = "";

            [ProtoMember(53)]
            public List<string> ordered_faction_tags = new List<string>();
            
            public GameState()
            {

            }

            public GameState(CurrentGameState currentgamestate, List<Flag> currentflags)
            {
                this.currentgamestate = currentgamestate;
                foreach (var flag in currentflags)
                {
                    if (flag.flag_type == FlagType.Single)
                    {
                        foreach (var faction_id in flag.capture_positions.Keys)
                        {
                            faction_scores.Add(faction_id,0);
                            IMyFaction faction = MyAPIGateway.Session.Factions.TryGetFactionById(faction_id);
                            if (faction != null)
                            {
                                ordered_faction_tags.Add(faction.Tag);
                            }
                        }
                    }
                    else
                    {
                        if (!faction_scores.ContainsKey(flag.owning_faction.FactionId))
                        {
                            faction_scores.Add(flag.owning_faction.FactionId, 0);
                        }
                    }
                }
            }

            public void UpdateScore(long incoming_faction)
            {
                if (faction_scores.ContainsKey(incoming_faction))
                {
                    faction_scores[incoming_faction] += 1;
                }
            }
        }

        [ProtoContract]
        public class EventInfo
        {
            [ProtoMember(600)]
            public string info;
            [ProtoMember(601)]
            public InfoType infotype;

            public EventInfo()
            {

            }
        }

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
        }

        private void Event_Handler(byte[] obj)
        {
            if (!MyAPIGateway.Utilities.IsDedicated)
            {
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
                                emitter.PlaySound(ctf_ping,force2D:true);
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
                packet = MyAPIGateway.Utilities.SerializeFromBinary<PacketBase>(obj);
                if (packet != null)
                {
                    if (packet.packet_op == PacketOp.UpdateFlags)
                    {
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
                    }

                }
            }
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
                        if (ini.TryParse(file,out res))
                        {
                            bool is_single_flag = ini.Get("Single Flag","bool").ToBoolean();
                            if (is_single_flag)
                            {
                                string single_flag_data_string = ini.Get("Single Flag Data","string").ToString();
                                List<string> data = single_flag_data_string.Split('@').ToList();
                                Vector3D single_flag_homepostemp = Vector3D.Zero;
                                Vector3D capture_pos_reuse = Vector3D.Zero;
                                SerializableMatrix single_flag_homepos = MatrixD.Identity;
                                IMyFaction faction1;
                                IMyFaction faction2;
                                SerializableMatrix capture_pos_faction1;
                                SerializableMatrix capture_pos_faction2;
                                Dictionary<long,SerializableMatrix> capture_positions = new Dictionary<long, SerializableMatrix>();
                                List<string> single_flag_color_string = new List<string>();

                                ParseVector3DFromGPS(data[0],out single_flag_homepostemp);
                                single_flag_homepos = GetHomePosition(single_flag_homepostemp);

                                faction1 = MyAPIGateway.Session.Factions.TryGetFactionByTag(data[1]);
                                faction2 = MyAPIGateway.Session.Factions.TryGetFactionByTag(data[2]);
                                ParseVector3DFromGPS(data[3],out capture_pos_reuse);
                                capture_pos_faction1 = GetHomePosition(capture_pos_reuse);
                                ParseVector3DFromGPS(data[4],out capture_pos_reuse);
                                capture_pos_faction2 = GetHomePosition(capture_pos_reuse);
                                capture_positions.Add(faction1.FactionId,capture_pos_faction1);
                                capture_positions.Add(faction2.FactionId,capture_pos_faction2);
                                single_flag_color_string = data[5].Split(',').ToList();
                                Color single_flag_color = new Color(int.Parse(single_flag_color_string[0]),int.Parse(single_flag_color_string[1]),int.Parse(single_flag_color_string[2]),
                                    int.Parse(single_flag_color_string[3]));

                                Flag single_flag = new Flag(PrimeEntityActivator().EntityId,FlagState.Home,single_flag_homepos,capture_positions,0,single_flag_color,
                                    FlagType.Single);

                                single_flag.Init();
                                allflags.Add(single_flag);
                            }
                            else
                            {
                                Vector3D flag_1_temp_pos = Vector3D.Zero;
                                Vector3D flag_2_temp_pos = Vector3D.Zero;
                                SerializableMatrix flag_1_temp_homepos = MatrixD.Identity;
                                SerializableMatrix flag_2_temp_homepos = MatrixD.Identity;
                                IMyFaction faction1;
                                IMyFaction faction2;
                                List<string> flag_1_color_string = new List<string>();
                                List<string> flag_2_color_string = new List<string>();

                                //Flag settings
                                ParseVector3DFromGPS(ini.Get("Flag Position Faction 1", "GPS").ToString(), out flag_1_temp_pos);
                                ParseVector3DFromGPS(ini.Get("Flag Position Faction 2", "GPS").ToString(), out flag_2_temp_pos);
                                faction1 = MyAPIGateway.Session.Factions.TryGetFactionByTag(ini.Get("Faction 1 Tag", "string").ToString());
                                faction2 = MyAPIGateway.Session.Factions.TryGetFactionByTag(ini.Get("Faction 2 Tag", "string").ToString());

                                flag_1_temp_homepos = GetHomePosition(flag_1_temp_pos);
                                flag_2_temp_homepos = GetHomePosition(flag_2_temp_pos);

                                flag_1_color_string = ini.Get("Faction 1 Color","string").ToString().Split(',').ToList();
                                flag_2_color_string = ini.Get("Faction 2 Color","string").ToString().Split(',').ToList();

                                Color flag_1_color = new Color(int.Parse(flag_1_color_string[0]),int.Parse(flag_1_color_string[1]),int.Parse(flag_1_color_string[2]),
                                    int.Parse(flag_1_color_string[3]));
                                Color flag_2_color = new Color(int.Parse(flag_2_color_string[0]),int.Parse(flag_2_color_string[1]),int.Parse(flag_2_color_string[2]),
                                    int.Parse(flag_2_color_string[3]));

                                Flag flag1 = new Flag(PrimeEntityActivator().EntityId, FlagState.Home, flag_1_temp_homepos, faction1.FactionId,flag_1_color,FlagType.Double);
                                Flag flag2 = new Flag(PrimeEntityActivator().EntityId, FlagState.Home, flag_2_temp_homepos, faction2.FactionId,flag_2_color,FlagType.Double);

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
                            MyVisualScriptLogicProvider.SendChatMessageColored("Incorrect or missing CTF config. Blank config generated",Color.Orange,"Server");
                            CreateBlankFile();
                            rdy = false;
                        }
                    }
                    else
                    {
                        MyVisualScriptLogicProvider.SendChatMessageColored("Incorrect or missing CTF config. Blank config generated",Color.Orange,"Server");
                        CreateBlankFile();
                        rdy = false;
                    }
                }
            }
            catch (Exception e)
            {
                MyVisualScriptLogicProvider.SendChatMessageColored("Incorrect or missing CTF config. Blank config generated\n" + e.Message ,Color.Orange,"Server");
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
            ini.Set("Faction 1 Color","string","255,0,0,255");
            ini.Set("Faction 2 Color","string","0,0,255,255");
            ini.Set("Use Game Radius", "bool", false);
            ini.Set("Game Center", "GPS", "GPSHERE");
            ini.Set("Game Radius", "double", 5000);
            ini.Set("Max Caps", "int", 3);
            ini.Set("Pickup in Cockpit", "bool", false);
            ini.Set("Drop in Cockpit", "bool", false);
            ini.Set("Drop Type", "Instant/Ground/Floating", "Ground");
            ini.Set("Drop Reset Time", "int", 300);
            ini.Set("Single Flag","bool",false);
            ini.Set("Single Flag Data","string","ReplaceThis");
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
            score_billboard.BillBoardColor = new Color(Color.White,1);
            score_billboard.Material = MyStringId.GetOrCompute("ctf_score_background");
            score_billboard.Origin = new Vector2D(0.02, 0.82);
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
            score_message.Origin = new Vector2D(-0.055, 0.8);
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

        }

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
                    {
                        allplayerdict.Add(player.IdentityId, player);
                    }
                }
                if (MyAPIGateway.Session.IsServer)
                {
                    if (timer == 10)
                    {
                        GetConfig();
                        if (rdy)
                        {
                            gamestate = new GameState(CurrentGameState.Combat,allflags);
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
                                    subflag.flag_entity.WorldMatrix = subflag.home_matrix;
                                    foreach (var player in subflag.GetNearbyPlayers(ref allplayers, ref reuse_players,pickup_in_cockpit))
                                    {
                                        string faction_tag = MyVisualScriptLogicProvider.GetPlayersFactionTag(player.IdentityId);
                                        if (subflag.flag_type == FlagType.Single)
                                        {
                                            if (faction_tag != "")
                                            {
                                                subflag.state = FlagState.Active;
                                                subflag.carrying_player_id = player.IdentityId;
                                                subflag.carrying_player = player;
                                                SendEvent(player.DisplayName + " grabbed the flag!",InfoType.FlagTaken);
                                            }
                                        }
                                        else
                                        {
                                            if (faction_tag != "" && faction_tag != subflag.owning_faction.Tag)
                                            {
                                                subflag.state = FlagState.Active;
                                                subflag.carrying_player_id = player.IdentityId;
                                                subflag.carrying_player = player;
                                                SendEvent(player.DisplayName + " stole " + subflag.owning_faction.Tag + " flag!",InfoType.FlagTaken);
                                            }
                                        }
                                    }
                                }

                                if (subflag.state == FlagState.Active)
                                {
                                    if (subflag.carrying_player != null && subflag.carrying_player.Character != null && !subflag.carrying_player.Character.IsDead)
                                    {
                                        reuse_matrix = subflag.carrying_player.Character.WorldMatrix;
                                        reuse_matrix.Translation += reuse_matrix.Backward * 0.4f + reuse_matrix.Up * 1.5f + reuse_matrix.Left * 0.25f;
                                        subflag.flag_entity.WorldMatrix = reuse_matrix;

                                        if (drop_in_cockpit && !pickup_in_cockpit)
                                        {
                                            if (subflag.carrying_player.Controller?.ControlledEntity?.Entity is IMyCockpit)
                                            {
                                                subflag.state = FlagState.Dropped;
                                                SendEvent(subflag.carrying_player.DisplayName + " dropped " + subflag.owning_faction.Tag + " flag!", InfoType.FlagDropped);
                                            }
                                        }

                                        if (use_game_radius)
                                        {
                                            if (Vector3D.Distance(gamecenter,subflag.flag_entity.WorldMatrix.Translation) >= radius)
                                            {
                                                subflag.carrying_player.Character.Kill();
                                                subflag.state = FlagState.Dropped;
                                                SendEvent(subflag.carrying_player.DisplayName + " dropped " + subflag.owning_faction.Tag + " flag!", InfoType.FlagDropped);
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
                                                        MatrixD capture_matrix = subflag.capture_positions[faction];
                                                        Vector3D capture_pos = capture_matrix.Translation;

                                                        double distance = Vector3D.Distance(subflag.flag_entity.WorldMatrix.Translation,capture_pos);
                                                        bool valid_cap = false;

                                                        if (pickup_in_cockpit)
                                                        {
                                                            if (distance <= 50)
                                                            {
                                                                valid_cap = true;
                                                            }
                                                        }
                                                        else
                                                        {
                                                            if (distance <= 40)
                                                            {
                                                                valid_cap = true;
                                                            }
                                                        }

                                                        if (valid_cap)
                                                        {
                                                            subflag.state = FlagState.Home;
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
                                                        if (distance <= 50)
                                                        {
                                                            valid_cap = true;
                                                        }
                                                    }
                                                    else
                                                    {
                                                        if (distance <= 40)
                                                        {
                                                            valid_cap = true;
                                                        }
                                                    }

                                                    if (valid_cap)
                                                    {
                                                        subflag.state = FlagState.Home;
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
                                        subflag.state = FlagState.Dropped;
                                        if (subflag.flag_type == FlagType.Single)
                                        {
                                            SendEvent(subflag.carrying_player.DisplayName + " dropped the flag!",InfoType.FlagDropped);
                                        }
                                        else
                                        {
                                            SendEvent(subflag.carrying_player.DisplayName + " dropped " + subflag.owning_faction.Tag + " flag!", InfoType.FlagDropped);
                                        }
                                    }

                                    if (subflag.state == FlagState.Active)
                                    {
                                        if (!MySessionComponentSafeZones.IsActionAllowed(subflag.flag_entity.WorldMatrix.Translation, CastProhibit(MySessionComponentSafeZones.AllowedActions, 1)))
                                        {
                                            subflag.state = FlagState.Home;
                                            if (subflag.flag_type == FlagType.Single)
                                            {
                                                SendEvent(subflag.carrying_player.DisplayName + " dropped the flag!", InfoType.FlagDropped);
                                            }
                                            else
                                            {
                                                SendEvent(subflag.carrying_player.DisplayName + " dropped " + subflag.owning_faction.Tag + " flag!", InfoType.FlagDropped);
                                            }
                                        }
                                    }
                                }

                                if (subflag.state == FlagState.Dropped)
                                {
                                    if (subflag.current_drop_life >= drop_reset_time)
                                    {
                                        subflag.current_drop_life = 0;
                                        subflag.state = FlagState.Home;
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

                                    foreach (var player in subflag.GetNearbyPlayers(ref allplayers, ref reuse_players,pickup_in_cockpit))
                                    {
                                        string faction_tag = MyVisualScriptLogicProvider.GetPlayersFactionTag(player.IdentityId);
                                        if (subflag.flag_type == FlagType.Single)
                                        {
                                            if (faction_tag != "")
                                            {
                                                subflag.state = FlagState.Active;
                                                subflag.carrying_player_id = player.IdentityId;
                                                subflag.carrying_player = player;
                                                subflag.current_drop_life = 0;
                                                SendEvent(player.DisplayName + " grabbed the flag!",InfoType.FlagTaken);
                                            }
                                        }
                                        else
                                        {
                                            if (faction_tag != "" && faction_tag != subflag.owning_faction.Tag)
                                            {
                                                subflag.state = FlagState.Active;
                                                subflag.carrying_player_id = player.IdentityId;
                                                subflag.carrying_player = player;
                                                subflag.current_drop_life = 0;
                                                SendEvent(player.DisplayName + " stole " + subflag.owning_faction.Tag + " flag!",InfoType.FlagTaken);
                                            }
                                        }
                                    }
                                }
                                subflag.current_matrix = subflag.flag_entity.WorldMatrix;
                                subflag.lifetime += 1;
                            }
                        }

                        if (gamestate.currentgamestate == CurrentGameState.Victory)
                        {
                            foreach (var subflag in allflags)
                            {
                                subflag.flag_entity.WorldMatrix = subflag.home_matrix;
                            }
                        }
                    }

                    if (packet == null)
                    {
                        packet = new PacketBase();
                    }

                    packet.gamestate_packet = null;
                    packet.all_flags_packet = allflags;
                    packet.packet_op = PacketOp.UpdateFlags;

                    foreach (var player in allplayers)
                    {
                        MyAPIGateway.Multiplayer.SendMessageTo(netid, MyAPIGateway.Utilities.SerializeToBinary(packet), player.SteamUserId);
                    }

                    packet.all_flags_packet = null;
                    packet.gamestate_packet = gamestate;
                    packet.packet_op = PacketOp.UpdateGameState;

                    foreach (var player in allplayers)
                    {
                        MyAPIGateway.Multiplayer.SendMessageTo(netid, MyAPIGateway.Utilities.SerializeToBinary(packet), player.SteamUserId);
                    }
                }

            }
            catch (Exception e)
            {
                MyAPIGateway.Utilities.ShowMessage("", e.Message);
            }
            
            timer += 1;
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
                            score_sb.Append("\n\n" + "<color=white>" + gamestate.winning_tag + " VICTORY!");
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

            foreach (var player in allplayers)
            {
                if (player.Character != null)
                {
                    MyAPIGateway.Multiplayer.SendMessageTo(eventnetid, MyAPIGateway.Utilities.SerializeToBinary(reuse_event), player.SteamUserId);
                }
            }
        }

        private MyEntity PrimeEntityActivator()
        {
            var ent = new MyEntity();
            ent.Init(null, ModelPath, null , null, null);
            ent.Render.CastShadows = false;
            ent.IsPreview = true;
            ent.Save = false;
            ent.SyncFlag = false;
            ent.NeedsWorldMatrix = false;
            ent.Flags |= EntityFlags.IsNotGamePrunningStructureObject;
            MyEntities.Add(ent,true);
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
        }
    }
}