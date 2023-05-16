using System.Collections;
using System.Collections.Generic;
using Statistics;
using UnityEngine;

public class RoadColorManager : MonoBehaviour
{
    
    [SerializeField] private GameObject _roadsParent;
    private List<GameObject> _roads;

    void Start()
    {
        foreach(Transform child in _roadsParent.transform)
        {
            _roads.Add(child.gameObject);
        }
    }
    
    public void SetRoadColor()
    {
        foreach (GameObject road in _roads)
        {
            road.GetComponent<EmissionColor>().EmissionEnabled = true;
        }
    }
}
