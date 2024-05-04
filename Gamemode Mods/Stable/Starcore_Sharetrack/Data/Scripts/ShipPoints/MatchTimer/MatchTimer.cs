using System;
using Sandbox.ModAPI;
using VRage.Game.Components;
using VRage.Utils;

namespace ShipPoints.MatchTiming
{
    /// <summary>
    ///     Keeps track of the match timer for networking and other mods.
    /// </summary>
    [MySessionComponentDescriptor(MyUpdateOrder.AfterSimulation)]
    public class MatchTimer : MySessionComponentBase
    {
        /// <summary>
        ///     How often the match timer should be synced, in ticks.
        /// </summary>
        public const int TimerUpdateInterval = 600;

        public const ushort NetworkId = 8576;
        public const long ModMessageId = 8573643466;
        public static MatchTimer I;

        private double _matchDurationMinutes = 20;
        public bool IsMatchEnded = true;

        /// <summary>
        ///     Match duration, in format [mm:ss].
        /// </summary>
        public string MatchDurationString = "20:00";

        /// <summary>
        ///     The time at which the match started.
        /// </summary>
        public DateTime StartTime { get; internal set; } = DateTime.MinValue;

        /// <summary>
        ///     The time at which the match will end.
        /// </summary>
        public DateTime EndTime { get; internal set; } = DateTime.MinValue;

        /// <summary>
        ///     Current time in the match, offset from StartTime.
        /// </summary>
        public TimeSpan CurrentMatchTime => (DateTime.UtcNow < EndTime ? DateTime.UtcNow : EndTime) - StartTime;

        /// <summary>
        ///     Total length of the match, in fractional minutes.
        /// </summary>
        public double MatchDurationMinutes
        {
            get { return _matchDurationMinutes; }
            set
            {
                _matchDurationMinutes = value;
                EndTime = StartTime.AddMinutes(_matchDurationMinutes);
                var seconds = Math.Round((_matchDurationMinutes - (int)_matchDurationMinutes) * 60);
                MatchDurationString =
                    $"{(_matchDurationMinutes < 10 ? "0" : "")}{(int)_matchDurationMinutes}:{(seconds < 10 ? "0" : "")}{seconds}";
            }
        }


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

        /// <summary>
        ///     NON-SYNCHRONIZED tick counter, incremented once per UpdateAfterSimulation().
        /// </summary>
        public int Ticks;

        public override void UpdateAfterSimulation()
        {
            try
            {
                Ticks++;
                //MyAPIGateway.Utilities.SendModMessage(ModMessageId, CurrentMatchTime);

                if (DateTime.UtcNow > EndTime && !IsMatchEnded && MyAPIGateway.Session.IsServer)
                {
                    PointCheck.EndMatch();
                    MyLog.Default.WriteLineAndConsole("[MatchTimer] Auto-Stopped Match. " + CurrentMatchTime);
                }

                // Update every 10 seconds if is server
                if (!MyAPIGateway.Session.IsServer || Ticks % TimerUpdateInterval != 0)
                    return;

                MatchTimerPacket.SendMatchUpdate(this);
            }
            catch (Exception ex)
            {
                Log.Error(ex);
            }
        }

        #endregion

        #region Custom Methods

        /// <summary>
        ///     Start with an optional match duration.
        /// </summary>
        /// <param name="matchDurationMinutes">Match length in minutes.</param>
        public void Start(double matchDurationMinutes = 20)
        {
            StartTime = DateTime.UtcNow;
            MatchDurationMinutes = matchDurationMinutes;
            MatchTimerPacket.SendMatchUpdate(this);
            IsMatchEnded = false;
            MyLog.Default.WriteLineAndConsole("[MatchTimer] Started Match. " + CurrentMatchTime);
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

        public void SetMatchTime(double timeMinutes)
        {
            EndTime = DateTime.UtcNow + TimeSpan.FromMinutes(MatchDurationMinutes - timeMinutes);
            StartTime = DateTime.UtcNow - TimeSpan.FromMinutes(timeMinutes);
        }

        #endregion
    }
}