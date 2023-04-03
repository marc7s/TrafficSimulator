using System;

namespace Simulation
{
    public class TimeManagerEvent : System.IComparable<TimeManagerEvent>, System.IComparable<DateTime>, System.IEquatable<TimeManagerEvent>
    {
        private string _timeStamp;
        private DateTime _dateTime;

        public Action OnEvent;



        public TimeManagerEvent(DateTime dateTime)
        {
            _dateTime = TimeManager.Instance.FormatDateTime(dateTime);
            _timeStamp = _dateTime.ToString("yyyy-MM-dd HH:mm:ss");
        }

        public DateTime DateTime
        {
            get => _dateTime;
        }

        public string TimeStamp
        {
            get => _timeStamp;
        }

        public bool IsOnOrBefore(DateTime dateTime)
        {
            return _dateTime <= dateTime;
        }

        /// <summary> TimeManagerEvents are compared by their timestamps </summary>
        public int CompareTo(TimeManagerEvent other)
        {
            return _dateTime.CompareTo(other._dateTime);
        }

        /// <summary> Compare TimeManagerEvents to DateTimes by the timestamps </summary>
        public int CompareTo(DateTime other)
        {
            return _dateTime.CompareTo(other);
        }

        /// <summary> TimeManagerEvents are compared by their timestamps </summary>
        public bool Equals(TimeManagerEvent other)
        {
            return _dateTime.Equals(other._dateTime);
        }
    }
}

