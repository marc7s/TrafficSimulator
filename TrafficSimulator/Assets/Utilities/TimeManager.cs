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

    public static int Minute { get; private set; }
    public static int Hour { get; private set; }

    public float minuteToRealTime = 60f;
    private float _targetMinuteToRealTime;

    private float _timer;

    public static TimeMode Mode { get; private set; }

    void Start()
    {
        Minute = 0;
        Hour = 23;
        _timer = minuteToRealTime;
        _targetMinuteToRealTime = minuteToRealTime;
        Mode = TimeMode.Running;
    }

    void Update()
    {
        if(Mode == TimeMode.Fast)
        {
            minuteToRealTime = _targetMinuteToRealTime * 0.5f;
        } else if(Mode == TimeMode.Rewind)
        {
            minuteToRealTime = _targetMinuteToRealTime * 2f;
        }
        
        if(Mode != TimeMode.Paused)
        {
            _timer -= Time.deltaTime;

            if(_timer <= 0)
            {
                Minute++;
                OnMinuteChanged?.Invoke();
                OnHourChanged?.Invoke();
                
                if(Minute >= 60)
                {
                    Hour++;
                    if(Hour >= 24)
                    {
                        Hour = 0;
                    }
                    Minute = 0;
                }

                _timer = minuteToRealTime;
            }
            minuteToRealTime = _targetMinuteToRealTime;
        }
    }

    public static void FastForward()
    {
        if(Mode != TimeMode.Fast)
        {
            Mode = TimeMode.Fast;
        } else
        {
            Mode = TimeMode.Running;
        }
    }

    public static void Rewind()
    {
        if(Mode != TimeMode.Rewind)
        {
            Mode = TimeMode.Rewind;
        } else
        {
            Mode = TimeMode.Running;
        }
    }

    public static void Pause()
    {
        if(Mode != TimeMode.Paused)
        {
            Mode = TimeMode.Paused;
        } else
        {
            Mode = TimeMode.Running;
        }
    }
}
