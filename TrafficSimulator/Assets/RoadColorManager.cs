using System.Collections.Generic;
using Statistics;
using UnityEngine;
using RoadGenerator;

public class RoadColorManager : MonoBehaviour
{
    
    [SerializeField] private GameObject _roadsParent;
    [HideInInspector] public bool ShowRoadColors { get; private set; }
    private List<GameObject> _roads = new List<GameObject>();

    void Start()
    {
        foreach(Transform child in _roadsParent.transform)
            _roads.Add(child.gameObject);
    }
    
    public void SetRoadColor(bool enabled)
    {   
        ShowRoadColors = enabled;
        
        foreach (GameObject road in _roads)
            road.GetComponent<EmissionColor>().EmissionType = enabled ? road.GetComponent<Road>().RoadSystem.CurrentStatisticsGathered : StatisticsType.None;
    }

    public void UpdateRoadColorEmissionType()
    {   
        SetRoadColor(ShowRoadColors);
    }
}
