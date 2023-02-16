using System.Collections.Generic;
using UnityEngine;
using RoadGenerator.Utility;

namespace RoadGenerator{
public static class IntersectionCreator
{
public static void UpdateIntersections(PathCreator creator, Road thisRoad, int segmentIndex = 0)
{
    RoadSystem roadSystem = thisRoad.RoadSystem;
    Vector3[] segment1 = creator.bezierPath.GetPointsInSegment(creator.bezierPath.NumSegments - 1);
    int startingVertexIndexThisRoad = creator.path.GetPointClosestPointIndex(segment1[0]);
    int endingVertexIndexThisRoad = creator.path.GetPointClosestPointIndex(segment1[3], false);
    List<IntersectionPointData> intersectionPointDatas = new List<IntersectionPointData>();
    foreach(Road road in roadSystem.Roads) {
        if (road == thisRoad)
            continue;
        PathCreator pathCreator = road.RoadObject.GetComponent<PathCreator>();
        // If the two road bounds are not overlapping, then intersection is not possible
        if (!creator.bezierPath.PathBounds.Intersects(pathCreator.bezierPath.PathBounds)) {
            continue;
        }
        // Loop through all bezier anchor points of the other road
        for (int i = 0; i < pathCreator.bezierPath.NumSegments; i++) 
        {
            Vector3[] segment2 = pathCreator.bezierPath.GetPointsInSegment(i);
            // If the two segment bounds are not overlapping, then intersection is not possible
            if (!IntersectionUtility.IsBezierPathIntersectionPossible(segment1, segment2)) {
                continue;
            }
             List<IntersectionPointData> test = GetBezierPathIntersections(startingVertexIndexThisRoad, endingVertexIndexThisRoad, creator,  pathCreator, segment2);
            if (test.Count > 0)
            {
                Debug.Log(road.name + " hehehe" + i);
                Debug.Log(segment2[0] + " öö " + segment2[3]);
                Debug.Log(pathCreator.bezierPath.NumPoints);
            }
                
            intersectionPointDatas.AddRange(test);
        }
    }
    for (int i = 0; i < intersectionPointDatas.Count; i++) {
        CreateIntersectionAtPosition(intersectionPointDatas[i], thisRoad);
    }
}

/// <summary> Get the intersections from the starting vertex index to the endingIndex  </summary>
static List<IntersectionPointData> GetBezierPathIntersections(int startingVertexIndexRoad1, int endingVertexIndexRoad1, PathCreator road1PathCreator, PathCreator road2PathCreator, Vector3[] Road2SegmentPoints)
{
List<IntersectionPointData> intersectionsPoints = new List<IntersectionPointData>();
Vector3 Road2SegmentAnchorPosition1 = Road2SegmentPoints[0];
Vector3 Road2SegmentAnchorPosition2 = Road2SegmentPoints[3];

int startingVertexIndexRoad2 = road2PathCreator.path.GetPointClosestPointIndex(Road2SegmentAnchorPosition1);
int endingVertexIndexRoad2 = road2PathCreator.path.GetPointClosestPointIndex(Road2SegmentAnchorPosition2, false);
for (int i = 0; i < endingVertexIndexRoad1 - startingVertexIndexRoad1; i++)
{
    for (int j = 0; j < endingVertexIndexRoad2 - startingVertexIndexRoad2; j++)
    {
    Vector2 Road1VertexPoint1 = IntersectionUtility.Get2DPointFromXZPlane(road1PathCreator.path.GetPoint(startingVertexIndexRoad1 + i));
    Vector2 Road1VertexPoint2 = IntersectionUtility.Get2DPointFromXZPlane(road1PathCreator.path.GetPoint(startingVertexIndexRoad1 + i+1));
    Vector2 Road2VertexPoint1 = IntersectionUtility.Get2DPointFromXZPlane(road2PathCreator.path.GetPoint(startingVertexIndexRoad2 + j));
    Vector2 Road2VertexPoint2 = IntersectionUtility.Get2DPointFromXZPlane(road2PathCreator.path.GetPoint(startingVertexIndexRoad2 + j+1));

    // If the vertex lines are intercepting
    if (IntersectionUtility.IntersectLineSegments2D(Road1VertexPoint1, Road1VertexPoint2, Road2VertexPoint1, Road2VertexPoint2, out Vector2 intersectionPoint))
    {
        // The intersections rotation is the rotation of the active roads vertex line at the intersection point
        Quaternion rotation = Quaternion.FromToRotation(Vector3.forward, new Vector3(Road1VertexPoint1.x, 0, Road1VertexPoint1.y) - new Vector3(Road1VertexPoint2.x, 0, Road1VertexPoint2.y));
        IntersectionPointData intersectionPointData = new IntersectionPointData(intersectionPoint, rotation, road1PathCreator, road2PathCreator);
        intersectionsPoints.Add(intersectionPointData);
    }
    }
}
return intersectionsPoints;
}

/// <summary> Creates an intersection at the given position </summary>
static void CreateIntersectionAtPosition(IntersectionPointData intersectionPointData, Road thisRoad)
{
    Debug.Log("Creating intersection at position: " + intersectionPointData.IntersectionPosition);
    RoadSystem roadSystem = thisRoad.RoadSystem;
    Vector3 intersectionPoint = new Vector3(intersectionPointData.IntersectionPosition.x, 0, intersectionPointData.IntersectionPosition.y);
    PathCreator road1PathCreator = intersectionPointData.Road1PathCreator;
    PathCreator road2PathCreator = intersectionPointData.Road2PathCreator;
    Intersection intersection = roadSystem.AddNewIntersection(intersectionPointData, thisRoad, GetRoad(road2PathCreator));

    int segmentIndex = GetClosestSegmentIndex(road1PathCreator, intersectionPoint);
    Vector3 segmentStartPoint = road1PathCreator.bezierPath.GetPointsInSegment(segmentIndex)[0];
    float distanceAtIntersection = road1PathCreator.path.GetClosestDistanceAlongPath(intersectionPoint);
    Vector3 position = road1PathCreator.path.GetPointAtDistance(distanceAtIntersection + intersection.IntersectionLength/2);
    Vector3 postion2 = road1PathCreator.path.GetPointAtDistance(distanceAtIntersection - intersection.IntersectionLength/2);
    intersection.Road1AnchorPoint1 = position;
    intersection.Road1AnchorPoint2 = postion2;
    if (Vector3.Distance(segmentStartPoint, intersection.Road1AnchorPoint1) < Vector3.Distance(segmentStartPoint, intersection.Road1AnchorPoint2))
    {
        road1PathCreator.bezierPath.SplitSegment(intersection.Road1AnchorPoint1, segmentIndex, road1PathCreator.path.GetClosestTimeOnPath(intersection.Road1AnchorPoint1));
        road1PathCreator.bezierPath.SplitSegment(intersection.Road1AnchorPoint2, segmentIndex, road1PathCreator.path.GetClosestTimeOnPath(intersection.Road1AnchorPoint2));
    }
    else
    {
        road1PathCreator.bezierPath.SplitSegment(intersection.Road1AnchorPoint2, segmentIndex, road1PathCreator.path.GetClosestTimeOnPath(intersection.Road1AnchorPoint2));
        road1PathCreator.bezierPath.SplitSegment(intersection.Road1AnchorPoint1, segmentIndex + 1, road1PathCreator.path.GetClosestTimeOnPath(intersection.Road1AnchorPoint1));
    }

    segmentIndex = GetClosestSegmentIndex(road2PathCreator, intersectionPoint);
    segmentStartPoint = road2PathCreator.bezierPath.GetPointsInSegment(segmentIndex)[0];
    distanceAtIntersection = road2PathCreator.path.GetClosestDistanceAlongPath(intersectionPoint);
    position = road2PathCreator.path.GetPointAtDistance(distanceAtIntersection + intersection.IntersectionLength/2);
    postion2 = road2PathCreator.path.GetPointAtDistance(distanceAtIntersection - intersection.IntersectionLength/2);
    intersection.Road2AnchorPoint1 = position;
    intersection.Road2AnchorPoint2 = postion2;
    if (Vector3.Distance(segmentStartPoint, position) < Vector3.Distance(segmentStartPoint, postion2))
    {
        road2PathCreator.bezierPath.SplitSegment(position, segmentIndex, road2PathCreator.path.GetClosestTimeOnPath(position));
        road2PathCreator.bezierPath.SplitSegment(postion2, segmentIndex + 1, road2PathCreator.path.GetClosestTimeOnPath(postion2));
    }
    else
    {
        road2PathCreator.bezierPath.SplitSegment(postion2, segmentIndex, road2PathCreator.path.GetClosestTimeOnPath(postion2));
        road2PathCreator.bezierPath.SplitSegment(position, segmentIndex + 1, road2PathCreator.path.GetClosestTimeOnPath(position));
    }
    DeleteAnchorsInsideIntersectionBounds(intersection);
}

/// <summary> Get the index of the segment that is closest to the given position </summary>
static int GetClosestSegmentIndex(PathCreator creator, Vector3 position)
{
int closestSegmentIndex = -1;
// Finding the closest segment by comparing the distance along the vertex path and finding the segment with the smallest distance
float positionDistance = creator.path.GetClosestDistanceAlongPath(position);
for (int i = 0; i < creator.bezierPath.NumSegments; i++)
{
    float segmentPoint1 = creator.path.GetClosestDistanceAlongPath(creator.bezierPath.GetPointsInSegment(i)[0]);
    float segmentPoint2 = creator.path.GetClosestDistanceAlongPath(creator.bezierPath.GetPointsInSegment(i)[3]);
    if (positionDistance >= segmentPoint1 && positionDistance <= segmentPoint2)
    {
        return i;
    }
}
return closestSegmentIndex;
}

/// <summary> Deletes all non intersection anchors that are inside the intersection bounds </summary>
static void DeleteAnchorsInsideIntersectionBounds(Intersection intersection)
{
    float distanceToIntersectionAnchor = Vector3.Distance(intersection.IntersectionPosition, intersection.Road1AnchorPoint1);
    Bounds intersectionBounds = new Bounds(intersection.IntersectionPosition, new Vector3(distanceToIntersectionAnchor*2.5f, 1, distanceToIntersectionAnchor*2.5f));
    // If there is an existing handle in the intersection area, delete it
    for (int i = 0; i < intersection.Road2PathCreator.bezierPath.NumPoints; i += 3)
    {
        int handleIndex1 = i % intersection.Road2PathCreator.bezierPath.NumPoints;
        // Don't delete the intersection handles
        if (intersection.Road2PathCreator.bezierPath[handleIndex1] == intersection.Road2AnchorPoint1 || intersection.Road2PathCreator.bezierPath[handleIndex1] == intersection.Road2AnchorPoint2) 
        {
            continue;
        }
        if (intersectionBounds.Contains(intersection.Road2PathCreator.bezierPath[handleIndex1])){
            intersection.Road2PathCreator.bezierPath.DeleteSegment(handleIndex1);
        }
    }
    for (int i = 0; i < intersection.Road1PathCreator.bezierPath.NumPoints; i += 3)
    {
        int handleIndex1 = i % intersection.Road1PathCreator.bezierPath.NumPoints;
        // Don't delete the intersection handles
        if (intersection.Road1PathCreator.bezierPath[handleIndex1] == intersection.Road1AnchorPoint1 || intersection.Road1PathCreator.bezierPath[handleIndex1] == intersection.Road1AnchorPoint2) 
        {
            continue;
        }
        if (intersectionBounds.Contains(intersection.Road1PathCreator.bezierPath[handleIndex1])){
            intersection.Road1PathCreator.bezierPath.DeleteSegment(handleIndex1);
        }
    }
}
/// <summary> Get the road object that corresponds to the given path creator </summary>
static Road GetRoad(PathCreator pathCreator)
{
    RoadSystem roadSystem = GameObject.Find("RoadSystem").GetComponent<RoadSystem>();
    Road thisRoad = null;
    foreach (Road road in roadSystem.Roads) {
        if (road.RoadObject.GetComponent<PathCreator>() == pathCreator) {
            thisRoad = road;
            break;
        }
    }
    if (thisRoad == null) {
        Debug.Log("ERROR, Road not found");
        return null;
    }
    return thisRoad;
}
}

/// <summary> Data structure for storing information about an intersection point </summary>
public struct IntersectionPointData {
	public Vector2 IntersectionPosition;
	public Quaternion rotation;
	public PathCreator Road1PathCreator;
    public PathCreator Road2PathCreator;

    public IntersectionPointData(Vector2 intersectionPoint, Quaternion rotation, PathCreator road1PathCreator, PathCreator road2PathCreator)
    {
        this.IntersectionPosition = intersectionPoint;
        this.rotation = rotation;
		this.Road1PathCreator = road1PathCreator;
        this.Road2PathCreator = road2PathCreator;
    }
}
}

