using ProtoBuf;

namespace ShipPoints.HeartNetworking.Custom
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
                PointCheck.I.ReportProblem(IssueMessage, false);
            else
                PointCheck.I.ResolvedProblem(false);
        }
    }
}