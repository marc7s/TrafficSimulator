using UnityEngine;
using System.Collections.Generic;
using System;

namespace RoadGenerator
{
    enum Mode{FIRST, SECOND, TOFIRST, TOSECOND};

    public class TrafficLightController : MonoBehaviour
    {
        [SerializeField] public List<TrafficLight> TrafficLightsGroup1; // Starts green
        [SerializeField] public List<TrafficLight> TrafficLightsGroup2; // Starts red

        public float Delay = 10f;
        public float TransitionDelay = 2f;
        private float _lastSwitchTime = 0f;

        private Mode _currentMode = Mode.FIRST;

        void Update()
        {
            if (_currentMode == Mode.FIRST || _currentMode == Mode.SECOND)
            {
                if (Time.time - _lastSwitchTime > Delay)
                {
                    SwitchToTransitionalMode();
                    _lastSwitchTime = Time.time;
                }
            } else
            {
                if (Time.time - _lastSwitchTime > TransitionDelay)
                {
                    SwitchToFinalMode();
                    _lastSwitchTime = Time.time;
                }
            }
        }

        private void SwitchToFinalMode()
        {
            switch (_currentMode)
            {
                case Mode.TOFIRST:
                    SetGroupState(TrafficLightsGroup2, true);
                    _currentMode = Mode.FIRST;
                    break;
                case Mode.TOSECOND:
                    SetGroupState(TrafficLightsGroup1, true);
                    _currentMode = Mode.SECOND;
                    break;
            }
        }

        private void SwitchToTransitionalMode()
        {
            switch (_currentMode)
            {
                case Mode.FIRST:
                    SetGroupState(TrafficLightsGroup1, false);
                    SetGroupState(TrafficLightsGroup2, false);
                    _currentMode = Mode.TOSECOND;
                    break;
                case Mode.SECOND:
                    SetGroupState(TrafficLightsGroup1, false);
                    SetGroupState(TrafficLightsGroup2, false);
                    _currentMode = Mode.TOFIRST;
                    break;
            }
        }

        private void SetGroupState(List<TrafficLight> group, bool isGo)
        {
            foreach(TrafficLight trafficLight in group)
            {
                if (isGo)
                    trafficLight.Go();
                else
                    trafficLight.Stop();
            }
        }
    }
}
