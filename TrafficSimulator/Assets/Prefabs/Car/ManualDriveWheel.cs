using UnityEngine;
using UnityEngine.InputSystem;

namespace VehicleBrain
{
    public class ManualDriveWheel : ManualDrive
    {
        private bool _isReversing = false;

        protected override void OnThrottle(InputValue value)
        {
            _vehicleController.throttleInput = (_isReversing ? -1 : 1) * Mathf.Abs(value.Get<float>());
        }

        protected override void OnBrake(InputValue value)
        {
            _vehicleController.brakeInput = value.Get<float>();
        }

        protected override void OnSteer(InputValue value)
        {
            _vehicleController.steerInput = value.Get<float>();
        }

        protected override void OnHandbrake(InputValue value)
        {
            _vehicleController.handbrakeInput = value.Get<float>();
        }

        protected override void OnReverseGear(InputValue value)
        {
            _isReversing = value.isPressed;
        }
    }
}