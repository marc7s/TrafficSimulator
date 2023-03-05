using UnityEngine;

namespace RoadGenerator
{
    enum State{RED, TOGO, TOSTOP, GREEN};

    public class TrafficLight : MonoBehaviour
    {
        public GameObject RedLight;
        public GameObject YellowLight;
        public GameObject GreenLight;

        private float _lastSwitchTime;
        public float SwitchTime = 2f;

        private State _currentState = State.RED;
        private State _lastState = State.RED;

        void Update()
        {
            // Switches from transitional state to final state after a certain time
            if(Time.time - _lastSwitchTime > SwitchTime)
            {
                switch (_currentState)
                {
                    case State.TOGO:
                        setState(State.GREEN);
                        break;
                    case State.TOSTOP:
                        setState(State.RED);
                        break;
                }
            }
        }

        // Red light to green light
        public void Go() {
            switch (_currentState)
            {
                case State.RED:
                case State.TOSTOP:
                    setState(State.TOGO);
                    break;
            }
        }

        // Green light to red light
        public void Stop() {
            switch (_currentState)
            {
                case State.GREEN:
                case State.TOGO:
                    setState(State.TOSTOP);
                    break;
            }
        }

        // Change the state
        private void setState(State newState)
        {
            _lastSwitchTime = Time.time;
            _lastState = _currentState;
            _currentState = newState;
            UpdateLightColor();
        }

        // Change the light color
        private void UpdateLightColor()
        {
            switch (_currentState)
            {
                case State.RED:
                    RedLight.GetComponent<Light>().enabled = true;
                    YellowLight.GetComponent<Light>().enabled = false;
                    GreenLight.GetComponent<Light>().enabled = false;
                    break;
                case State.TOSTOP:
                    RedLight.GetComponent<Light>().enabled = false;
                    YellowLight.GetComponent<Light>().enabled = true;
                    GreenLight.GetComponent<Light>().enabled = false;
                    break;
                case State.TOGO:
                    RedLight.GetComponent<Light>().enabled = true;
                    YellowLight.GetComponent<Light>().enabled = true;
                    GreenLight.GetComponent<Light>().enabled = false;
                    break;
                case State.GREEN:
                    RedLight.GetComponent<Light>().enabled = false;
                    YellowLight.GetComponent<Light>().enabled = false;
                    GreenLight.GetComponent<Light>().enabled = true;
                    break;
            }
        }
    }
}