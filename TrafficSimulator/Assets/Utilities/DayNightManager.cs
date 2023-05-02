using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Simulation;

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
        // Sun
        [SerializeField] private GameObject target;

        public SunMode _sunMode = SunMode.Time;

        void Start()
        {
            SetSunLocation(_sunMode);

            // Subscribe to time manager events for hour
            TimeManager.OnHourChanged += MoveSunByTime;
        }

        private void SetSunLocation(SunMode mode)
        {
            switch (mode)
            {
                case SunMode.Time:
                    break;
                case SunMode.Day:
                    transform.rotation = Quaternion.Euler(90, 0, 0);
                    break;
                case SunMode.Night:
                    transform.rotation = Quaternion.Euler(-90, 0, 0);
                    break;
            }
        }

        private void MoveSunByTime()
        {
            int hour = TimeManager.Hour;

            transform.rotation = Quaternion.Euler((hour * 15 - 90), 0, 0);
        }
    }
}
