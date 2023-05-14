using ChartAndGraph;
using UnityEngine;

namespace Old_UI
{
    public class GraphChartFeed : MonoBehaviour
    {
        private GraphChartBase _graph;
        private WorldDataGatherer _worldDataGatherer;

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
            LoadGraph(0, _worldDataGatherer.TotalSecondsElapsed);
        }

        public void LoadLast3MinGraph()
        {
            int totalSecondsElapsed = _worldDataGatherer.TotalSecondsElapsed;
            int startIndex = Mathf.Max(totalSecondsElapsed - 180, 0);
            LoadGraph(startIndex, 180);
        }

        public void LoadLast30SecondsGraph()
        {
            int totalSecondsElapsed = _worldDataGatherer.TotalSecondsElapsed;
            int startIndex = Mathf.Max(totalSecondsElapsed - 30, 0);
            LoadGraph(startIndex, 30);
        }

        private void LoadGraph(int startIndex, int length)
        {
            _graph.DataSource.StartBatch();
            _graph.DataSource.ClearCategory("Emission");

            for (int i = startIndex; i < startIndex + length; i++)
            {
                _graph.DataSource.AddPointToCategory("Emission", i, _worldDataGatherer.FuelConsumedPerSecondHistory[i]);
            }
            _graph.DataSource.EndBatch();
        }
    }
}