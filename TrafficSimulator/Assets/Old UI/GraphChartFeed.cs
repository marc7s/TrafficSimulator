using ChartAndGraph;
using UnityEngine;

namespace Old_UI
{
    public class GraphChartFeed : MonoBehaviour
    {
        private enum TimeSpan
        {
            AllTime,
            ThreeMinutes,
            ThirtySeconds
        }

        private GraphChartBase _graph;
        private WorldDataGatherer _worldDataGatherer;
        private TimeSpan _currentGraph; 

        private void Start()
        {
            _worldDataGatherer = GameObject.Find("RoadSystem").GetComponent<WorldDataGatherer>();
            _graph = GetComponent<GraphChartBase>();

            if (_graph != null)
            {
                _graph.Scrollable = false;
                LoadAllTimeGraph();
            }
        }

        public void LoadAllTimeGraph()
        {
            LoadGraph(_worldDataGatherer.TotalSecondsElapsed);
        }

        public void LoadLast3MinGraph()
        {
            LoadGraph(180);
        }

        public void LoadLast30SecondsGraph()
        {
            LoadGraph(30);
        }

        private void LoadGraph(int timeSpanInSeconds)
        {
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