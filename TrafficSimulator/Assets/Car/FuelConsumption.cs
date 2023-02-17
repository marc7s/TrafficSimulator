using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;
using EVP;

public class FuelConsumption : MonoBehaviour
{
    // Variables
    private VehicleController _vehicleController;
    private Rigidbody _rigidbody;
    private double _totalFuelConsumed = 0;
    private float _fuelConsumed = 0;
    private Vector3 _lastPosition = Vector3.zero;
    private double _totalDistance = 0;
    private float _lastSpeed = 0;
    private float _vehicleWeight = 0;
    private float avrgAcc = 0;

    // Start is called before the first frame update
    void Start()
    {
        _vehicleController = GetComponent<VehicleController>();
        _rigidbody = GetComponent<Rigidbody>();
        _vehicleWeight = _rigidbody.mass;
        _lastPosition = transform.position;
    }

    // Update is called once per frame
    void Update()
    {
        calculateFuelConsumption();
        updateVariables();
        print("Avrage acceleration: " + avrgAcc + " m/s");
        print("Current consumption: " + _fuelConsumed + "L");
        print("Fuel consumed: " + _totalFuelConsumed + "L");
    }

    void updateVariables()
    {
        _lastSpeed = _vehicleController.speed;
        _lastPosition = transform.position;
    }

    void calculateFuelConsumption()
    {
        // Calculate the distance traveled since last frame and add it to the total distance
        float distance = Vector3.Distance(_lastPosition, transform.position);
        _totalDistance += distance;

        // Calculate the avrage acceleration since last frame
        avrgAcc = MathF.Max((_vehicleController.speed - _lastSpeed) /Time.deltaTime, 0);

        // Calculate the fuel consumed since last frame and add it to the total fuel consumed
        _fuelConsumed = MathF.Max(avrgAcc * Time.deltaTime / 1000, (float)0.6/60/60 * Time.deltaTime);
        _totalFuelConsumed += _fuelConsumed;

    }
}
