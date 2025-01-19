using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace KillFeed
{
    static class Config
    {
        // How long it will ignore subsequent kills on a victim
        public static TimeSpan ignoreVictimTimespan = new TimeSpan(0, 0, 10);

        // How long until it checks for the lack of a cockpit for the first time
        public static TimeSpan firstCheckTimespan = new TimeSpan(0, 0, 5);

        // How long until it checks for the lack of a cockpit for the last time
        public static TimeSpan finalCheckTimespan = new TimeSpan(0, 0, 5);

        // Should a xml file be used to describe kills
        public static bool useXmlFile = true;

        // Where the kill feed xml is output to
        public static string xmlFile = "KillFeed.xml";

        // The network message ID for the chat message
        public static ushort NetworkMessageId = 31569;
    }
}
