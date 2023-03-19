using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TimeManagerEvent : System.IComparable<TimeManagerEvent>, System.IEquatable<TimeManagerEvent>
{
    private TimeManagerEvent _prev;
    private TimeManagerEvent _next;
    public string TimeStamp;
    public int Priority;

    public TimeManagerEvent(int priority, string timeStamp) : this(priority, null, null, timeStamp){}
    public TimeManagerEvent(int priority, TimeManagerEvent prev, TimeManagerEvent next, string timeStamp)
    {
        Priority = priority;
        _prev = prev;
        _next = next;
        TimeStamp = timeStamp;
    } 

    public int CompareTo(TimeManagerEvent other)
    {
        return Priority.CompareTo(other.Priority);
    }

    public bool Equals(TimeManagerEvent other) {
        return TimeStamp.ToString() == other.TimeStamp.ToString();
    }
}
