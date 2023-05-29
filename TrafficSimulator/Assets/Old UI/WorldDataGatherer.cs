using System;
using UnityEngine;

public class WorldDataGatherer : MonoBehaviour
{
    [HideInInspector] public int TotalSecondsElapsed;
    [HideInInspector] public float TotalFuelConsumed;

    // This would require the streaming graph which the project had not obtained at the time of writing the code
    // to implement for real. Set one hour to represent all time;
    public float FuelConsumedAllTime => CalculateTotalFuelConsumedLastSeconds(3600);
    public float FuelConsumedLast30Sec => CalculateTotalFuelConsumedLastSeconds(30);
    public float FuelConsumedLast3Min => CalculateTotalFuelConsumedLastSeconds(180);
    
    public float Co2EmissionsAllTime => CalculateTotalCo2EmissionsLastSeconds(3600);
    public float Co2EmissionsLast30Sec => CalculateTotalCo2EmissionsLastSeconds(30);
    public float Co2EmissionsLast3Min => CalculateTotalCo2EmissionsLastSeconds(180);

    
    // Buffer size to store fuel consumption for the hour.
    private const int BufferSize = 3600; 
    
    // Circular buffer to store fuel consumption. 
    [HideInInspector] public float[] FuelConsumedPerSecondHistory = new float[BufferSize]; 
    private int _bufferIndex = 0; 

    private float _fuelConsumedThisSecond = 0;
    private float _timeElapsedThisSecond = 0;

    public float TotalCo2Emissions => TotalFuelConsumed * 2.3f; // co2 per kilogram
    
    public void AddFuelConsumed(float fuelConsumed)
    {
        TotalFuelConsumed += fuelConsumed;
        _fuelConsumedThisSecond += fuelConsumed;
        
        _timeElapsedThisSecond += Time.deltaTime;
        if (_timeElapsedThisSecond >= 1)
        {
            FuelConsumedPerSecondHistory[_bufferIndex] = _fuelConsumedThisSecond;
            _bufferIndex = (_bufferIndex + 1) % BufferSize;
            _timeElapsedThisSecond  = 0;
            _fuelConsumedThisSecond = 0;
            TotalSecondsElapsed += 1;
        }
    }
    
    public float CalculateTotalFuelConsumedLastSeconds(int seconds) => CalculateTotalEmissionsLastSeconds(seconds, 1);
    public float CalculateTotalCo2EmissionsLastSeconds(int seconds) => CalculateTotalEmissionsLastSeconds(seconds, 2.3f);

    private float CalculateTotalEmissionsLastSeconds(int seconds, float conversionFactor)
    {
        float total = 0;
        for(int i = 0; i < seconds; i++)
        {
            int index = (_bufferIndex - 1 - i + BufferSize) % BufferSize;
            total += FuelConsumedPerSecondHistory[index] * conversionFactor;
        }
        return total;
    }
}