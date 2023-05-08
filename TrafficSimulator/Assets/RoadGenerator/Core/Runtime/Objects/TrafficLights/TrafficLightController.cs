using UnityEngine;
using System.Collections.Generic;
using System.Collections;

namespace RoadGenerator
{
    enum Mode
    {
        FIRST, 
        SECOND, 
        TOFIRST, 
        TOSECOND
    };

    public class TrafficLightController : MonoBehaviour
    {
        [SerializeField] public List<TrafficLight> TrafficLightsGroup1 = new List<TrafficLight>(); // Starts green
        [SerializeField] public List<TrafficLight> TrafficLightsGroup2 = new List<TrafficLight>(); // Starts red

        public int Delay = 10;
        public int TransitionDelay = 2;
        private int _lastSwitchTime = 0;
        private int _clock = 0;

        private Mode _currentMode = Mode.FIRST;

        IEnumerator Start()
        {
            while(true)
            {
                UpdateTrafficLight();
                yield return new WaitForSeconds(1);
                _clock++;
            }
        }

        private void UpdateTrafficLight()
        {
            if (_currentMode == Mode.FIRST || _currentMode == Mode.SECOND)
            {
                if (_clock - _lastSwitchTime > Delay)
                {
                    SwitchToTransitionalMode();
                    _lastSwitchTime = _clock;
                }
            } 
            else
            {
                if (_clock - _lastSwitchTime > TransitionDelay)
                {
                    SwitchToFinalMode();
                    _lastSwitchTime = _clock;
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
