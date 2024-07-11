using System;
using Sandbox.ModAPI;

namespace SC.SUGMA
{
    public abstract class GamemodeBase : ComponentBase
    {
        public bool IsStarted;
        public abstract string ReadableName { get; internal set; }
        public abstract string Description { get; internal set; }
        public string[] Arguments { get; internal set; } = Array.Empty<string>();

        public virtual void StartRound(string[] arguments = null)
        {
            if (SUGMA_SessionComponent.I.CurrentGamemode != null && SUGMA_SessionComponent.I.CurrentGamemode != this &&
                SUGMA_SessionComponent.I.CurrentGamemode.IsStarted)
                SUGMA_SessionComponent.I.CurrentGamemode.StopRound();
            SUGMA_SessionComponent.I.CurrentGamemode = this;
            Arguments = arguments ?? Array.Empty<string>();

            DisplayStartMessage();
            IsStarted = true;
        }

        public sealed override void UpdateTick()
        {
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