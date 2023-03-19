namespace Simulation
{
    public class TimeManagerEvent : System.IComparable<TimeManagerEvent>, System.IEquatable<TimeManagerEvent>
    {
        private TimeManagerEvent _prev;
        private TimeManagerEvent _next;
        public string TimeStamp;
        private int _priority;

        public TimeManagerEvent(int priority, string timeStamp) : this(priority, null, null, timeStamp){}
        public TimeManagerEvent(int priority, TimeManagerEvent prev, TimeManagerEvent next, string timeStamp)
        {
            _priority = priority;
            _prev = prev;
            _next = next;
            TimeStamp = timeStamp;
        } 

        public int CompareTo(TimeManagerEvent other)
        {
            return _priority.CompareTo(other._priority);
        }

        public bool Equals(TimeManagerEvent other) {
            return TimeStamp.ToString() == other.TimeStamp.ToString();
        }
    }
}

