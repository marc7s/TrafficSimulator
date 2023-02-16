using System.Collections.Generic;
using UnityEngine;
using RoadGenerator.Utility;

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

                    // Otherwise, intersection might be possible so we have to check thoroughly
                    // Loop through all segments of the other road
                    for (int i = 0; i < otherPathCreator.bezierPath.NumSegments; i++) 
                    {
                        // Get the segment points for the other road
                        Vector3[] otherSegmentPoints = otherPathCreator.bezierPath.GetPointsInSegment(i);
                        
                        // If the two segment bounds are overlapping there might be an intersection, so we need to check it thoroughly
                        if (IntersectionUtility.IsBezierPathIntersectionPossible(segmentPoints, otherSegmentPoints)) 
                        {
                            // Get all intersections of this segment and add them to the list
                            intersectionPointDatas.AddRange(GetBezierPathIntersections(startVertexIndex, endVertexIndex, pathCreator,  otherPathCreator, otherSegmentPoints));
                        }
                    }
                }

                // Go through all the intersections and create an intersection at every position
                foreach (IntersectionPointData intersectionPointData in intersectionPointDatas)
                {
                    // Project the 2D intersection point to the XZ plane since we are ignoring the vertical axis when checking for intersections
                    Vector3 intersectionPosition = new Vector3(intersectionPointData.Position.x, 0, intersectionPointData.Position.y);
                    
                    // If the vertex count is small in a segment, then there is a possibility that the same intersection is added multiple times
                    // Therefore only add an intersection if it does not already exist
                    if (!roadSystem.DoesIntersectionExist(intersectionPosition))
                    {
                        CreateIntersectionAtPosition(intersectionPointData, roadSystem, road);
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

        /// <summary>Get the intersections from the start vertex index to the end index</summary>
        static List<IntersectionPointData> GetBezierPathIntersections(int road1StartVertexIndex, int road1EndVertexIndex, PathCreator road1PathCreator, PathCreator road2PathCreator, Vector3[] road2SegmentPoints)
        {
            List<IntersectionPointData> intersectionPoints = new List<IntersectionPointData>();
            
            // Get the anchor positions for the start and end of the segment
            Vector3 road2SegmentAnchorPosition1 = road2SegmentPoints[SEGMENT_ANCHOR1_INDEX];
            Vector3 road2SegmentAnchorPosition2 = road2SegmentPoints[SEGMENT_ANCHOR2_INDEX];
            
            // Get the start and end vertex indices on this segment of road2 that are closest to its anchor positions for this segment
            int startVertexIndexRoad2 = road2PathCreator.path.GetClosestIndexOnPath(road2SegmentAnchorPosition1);
            int endVertexIndexRoad2 = road2PathCreator.path.GetClosestIndexOnPath(road2SegmentAnchorPosition2, false);

            // Go through all vertex point combinations between road1 and road2
            for (int i = 0; i < road1EndVertexIndex - road1StartVertexIndex; i++)
            {
                for (int j = 0; j < endVertexIndexRoad2 - startVertexIndexRoad2; j++)
                {
                    // Project this and the next point on road1 to the XZ plane since we ignore the vertical axises when creating intersections
                    Vector2 road1VertexPoint1 = IntersectionUtility.GetXZPlaneProjection(road1PathCreator.path.GetPoint(road1StartVertexIndex + i));
                    Vector2 road1VertexPoint2 = IntersectionUtility.GetXZPlaneProjection(road1PathCreator.path.GetPoint(road1StartVertexIndex + i+1));
                    
                   // Project this and the next point on road2 to the XZ plane since we ignore the vertical axises when creating intersections
                    Vector2 road2VertexPoint1 = IntersectionUtility.GetXZPlaneProjection(road2PathCreator.path.GetPoint(startVertexIndexRoad2 + j));
                    Vector2 road2VertexPoint2 = IntersectionUtility.GetXZPlaneProjection(road2PathCreator.path.GetPoint(startVertexIndexRoad2 + j+1));
                    
                    // Check if the lines drawn between the two points in one road intersects with the other
                    if (IntersectionUtility.IntersectLineSegments2D(road1VertexPoint1, road1VertexPoint2, road2VertexPoint1, road2VertexPoint2, out Vector2 intersectionPoint))
                    {
                        // Get the direction from the first point to the second point on road1
                        Vector3 lineDirection = new Vector3(road1VertexPoint1.x, 0, road1VertexPoint1.y) - new Vector3(road1VertexPoint2.x, 0, road1VertexPoint2.y);
                        
                        // Set the rotation of the intersection to that of the line
                        Quaternion rotation = Quaternion.FromToRotation(Vector3.forward, lineDirection);
                        intersectionPoints.Add(CalculateIntersectionData(intersectionPoint, rotation, road1PathCreator, road2PathCreator));
                    }
                }
            }
            return intersectionPoints;
        }

        static IntersectionPointData CalculateIntersectionData(Vector2 intersectionPosition, Quaternion rotation, PathCreator road1PathCreator, PathCreator road2PathCreator)
        {
            // Project the intersection position on the XZ plane
            Vector3 intersectionPoint = new Vector3(intersectionPosition.x, 0, intersectionPosition.y);
            
            // Get the closest segment indices at the intersection point
            int road1SegmentIndex = GetClosestSegmentIndex(road1PathCreator, intersectionPoint);
            int road2SegmentIndex = GetClosestSegmentIndex(road2PathCreator, intersectionPoint);

            // Get the distance along the path at the intersection point
            float road1DistanceAtIntersection = road1PathCreator.path.GetClosestDistanceAlongPath(intersectionPoint);
            float road2DistanceAtIntersection = road2PathCreator.path.GetClosestDistanceAlongPath(intersectionPoint);
            
            // Get the anchor points on either side of the intersection for the two roads
            (Vector3 road1AnchorPoint1, Vector3 road1AnchorPoint2) = GetIntersectionAnchorPoints(road1PathCreator.path, road1DistanceAtIntersection);
            (Vector3 road2AnchorPoint1, Vector3 road2AnchorPoint2) = GetIntersectionAnchorPoints(road2PathCreator.path, road2DistanceAtIntersection);


            IntersectionPointData intersectionPointData = new IntersectionPointData(
                intersectionPosition, rotation, road1PathCreator, road2PathCreator, road1AnchorPoint1,
                road1AnchorPoint2, road2AnchorPoint1, road2AnchorPoint2, road1SegmentIndex, road2SegmentIndex
                );
            // returning the intersection point data :)
            return intersectionPointData;
        }

        /// <summary>Calculate the positions of the two anchor points to be created on either side of the intersection</summary>
        static (Vector3, Vector3) GetIntersectionAnchorPoints(VertexPath vertexPath, float distanceAtIntersection)
        {
            Vector3 firstAnchorPoint = vertexPath.GetPointAtDistance(distanceAtIntersection + Intersection.IntersectionLength/2);
            Vector3 secondAnchorPoint = vertexPath.GetPointAtDistance(distanceAtIntersection - Intersection.IntersectionLength/2);
            
            return (firstAnchorPoint, secondAnchorPoint);
        }

        /// <summary>Creates an intersection at the given position</summary>
        static void CreateIntersectionAtPosition(IntersectionPointData intersectionPointData, RoadSystem roadSystem, Road road)
        {
            // Get the path creators for the roads
            PathCreator road1PathCreator = intersectionPointData.Road1PathCreator;
            PathCreator road2PathCreator = intersectionPointData.Road2PathCreator;
            
            // Create a new intersection at the intersection point
            Intersection intersection = road.RoadSystem.AddNewIntersection(intersectionPointData, road, GetRoad(roadSystem, road2PathCreator));

            // Split the road paths on the intersection, creating an anchor point on each side of the intersection for both roads
            SplitPathOnIntersection(road1PathCreator, intersection.Road1AnchorPoint1, intersection.Road1AnchorPoint2, intersectionPointData.Road1SegmentIndex);
            SplitPathOnIntersection(road2PathCreator, intersection.Road2AnchorPoint1, intersection.Road2AnchorPoint2, intersectionPointData.Road2SegmentIndex);
            
            DeleteAnchorsInsideIntersectionBounds(intersection);
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
            float intersectionBoundHeight = 0f;
            float intersectionBoundLength = Intersection.IntersectionLength * Intersection.IntersectionBoundsLengthMultiplier;

            BezierPath road1BezierPath = intersection.Road1PathCreator.bezierPath;
            BezierPath road2BezierPath = intersection.Road2PathCreator.bezierPath;
            
            // Create a bounding box around the intersection
            Bounds intersectionBounds = new Bounds(intersection.IntersectionPosition, new Vector3(intersectionBoundLength, intersectionBoundHeight, intersectionBoundLength));
            
            // Delete any anchors inside the intersection bounds for both roads, except for the intersection anchors
            DeleteAnchorsInsideBounds(road1BezierPath, intersectionBounds, new List<Vector3>{ intersection.Road1AnchorPoint1, intersection.Road1AnchorPoint2 });
            DeleteAnchorsInsideBounds(road2BezierPath, intersectionBounds, new List<Vector3>{ intersection.Road2AnchorPoint1, intersection.Road2AnchorPoint2 });
        }

        /// <summary>Delete any anchors inside a bounded area</summary>
        ///<param name="path">The path to delete anchors from</param>
        ///<param name="bounds">The bounds to delete anchors inside</param>
        ///<param name="ignoreAnchors">Any anchors that should not be deleted</param>
        static void DeleteAnchorsInsideBounds(BezierPath path, Bounds bounds, List<Vector3> ignoreAnchors)
        {
            for (int i = 0; i < path.NumPoints; i += 3)
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
        public Vector2 Position;
        public Quaternion Rotation;
        public PathCreator Road1PathCreator;
        public PathCreator Road2PathCreator;
        public Vector3 Road1AnchorPoint1;
        public Vector3 Road1AnchorPoint2;
        public Vector3 Road2AnchorPoint1;
        public Vector3 Road2AnchorPoint2;
        public int Road1SegmentIndex;
        public int Road2SegmentIndex;

        public IntersectionPointData(Vector2 position, Quaternion rotation, PathCreator road1PathCreator, PathCreator road2PathCreator, Vector3 road1AnchorPoint1, Vector3 road1AnchorPoint2, Vector3 road2AnchorPoint1, Vector3 road2AnchorPoint2, int road1SegmentIndex, int road2SegmentIndex)
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
        }
    }
}