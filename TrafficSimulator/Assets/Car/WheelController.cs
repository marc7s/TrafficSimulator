using UnityEngine;

namespace Car
{
    public class WheelController : MonoBehaviour
    {
        #region Wheel Components

        [SerializeField] private WheelCollider _frontRight;
        [SerializeField] private WheelCollider _frontLeft;
        [SerializeField] private WheelCollider _backRight;
        [SerializeField] private WheelCollider _backLeft;

        [SerializeField] private Transform _frontRightTransform;
        [SerializeField] private Transform _frontLeftTransform;
        [SerializeField] private Transform _backRightTransform;
        [SerializeField] private Transform _backLeftTransform;

        #endregion
        
        [SerializeField] private float _acceleration = 500f;
        [SerializeField] private float _breakingForce = 3000f;
        [SerializeField] private float _maxTurnAngle = 15f;

        private float _currentAcceleration;
        private float _currentBrakingForce;
        private float _currentTurnAngle;

        private PlayerInputController _input;

        private void Start()
        {
            _input = GetComponent<PlayerInputController>();
        }

        private void FixedUpdate()
        {
            Accelerate();
            Brake();
            Steer();
            UpdateAllWheelTransform();
        }

        private void Accelerate()
        {
            _currentAcceleration = _acceleration * _input.AccelerationInput;
            _frontRight.motorTorque = _currentAcceleration;
            _frontLeft.motorTorque = _currentAcceleration;
        }

        private void Brake()
        {
            if (_input.BrakingInput != 0)
                _currentBrakingForce = _breakingForce;
            else
                _currentBrakingForce = 0;

            _frontRight.brakeTorque = _currentBrakingForce;
            _frontLeft.brakeTorque = _currentBrakingForce;
            _backRight.brakeTorque = _currentBrakingForce;
            _backLeft.brakeTorque = _currentBrakingForce;
        }

        private void Steer()
        {
            _currentTurnAngle = _maxTurnAngle * _input.SteeringInput;
            _frontLeft.steerAngle = _currentTurnAngle;
            _frontRight.steerAngle = _currentTurnAngle;
        }

        private void UpdateAllWheelTransform()
        {
            UpdateWheelTransform(_frontRight, _frontRightTransform);
            UpdateWheelTransform(_frontLeft, _frontLeftTransform);
            UpdateWheelTransform(_backRight, _backRightTransform);
            UpdateWheelTransform(_backLeft, _backLeftTransform);
        }

        private void UpdateWheelTransform(WheelCollider col, Transform trans)
        {
            col.GetWorldPose(out var position, out var rotation);

            trans.position = position;
            trans.rotation = rotation;
        }
    }
}