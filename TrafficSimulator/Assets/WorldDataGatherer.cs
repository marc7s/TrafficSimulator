using System;
using UnityEngine;

public class WorldDataGatherer : MonoBehaviour
{
    public float TotalFuelConsumed;
    public float FuelConsumedLast30Sec => CalculateTotalFuelConsumedLastSeconds(30);
    public float FuelConsumedLast3Min => CalculateTotalFuelConsumedLastSeconds(180);
    
    // Buffer size to store fuel consumption for the last 3 minutes
    private const int BufferSize = 180; 
    // Circular buffer to store fuel consumption
    private float[] _buffer = new float[BufferSize]; 
    private int _bufferIndex = 0; 

    private float _fuelConsumedThisSecond = 0;
    private float _timeElapsedThisSecond = 0;
    
    public void AddFuelConsumed(float fuelConsumed)
    {
        TotalFuelConsumed += fuelConsumed;
        _fuelConsumedThisSecond += fuelConsumed;
        
        _timeElapsedThisSecond += Time.deltaTime;
        if (_timeElapsedThisSecond >= 1)
        {
            _buffer[_bufferIndex] = _fuelConsumedThisSecond;
            _bufferIndex = (_bufferIndex + 1) % BufferSize; 
            _timeElapsedThisSecond  = 0;
            _fuelConsumedThisSecond = 0;
        }
    }

    private float CalculateTotalFuelConsumedLastSeconds(int seconds)
    {
        float total = 0;
        for(int i = 0; i < seconds; i++)
        {
            int index = (_bufferIndex - 1 - i + BufferSize) % BufferSize;
            total += _buffer[index];
        }
        return total;
    }

    private void Update()
    {
        print($"{TotalFuelConsumed}, {FuelConsumedLast3Min}, {FuelConsumedLast30Sec}");
    }
}