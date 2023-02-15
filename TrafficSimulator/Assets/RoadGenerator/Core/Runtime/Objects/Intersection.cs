using UnityEngine;

namespace RoadGenerator
{
[ExecuteInEditMode()]
public class Intersection : MonoBehaviour
{
    [HideInInspector]
    [SerializeField]
    public GameObject IntersectionObject;
    [HideInInspector]
    public RoadSystem RoadSystem;
    [HideInInspector]
    public Vector3 IntersectionPosition;
    [HideInInspector]
    public Road Road1;
    [HideInInspector]
    public Road Road2;
    [HideInInspector]
    public PathCreator Road1PathCreator;
    [HideInInspector]
    public PathCreator Road2PathCreator;
    [HideInInspector]
    public Vector3 Road1AnchorPoint1;
    [HideInInspector]
    public Vector3 Road1AnchorPoint2;
    [HideInInspector]
    public Vector3 Road2AnchorPoint1;
    [HideInInspector]
    public Vector3 Road2AnchorPoint2;

    public float IntersectionLength = 20f;
    void OnDestroy()
    {
        // Remove reference to intersection in the road system
        RoadSystem.RemoveIntersection(this);
        // Finding the intersection anchors and remove them
        for (int i = 0; i < Road1PathCreator.bezierPath.NumPoints; i += 3)
        {
            int handleIndex = i % Road1PathCreator.bezierPath.NumPoints;
            if (Road1PathCreator.bezierPath[handleIndex] == Road1AnchorPoint1 || Road1PathCreator.bezierPath[handleIndex] == Road1AnchorPoint2) {
                Road1PathCreator.bezierPath.DeleteSegment(handleIndex);
                Road1PathCreator.bezierPath.DeleteSegment(handleIndex);
                break;
            }
        }
        for (int i = 0; i < Road2PathCreator.bezierPath.NumPoints; i += 3)
        {
            int handleIndex = i % Road2PathCreator.bezierPath.NumPoints;
            if (Road2PathCreator.bezierPath[handleIndex] == Road2AnchorPoint1 || Road2PathCreator.bezierPath[handleIndex] == Road2AnchorPoint2) {
                Road2PathCreator.bezierPath.DeleteSegment(handleIndex);
                Road2PathCreator.bezierPath.DeleteSegment(handleIndex);
                break;
            }
        }
    }
}
}
