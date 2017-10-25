using System;

namespace ThreadSave {
    class TimeOut {
        public static bool NotTime(int timeout) {
            return timeout == 0;
        }

        public static int EndTime(int timeout) {
            return Environment.TickCount + timeout;
        }

        public static int Remaining(int time) {
            return time - Environment.TickCount;
        }

        public static bool InvalidTime(int remaining) {
            return remaining <= 0;
        }
    }
}