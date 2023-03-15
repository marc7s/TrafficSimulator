using System.Collections.Generic;
using UnityEngine;
using RoadGenerator.Utility;
using System.Linq;

namespace RoadGenerator
{
    public static class IntersectionCreator
    {
        const int SEGMENT_ANCHOR1_INDEX = 0;
        const int SEGMENT_ANCHOR2_INDEX = 3;
        public static void UpdateIntersections(Road road)
        {
            PathCreator pathCreator = road.RoadObject.GetComponent<PathCreator>();
            RoadSystem roadSystem = road.RoadSystem;
            
            // Go through all segments of this road
            for (int roadSegmentIndex = 0; roadSegmentIndex < pathCreator.bezierPath.NumSegments; roadSegmentIndex++)
            {
                // Get the points on the current segment
                Vector3[] segmentPoints = pathCreator.bezierPath.GetPointsInSegment(roadSegmentIndex);
                
                // Get the indices of the closest points on the vertex path to the bezier anchors
                int startVertexIndex = pathCreator.path.GetClosestIndexOnPath(segmentPoints[SEGMENT_ANCHOR1_INDEX]);
                int endVertexIndex = pathCreator.path.GetClosestIndexOnPath(segmentPoints[SEGMENT_ANCHOR2_INDEX], false);
                
                List<IntersectionPointData> intersectionPointDatas = new List<IntersectionPointData>();
                
                // Check all other roads for intersections
                foreach(Road otherRoad in roadSystem.Roads) 
                {
                    // Do not check intersections with itself
                    if (otherRoad == road)
                        continue;

                    PathCreator otherPathCreator = otherRoad.RoadObject.GetComponent<PathCreator>();
                    
                    // If the two road bounds are not overlapping, then intersection is not possible
                    if (!otherPathCreator.bezierPath.PathBounds.Intersects(otherPathCreator.bezierPath.PathBounds))
                        continue;

                    intersectionPointDatas.AddRange(GetBezierPathIntersections(road.RoadObject.transform, otherRoad.RoadObject.transform, pathCreator,  otherPathCreator));
                }

                // Go through all the intersections and create an intersection at every position
                foreach (IntersectionPointData intersectionPointData in intersectionPointDatas)
                {
                    if (road.ConnectedToAtStart != null && Vector3.Distance(intersectionPointData.Position, pathCreator.bezierPath.GetFirstAnchorPos()) < 1f)
                        continue;
                    if (road.ConnectedToAtEnd != null && Vector3.Distance(intersectionPointData.Position, pathCreator.bezierPath.GetLastAnchorPos()) < 1f)
                        continue;
                    // If the vertex count is small in a segment, then there is a possibility that the same intersection is added multiple times
                    // Therefore only add an intersection if it does not already exist
                    if (!roadSystem.DoesIntersectionExist(intersectionPointData.Position))
                    {
                        CreateIntersectionAtPosition(intersectionPointData, roadSystem, intersectionPointData.MainRoad);
                        // Update the segment indices if a four way intersection was added. For three way intersections, the points are already correctly added
                        if(intersectionPointData.Type == IntersectionType.FourWayIntersection)
                            UpdateSegmentIndex(intersectionPointDatas, intersectionPointData.Road1SegmentIndex, intersectionPointData.Road2SegmentIndex);
                    }
                }
            }
        }
        /// <summary>Updates the segment indices after a new segment is placed</summary>
        static void UpdateSegmentIndex(List<IntersectionPointData> intersectionPointDatas, int road1PlacedSegmentIndex, int road2PlacedSegmentIndex)
        {
            // Go through all intersections and increment the segment indices for all prior segments
            // This is because adding an intersection creates an additional segment
            for (int i = 0; i < intersectionPointDatas.Count; i++)
            {
                // Update all segment indices for road1
                if (intersectionPointDatas[i].Road1SegmentIndex > road1PlacedSegmentIndex)
                    intersectionPointDatas[i].Road1SegmentIndex++;
                
                // Update all segment indices for road2
                if (intersectionPointDatas[i].Road2SegmentIndex > road2PlacedSegmentIndex)
                    intersectionPointDatas[i].Road2SegmentIndex++;
            }
        }

