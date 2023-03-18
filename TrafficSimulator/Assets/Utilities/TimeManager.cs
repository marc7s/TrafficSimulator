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

    public static TimeMode Mode { get; private set; }

    public static int Second { get; private set; }
    public static int Minute { get; private set; }
    public static int Hour { get; private set; }

    // Ratio between ingame time and real world time. Ex. 1f is 1:1 ratio, 5f is 5:1 ratio.
    public float secondToRealTime = 1f;

    private float _targetSecondToRealTime;
    private float _timer;

    void Start()
    {
        Hour = 8; Minute = 0; Second = 0;
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
    }

    // Determines if time should move forwards or backwards depending on current mode
    private void RunTime()
    {
        if(Mode != TimeMode.Rewind)
        {
            Second++;
            OnSecondChanged?.Invoke();
            OnMinuteChanged?.Invoke();
            OnHourChanged?.Invoke();
            if(Second >= 60)
            {
                Minute++;
                if(Minute >= 60)
                {
                    Hour++;
                    if(Hour >= 24)
                        Hour = 0;
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
            if(Second < 0)
            {
                Minute--;
                if(Minute < 0)
                {
                    Hour--;
                    if(Hour < 0)
                        Hour = 23;
                    Minute = 59;
                }
                Second = 59;
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
