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

    public Vector3 IntersectionPosition;

    public Road Road1;

    public Road Road2;

    public PathCreator otherRoadCreator;

    public const float RADIUS = 5f;

    // TODO find out why this is not being called
    [ExecuteInEditMode]
    void OnDestroy()
    {
        Debug.Log("Intersection Destroyed");
        _roadSystem.RemoveIntersection(this);
    }
    void OnDisable()
    {
        Debug.Log("Intersection Disabled");
        _roadSystem.RemoveIntersection(this);
    }
  [ExecuteInEditMode]
    void OnUndoRedo()
    {
        Debug.Log("Intersection UndoRedo");
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
