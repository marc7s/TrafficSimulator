using System;
using UnityEngine;
using EVP;

namespace VehicleBrain {
    enum FuelMode
    {
        Realistic,
        Performance
    }
    public class FuelConsumption : MonoBehaviour
    {
        // Editor variables
        [Header("Settings")]
        [SerializeField] private FuelMode _fuelMode = FuelMode.Realistic;

        [Header("Pre defined fuel consumption values (For preformance based fuel consumption)")]
        [SerializeField] private float _fuelConsumptionAtIdle = 0.96f; // L/h
        [SerializeField] private float _fuelConsumptionCity = 10f; // L/100km
        [SerializeField] private float _fuelConsumptionHighway = 6f; // L/100km

        [Header("Engine Specs (For realistic fuel consumption)")]
        [SerializeField] private float _engineDisplacement = 1.6f; // L
        [SerializeField] private float _numberOfCylinders = 4;
        [SerializeField] private float _idlingRPM = 650; // RPM

        // Variables
        private VehicleController _vehicleController;
        private Rigidbody _rigidbody;
        public float _maxFuelCapacity = 60; // L
        public double _totalFuelConsumed = 0;
        public float FuelConsumedSinceLastFrame = 0;
        private Vector3 _lastPosition = Vector3.zero;

        // Vehicle specific values used in the fuel consumption calculation
        private float _idleFuelConsumption = 0; // L/s
        private float _resistance = 0; // N
        private float _lastSpeed = 0; // m/s
        private float _vehicleWeight = 0; // kg
        private float _averageAcceleration = 0; // m/s^2
        private float _airDensity = 1.2256f; // kg/m^3
        private float _vehicleDragCoeff = 0; // Cd
        private float _frontalArea = 0; // m^2
        private float _fuelMeanPressure = 400000f; // Pa
        private float _fuelLowerHeatingValue = 43000000f; // J/kg

        // Start is called before the first frame update
        void Start()
        {
            _vehicleController = GetComponent<VehicleController>();
            _rigidbody = GetComponent<Rigidbody>();
            _vehicleWeight = _rigidbody.mass;
            _lastPosition = transform.position;
            if (_fuelMode == FuelMode.Realistic)
            {
                Q_CalculateIdleFuelConsumption();
            }
        }

        // Update is called once per frame
        void Update()
        {
            if (_fuelMode == FuelMode.Realistic)
            {
                Q_CalculateResistance();
                Q_CalculateFuelConsumption();
            }
            else
            {
                P_CalculateFuelConsumption();
            }
            UpdateVariables();
        }

        private void UpdateVariables()
        {
            _lastSpeed = _vehicleController.speed;
            _lastPosition = transform.position;
        }

        // Calculate the fuel consumed since last frame and add it to the total fuel consumed
        private void P_CalculateFuelConsumption()
        {
            // TODO 
            // calculate the distance more accurately

            // If the vehicle is not moving, calculate the fuel consumed at idle per second
            if (_vehicleController.speed < 0.1f)
            {
                FuelConsumedSinceLastFrame = _fuelConsumptionAtIdle * Time.deltaTime / 3600;
            }
            // If the vehicle is moving at a low speed, calculate the fuel consumed at city speed per 100km
            else if (_vehicleController.speed < 20)
            {
                float distance = Vector3.Distance(_lastPosition, transform.position);
                FuelConsumedSinceLastFrame = _fuelConsumptionCity * distance / 100000;
            }
            // If the vehicle is moving at a high speed, calculate the fuel consumed at highway speed per 100km
            else
            {
                float distance = Vector3.Distance(_lastPosition, transform.position);
                FuelConsumedSinceLastFrame = _fuelConsumptionHighway * distance / 100000;
            }
            _totalFuelConsumed += FuelConsumedSinceLastFrame;
        }

        private void Q_CalculateFuelConsumption()
        {
            // TODO
            // Decice wheter to keep the realistic fuel consumption model and if so
            // complete the calculations and add the missing variables

            // Calculate the distance traveled since last frame and add it to the total distance
            float distance = Vector3.Distance(_lastPosition, transform.position);

            // Calculate the average acceleration since last frame
            _averageAcceleration = MathF.Max((_vehicleController.speed - _lastSpeed) /Time.deltaTime, 0);

            // Calculate the fuel consumed since last frame and add it to the total fuel consumed
            FuelConsumedSinceLastFrame = MathF.Max(_averageAcceleration * Time.deltaTime / 1000, _idleFuelConsumption * Time.deltaTime);
            _totalFuelConsumed += FuelConsumedSinceLastFrame;
        }

        private void Q_CalculateIdleFuelConsumption()
        {
            // Calculate the idle fuel consumption
            _idleFuelConsumption = (_fuelMeanPressure * _idlingRPM * _engineDisplacement) / (22164 * _numberOfCylinders * _fuelLowerHeatingValue);
        }

        private void Q_CalculateResistance()
        {
            // Calculate the resistance
            _resistance = (float) (_airDensity / 25.92) * _vehicleDragCoeff * _frontalArea * MathF.Pow(_vehicleController.speed, 2) + _vehicleWeight * 9.81f * (0.01f / 1000) * (0.0328f * _vehicleController.speed + 4.575f) + _vehicleWeight * 9.81f * transform.rotation.x;
        }
    }
}