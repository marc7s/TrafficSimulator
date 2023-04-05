using UnityEngine;
using DataModel;

namespace Car
{
    public enum IndicatorState
    {
        Left,
        Off,
        Right,
        Hazard
    }
    public class IndicatorController : MonoBehaviour
    {
        [SerializeField] private Light[] _leftIndicatorLights;
        [SerializeField] private Light[] _rightIndicatorLights;
        [SerializeField] private float _intervalTime = 0.5f;
        private float _lastSwitchTime = 0f;
        private IndicatorState _indicatorState = IndicatorState.Off;
        private bool _isLightOn;

        public void SetIndicator(IndicatorState indicatorState)
        {
            if (indicatorState == _indicatorState)
                return;
            _indicatorState = indicatorState;
            _lastSwitchTime = Time.time;
        }

        void Update()
        {
            if (Time.time - _lastSwitchTime > _intervalTime)
            {
                _isLightOn = !_isLightOn;
                _lastSwitchTime = Time.time;
                bool turnOnLeftIndicators = _isLightOn && (_indicatorState == IndicatorState.Left || _indicatorState == IndicatorState.Hazard);
                bool turnOnRightIndicators = _isLightOn && (_indicatorState == IndicatorState.Right || _indicatorState == IndicatorState.Hazard);

                foreach (Light leftIndicatorLight in _leftIndicatorLights)
                    leftIndicatorLight.enabled = turnOnLeftIndicators;

                foreach (Light rightIndicatorLight in _rightIndicatorLights)
                    rightIndicatorLight.enabled = turnOnRightIndicators;
            }
        }
        public static IndicatorState TurnDirectionToIndicatorState(TurnDirection turnDirection)
        {
            switch (turnDirection)
            {
                case TurnDirection.Left:
                    return IndicatorState.Left;
                case TurnDirection.Right:
                    return IndicatorState.Right;
                case TurnDirection.Straight:
                    return IndicatorState.Off;
                default:
                    return IndicatorState.Off;
            }
        }
    }
}