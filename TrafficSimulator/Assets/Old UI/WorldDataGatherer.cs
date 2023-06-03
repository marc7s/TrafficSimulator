using System;
using UnityEngine;
using System.Collections.Generic;

public class WorldDataGatherer : MonoBehaviour
{
    private struct DataHistory
    {
        public float[] Data;
        public int Index;
        public int SecondsElapsed;

        private int DataSize => Data.Length;

        public void Update(float value)
        {
            Data[Index] = value;
            Index = (Index + 1) % DataSize;
        }

        public float GetLastNSecondsTotal(int seconds, float conversionFactor = 1)
        {
            float total = 0;
            
            for(int i = 0; i < seconds; i++)
            {
                int index = (Index - i + DataSize - 1) % DataSize;
                total += Data[index] * conversionFactor;
            }

            return total;
        }

        public DataHistory(int size)
        {
            Data = new float[size];
            Index = 0;
            SecondsElapsed = 0;
        }
    }

    private struct StatisticsSample
    {
        public string RoadID { get; private set; }
        public FuelSample? FuelSample { get; private set; }
        public CongestionSample? CongestionSample { get; private set; }

        public StatisticsSample(string roadID, FuelSample? fuelSample, CongestionSample? congestionSample)
        {
            RoadID = roadID;
            FuelSample = fuelSample;
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
    
    // Buffer size to store fuel consumption for the hour.
    private const int BufferSize = 3600; 
    
    // Circular buffer to store fuel consumption
    [HideInInspector] private DataHistory FuelConsumedPerSecondDataset = new DataHistory(BufferSize);
    public float[] FuelConsumedPerSecondHistory => FuelConsumedPerSecondDataset.Data;

    // Circular buffer to store congestion
    [HideInInspector] private DataHistory CongestionPerSecondDataset = new DataHistory(BufferSize);
    public float[] CongestionPerSecondHistory => CongestionPerSecondDataset.Data;

    public Action OnNewStatisticsSample;
    
    public void AddFuelConsumed(string roadID, float fuelConsumed)
    {
        FuelSample sample = new FuelSample(fuelConsumed);

        if(_statisticsSamples.ContainsKey(roadID))
            _statisticsSamples[roadID] = new StatisticsSample(roadID, sample, _statisticsSamples[roadID].CongestionSample);
        else
            _statisticsSamples.Add(roadID, new StatisticsSample(roadID, sample));
    }

    public void AddCongestion(string roadID, int vehicles, float roadLength, float congestionCoef)
    {
        CongestionSample sample = new CongestionSample(vehicles, roadLength, congestionCoef);

        if(_statisticsSamples.ContainsKey(roadID))
            _statisticsSamples[roadID] = new StatisticsSample(roadID, _statisticsSamples[roadID].FuelSample, sample);
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