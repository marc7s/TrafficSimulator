using UnityEngine;
using UnityEngine.InputSystem;

namespace VehicleBrain
{
    public class PlayerInputController : MonoBehaviour
    {
        public float SteeringInput { get; private set; }
        public float AccelerationInput { get; private set; }
        public float BrakingInput { get; private set; }

        private void OnSteering(InputValue value)
        {
            SteeringInput = value.Get<float>();
        }

        private void OnAcceleration(InputValue value)
        {
            AccelerationInput = value.Get<float>();
        }

        private void OnBraking(InputValue value)
        {
            BrakingInput = value.Get<float>();
        }
    }
}