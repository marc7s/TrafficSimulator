//#define TRAFFIC_LIGHTS_DISABLED
using UnityEngine;

namespace RoadGenerator
{
    public enum TrafficLightState
    { 
        Red, 
        ToGo, 
        ToStop, 
        Green 
    };
    
    [ExecuteInEditMode()]
    public class TrafficLight : MonoBehaviour
    {
        private float _lastSwitchTime;
        public float SwitchTime = 2f;
        public RoadNode RoadNode;

        private TrafficLightState _currentState = TrafficLightState.Red;
        private TrafficLightState _lastState = TrafficLightState.Red;
        public TrafficLightController trafficLightController;
        private Renderer _objRenderer;

        void Start()
        {
            _objRenderer = GetComponent<Renderer>();
        }

        void Update()
        {
            // Switches from transitional state to final state after a certain time
            if(Time.time - _lastSwitchTime > SwitchTime)
            {
                switch (_currentState)
                {
                    case TrafficLightState.ToGo:
                        SetState(TrafficLightState.Green);
                        break;
                    case TrafficLightState.ToStop:
                        SetState(TrafficLightState.Red);
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
                    SetState(TrafficLightState.ToGo);
                    break;
            }
        }

        // Green light to red light
        public void Stop() {
            switch (_currentState)
            {
                case TrafficLightState.Green:
                case TrafficLightState.ToGo:
                    SetState(TrafficLightState.ToStop);
                    break;
            }
        }

        // Change the state
        private void SetState(TrafficLightState newState)
        {
            _lastSwitchTime = Time.time;
            _lastState = _currentState;
            _currentState = newState;
            UpdateLightColor();
        }
        public TrafficLightState CurrentState
        {
#if TRAFFIC_LIGHTS_DISABLED
            get => TrafficLightState.Green;
#else
            get => _currentState;
#endif
        }
        // Change the light color
        private void UpdateLightColor()
        {
            switch (_currentState)
            {
                case TrafficLightState.Red:
                    _objRenderer.materials[1].SetTextureOffset("_MainTex", new Vector2(0.0625f * 0.0f, 0.0f));
                    break;
                case TrafficLightState.ToStop:
                    _objRenderer.materials[1].SetTextureOffset("_MainTex", new Vector2(0.0625f * 1.0f, 0.0f));
                    break;
                case TrafficLightState.ToGo:
                    _objRenderer.materials[1].SetTextureOffset("_MainTex", new Vector2(0.0625f * 3.0f, 0.0f));
                    break;
                case TrafficLightState.Green:
                    _objRenderer.materials[1].SetTextureOffset("_MainTex", new Vector2(0.0625f * 2.0f, 0.0f));
                    break;
            }
        }
        public void OnDestroy()
        {
            trafficLightController.TrafficLightsGroup1.Remove(this);
            trafficLightController.TrafficLightsGroup2.Remove(this);
        }
    }
}