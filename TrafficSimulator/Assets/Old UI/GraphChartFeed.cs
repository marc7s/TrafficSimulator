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
            _graph.DataSource.StartBatch();
            _graph.DataSource.ClearCategory("Emission");
            for (int i = 0; i < _worldDataGatherer.TotalSecondsElapsed; i++)
            {
                _graph.DataSource.AddPointToCategory("Emission", i, _worldDataGatherer.FuelConsumedPerSecondHistory[i]);
            }
            _graph.DataSource.EndBatch();
        }
    
        public void LoadLast3MinGraph()
        {
            _graph.DataSource.StartBatch();
            _graph.DataSource.ClearCategory("Emission");
            
            int xIntervalSkip = 4;
            int totalSecondsElapsed = _worldDataGatherer.TotalSecondsElapsed;

            int startIndex = Mathf.Max(totalSecondsElapsed - 180, 0);

            for (int i = startIndex; i < (startIndex + 180); i += xIntervalSkip)
            {
                _graph.DataSource.AddPointToCategory("Emission", i, _worldDataGatherer.FuelConsumedPerSecondHistory[i]);
            }
            _graph.DataSource.EndBatch();
        }

        public void LoadLast30SecondsGraph()
        {
            _graph.DataSource.StartBatch();
            _graph.DataSource.ClearCategory("Emission");
            
            int xIntervalSkip = 1;
            int totalSecondsElapsed = _worldDataGatherer.TotalSecondsElapsed;
            
            int startIndex = Mathf.Max(totalSecondsElapsed - 30, 0);

            for (int i = startIndex; i < (startIndex + 30); i += xIntervalSkip)
            {
                _graph.DataSource.AddPointToCategory("Emission", i, _worldDataGatherer.FuelConsumedPerSecondHistory[i]);
            }
            _graph.DataSource.EndBatch();
        }
    }
}