using UnityEngine.InputSystem;


namespace VehicleBrain
{
    public class ManualDriveKeyboardController : ManualDrive
    {
        protected override void OnSteer(InputValue value)
        {
            _vehicleController.steerInput = value.Get<float>();
        }

        protected override void OnThrottle(InputValue value)
        {
            _vehicleController.throttleInput = value.Get<float>();
        }

        protected override void OnBrake(InputValue value)
        {
            _vehicleController.brakeInput = value.Get<float>();
        }

        protected override void OnHandbrake(InputValue value)
        {
            _vehicleController.handbrakeInput = value.Get<float>();
        }

        protected override void OnReverseGear(InputValue value) {}
    }
}