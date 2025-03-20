using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StealthSystem
{
    internal class Definitions
    {
        internal class DriveDefinition
        {
            internal int Duration;
            internal float PowerScale;
            internal float SignalRangeScale;

            public DriveDefinition(int duration, float powerScale, float signalScale)
            {
                Duration = duration;
                PowerScale = powerScale;
                SignalRangeScale = signalScale;
            }
        }

        internal class SinkDefinition
        {
            internal int Duration;
            internal float Power;
            internal bool DoDamage;

            public SinkDefinition(int duration, float power, bool damage)
            {
                Duration = duration;
                Power = power;
                DoDamage = damage;
            }
        }

    }
}
