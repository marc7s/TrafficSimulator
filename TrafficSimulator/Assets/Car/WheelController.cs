using UnityEngine;
using System;

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

        [SerializeField] private bool _manualDriveMode = false;
        [SerializeField] private float _maxSpeed = 200f;
        [SerializeField] private float _maxAcceleration = 500f;
        [SerializeField] private float _maxBrakingForce = 3000f;
        [SerializeField] private float _maxTurnAngle = 15f;

        private float _currentAcceleration;
        private float _currentBrakingForce;
        private float _currentTurnAngle;

        private PlayerInputController _input;
        private Rigidbody _rigidbody;

        private void Start()
        {
            _input = GetComponent<PlayerInputController>();
            _rigidbody = GetComponent<Rigidbody>();
        }

        private void FixedUpdate()
        {
            if(_manualDriveMode)
            {
                SetAccelerationPercent(_input.AccelerationInput);
                if (_input.BrakingInput != 0)
                    SetBrakingForcePercent(_input.BrakingInput);
                else
                    _currentBrakingForce = 0;
                SetTurnAnglePercent(_input.SteeringInput);
            }
                
            Accelerate();
            Brake();
            Steer();
            UpdateAllWheelTransform();
        }

        public void SetAcceleration(float acceleration) => _currentAcceleration = limit(acceleration, _maxAcceleration);
        public void SetAccelerationPercent(float accelerationPercent) => _currentAcceleration = limit(accelerationPercent, 1f) * _maxAcceleration;
        public void SetBrakingForce(float brakingForce) => _currentBrakingForce = limit(brakingForce, _maxBrakingForce);
        public void SetBrakingForcePercent(float brakingForcePercent) => _currentBrakingForce = limit(brakingForcePercent, 1f) * _maxBrakingForce;
        public void SetTurnAngle(float turnAngle) => _currentTurnAngle = limit(turnAngle, _maxTurnAngle);
        public void SetTurnAnglePercent(float turnAnglePercent) => _currentTurnAngle = limit(turnAnglePercent, 1f) * _maxTurnAngle;
        public float GetMaxAcceleration() => _maxAcceleration;
        public float GetTurnAngle() => _currentTurnAngle;
        public float GetTurnAnglePercent() => _currentTurnAngle / _maxTurnAngle;
        public float GetMaxSpeed() => _maxSpeed;
        public void SetMaxSpeed(float maxSpeed) => _maxSpeed = maxSpeed;

        public float GetCurrentSpeed() => _rigidbody.velocity.sqrMagnitude;


        private void Accelerate()
        {
            float torque = _rigidbody.velocity.sqrMagnitude <= _maxSpeed ? _currentAcceleration : 0;
            _frontRight.motorTorque = torque;
            _frontLeft.motorTorque = torque;
        }

        private void Brake()
        {
            _frontRight.brakeTorque = _currentBrakingForce;
            _frontLeft.brakeTorque = _currentBrakingForce;
            _backRight.brakeTorque = _currentBrakingForce;
            _backLeft.brakeTorque = _currentBrakingForce;
        }

        private void Steer()
        {
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

        /// <summary> Limits a value so that it cannot exceed the provided limit, in either positive or negative direction </summary>
        private float limit(float value, float limit) {
            float signCoef = Math.Sign(value);
            return Math.Min(Math.Abs(value), limit) * signCoef;
        }
    }
}