        static bool IntersectionIsUnique(List<IntersectionPointData> intersectionPointDatas, Vector3 intersectionPosition)
        {
            float uniqueDistance = Intersection.IntersectionLength;
            return intersectionPointDatas.Count < 1 || intersectionPointDatas.Any(x => Vector3.Distance(x.Position, intersectionPosition) < uniqueDistance);
        }

        private static List<IntersectionPointData> GetBezierPathIntersections(Transform road1Transform, Transform road2Transform, PathCreator road1PathCreator, PathCreator road2PathCreator)
        {
            List<IntersectionPointData> intersectionPointDatas = new List<IntersectionPointData>();

            List<BezierIntersection> bezierIntersections = road1PathCreator.bezierPath.IntersectionPoints(road1Transform, road2Transform, road2PathCreator.bezierPath);
            
            foreach(BezierIntersection bezierIntersection in bezierIntersections)
            {
                // Set the rotation of the intersection to that of the line
                Quaternion rotation = Quaternion.FromToRotation(Vector3.forward, bezierIntersection.direction);
                intersectionPointDatas.Add(CalculateIntersectionData(bezierIntersection.point, rotation, road1PathCreator, road2PathCreator));
            }

            return intersectionPointDatas;
        }

        static IntersectionType GetThreeWayIntersectionType(PathCreator pathCreator, float distanceAlongPath)
        {
            bool isAtEnd = Mathf.Abs(pathCreator.path.length - distanceAlongPath) < distanceAlongPath;
            
            return isAtEnd ? IntersectionType.ThreeWayIntersectionAtEnd : IntersectionType.ThreeWayIntersectionAtStart;
        }

        static IntersectionPointData CalculateIntersectionData(Vector3 intersectionPosition, Quaternion rotation, PathCreator road1PathCreator, PathCreator road2PathCreator)
        {
            // Get the closest segment indices at the intersection point
            int road1SegmentIndex = GetClosestSegmentIndex(road1PathCreator, intersectionPosition);
            int road2SegmentIndex = GetClosestSegmentIndex(road2PathCreator, intersectionPosition);

            // Get the distance along the path at the intersection point
            float road1DistanceAtIntersection = road1PathCreator.path.GetClosestDistanceAlongPath(intersectionPosition);
            float road2DistanceAtIntersection = road2PathCreator.path.GetClosestDistanceAlongPath(intersectionPosition);
            
            // Get the anchor points on either side of the intersection for the two roads
            (Vector3 road1AnchorPoint1, Vector3 road1AnchorPoint2) = GetIntersectionAnchorPoints(road1PathCreator.path, road1DistanceAtIntersection);
            (Vector3 road2AnchorPoint1, Vector3 road2AnchorPoint2) = GetIntersectionAnchorPoints(road2PathCreator.path, road2DistanceAtIntersection);

            float intersectionExtendCoef = 1.2f;
            float intersectionExtension = Intersection.IntersectionLength * 0.5f * intersectionExtendCoef;

            IntersectionType type = IntersectionType.FourWayIntersection;

            // If Road1 is a 3 way intersection
            if(road1PathCreator.path.length < road1DistanceAtIntersection + intersectionExtension || road1DistanceAtIntersection - intersectionExtension < 0)
            {
                type = GetThreeWayIntersectionType(road1PathCreator, road1DistanceAtIntersection);
                
                // We always want Road1 to be the road that does not end at the intersection, so we swap the roads
                PathCreator temp = road1PathCreator;
                road1PathCreator = road2PathCreator;
                road2PathCreator = temp;

                Vector3 tempAP1 = road1AnchorPoint1;
                Vector3 tempAP2 = road1AnchorPoint2;
                road1AnchorPoint1 = road2AnchorPoint1;
                road1AnchorPoint2 = road2AnchorPoint2;
                road2AnchorPoint1 = tempAP1;
                road2AnchorPoint2 = tempAP2;

                int tempIndex = road1SegmentIndex;
                road1SegmentIndex = road2SegmentIndex;
                road2SegmentIndex = tempIndex;

                // Since we capped the distance at the end of the road, the correct anchor point will be the one furthest away
                if(Vector3.Distance(road2AnchorPoint2, intersectionPosition) > Vector3.Distance(road2AnchorPoint1, intersectionPosition))
                    road2AnchorPoint1 = road2AnchorPoint2;
                

                // For three way intersections Road2 will only have one anchor point, so we zero out the other one to avoid hard to find bugs if the wrong one is used
                road2AnchorPoint2 = Vector3.zero;                
            }
            // If Road2 is a 3 way intersection
            else if(road2PathCreator.path.length < road2DistanceAtIntersection + intersectionExtension || road2DistanceAtIntersection - intersectionExtension < 0)
            {
                type = GetThreeWayIntersectionType(road2PathCreator, road2DistanceAtIntersection);

                // Since we capped the distance at the end of the road, the correct anchor point will be the one furthest away
                if(Vector3.Distance(road2AnchorPoint2, intersectionPosition) > Vector3.Distance(road2AnchorPoint1, intersectionPosition))
                    road2AnchorPoint1 = road2AnchorPoint2;
                
                // For three way intersections Road2 will only have one anchor point, so we zero out the other one to avoid hard to find bugs if the wrong one is used
                road2AnchorPoint2 = Vector3.zero;
            }

            IntersectionPointData intersectionPointData = new IntersectionPointData(
                intersectionPosition, rotation, road1PathCreator, road2PathCreator, road1AnchorPoint1,
                road1AnchorPoint2, road2AnchorPoint1, road2AnchorPoint2, road1SegmentIndex, road2SegmentIndex, type, road1PathCreator.GetComponent<Road>()
                );

            return intersectionPointData;
        }

