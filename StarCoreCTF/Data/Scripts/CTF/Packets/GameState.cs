using ProtoBuf;
using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Klime.CTF.CTF;
using VRage.Game.ModAPI;

namespace Jnick_SCModRepository.StarCoreCTF.Data.Scripts.CTF
{
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
                        faction_scores.Add(faction_id, 0);
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
}
