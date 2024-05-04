using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ProtoBuf;
using ShipPoints;
using ShipPoints.HeartNetworking;

namespace ShipPoints.HeartNetworking.Custom
{
    [ProtoContract]
    internal class ProblemReportPacket : PacketBase
    {
        [ProtoMember(1)] public bool IssueState;
        [ProtoMember(1)] public string IssueMessage;

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
