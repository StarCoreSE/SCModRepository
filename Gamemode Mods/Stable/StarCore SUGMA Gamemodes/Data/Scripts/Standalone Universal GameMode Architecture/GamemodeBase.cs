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

        public virtual void StartRound()
        {
            if (SUGMA_SessionComponent.I.CurrentGamemode != null && SUGMA_SessionComponent.I.CurrentGamemode != this && SUGMA_SessionComponent.I.CurrentGamemode.IsStarted)
                SUGMA_SessionComponent.I.CurrentGamemode.StopRound();
            SUGMA_SessionComponent.I.CurrentGamemode = this;

            // TODO
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
