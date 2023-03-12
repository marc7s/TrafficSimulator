using UnityEngine;

namespace RoadGenerator
{
    public enum TrafficLightState{Red, ToGo, ToStop, Green};

    public class TrafficLight : MonoBehaviour
    {
        public GameObject RedLight;
        public GameObject YellowLight;
        public GameObject GreenLight;

        private float _lastSwitchTime;
        public float SwitchTime = 2f;
        public RoadNode RoadNode;

        private TrafficLightState _currentState = TrafficLightState.Red;
        private TrafficLightState _lastState = TrafficLightState.Red;

        void Update()
        {
            // Switches from transitional state to final state after a certain time
            if(Time.time - _lastSwitchTime > SwitchTime)
            {
                switch (_currentState)
                {
                    case TrafficLightState.ToGo:
                        setState(TrafficLightState.Green);
                        break;
                    case TrafficLightState.ToStop:
                        setState(TrafficLightState.Red);
                        break;
                }
            }
        }

        // Red light to green light
        public void Go() {
            switch (_currentState)
            {
                case TrafficLightState.Red:
                case TrafficLightState.ToStop:
                    setState(TrafficLightState.ToGo);
                    break;
            }
        }

        // Green light to red light
        public void Stop() {
            switch (_currentState)
            {
                case TrafficLightState.Green:
                case TrafficLightState.ToGo:
                    setState(TrafficLightState.ToStop);
                    break;
            }
        }

        // Change the state
        private void setState(TrafficLightState newState)
        {
            _lastSwitchTime = Time.time;
            _lastState = _currentState;
            _currentState = newState;
            UpdateLightColor();
        }

        public TrafficLightState GetState()
        {
            return _currentState;
        }

        // Change the light color
        private void UpdateLightColor()
        {
            switch (_currentState)
            {
                case TrafficLightState.Red:
                    RedLight.GetComponent<Light>().enabled = true;
                    YellowLight.GetComponent<Light>().enabled = false;
                    GreenLight.GetComponent<Light>().enabled = false;
                    break;
                case TrafficLightState.ToStop:
                    RedLight.GetComponent<Light>().enabled = false;
                    YellowLight.GetComponent<Light>().enabled = true;
                    GreenLight.GetComponent<Light>().enabled = false;
                    break;
                case TrafficLightState.ToGo:
                    RedLight.GetComponent<Light>().enabled = true;
                    YellowLight.GetComponent<Light>().enabled = true;
                    GreenLight.GetComponent<Light>().enabled = false;
                    break;
                case TrafficLightState.Green:
                    RedLight.GetComponent<Light>().enabled = false;
                    YellowLight.GetComponent<Light>().enabled = false;
                    GreenLight.GetComponent<Light>().enabled = true;
                    break;
            }
        }
    }
}