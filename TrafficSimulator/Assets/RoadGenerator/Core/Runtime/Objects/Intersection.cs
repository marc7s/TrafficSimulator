using UnityEngine;
using System.Collections.Generic;
using System;

namespace RoadGenerator
{
[ExecuteInEditMode()]
[Serializable]
public class Intersection : MonoBehaviour
{
    [HideInInspector] public GameObject IntersectionObject;
    [HideInInspector] public RoadSystem RoadSystem;
    [HideInInspector] public Vector3 IntersectionPosition;
    [HideInInspector] public Road Road1;
    [HideInInspector] public Road Road2;
    [HideInInspector] public PathCreator Road1PathCreator;
    [HideInInspector] public PathCreator Road2PathCreator;
    [HideInInspector] public Vector3 Road1AnchorPoint1;
    [HideInInspector] public Vector3 Road1AnchorPoint2;
    [HideInInspector] public Vector3 Road2AnchorPoint1;
    [HideInInspector] public Vector3 Road2AnchorPoint2;
    public const float IntersectionLength = 20f;
    public const float IntersectionBoundsLengthMultiplier = 1.2f;
    
    /// <summary>Cleans up the intersection and removes the references to it from the road system and roads</summary>
    void OnDestroy()
    {
        // Remove reference to intersection in the road system
        RoadSystem.RemoveIntersection(this);

        // Remove the anchor points for the intersection
        Road1PathCreator.bezierPath.RemoveAnchors(new List<Vector3>{ Road1AnchorPoint1, Road1AnchorPoint2 });
        Road2PathCreator.bezierPath.RemoveAnchors(new List<Vector3>{ Road2AnchorPoint1, Road2AnchorPoint2 });
        
        // Remove reference to intersection in the roads
        if (Road1?.HasIntersection(this) == true)
            Road1.RemoveIntersection(this);
        if (Road2?.HasIntersection(this) == true)
            Road2.RemoveIntersection(this);
    }
}
}
