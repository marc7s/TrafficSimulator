using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

namespace Simulation
{
    public enum SunMode
    {
        Time,
        Day,
        Night
    }

    public class DayNightManager : MonoBehaviour
    {
        public SunMode _sunMode = SunMode.Day;

        private Quaternion _currentSunRotation;

        void Start()
        {
            SetSunLocation(_sunMode);

            // Subscribe to time manager events for hour
            TimeManager.OnHourChanged += IncrementSunPosition;
        }

        private void SetSunLocation(SunMode mode)
        {
            switch (mode)
            {
                case SunMode.Time:
                    _currentSunRotation = Quaternion.Euler((DateTime.Now.Hour * 15 - 90), 0, 0);
                    break;
                case SunMode.Day:
                    _currentSunRotation = Quaternion.Euler(90, 0, 0);
                    break;
                case SunMode.Night:
                    _currentSunRotation = Quaternion.Euler(-90, 0, 0);
                    break;
            }

            transform.rotation = _currentSunRotation;
        }

        private void IncrementSunPosition()
        {
            // Increment sun position by 15 degrees (1 hour)
            if (_sunMode == SunMode.Time)
                _currentSunRotation *= Quaternion.Euler(15, 0, 0);
            
            transform.rotation = _currentSunRotation;
        }
    }
}
