using System;
using Sandbox.ModAPI;
using SC.SUGMA.HeartNetworking.Custom;
using VRage.Utils;

namespace SC.SUGMA.GameState
{
    /// <summary>
    ///     Keeps track of the match timer for networking and other mods.
    /// </summary>
    public class MatchTimer : ComponentBase
    {
        // TODO Create an OnInterval action.

        /// <summary>
        ///     How often the match timer should be synced, in ticks.
        /// </summary>
        public const int TimerUpdateInterval = 600;

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

        public override void Close()
        {
        }

        /// <summary>
        ///     NON-SYNCHRONIZED tick counter, incremented once per UpdateAfterSimulation().
        /// </summary>
        public int Ticks;

        public override void UpdateTick()
        {
            try
            {
                Ticks++;
                //MyAPIGateway.Utilities.SendModMessage(ModMessageId, CurrentMatchTime);

                if (DateTime.UtcNow > EndTime && !IsMatchEnded && MyAPIGateway.Session.IsServer)
                {
                    IsMatchEnded = true;
                    MyLog.Default.WriteLineAndConsole("[MatchTimer] Auto-Stopped Match. " + CurrentMatchTime);
                    Log.Info("[MatchTimer] Auto-Stopped Match. " + CurrentMatchTime);
                }

                // Update every 10 seconds if is server
                if (!MyAPIGateway.Session.IsServer || Ticks % TimerUpdateInterval != 0)
                    return;

                MatchTimerPacket.SendMatchUpdate(this);
            }
            catch (Exception ex)
            {
                Log.Exception(ex, typeof(MatchTimer));
            }
        }

        public string RemainingTimeString()
        {
            var remainingMinutes = (int)Math.Floor(MatchDurationMinutes - CurrentMatchTime.TotalMinutes);
            var remainingSeconds =
                (int)((MatchDurationMinutes - CurrentMatchTime.TotalMinutes - remainingMinutes) * 60);

            return
                $"{(remainingMinutes < 10 ? "0" + remainingMinutes : remainingMinutes.ToString())}:{(remainingSeconds < 10 ? "0" + remainingSeconds : remainingSeconds.ToString())}";
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
            if (MyAPIGateway.Session.IsServer)
                MatchTimerPacket.SendMatchUpdate(this);
            IsMatchEnded = false;
            MyLog.Default.WriteLineAndConsole("[MatchTimer] Started Match. " + CurrentMatchTime);
            Log.Info(
                $"[MatchTimer] Started Match.\n- CurrentMatchTime: {CurrentMatchTime}\n- StartTime: {StartTime}\n- EndTime: {EndTime}\n- MatchDuration: {MatchDurationMinutes}");
        }

        public void Stop()
        {
            EndTime = DateTime.UtcNow;
            IsMatchEnded = true;
        }

        public void Update(DateTime startTime, DateTime endTime)
        {
            StartTime = startTime;
            EndTime = endTime;
        }

        public void SetMatchTime(double timeMinutes)
        {
            EndTime = DateTime.UtcNow + TimeSpan.FromMinutes(MatchDurationMinutes - timeMinutes);
            StartTime = DateTime.UtcNow - TimeSpan.FromMinutes(timeMinutes);
        }

        #endregion
    }
}