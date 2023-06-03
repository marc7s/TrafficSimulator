using System;
using UnityEngine;
using System.Collections.Generic;

public class WorldDataGatherer : MonoBehaviour
{
    public class DataHistory
    {
        public float[] Data;
        public int Index;

        private int DataSize => Data.Length;

        public void Update(float value)
        {
            Data[Index] = value;
            Index = (Index + 1) % DataSize;
        }

        public float GetValueNSecondsAgo(int secondsAgo)
        {
            if(secondsAgo >= DataSize)
            {
                Debug.LogError($"Data out of bounds error. Data for {secondsAgo} seconds ago is not available with a buffer size of {DataSize} seconds");
                return 0;
            }

            int index = (Index - secondsAgo + DataSize) % DataSize;
            return Data[index];
        }

        public float GetLastNSecondsTotal(int seconds, float conversionFactor = 1)
        {
            float total = 0;
            
            for(int i = 0; i < seconds; i++)
                total += GetValueNSecondsAgo(i) * conversionFactor;

            return total;
        }

        public DataHistory(int size)
        {
            Data = new float[size];
            Index = 0;
        }
    }

    private class StatisticsSample
    {
        public string RoadID { get; private set; }
        public FuelSample? FuelSample { get; private set; }
        public CongestionSample? CongestionSample { get; private set; }

        public void Update(FuelSample fuelSample)
        {
            FuelSample = fuelSample;
        }

        public void Update(CongestionSample congestionSample)
        {
            CongestionSample = congestionSample;
        }

        public StatisticsSample(string roadID, FuelSample fuelSample)
        {
            RoadID = roadID;
            FuelSample = fuelSample;
            CongestionSample = null;
        }

        public StatisticsSample(string roadID, CongestionSample congestionSample)
        {
            RoadID = roadID;
            CongestionSample = congestionSample;
            FuelSample = null;
        }
    }

    private struct FuelSample
    {
        public float ConsumedFuel { get; private set; }
        
        public FuelSample(float consumedFuel)
        {
            ConsumedFuel = consumedFuel;
        }
    }

    private struct CongestionSample
    {
        public int Vehicles { get; private set; }
        public float RoadLength { get; private set; }
        public float CongestionCoef { get; private set; }
        
        public CongestionSample(int vehicles, float roadLength, float congestionCoef)
        {
            Vehicles = vehicles;
            RoadLength = roadLength;
            CongestionCoef = congestionCoef;
        }
    }
    
    public float Co2EmissionsAllTime => CalculateTotalCo2EmissionsLastSeconds(BufferSize);
    public float Co2EmissionsLast30Sec => CalculateTotalCo2EmissionsLastSeconds(30);
    public float Co2EmissionsLast3Min => CalculateTotalCo2EmissionsLastSeconds(180);

    private Dictionary<string, StatisticsSample> _statisticsSamples = new Dictionary<string, StatisticsSample>();
    private float _elapsedTime = 0;
    public int ElapsedSeconds { get; private set; } = 0;
    public int CurrentAllTimeSeconds => Mathf.Min(ElapsedSeconds, BufferSize);
    
    // Buffer size to store fuel consumption for the hour.
    private const int BufferSize = 3600;
    
    // Circular buffer to store fuel consumption
    [HideInInspector] public DataHistory FuelConsumedPerSecondDataset { get; private set; } = new DataHistory(BufferSize);

    // Circular buffer to store congestion
    [HideInInspector] public DataHistory CongestionPerSecondDataset { get; private set; } = new DataHistory(BufferSize);

    public Action OnNewStatisticsSample;
    
    public void AddFuelConsumed(string roadID, float fuelConsumed)
    {
        FuelSample sample = new FuelSample(fuelConsumed);

        if(_statisticsSamples.ContainsKey(roadID))
            _statisticsSamples[roadID].Update(sample);
        else
            _statisticsSamples.Add(roadID, new StatisticsSample(roadID, sample));
    }

    public void AddCongestion(string roadID, int vehicles, float roadLength, float congestionCoef)
    {
        CongestionSample sample = new CongestionSample(vehicles, roadLength, congestionCoef);

        if(_statisticsSamples.ContainsKey(roadID))
            _statisticsSamples[roadID].Update(sample);
        else
            _statisticsSamples.Add(roadID, new StatisticsSample(roadID, sample));
    }

    public void Update()
    {
        _elapsedTime += Time.deltaTime;

        if (_elapsedTime >= 1)
        {
            _elapsedTime = 0;
            ElapsedSeconds++;
            CreateStatisticsSample();
        }
    }

    private void CreateStatisticsSample()
    {
        float totalFuelConsumed = 0;
        int totalCongestionSamples = 0;
        int totalCongestedSamples = 0;

        foreach(StatisticsSample statSample in _statisticsSamples.Values)
        {
            if(statSample.FuelSample != null)
            {
                FuelSample fuelSample = statSample.FuelSample.Value;
                totalFuelConsumed += fuelSample.ConsumedFuel;
            }

            if(statSample.CongestionSample != null)
            {
                CongestionSample congestionSample = statSample.CongestionSample.Value;
                
                if(congestionSample.RoadLength == 0)
                    break;
                
                float congestionRatio = congestionSample.Vehicles * congestionSample.CongestionCoef / congestionSample.RoadLength;
                
                // The threshold to determine if the road is considered congested or not
                if(congestionRatio > 0.7f)
                    totalCongestedSamples++;
                
                totalCongestionSamples++;
            }
        }

        FuelConsumedPerSecondDataset.Update(totalFuelConsumed);
        CongestionPerSecondDataset.Update(totalCongestionSamples == 0 ? 0 : (float)totalCongestedSamples / totalCongestionSamples);
        
        _statisticsSamples.Clear();
        OnNewStatisticsSample?.Invoke();
    }
    
    public float CalculateTotalFuelConsumedLastSeconds(int seconds) => CalculateTotalEmissionsLastSeconds(seconds, 1);
    public float CalculateTotalCo2EmissionsLastSeconds(int seconds) => CalculateTotalEmissionsLastSeconds(seconds, 2.3f);
    private float CalculateTotalEmissionsLastSeconds(int seconds, float conversionFactor) => FuelConsumedPerSecondDataset.GetLastNSecondsTotal(seconds, conversionFactor);
}