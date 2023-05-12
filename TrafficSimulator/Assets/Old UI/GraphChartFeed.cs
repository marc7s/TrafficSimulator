using UnityEngine;
using ChartAndGraph;
using System.Collections;

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
            print(_worldDataGatherer.FuelConsumedPerSecondHistory[i]);
        }
        _graph.DataSource.EndBatch();
    }
}