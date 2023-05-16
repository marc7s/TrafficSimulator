using ChartAndGraph;
using UnityEngine;

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
        private GraphChartBase _graph;
        private WorldDataGatherer _worldDataGatherer;
        private TimeSpan _currentGraph = TimeSpan.None; 

        private void Start()
        {
            _worldDataGatherer = GameObject.Find("RoadSystem").GetComponent<WorldDataGatherer>();
            _graph = GetComponent<GraphChartBase>();

            if (_graph != null)
                _graph.Scrollable = false;
        }
        
        public void SetCurrentGraphToNone()
        {
            _currentGraph = TimeSpan.None;
        }

        public void LoadAllTimeGraph()
        {
            LoadGraph(TimeSpan.AllTime);
        }

        public void LoadLast3MinGraph()
        {
            LoadGraph(TimeSpan.ThreeMinutes);
        }

        public void LoadLast30SecondsGraph()
        {
            LoadGraph(TimeSpan.ThirtySeconds);
        }

        private void LoadGraph(TimeSpan timeSpan)
        {
            int timeSpanInSeconds;
            if (_currentGraph == timeSpan || timeSpan == TimeSpan.None) return;
            if (timeSpan == TimeSpan.AllTime)
                timeSpanInSeconds = _worldDataGatherer.TotalSecondsElapsed;
            else
                timeSpanInSeconds = (int) timeSpan;
            _currentGraph = timeSpan;
            _graph.DataSource.StartBatch();
            _graph.DataSource.ClearCategory("Emission");

            int totalSecondsElapsed = _worldDataGatherer.TotalSecondsElapsed;
            int startIndex = Mathf.Max(totalSecondsElapsed - timeSpanInSeconds, 0);
            int endIndex = startIndex + timeSpanInSeconds;

            for (int i = startIndex; i < endIndex && i < totalSecondsElapsed; i++)
            {
                _graph.DataSource.AddPointToCategory("Emission", i, _worldDataGatherer.FuelConsumedPerSecondHistory[i]);
            }
            _graph.DataSource.EndBatch();
        }
    }
}
