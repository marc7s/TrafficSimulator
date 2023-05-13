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

            float timeToUse = 180 > _worldDataGatherer.TotalSecondsElapsed ? _worldDataGatherer.TotalSecondsElapsed : 180;
        
            for (int i = 0; i < timeToUse; i++)
            {
                _graph.DataSource.AddPointToCategory("Emission", i, _worldDataGatherer.FuelConsumedPerSecondHistory[i]);
            }
            _graph.DataSource.EndBatch();
        }

        public void LoadLast30SecondsGraph()
        {
            _graph.DataSource.StartBatch();
            _graph.DataSource.ClearCategory("Emission");
        
            float timeToUse = 30 > _worldDataGatherer.TotalSecondsElapsed ? _worldDataGatherer.TotalSecondsElapsed : 30;
        
            for (int i = 0; i < timeToUse; i++)
            {
                _graph.DataSource.AddPointToCategory("Emission", i, _worldDataGatherer.FuelConsumedPerSecondHistory[i]);
            }
            _graph.DataSource.EndBatch();
        }
    }
}