        /// <summary>Calculate the positions of the two anchor points to be created on either side of the intersection</summary>
        static (Vector3, Vector3) GetIntersectionAnchorPoints(VertexPath vertexPath, float distanceAtIntersection)
        {
            // Use EndOfPathInstruction.Stop to cap the anchor points at the end of the road
            Vector3 firstAnchorPoint = vertexPath.GetPointAtDistance(distanceAtIntersection + Intersection.IntersectionLength/2, EndOfPathInstruction.Stop);
            Vector3 secondAnchorPoint = vertexPath.GetPointAtDistance(distanceAtIntersection - Intersection.IntersectionLength/2, EndOfPathInstruction.Stop);
            
            return (firstAnchorPoint, secondAnchorPoint);
        }

        /// <summary>Creates an intersection at the given position</summary>
        static void CreateIntersectionAtPosition(IntersectionPointData intersectionPointData, RoadSystem roadSystem, Road road)
        {
            // Get the path creators for the roads
            PathCreator road1PathCreator = intersectionPointData.Road1PathCreator;
            PathCreator road2PathCreator = intersectionPointData.Road2PathCreator;

            Road otherRoad = GetRoad(roadSystem, road2PathCreator);
            
            // Create a new intersection at the intersection point
            Intersection intersection = road.RoadSystem.AddNewIntersection(intersectionPointData, road, otherRoad);
            
            // Split Road1 on the intersection, creating an anchor point on each side of the intersection
            SplitPathOnIntersection(road1PathCreator, intersection.Road1AnchorPoint1, intersection.Road1AnchorPoint2, intersectionPointData.Road1SegmentIndex);
            
            if(intersectionPointData.Type == IntersectionType.ThreeWayIntersectionAtStart)
            {
                // If the 3-way intersection is at the start, we move the first point to the junction edge, then add a new node to the start at the intersection point
                road2PathCreator.bezierPath.MovePoint(0, intersection.Road2AnchorPoint1);
                road2PathCreator.bezierPath.AddSegmentToStart(intersectionPointData.Position);
            }
            else if(intersectionPointData.Type == IntersectionType.ThreeWayIntersectionAtEnd)
            {
                // If the 3-way intersection is at the end, we move the last point to the junction edge, then add a new node to the end at the intersection point
                road2PathCreator.bezierPath.MovePoint(road2PathCreator.bezierPath.NumPoints - 1, intersection.Road2AnchorPoint1);
                road2PathCreator.bezierPath.AddSegmentToEnd(intersection.IntersectionPosition);
            }
            else
            {
                // If the intersection is a 4-way intersection, we split Road2 at the intersection point creating an anchor point on either side
                SplitPathOnIntersection(road2PathCreator, intersection.Road2AnchorPoint1, intersection.Road2AnchorPoint2, intersectionPointData.Road2SegmentIndex);
            }
            
            DeleteAnchorsInsideIntersectionBounds(intersection);

            road.UpdateMesh();
            otherRoad.UpdateMesh();
            intersection.UpdateMesh();
        }

