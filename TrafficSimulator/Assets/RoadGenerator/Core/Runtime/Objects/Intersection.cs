using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace RoadGenerator
{
public class Intersection : MonoBehaviour
{
    [SerializeField]
    public GameObject IntersectionObject;

    private RoadSystem _roadSystem;

    public Vector3 StartConnectionPoint;
    public Vector3 EndConnectionPoint;
    public Vector3 ConnectionPointOtherDirection1;
    public Vector3 ConnectionPointOtherDirection2;

    void Start()
    {

    }
    void OnDestroy()
    {
        Debug.Log("Intersection Destroyed");
        _roadSystem.RemoveIntersection(this);
    }
    public void SetConnectionPoints()
    {
        StartConnectionPoint = IntersectionObject.transform.GetChild(1).GetChild(0).transform.position;
        EndConnectionPoint = IntersectionObject.transform.GetChild(1).GetChild(1).transform.position;
        ConnectionPointOtherDirection1 = IntersectionObject.transform.GetChild(0).GetChild(0).transform.position;
        ConnectionPointOtherDirection2 = IntersectionObject.transform.GetChild(0).GetChild(1).transform.position;
    }
}
}
