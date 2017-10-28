using System;

namespace ThreadSave {
    class TimeOut {
        public static bool NoWait(int timeout) {
            return timeout == 0;
        }

        public static int Start(int timeout) {
            return Environment.TickCount + timeout;
        }

        public static int Remaining(int time) {
            return time - Environment.TickCount;
        }

        public static bool IsTimeout(int remaining) {
            return remaining <= 0;
        }
    }
}