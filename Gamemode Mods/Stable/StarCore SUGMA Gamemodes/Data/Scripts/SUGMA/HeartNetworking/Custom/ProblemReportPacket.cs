using ProtoBuf;
using Sandbox.ModAPI;
using SC.SUGMA.GameState;

namespace SC.SUGMA.HeartNetworking.Custom
{
    [ProtoContract]
    internal class ProblemReportPacket : PacketBase
    {
        [ProtoMember(1)] public string IssueMessage;
        [ProtoMember(2)] public bool IssueState;

        private ProblemReportPacket()
        {
        }

        public ProblemReportPacket(bool issueState, string issueMessage = "")
        {
            IssueState = issueState;
            IssueMessage = issueMessage;
        }

        public override void Received(ulong SenderSteamId)
        {
            if (IssueState)
            {
                if (IssueMessage.Length > 500)
                    IssueMessage = IssueMessage.Substring(0, 500);

                if (!MyAPIGateway.Utilities.IsDedicated)
                {
                    SUGMA_SessionComponent.I?.UnregisterComponent("problemReport");
                    SUGMA_SessionComponent.I?.RegisterComponent("problemReport", new ProblemReport(IssueMessage));
                }
            }
            else
            {
                if (!MyAPIGateway.Utilities.IsDedicated)
                    SUGMA_SessionComponent.I?.UnregisterComponent("problemReport");
            }

            if (MyAPIGateway.Session.IsServer)
                HeartNetwork.I.SendToEveryone(this);
        }
    }
}