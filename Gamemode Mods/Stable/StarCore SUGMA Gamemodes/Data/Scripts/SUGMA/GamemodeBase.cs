using Sandbox.ModAPI;
using SC.SUGMA.GameState;
using System.Runtime.Serialization.Formatters;
using VRage.Game.ModAPI;

namespace SC.SUGMA
{
    public abstract class GamemodeBase : ComponentBase
    {
        public abstract string ReadableName { get; internal set; }
        public abstract string Description { get; internal set; }
        public bool IsStarted = false;
        internal bool AwaitingStart = false;

        private string[] _bufferArguments = null;

        public virtual void StartRound(string[] arguments = null)
        {
            if (!SUGMA_SessionComponent.I.ShareTrackApi.AreTrackedGridsLoaded())
            {
                AwaitingStart = true;
                _bufferArguments = arguments;
                return;
            }

            if (SUGMA_SessionComponent.I.CurrentGamemode != null && SUGMA_SessionComponent.I.CurrentGamemode != this &&
                SUGMA_SessionComponent.I.CurrentGamemode.IsStarted)
                SUGMA_SessionComponent.I.CurrentGamemode.StopRound();
            SUGMA_SessionComponent.I.CurrentGamemode = this;

            DisplayStartMessage();
            IsStarted = true;
        }

        public sealed override void UpdateTick()
        {
            if (AwaitingStart)
            {
                MyAPIGateway.Utilities.ShowNotification("[SUGMA] Not all grids are loaded, waiting to start match!", 1000/60);
                StartRound(_bufferArguments);
            }

            if (IsStarted)
                UpdateActive();
        }

        public abstract void UpdateActive();

        public virtual void StopRound()
        {
            DisplayWinMessage();
            IsStarted = false;
            SUGMA_SessionComponent.I.StopGamemode();
        }

        internal virtual void DisplayStartMessage()
        {
            // TODO tie this into TextHudApi
            MyAPIGateway.Utilities.ShowNotification($"[{ReadableName}] GAME STARTED", 10000);
            MyAPIGateway.Utilities.ShowNotification(Description, 10000);
        }

        internal abstract void DisplayWinMessage();
    }
}