        /// <summary>Splits a path on an intersection, creating an anchor point on each side of the intersection</summary>
        static void SplitPathOnIntersection(PathCreator pathCreator, Vector3 intersectionAnchorPoint1, Vector3 intersectionAnchorPoint2, int segmentIndex)
        {
            // Get the first point on the segment
            Vector3 segmentStartPoint = pathCreator.bezierPath.GetPointsInSegment(segmentIndex)[SEGMENT_ANCHOR1_INDEX];
            
            // Determine if the first intersection anchor point is closer. This determines in which direction the path is split
            bool anchorPoint1Closer = Vector3.Distance(segmentStartPoint, intersectionAnchorPoint1) < Vector3.Distance(segmentStartPoint, intersectionAnchorPoint2);
            
            // Swap the intersection anchor points if the first anchor point is further away than the second
            Vector3 firstAnchorPoint = anchorPoint1Closer ? intersectionAnchorPoint1 : intersectionAnchorPoint2;
            Vector3 secondAnchorPoint = anchorPoint1Closer ? intersectionAnchorPoint2 : intersectionAnchorPoint1;

            // The if the first anchor point was closer we are splitting in the reverse direction of the path, which means the second (and therefore lower) index will remain unaffected
            // Otherwise, splitting the path will create a new segment which means the segment index on the other side of the intersection will increase by 1
            int secondSegmentIndex = anchorPoint1Closer ? segmentIndex : segmentIndex + 1;

            // Split the first path on the anchor points before and after the intersection
            SplitPathOnPoint(pathCreator, firstAnchorPoint, segmentIndex);
            SplitPathOnPoint(pathCreator, secondAnchorPoint, secondSegmentIndex);
        }

        /// <summary>Splits a path on a point</summary>
        static void SplitPathOnPoint(PathCreator pathCreator, Vector3 anchorPoint, int segmentIndex)
        {
            pathCreator.bezierPath.SplitSegment(anchorPoint, segmentIndex, pathCreator.path.GetClosestTimeOnPath(anchorPoint));
        }
 
        /// <summary>Get the index of the segment that is closest to the given position</summary>
        static int GetClosestSegmentIndex(PathCreator creator, Vector3 position)
        {
            int closestSegmentIndex = -1;
            
            // Get the distance along the vertex path to the position
            float positionDistance = creator.path.GetClosestDistanceAlongPath(position);
            
            // Loop through all segments
            for (int i = 0; i < creator.bezierPath.NumSegments; i++)
            {
                // Calculate the distance along the vertex path for the two anchor points of the segment
                float segmentPoint1 = creator.path.GetClosestDistanceAlongPath(creator.bezierPath.GetPointsInSegment(i)[SEGMENT_ANCHOR1_INDEX]);
                float segmentPoint2 = creator.path.GetClosestDistanceAlongPath(creator.bezierPath.GetPointsInSegment(i)[SEGMENT_ANCHOR2_INDEX]);
                
                // If the positions distance is between the two anchor point distances, return the index of the segment
                if (positionDistance >= segmentPoint1 && positionDistance <= segmentPoint2)
                    return i;
            }
            return closestSegmentIndex;
        }

