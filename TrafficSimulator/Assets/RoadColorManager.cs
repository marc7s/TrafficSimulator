using System.Collections.Generic;
using Statistics;
using UnityEngine;

public class RoadColorManager : MonoBehaviour
{
    
    [SerializeField] private GameObject _roadsParent;
    private List<GameObject> _roads = new List<GameObject>();

    void Start()
    {
        foreach(Transform child in _roadsParent.transform)
            _roads.Add(child.gameObject);
    }
    
    public void SetRoadColor(bool enabled)
    {
        foreach (GameObject road in _roads)
            road.GetComponent<EmissionColor>().EmissionEnabled = enabled;
    }
}
