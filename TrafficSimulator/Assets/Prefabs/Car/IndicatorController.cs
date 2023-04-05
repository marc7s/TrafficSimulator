using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Car
{
    public enum IndicatorState
    {
        Right,
        Left,
        Off,
        Warning
    }
public class IndicatorController : MonoBehaviour
{
    [SerializeField] private Light[] _leftIndicatorLights;
    [SerializeField] private Light[] _rightIndicatorLights;
    [SerializeField] private float _intervalTime = 0.5f;
    private float _lastSwitchTime = 0f;
    private IndicatorState _indicatorState = IndicatorState.Off;
    private bool _isLightOn;

    public void SetIndicator(TurnDirection turnDirection)
    {
        IndicatorState newIndicatorState = turnDirection == TurnDirection.Left ? IndicatorState.Left : turnDirection == TurnDirection.Right ? IndicatorState.Right : IndicatorState.Off;
        if (newIndicatorState == _indicatorState)
            return;
        _indicatorState = newIndicatorState;
        _lastSwitchTime = Time.time;
    }

    void Update()
    {
        if (Time.time - _lastSwitchTime > _intervalTime)
        {
            _isLightOn = !_isLightOn;
            _lastSwitchTime = Time.time;
            bool turnOnLeftIndicators = _isLightOn && (_indicatorState == IndicatorState.Left || _indicatorState == IndicatorState.Warning);
            bool turnOnRightIndicators = _isLightOn && (_indicatorState == IndicatorState.Right || _indicatorState == IndicatorState.Warning);

            foreach (Light leftIndicatorLight in _leftIndicatorLights)
                leftIndicatorLight.enabled = turnOnLeftIndicators;
            foreach (Light rightIndicatorLight in _rightIndicatorLights)
                rightIndicatorLight.enabled = turnOnRightIndicators;
        }
    }
}
}