using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Car
{
    public enum BrakeLightState
    {
        On,
        Off
    }
    public class BrakeLightController : MonoBehaviour
    {
        [SerializeField] private Light[] _brakeLights;
        private BrakeLightState _brakeLightState = BrakeLightState.Off;

        public void SetBrakeLights(BrakeLightState state)
        {
            if (state == _brakeLightState)
                return;

            foreach (Light brakeLight in _brakeLights)
                brakeLight.enabled = state == BrakeLightState.On;

            _brakeLightState = state;
        }
    }
}