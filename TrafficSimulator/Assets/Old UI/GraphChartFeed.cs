using ChartAndGraph;
using UnityEngine;
using RoadGenerator;
using System;

namespace Old_UI
{
    public enum TimeSpan
    {
        None = 0,
        AllTime = -1,
        ThreeMinutes = 180,
        ThirtySeconds = 30
    }

    public class GraphChartFeed : MonoBehaviour
    {
        public StatisticsType ChartType;
        private GraphChartBase _graph;
        private WorldDataGatherer _worldDataGatherer;
        public Action OnTimeSpanChanged;
        private bool _isSetup = false;
        private TimeSpan _currentTimeSpan = TimeSpan.None;
        private static GraphChartFeed _activeGraphChartFeed;

        public TimeSpan CurrentTimeSpan 
        {
            get => _currentTimeSpan;
            set
            {
                bool changed = _currentTimeSpan != value;
                _currentTimeSpan = value;

                if(changed)
                    OnTimeSpanChanged?.Invoke();
            }
        }

        private void Start()
        {
            _worldDataGatherer = GameObject.Find("RoadSystem").GetComponent<WorldDataGatherer>();
            _worldDataGatherer.OnNewStatisticsSample += LoadNewData;
            _graph = GetComponent<GraphChartBase>();

            if (_graph != null)
                _graph.Scrollable = false;

            _isSetup = true;
        }

        public void SetActiveGraph()
        {
            _activeGraphChartFeed = this;
        }

        public void ClearActiveGraph()
        {
            _activeGraphChartFeed = null;
        }

        private void LoadNewData()
        {
            LoadGraph(CurrentTimeSpan);
        }

        private string GetCategory()
        {
            switch(ChartType)
            {
                case StatisticsType.Emissions:
                    return "Emission";
                case StatisticsType.Congestion:
                    return "Congestion";
                default:
                    return "";
            }
        }

        private WorldDataGatherer.DataHistory GetDataset()
        {
            switch(ChartType)
            {
                case StatisticsType.Emissions:
                    return _worldDataGatherer.FuelConsumedPerSecondDataset;
                case StatisticsType.Congestion:
                    return _worldDataGatherer.CongestionPerSecondDataset;
                default:
                    return new WorldDataGatherer.DataHistory(0);
            }
        }
        
        // Used by GraphTab prefab
        public void SetCurrentGraphToNone()
        {
            CurrentTimeSpan = TimeSpan.None;
        }

        // Used by GraphTab prefab
        public void LoadAllTimeGraph()
        {
            CurrentTimeSpan = TimeSpan.AllTime;
        }

        // Used by GraphTab prefab
        public void LoadLast3MinGraph()
        {
            CurrentTimeSpan = TimeSpan.ThreeMinutes;
        }

        // Used by GraphTab prefab
        public void LoadLast30SecondsGraph()
        {
            CurrentTimeSpan = TimeSpan.ThirtySeconds;
        }

        private void LoadGraph(TimeSpan timeSpan)
        {
            if(!_isSetup)
                Start();
            
            if(timeSpan == TimeSpan.None)
                return;

            if(_activeGraphChartFeed != this)
                return;
            
            // Get the time span in seconds
            int timeSpanInSeconds = timeSpan == TimeSpan.AllTime ? _worldDataGatherer.CurrentAllTimeSeconds : (int)timeSpan;
            
            // In the beginning while data is still being collected, limit the time span to the amount of data collected
            timeSpanInSeconds = Mathf.Min(timeSpanInSeconds, _worldDataGatherer.CurrentAllTimeSeconds);
            
            CurrentTimeSpan = timeSpan;
            _graph.DataSource.StartBatch();
            _graph.DataSource.ClearCategory(GetCategory());

            
            // Convert the data stored in L/s to L/h and add it to the graph
            for (int i = 0; i < timeSpanInSeconds; i++)
                _graph.DataSource.AddPointToCategory(GetCategory(), i, GetDataset().GetValueNSecondsAgo(timeSpanInSeconds - i) * 3600);

            _graph.DataSource.EndBatch();
        }
    }
}
