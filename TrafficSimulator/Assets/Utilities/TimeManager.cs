using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public enum TimeMode
    {
        Fast,
        Running,
        Rewind,
        Paused
    }

public class TimeManager : MonoBehaviour
{
    public static Action OnSecondChanged;
    public static Action OnMinuteChanged;
    public static Action OnHourChanged;
    public static Action OnDayChanged;
    public static Action OnMonthChanged;
    public static Action OnYearChanged;

    public static TimeMode Mode { get; private set; }

    public static int Second { get; private set; }
    public static int Minute { get; private set; }
    public static int Hour { get; private set; }
    public static int Day { get; private set; }
    public static int Month { get; private set; }
    public static int Year { get; private set; }

    // Ratio between ingame time and real world time. Ex. 1f is 1:1 ratio, 5f is 5:1 ratio.
    public float secondToRealTime = 1f;

    private float _targetSecondToRealTime;
    private float _timer;

    // Calendar for each month. 0 = January, 11 = December
    List<PriorityQueue<TimeManagerEvent>> _calendar = new List<PriorityQueue<TimeManagerEvent>>();

    void Start()
    {
        // Set start time
        SetTime(2023, 3, 19, 13, 0, 0);

        // Initialize calendar
        InitCalendar();

        _timer = secondToRealTime;
        _targetSecondToRealTime = secondToRealTime;
        Mode = TimeMode.Running;
    }

    void Update()
    {
        if(Mode == TimeMode.Fast)
            secondToRealTime = _targetSecondToRealTime * 0.5f;

        if(Mode != TimeMode.Paused)
        {
            _timer -= Time.deltaTime;
            if(_timer <= 0)
            {
                RunTime();
                _timer = secondToRealTime;
            }
            secondToRealTime = _targetSecondToRealTime;
        }
        // Check if there are any events scheduled for the current time
        CheckSchedule();
    }

    /// <summary> Looks if the next scheduled event should execute at the current time </summary>
    private void CheckSchedule()
    {
        // Check if there are any events scheduled for the current time
        PriorityQueue<TimeManagerEvent> currentMonth = _calendar[Month];
        if(currentMonth.Count > 0)
        {
            TimeManagerEvent currentEvent = currentMonth.Peek();
            if(currentEvent.TimeStamp == $"{Year:0000}:{Month:00}:{Day:00}:{Hour:00}:{Minute:00}:{Second:00}")
            {
                Debug.Log("Event triggered: " + currentEvent.TimeStamp);
                // Remove event from queue
                currentMonth.Dequeue();
            }
        }
    }

    /// <summary> Determines if time should move forwards or backwards depending on current mode </summary>
    private void RunTime()
    {
        if(Mode != TimeMode.Rewind)
        {
            Second++;
            OnSecondChanged?.Invoke();
            OnMinuteChanged?.Invoke();
            OnHourChanged?.Invoke();
            OnDayChanged?.Invoke();
            OnMonthChanged?.Invoke();
            OnYearChanged?.Invoke();
            if(Second >= 60)
            {
                Minute++;
                if(Minute >= 60)
                {
                    Hour++;
                    if(Hour >= 24)
                    {
                        Day++;
                        if(Day >= 31)
                        {
                            Month++;
                            if(Month >= 12)
                            {
                                Year++;
                                Month = 1;
                            }
                            Day = 1;
                        }
                        Hour = 0;
                    }
                    Minute = 0;
                }
                Second = 0;
            }

        } else if(Mode == TimeMode.Rewind)
        {
            Second--;
            OnSecondChanged?.Invoke();
            OnMinuteChanged?.Invoke();
            OnHourChanged?.Invoke();
            OnDayChanged?.Invoke();
            OnMonthChanged?.Invoke();
            OnYearChanged?.Invoke();
            if(Second < 0)
            {
                Minute--;
                if(Minute < 0)
                {
                    Hour--;
                    if(Hour < 0)
                    {
                        Day--;
                        if(Day < 0)
                        {
                            Month--;
                            if(Month < 0)
                            {
                                Year--;
                                Month = 12;
                            }
                            Day = 31;
                        }
                        Hour = 23;
                    }
                    Minute = 59;
                }
                Second = 59;
            }
        }
    }


    /// </summary> Initializes calendar with an empty priority queue for each month </summary>
    private void InitCalendar()
    {
        for(int i = 0; i < 12; i++)
        {
            _calendar.Add(new PriorityQueue<TimeManagerEvent>());
        }
    }

    /// </summary> Sets the current time to the given values </summary>
    private void SetTime(int year, int month, int day, int hour, int minute, int second)
    {
        Year = year;
        Month = month;
        Day = day;
        Hour = hour;
        Minute = minute;
        Second = second;
    }

    /// </summary> Returns a string with the given time values in the format "yyyy:MM:dd:HH:mm:ss" </summary>
    private string GetTimeStamp(int year, int month, int day, int hour, int minute, int second)
    {
        string timeStamp = $"{year:0000}:{month:00}:{day:00}:{hour:00}:{minute:00}:{second:00}";
        return timeStamp;
    }

    /// </summary> Calculates the priority of an event based on the given time values </summary>
    private int CalculatePriority(int year, int day, int hour, int minute, int second)
    {
        int priority = 0;
        priority += second;
        priority += minute * 60;
        priority += hour * 60 * 60;
        priority += day * 60 * 60 * 24;
        priority += year * 60 * 60 * 24 * 365;
        return priority;
    }

    /// </summary> Adds an event to the calendar </summary>
    private void AddEvent(int year, int month, int day, int hour, int minute, int second)
    {
        string timeStamp = GetTimeStamp(year, month, day, hour, minute, second);
        int priority = CalculatePriority(year, day, hour, minute, second);

        _calendar[month].Enqueue(new TimeManagerEvent(priority, timeStamp));
    }

    /// </summary> Sets current mode to fast forward </summary>
    public static void FastForward()
    {
        Mode = Mode != TimeMode.Fast ? TimeMode.Fast : TimeMode.Running;
    }

    /// </summary> Sets current mode to rewind </summary>
    public static void Rewind()
    {
        Mode = Mode != TimeMode.Rewind ? TimeMode.Rewind : TimeMode.Running;
    }

    /// </summary> Sets current mode to paused </summary>
    public static void Pause()
    {
        Mode = Mode != TimeMode.Paused ? TimeMode.Paused : TimeMode.Running;
    }
}
