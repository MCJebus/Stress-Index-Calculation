using System;

namespace DataInputFormats {
    class StressDataEntry {
        private DateTime timestamp;
        private double rrInterval;

        public DateTime Timestamp {
            get { return timestamp; }
            set { timestamp = value; }
        }
        public double RrInterval {
            get { return rrInterval; }
            set { rrInterval = value; }
        }
        public StressDataEntry() {
        }

        override public string ToString() {
            return timestamp.ToString("yyyy:M:d:H:m:s:fff") + ", " + rrInterval.ToString();
        }

    }
}