        /// <summary>Deletes all non intersection anchors that are inside the intersection bounds</summary>
        static void DeleteAnchorsInsideIntersectionBounds(Intersection intersection)
        {
            // Set the intersection bounds extents
            float intersectionBoundHeight = 2f;
            float intersectionBoundLength = Intersection.IntersectionLength * Intersection.IntersectionBoundsLengthMultiplier;

            BezierPath road1BezierPath = intersection.Road1PathCreator.bezierPath;
            BezierPath road2BezierPath = intersection.Road2PathCreator.bezierPath;
            
            // Create a bounding box around the intersection
            Bounds intersectionBounds = new Bounds(intersection.IntersectionPosition, new Vector3(intersectionBoundLength, intersectionBoundHeight, intersectionBoundLength));
            
            // Delete any anchors inside the intersection bounds for both roads, except for the intersection anchors
            DeleteAnchorsInsideBounds(road1BezierPath, intersectionBounds, new List<Vector3>{ intersection.Road1AnchorPoint1, intersection.Road1AnchorPoint2 });
            DeleteAnchorsInsideBounds(road2BezierPath, intersectionBounds, new List<Vector3>{ intersection.Road2AnchorPoint1, intersection.Road2AnchorPoint2, intersection.IntersectionPosition });
        }

        /// <summary>Delete any anchors inside a bounded area</summary>
        ///<param name="path">The path to delete anchors from</param>
        ///<param name="bounds">The bounds to delete anchors inside</param>
        ///<param name="ignoreAnchors">Any anchors that should not be deleted</param>
        static void DeleteAnchorsInsideBounds(BezierPath path, Bounds bounds, List<Vector3> ignoreAnchors)
        {
            for (int i = path.NumPoints - 1; i >= 0; i -= 3)
            {
                int handleIndex = i % path.NumPoints;
                
                // Do not delete the ignored anchors
                if (ignoreAnchors.Contains(path[handleIndex])) 
                    continue;
                
                // Delete the segment if the anchor is inside the bounds
                if (bounds.Contains(path[handleIndex]))
                    path.DeleteSegment(handleIndex);
            }
        }

        /// <summary> Get the road object that corresponds to the given path creator </summary>
        static Road GetRoad(RoadSystem roadSystem, PathCreator pathCreator)
        {
            Road foundRoad = null;
            
            // Search each road in the road system for the road
            foreach (Road road in roadSystem.Roads)
            {
                if (road.RoadObject.GetComponent<PathCreator>() == pathCreator)
                {
                    foundRoad = road;
                    break;
                }
            }
            
            if (foundRoad == null)
                Debug.LogError("ERROR, Road not found");
            
            return foundRoad;
        }
    }

    /// <summary> Data structure for storing information about an intersection point </summary>
    public class IntersectionPointData
    {
        public Vector3 Position;
        public Quaternion Rotation;
        public PathCreator Road1PathCreator;
        public PathCreator Road2PathCreator;
        public Vector3 Road1AnchorPoint1;
        public Vector3 Road1AnchorPoint2;
        public Vector3 Road2AnchorPoint1;
        public Vector3 Road2AnchorPoint2;
        public int Road1SegmentIndex;
        public int Road2SegmentIndex;
        public IntersectionType Type;
        public Road MainRoad;

        public IntersectionPointData(Vector3 position, Quaternion rotation, PathCreator road1PathCreator, PathCreator road2PathCreator, Vector3 road1AnchorPoint1, Vector3 road1AnchorPoint2, Vector3 road2AnchorPoint1, Vector3 road2AnchorPoint2, int road1SegmentIndex, int road2SegmentIndex, IntersectionType type, Road mainRoad)
        {
            Position = position;
            Rotation = rotation;
            Road1PathCreator = road1PathCreator;
            Road2PathCreator = road2PathCreator;
            Road1AnchorPoint1 = road1AnchorPoint1;
            Road1AnchorPoint2 = road1AnchorPoint2;
            Road2AnchorPoint1 = road2AnchorPoint1;
            Road2AnchorPoint2 = road2AnchorPoint2;
            Road1SegmentIndex = road1SegmentIndex;
            Road2SegmentIndex = road2SegmentIndex;
            Type = type;
            MainRoad = mainRoad;
        }
    }
}