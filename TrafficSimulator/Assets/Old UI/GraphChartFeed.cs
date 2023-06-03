using ChartAndGraph;
using UnityEngine;
using RoadGenerator;

namespace Old_UI
{
    public class GraphChartFeed : MonoBehaviour
    {
        private enum TimeSpan
        {
            None = 0,
            AllTime = -1,
            ThreeMinutes = 180,
            ThirtySeconds = 30
        }

        public StatisticsType ChartType;
        private GraphChartBase _graph;
        private WorldDataGatherer _worldDataGatherer;
        private TimeSpan _currentGraph = TimeSpan.None;
        private bool _isSetup = false;

        private void Start()
        {
            _worldDataGatherer = GameObject.Find("RoadSystem").GetComponent<WorldDataGatherer>();
            _worldDataGatherer.OnNewStatisticsSample += LoadNewData;
            _graph = GetComponent<GraphChartBase>();

            if (_graph != null)
                _graph.Scrollable = false;

            _isSetup = true;
        }

        private void LoadNewData()
        {
            LoadGraph(_currentGraph);
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

        private float[] GetDataset()
        {
            switch(ChartType)
            {
                case StatisticsType.Emissions:
                    return _worldDataGatherer.FuelConsumedPerSecondHistory;
                case StatisticsType.Congestion:
                    return _worldDataGatherer.CongestionPerSecondHistory;
                default:
                    return new float[]{};
            }
        }
        
        // Used by GraphTab prefab
        public void SetCurrentGraphToNone()
        {
            _currentGraph = TimeSpan.None;
        }

        // Used by GraphTab prefab
        public void LoadAllTimeGraph()
        {
            LoadGraph(TimeSpan.AllTime);
        }

        // Used by GraphTab prefab
        public void LoadLast3MinGraph()
        {
            LoadGraph(TimeSpan.ThreeMinutes);
        }

        // Used by GraphTab prefab
        public void LoadLast30SecondsGraph()
        {
            LoadGraph(TimeSpan.ThirtySeconds);
        }

        private void LoadGraph(TimeSpan timeSpan)
        {
            if(!_isSetup)
                Start();

            int timeSpanInSeconds;
            
            if(timeSpan == TimeSpan.None)
                return;
            
            timeSpanInSeconds = timeSpan == TimeSpan.AllTime ? _worldDataGatherer.ElapsedSeconds : (int)timeSpan;
            
            _currentGraph = timeSpan;
            _graph.DataSource.StartBatch();
            _graph.DataSource.ClearCategory(GetCategory());

            int totalSecondsElapsed = _worldDataGatherer.ElapsedSeconds;
            int startIndex = Mathf.Max(totalSecondsElapsed - timeSpanInSeconds, 0);
            int endIndex = startIndex + timeSpanInSeconds;

            float[] dataset = GetDataset();

            for (int i = startIndex; i < endIndex && i < totalSecondsElapsed; i++)
                _graph.DataSource.AddPointToCategory(GetCategory(), i, dataset[i]);

            _graph.DataSource.EndBatch();
        }
    }
}
