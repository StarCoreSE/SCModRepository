using klime.PointCheck;
using ProtoBuf;
using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VRage.Game.Components;

namespace SCModRepository.Gamemode_Mods.Stable.Starcore_Sharetrack.Data.Scripts.ShipPoints.MatchTimer
{
    /// <summary>
    /// Keeps track of the match timer for networking and other mods.
    /// </summary>
    [MySessionComponentDescriptor(MyUpdateOrder.AfterSimulation)]
    internal class MatchTimer : MySessionComponentBase
    {
        public static MatchTimer I;
        /// <summary>
        /// How often the match timer should be synced, in ticks.
        /// </summary>
        public const int TimerUpdateInterval = 600;
        public const ushort NetworkId = 8576;
        public const long ModMessageId = 8573643466;

        public DateTime StartTime { get; internal set; } = DateTime.MinValue;
        public DateTime EndTime { get; internal set; } = DateTime.MinValue;
        public TimeSpan CurrentMatchTime => (DateTime.UtcNow < EndTime ? DateTime.UtcNow : EndTime) - StartTime;
        public bool IsMatchEnded = false;

        private double matchDurationMinutes = 20;

        public double MatchDurationMinutes
        {
            get
            {
                return matchDurationMinutes;
            }
            set
            {
                matchDurationMinutes = value;
                EndTime = StartTime.AddMinutes(matchDurationMinutes);
                MatchDurationString = $"{(matchDurationMinutes < 10 ? "0" : "")}{(int)matchDurationMinutes}:{Math.Round((matchDurationMinutes-(int)matchDurationMinutes)*60)}";
            }
        }

        public string MatchDurationString = "20:00";


        #region Overrides
        public override void LoadData()
        {
            I = this;
            MatchTimerPacket.RegisterToRecieve();
        }

        protected override void UnloadData()
        {
            I = null;
            MatchTimerPacket.Unregister();
        }

        int ticks = 0;
        public override void UpdateAfterSimulation()
        {
            ticks++;
            MyAPIGateway.Utilities.SendModMessage(ModMessageId, CurrentMatchTime);

            MyAPIGateway.Utilities.ShowNotification($"A: {DateTime.UtcNow > EndTime} B: {IsMatchEnded}", 1000/60);
            if (DateTime.UtcNow > EndTime && !IsMatchEnded)
            {
                PointCheck.EndMatch();
            }

            // Update every 10 seconds
            if (ticks % TimerUpdateInterval != 0)
                return;
            
            MatchTimerPacket.SendMatchUpdate(this);
        }
        #endregion

        #region Custom Methods

        /// <summary>
        /// Start with an optional match duration.
        /// </summary>
        /// <param name="matchDurationMinutes">Match length in minutes.</param>
        public void Start(double matchDurationMinutes = 20)
        {
            StartTime = DateTime.UtcNow;
            //MatchDurationMinutes = matchDurationMinutes;
            MatchDurationMinutes = 0.25;
            MatchTimerPacket.SendMatchUpdate(this);
            IsMatchEnded = false;
        }

        public void Stop()
        {
            EndTime = DateTime.UtcNow;
            IsMatchEnded = true;
        }

        public void UpdateFromPacket(MatchTimerPacket packet)
        {
            StartTime = packet.MatchStartTime;
            EndTime = packet.MatchEndTime;
        }

        #endregion
    }
}
