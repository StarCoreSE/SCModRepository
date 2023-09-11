using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MIG.Shared.CSharp {
    public class Timer {
        protected int time;

        public Timer(int time = 0) {
            this.time = time;
        }

        public virtual bool tick() {
            time--;
            return time < 0;
        }

        public bool needAction() {
            return time < 0;
        }

        public int getTime () {
            return time;
        }

        public void addTime(int time) {
            if (this.time < 0) {
                this.time = 0;
            }

            this.time += time;
        }

        public void setTime (int time) {
            this.time = time;
        }
    }

    public class AutoTimer : Timer {
        int interval;

        public void setInterval (int interval) {
            this.interval = interval;
        }
        public AutoTimer (int interval, int time = 0) : base(time) {
            this.interval = interval;
        }

        public int getInterval () {
            return interval;
        }

        public void setInterval (int interval, int time) {
            this.interval = interval;
            this.time = time;
        }

        public void reset () {
            time = interval;
        }

        public override bool tick() {
            if (base.tick()) {
                addTime(interval);
                return true;
            } else {
                return false;
            }
        }

        public void addTime () {
            addTime(interval);
        }

        public override string ToString() { return "AutoTimer ["+time + " / " +interval+"]"; }
    }
}
