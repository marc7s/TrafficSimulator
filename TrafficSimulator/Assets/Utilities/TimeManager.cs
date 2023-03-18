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
    public static Action OnMinuteChanged;
    public static Action OnHourChanged;
    public static TimeMode Mode { get; private set; }
    public static int Minute { get; private set; }
    public static int Hour { get; private set; }

    // Ratio between ingame time and real world time. Ex. 60f is 1:1 ratio, 30f is 2:1 ratio.
    public float minuteToRealTime = 60f;

    private float _targetMinuteToRealTime;
    private float _timer;

    void Start()
    {
        Minute = 0;
        Hour = 8;
        _timer = minuteToRealTime;
        _targetMinuteToRealTime = minuteToRealTime;
        Mode = TimeMode.Running;
    }

    void Update()
    {
        if(Mode == TimeMode.Fast)
            minuteToRealTime = _targetMinuteToRealTime * 0.5f;

        if(Mode != TimeMode.Paused)
        {
            _timer -= Time.deltaTime;
            if(_timer <= 0)
            {
                RunTime();
                _timer = minuteToRealTime;
            }
            
            minuteToRealTime = _targetMinuteToRealTime;
        }
    }

    // Determines if time should move forwards or backwards depending on current mode
    private void RunTime()
    {
        if(Mode != TimeMode.Rewind)
        {
            Minute++;
            OnMinuteChanged?.Invoke();
            OnHourChanged?.Invoke();
            if(Minute >= 60)
            {
                Hour++;
                if(Hour >= 24)
                    Hour = 0;
                Minute = 0;
            }
        } else if(Mode == TimeMode.Rewind)
        {
            Minute--;
            OnMinuteChanged?.Invoke();
            OnHourChanged?.Invoke();
            if(Minute < 1)
            {
                Hour--;
                if(Hour < 1)
                    Hour = 23;
                Minute = 59;
            }
        }
    }

    public static void FastForward()
    {
        Mode = Mode != TimeMode.Fast ? TimeMode.Fast : TimeMode.Running;
    }

    public static void Rewind()
    {
        Mode = Mode != TimeMode.Rewind ? TimeMode.Rewind : TimeMode.Running;
    }

    public static void Pause()
    {
        Mode = Mode != TimeMode.Paused ? TimeMode.Paused : TimeMode.Running;
    }
}
