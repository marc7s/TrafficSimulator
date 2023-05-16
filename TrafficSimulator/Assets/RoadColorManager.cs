using System.Collections;
using System.Collections.Generic;
using Statistics;
using UnityEngine;

public class RoadColorManager : MonoBehaviour
{
    
    [SerializeField] private GameObject _roadsParent;
    private List<GameObject> _roads = new List<GameObject>();
    [SerializeField] private bool _isEnabled = false;

    void Start()
    {
        foreach(Transform child in _roadsParent.transform)
        {
            _roads.Add(child.gameObject);
        }
    }
    
    public void SetRoadColor()
    {
        _isEnabled = !_isEnabled;
        foreach (GameObject road in _roads)
        {
            road.GetComponent<EmissionColor>().EmissionEnabled = _isEnabled;
        }
    }
}
