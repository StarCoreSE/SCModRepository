using System;
using System.Collections.Generic;
using Sandbox.ModAPI;
using SC.SUGMA.GameModes.Elimination;
using SC.SUGMA.GameState;
using SC.SUGMA.Utilities;
using VRage.Game.ModAPI;
using VRageMath;

namespace SC.SUGMA.GameModes.KOTH
{
    internal partial class KOTHGamemode : EliminationGamemode
    {
        public KOTHSphereZone ControlPoint;

        private Vector3D _controlPointPosition = new Vector3D(0, 0, 0);
        private float _controlPointRadius = 2500f;

        private int ActivationTime = 300;
        public int ActivationTimeCounter = 0;

        private int WinTime = 120;

        public override string ReadableName { get; internal set; } = "King Of The Hill";

        public override string Description { get; internal set; } =
            "Factions fight over a Central Capture Point. Either spend 2 minutes uncontested in the center zone, or eliminate the enemy team to win.";

        public KOTHGamemode()
        {
            ArgumentParser += new ArgumentParser(
                new ArgumentParser.ArgumentDefinition(
                    text => ActivationTime = int.Parse(text),
                    "at", "activation-time",
                    $"Delay before Center Zone is Capturable, in Seconds"
                ),
                new ArgumentParser.ArgumentDefinition(
                    text => WinTime = int.Parse(text),
                    "wt", "win-time",
                    "How long a team has to hold the zone uncontested to win, in Seconds"
                )
            );
        }

        public override void StartRound(string[] arguments = null)
        {
            base.StartRound(arguments);

            if (TrackedFactions.Count <= 1)
                return;

            ActivationTimeCounter = ActivationTime;
            ControlPoint = null;

            if (!MyAPIGateway.Utilities.IsDedicated)
                SUGMA_SessionComponent.I.RegisterComponent("KOTHHud", new KOTHHud(this));
        }

        public override void StopRound()
        {
            _winningFaction = ControlPoint._zoneOwner;
            base.StopRound();
            if (!_setWinnerFromArgs)
                _winningFaction = ControlPoint._zoneOwner;

            SUGMA_SessionComponent.I.GetComponent<KOTHHud>("KOTHHud")?.MatchEnded(_winningFaction);
            SUGMA_SessionComponent.I.UnregisterComponent("KOTHHud");

            ControlPoint = null;
            SUGMA_SessionComponent.I.UnregisterComponent("KOTHZone");
        }

        internal override void DisplayWinMessage()
        {
            if (ControlPoint._zoneOwner == null)
            {
                MyAPIGateway.Utilities.ShowNotification("YOU ARE ALL LOSERS.", 10000, "Red");
                return;
            }

            MyAPIGateway.Utilities.ShowNotification($"A WINNER IS [{ControlPoint._zoneOwner?.Name}]!", 10000);
        }

        public override void UpdateActive()
        {
            if (ActivationTimeCounter > 0)
            {
                if (_matchTimer.Ticks % 60 == 0)
                {
                    ActivationTimeCounter--;

                    if (ActivationTimeCounter <= 0)
                    {
                        ControlPoint = new KOTHSphereZone(_controlPointPosition, _controlPointRadius, WinTime);
                        SUGMA_SessionComponent.I.RegisterComponent("KOTHZone", ControlPoint);
                    }
                }
            }

            if (ControlPoint == null)
                return;

            if (ControlPoint.IsCaptured)
            {
                StopRound();
            }
        }
    }
}