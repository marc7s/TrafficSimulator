using System.Collections.Generic;
using UnityEngine;
using RoadGenerator.Utility;

namespace RoadGenerator{
    public static class IntersectionCreator
    {
        public static void UpdateIntersections(PathCreator creator, Road thisRoad)
        {
            RoadSystem roadSystem = thisRoad.RoadSystem;
            for (int road1SegmentIndex = 0; road1SegmentIndex < creator.bezierPath.NumSegments; road1SegmentIndex++)
            {
                Vector3[] segment1 = creator.bezierPath.GetPointsInSegment(road1SegmentIndex);
                int startingVertexIndexThisRoad = creator.path.GetPointClosestPointIndex(segment1[0]);
                int endingVertexIndexThisRoad = creator.path.GetPointClosestPointIndex(segment1[3], false);
                List<IntersectionPointData> intersectionPointDatas = new List<IntersectionPointData>();
                foreach(Road road in roadSystem.Roads) 
                {
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
                        if (!IntersectionUtility.IsBezierPathIntersectionPossible(segment1, segment2)) 
                        {
                            continue;
                        }
                        intersectionPointDatas.AddRange(GetBezierPathIntersections(startingVertexIndexThisRoad, endingVertexIndexThisRoad, creator,  pathCreator, segment2));
                    }
                }

                foreach (IntersectionPointData intersectionPointData in intersectionPointDatas) {
                    Vector3 intersectionPosition = new Vector3(intersectionPointData.IntersectionPosition.x, 0, intersectionPointData.IntersectionPosition.y);
                    // If the vertex count is small in a segment, then there is a possibility that the same intersection is added multiple times
                    // Therefore before creating the intersection check if the intersection already exists
                    if (!roadSystem.DoesIntersectionExist(intersectionPosition))
                    {
                        CreateIntersectionAtPosition(intersectionPointData, thisRoad);
                        UpdateSegmentIndex(intersectionPointDatas, intersectionPointData.Road1SegmentIndex, intersectionPointData.Road2SegmentIndex);
                    }
                }
            }
        }
        static void UpdateSegmentIndex(List<IntersectionPointData> intersectionPointDatas, int road1PlacedSegmentIndex, int road2PlacedSegmentIndex)
        {
            for (var i = 0; i < intersectionPointDatas.Count; i++)
            {
                if (intersectionPointDatas[i].Road1SegmentIndex > road1PlacedSegmentIndex)
                {
                    intersectionPointDatas[i].Road1SegmentIndex++;
                }
                if (intersectionPointDatas[i].Road2SegmentIndex > road2PlacedSegmentIndex)
                {
                    intersectionPointDatas[i].Road2SegmentIndex++;
                }
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
                intersectionsPoints.Add(CalculateIntersectionData(intersectionPoint, rotation, road1PathCreator, road2PathCreator, GetRoad(road1PathCreator)));
            }
            }
        }
        return intersectionsPoints;
        }

        static IntersectionPointData CalculateIntersectionData(Vector2 intersectionPosition, Quaternion rotation, PathCreator road1PathCreator, PathCreator road2PathCreator, Road thisRoad)
        {
            RoadSystem roadSystem = thisRoad.RoadSystem;
            Vector3 intersectionPoint = new Vector3(intersectionPosition.x, 0, intersectionPosition.y);



            int road1SegmentIndex = GetClosestSegmentIndex(road1PathCreator, intersectionPoint);
            float road1DistanceAtIntersection = road1PathCreator.path.GetClosestDistanceAlongPath(intersectionPoint);
            Vector3 road1AnchorPoint1 = road1PathCreator.path.GetPointAtDistance(road1DistanceAtIntersection + Intersection.IntersectionLength/2);
            Vector3 road1AnchorPoint2 = road1PathCreator.path.GetPointAtDistance(road1DistanceAtIntersection - Intersection.IntersectionLength/2);


            int road2SegmentIndex = GetClosestSegmentIndex(road2PathCreator, intersectionPoint);
            float road2DistanceAtIntersection = road2PathCreator.path.GetClosestDistanceAlongPath(intersectionPoint);
            Vector3 road2AnchorPoint1 = road2PathCreator.path.GetPointAtDistance(road2DistanceAtIntersection + Intersection.IntersectionLength/2);
            Vector3 road2AnchorPoint2 = road2PathCreator.path.GetPointAtDistance(road2DistanceAtIntersection - Intersection.IntersectionLength/2);


            return new IntersectionPointData(intersectionPosition, rotation, road1PathCreator, road2PathCreator, road1AnchorPoint1,
            road1AnchorPoint2, road2AnchorPoint1, road2AnchorPoint2, road1SegmentIndex, road2SegmentIndex);
        }

        /// <summary> Creates an intersection at the given position </summary>
        static void CreateIntersectionAtPosition(IntersectionPointData intersectionPointData, Road thisRoad)
        {
            Vector3 segmentStartPoint = intersectionPointData.Road1PathCreator.bezierPath.GetPointsInSegment(intersectionPointData.Road1SegmentIndex)[0];
            PathCreator road1PathCreator = intersectionPointData.Road1PathCreator;
            PathCreator road2PathCreator = intersectionPointData.Road2PathCreator;
            Intersection intersection = thisRoad.RoadSystem.AddNewIntersection(intersectionPointData, thisRoad, GetRoad(road2PathCreator));
            if (Vector3.Distance(segmentStartPoint, intersection.Road1AnchorPoint1) < Vector3.Distance(segmentStartPoint, intersection.Road1AnchorPoint2))
            {
                road1PathCreator.bezierPath.SplitSegment(intersection.Road1AnchorPoint1, intersectionPointData.Road1SegmentIndex, road1PathCreator.path.GetClosestTimeOnPath(intersection.Road1AnchorPoint1));
                road1PathCreator.bezierPath.SplitSegment(intersection.Road1AnchorPoint2, intersectionPointData.Road1SegmentIndex, road1PathCreator.path.GetClosestTimeOnPath(intersection.Road1AnchorPoint2));
            }
            else
            {
                road1PathCreator.bezierPath.SplitSegment(intersection.Road1AnchorPoint2, intersectionPointData.Road1SegmentIndex, road1PathCreator.path.GetClosestTimeOnPath(intersection.Road1AnchorPoint2));
                road1PathCreator.bezierPath.SplitSegment(intersection.Road1AnchorPoint1, intersectionPointData.Road1SegmentIndex + 1, road1PathCreator.path.GetClosestTimeOnPath(intersection.Road1AnchorPoint1));
            }

            segmentStartPoint = road2PathCreator.bezierPath.GetPointsInSegment(intersectionPointData.Road2SegmentIndex)[0];
            if (Vector3.Distance(segmentStartPoint, intersection.Road2AnchorPoint1) < Vector3.Distance(segmentStartPoint, intersection.Road2AnchorPoint2))
            {
                road2PathCreator.bezierPath.SplitSegment(intersection.Road2AnchorPoint1, intersectionPointData.Road2SegmentIndex, road2PathCreator.path.GetClosestTimeOnPath(intersection.Road2AnchorPoint1));
                road2PathCreator.bezierPath.SplitSegment(intersection.Road2AnchorPoint2, intersectionPointData.Road2SegmentIndex + 1, road2PathCreator.path.GetClosestTimeOnPath(intersection.Road2AnchorPoint2));
            }
            else
            {
                road2PathCreator.bezierPath.SplitSegment(intersection.Road2AnchorPoint2, intersectionPointData.Road2SegmentIndex, road2PathCreator.path.GetClosestTimeOnPath(intersection.Road2AnchorPoint2));
                road2PathCreator.bezierPath.SplitSegment(intersection.Road2AnchorPoint1, intersectionPointData.Road2SegmentIndex + 1, road2PathCreator.path.GetClosestTimeOnPath(intersection.Road2AnchorPoint1));
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
    public class IntersectionPointData {
        public Vector2 IntersectionPosition;
        public Quaternion Rotation;
        public PathCreator Road1PathCreator;
        public PathCreator Road2PathCreator;
        public Vector3 Road1AnchorPoint1;
        public Vector3 Road1AnchorPoint2;
        public Vector3 Road2AnchorPoint1;
        public Vector3 Road2AnchorPoint2;
        public int Road1SegmentIndex;
        public int Road2SegmentIndex;

        public IntersectionPointData(Vector2 intersectionPosition, Quaternion rotation, PathCreator road1PathCreator, PathCreator road2PathCreator, Vector3 road1AnchorPoint1, Vector3 road1AnchorPoint2, Vector3 road2AnchorPoint1, Vector3 road2AnchorPoint2, int road1SegmentIndex, int road2SegmentIndex) {
            IntersectionPosition = intersectionPosition;
            this.Rotation = rotation;
            Road1PathCreator = road1PathCreator;
            Road2PathCreator = road2PathCreator;
            Road1AnchorPoint1 = road1AnchorPoint1;
            Road1AnchorPoint2 = road1AnchorPoint2;
            Road2AnchorPoint1 = road2AnchorPoint1;
            Road2AnchorPoint2 = road2AnchorPoint2;
            Road1SegmentIndex = road1SegmentIndex;
            Road2SegmentIndex = road2SegmentIndex;
        }
    }

}

