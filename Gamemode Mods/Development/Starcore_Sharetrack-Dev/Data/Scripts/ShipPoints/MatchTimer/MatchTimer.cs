using klime.PointCheck;
using ProtoBuf;
using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using VRage.Game.Components;
using VRage.Utils;

namespace SCModRepository.Gamemode_Mods.Stable.Starcore_Sharetrack.Data.Scripts.ShipPoints.MatchTimer
{
    /// <summary>
    /// Keeps track of the match timer for networking and other mods.
    /// </summary>
    [MySessionComponentDescriptor(MyUpdateOrder.AfterSimulation)]
    public class MatchTimer : MySessionComponentBase
    {
        public static MatchTimer I;
        /// <summary>
        /// How often the match timer should be synced, in ticks.
        /// </summary>
        public const int TimerUpdateInterval = 600;
        public const ushort NetworkId = 8576;
        public const long ModMessageId = 8573643466;
        /// <summary>
        /// The time at which the match started.
        /// </summary>
        public DateTime StartTime { get; internal set; } = DateTime.MinValue;
        /// <summary>
        /// The time at which the match will end.
        /// </summary>
        public DateTime EndTime { get; internal set; } = DateTime.MinValue;
        /// <summary>
        /// Current time in the match, offset from StartTime.
        /// </summary>
        public TimeSpan CurrentMatchTime => (DateTime.UtcNow < EndTime ? DateTime.UtcNow : EndTime) - StartTime;
        public bool IsMatchEnded = true;

        private double matchDurationMinutes = 20;

        /// <summary>
        /// Total length of the match, in fractional minutes.
        /// </summary>
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
                double seconds = Math.Round((matchDurationMinutes - (int)matchDurationMinutes) * 60);
                MatchDurationString = $"{(matchDurationMinutes < 10 ? "0" : "")}{(int)matchDurationMinutes}:{(seconds < 10 ? "0" : "")}{seconds}";
            }
        }

        /// <summary>
        /// Match duration, in format [mm:ss].
        /// </summary>
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
            //MyAPIGateway.Utilities.SendModMessage(ModMessageId, CurrentMatchTime);

            if (DateTime.UtcNow > EndTime && !IsMatchEnded && MyAPIGateway.Session.IsServer)
            {
                PointCheck.EndMatch();
                MyLog.Default.WriteLineAndConsole("[MatchTimer] Auto-Stopped Match. " + CurrentMatchTime.ToString());
            }

            // Update every 10 seconds if is server
            if (!MyAPIGateway.Session.IsServer || ticks % TimerUpdateInterval != 0)
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
            MatchDurationMinutes = matchDurationMinutes;
            MatchTimerPacket.SendMatchUpdate(this);
            IsMatchEnded = false;
            MyLog.Default.WriteLineAndConsole("[MatchTimer] Started Match. " + CurrentMatchTime.ToString());
        }

        public void Stop()
        {
            EndTime = DateTime.UtcNow;
            IsMatchEnded = true;
        }

        internal void UpdateFromPacket(MatchTimerPacket packet)
        {
            StartTime = packet.MatchStartTime();
            EndTime = packet.MatchEndTime();
        }

        #endregion
    }
}
