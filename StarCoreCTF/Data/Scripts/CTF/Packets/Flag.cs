using ProtoBuf;
using Sandbox.Game.Entities;
using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Klime.CTF.CTF;
using VRage.Game.Entity;
using VRage.Game.ModAPI;
using VRageMath;

namespace Jnick_SCModRepository.StarCoreCTF.Data.Scripts.CTF
{
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
        public Vector3D homePos;

        [ProtoMember(9)]
        public Dictionary<long, Vector3D> capture_positions = new Dictionary<long, Vector3D>(); // This seems to be the majority of network load - fix?

        [ProtoMember(10)]
        public Color flag_color;

        [ProtoMember(11)]
        public FlagType flag_type;

        [ProtoMember(12)]
        public float grip_strength = 100;

        [ProtoMember(13)]
        public float regen_modifier = 0.2f;

        [ProtoMember(14)]
        public float lastTickAcceleration;

        [ProtoIgnore]
        public MyCubeGrid attachedGrid = null;

        [ProtoIgnore]
        public MatrixD attachedLocalMatrix = MatrixD.Identity;

        public Flag()
        {

        }

        //Single
        public Flag(long entity_id, FlagState state, Vector3D homePos, Dictionary<long, SerializableMatrix> capture_positions,
            long owning_faction_id, Color flag_color, FlagType flag_type, float grip_strength, float regen_modifier, float lastTickAcceleration)
        {
            this.entity_id = entity_id;
            this.state = state;
            this.homePos = homePos;
            this.current_matrix = MatrixD.CreateWorld(homePos);
            this.owning_faction_id = owning_faction_id;
            this.flag_color = flag_color;

            this.capture_positions = new Dictionary<long, Vector3D>();
            foreach (var captureMatrixPair in capture_positions)
            {
                this.capture_positions.Add(captureMatrixPair.Key, ((MatrixD) captureMatrixPair.Value).Translation);
            }

            this.flag_type = flag_type;
            this.grip_strength = grip_strength;
            this.regen_modifier = regen_modifier;
            this.lastTickAcceleration = lastTickAcceleration;
        }

        //Double
        public Flag(long entity_id, FlagState state, Vector3D homePos, long owning_faction_id,
            Color flag_color, FlagType flag_type, float grip_strength, float regen_modifier, float lastTickAcceleration)
        {
            this.entity_id = entity_id;
            this.state = state;
            this.homePos = homePos;
            this.current_matrix = MatrixD.CreateWorld(homePos);
            this.owning_faction_id = owning_faction_id;
            this.flag_color = flag_color;
            this.flag_type = flag_type;
            this.grip_strength = grip_strength;
            this.regen_modifier = regen_modifier;
            this.lastTickAcceleration = lastTickAcceleration;
        }

        public void Init()
        {
            flag_entity = MyAPIGateway.Entities.GetEntityById(entity_id) as MyEntity;
            if (owning_faction_id != 0)
            {
                owning_faction = MyAPIGateway.Session.Factions.TryGetFactionById(owning_faction_id);
            }
        }



        public List<IMyPlayer> GetNearbyPlayers(ref List<IMyPlayer> all_players, ref List<IMyPlayer> return_list, bool cockpit_allowed, double flagPickupRadius)
        {
            return_list.Clear();
            foreach (var player in all_players)
            {
                if (player.Character != null && !player.Character.IsDead)
                {
                    double distance = Vector3D.Distance(player.Character.WorldMatrix.Translation, flag_entity.WorldMatrix.Translation);

                    if (cockpit_allowed && distance <= flagPickupRadius && player.Controller?.ControlledEntity?.Entity is IMyCockpit)              //distance <= [number] is the pickup radius
                    {
                        return_list.Add(player);
                    }

                    //disabled pickup from suit
                    // else
                    // {
                    //     if (player.Controller?.ControlledEntity?.Entity is IMyCharacter && distance <= 40)
                    //     {
                    //         return_list.Add(player);
                    //     }
                    // }
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
            this.homePos = incoming_flag.homePos;
            this.flag_color = incoming_flag.flag_color;
            this.capture_positions = incoming_flag.capture_positions;
            this.flag_type = incoming_flag.flag_type;
            this.grip_strength = incoming_flag.grip_strength;
            this.regen_modifier = incoming_flag.regen_modifier;
            this.lastTickAcceleration = incoming_flag.lastTickAcceleration;
        }
    }
}
