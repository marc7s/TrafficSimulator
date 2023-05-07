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
            if(road == null || road.RoadSystem == null)
                return;
            
            PathCreator pathCreator = road.RoadObject.GetComponent<PathCreator>();
            RoadSystem roadSystem = road.RoadSystem;
            List<IntersectionPointData> intersectionPointDatas = new List<IntersectionPointData>();

            // Check all other roads for intersections
            foreach(Road otherRoad in roadSystem.DefaultRoads) 
            {
                // Do not check intersections with itself
                if (otherRoad == road)
                    continue;

                PathCreator otherPathCreator = otherRoad.PathCreator;
                
                // If the two road bounds are not overlapping, then intersection is not possible
                if (!otherPathCreator.bezierPath.PathBounds.Intersects(otherPathCreator.bezierPath.PathBounds))
                    continue;

                intersectionPointDatas.AddRange(GetBezierPathIntersections(road, otherRoad));
            }

            // Go through all the intersections and create an intersection at every position
            foreach (IntersectionPointData intersectionPointData in intersectionPointDatas)
            {
                // If the road is connected to another road at the start or end, do not create an intersection at the connection point
                if (road.ConnectedToAtStart != null && Vector3.Distance(intersectionPointData.Position, pathCreator.bezierPath.GetFirstAnchorPos()) < 1f)
                    continue;
                if (road.ConnectedToAtEnd != null && Vector3.Distance(intersectionPointData.Position, pathCreator.bezierPath.GetLastAnchorPos()) < 1f)
                    continue;

                // If the vertex count is small in a segment, then there is a possibility that the same intersection is added multiple times
                // Therefore only add an intersection if it does not already exist
                if (!roadSystem.DoesIntersectionExist(intersectionPointData.Position))
                    CreateIntersectionAtPosition(intersectionPointData, roadSystem);
            }
        }

        private static List<IntersectionPointData> GetBezierPathIntersections(Road road1, Road road2)
        {
            PathCreator road1PathCreator = road1.PathCreator;
            PathCreator road2PathCreator = road2.PathCreator;
            List<IntersectionPointData> intersectionPointDatas = new List<IntersectionPointData>();

            List<BezierIntersection> bezierIntersections = road1PathCreator.bezierPath.IntersectionPoints(road1.RoadObject.transform, road2.RoadObject.transform, road2PathCreator.bezierPath);
            
            foreach(BezierIntersection bezierIntersection in bezierIntersections)
                intersectionPointDatas.Add(CalculateIntersectionData(bezierIntersection.point, road1, road2));

            return intersectionPointDatas;
        }

        public static IntersectionType GetThreeWayIntersectionType(PathCreator pathCreator, float distanceAlongPath)
        {
            bool isAtEnd = Mathf.Abs(pathCreator.path.length - distanceAlongPath) < distanceAlongPath;
            
            return isAtEnd ? IntersectionType.ThreeWayIntersectionAtEnd : IntersectionType.ThreeWayIntersectionAtStart;
        }

        static IntersectionPointData CalculateIntersectionData(Vector3 intersectionPosition, Road road1, Road road2)
        {
            List<JunctionEdgeData> junctionEdgeDatas = new List<JunctionEdgeData>();
            // Get the anchor points on either side of the intersection for the two roads
            junctionEdgeDatas.AddRange(GetIntersectionAnchorPoints(road1, intersectionPosition, Intersection.CalculateIntersectionLength(road1, road2)));
            int road1JunctionEdgeCount = junctionEdgeDatas.Count;
            junctionEdgeDatas.AddRange(GetIntersectionAnchorPoints(road2, intersectionPosition, Intersection.CalculateIntersectionLength(road1, road2)));
            IntersectionType type = IntersectionType.FourWayIntersection;

            if (junctionEdgeDatas.Count == 3)
            {
                if (road1JunctionEdgeCount == 2)
                    type = GetThreeWayIntersectionType(road2.PathCreator, road2.PathCreator.path.GetClosestDistanceAlongPath(intersectionPosition));
                else
                    type = GetThreeWayIntersectionType(road1.PathCreator, road1.PathCreator.path.GetClosestDistanceAlongPath(intersectionPosition));
            }
            if (junctionEdgeDatas.Count < 3)
                Debug.LogError("Intersection has less than 3 junction edges" + intersectionPosition);


            return new IntersectionPointData(intersectionPosition, junctionEdgeDatas, type);
        }
        
        static IntersectionPointData CalculateIntersectionDataMultipleRoads(Vector3 intersectionPosition, List<Road> roads)
        {
            List<JunctionEdgeData> junctionEdgeDatas = new List<JunctionEdgeData>();
            // TODO fix
            float intersectionLength = Intersection.CalculateIntersectionLength(roads[0], roads[1]);
            // Get the anchor points on either side of the intersection for the two roads

            foreach(Road road in roads)
            {
                junctionEdgeDatas.AddRange(GetIntersectionAnchorPoints(road, intersectionPosition, intersectionLength));
            }


            IntersectionType type = IntersectionType.FourWayIntersection;
            if (junctionEdgeDatas.Count == 3)
            {
                type = IntersectionType.ThreeWayIntersectionAtStart;
            }
            return new IntersectionPointData(intersectionPosition, junctionEdgeDatas, type);
        }

        public static Intersection CreateIntersectionAtPosition(Vector3 intersectionPosition, Road road1, Road road2)
        {
            IntersectionPointData intersectionPointData = CalculateIntersectionData(intersectionPosition, road1, road2);
            if (!road1.RoadSystem.DoesIntersectionExist(intersectionPointData.Position))
                return CreateIntersectionAtPosition(intersectionPointData, road1.RoadSystem);
            return null;
        }

        public static Intersection CreateIntersectionAtPositionMultipleRoads(Vector3 intersectionPosition, List<Road> roads)
        {
            IntersectionPointData intersectionPointData = CalculateIntersectionDataMultipleRoads(intersectionPosition, roads);
            if (!roads[0].RoadSystem.DoesIntersectionExist(intersectionPointData.Position))
                return CreateIntersectionAtPosition(intersectionPointData, roads[0].RoadSystem);
            return null;
        }

        /// <summary>Calculate the positions of the two anchor points to be created on either side of the intersection</summary>
        static List<JunctionEdgeData> GetIntersectionAnchorPoints(Road road, Vector3 intersectionPosition, float intersectionLength)
        {
            if (road == null)
            {
                Debug.LogError("Road is null");
                return new List<JunctionEdgeData>();
            }
            float intersectionExtendCoef = 1.2f;
            float intersectionExtension = intersectionLength * 0.5f * intersectionExtendCoef;
            VertexPath vertexPath = road.PathCreator.path;
            int segmentIndex = GetClosestSegmentIndex(road.PathCreator, intersectionPosition);
            float distanceAtIntersection = road.PathCreator.path.GetClosestDistanceAlongPath(intersectionPosition);

            List<JunctionEdgeData> anchorPoints = new List<JunctionEdgeData>();
            bool addedAnchorPoint = false;

            // Use EndOfPathInstruction.Stop to cap the anchor points at the end of the road
            if (distanceAtIntersection > intersectionExtension)
            {
                Vector3 secondAnchorPoint = vertexPath.GetPointAtDistance(distanceAtIntersection - intersectionLength / 2, EndOfPathInstruction.Stop);
                anchorPoints.Add(new JunctionEdgeData(secondAnchorPoint, segmentIndex, road));
                addedAnchorPoint = true;
            }
            else
            {
                // If the intersection is too close to the start of the road, we just move the first anchor point to the intersection
                road.PathCreator.bezierPath.MovePoint(0, intersectionPosition);
            }
            if (distanceAtIntersection < vertexPath.length - intersectionExtension)
            {
                Vector3 firstAnchorPoint = vertexPath.GetPointAtDistance(distanceAtIntersection + intersectionLength / 2, EndOfPathInstruction.Stop);
                anchorPoints.Add(new JunctionEdgeData(firstAnchorPoint, segmentIndex + (addedAnchorPoint ? 1 : 0), road));
            }
            else
            {
                // If the intersection is too close to the end of the road, we just move the last anchor point to the intersection
                road.PathCreator.bezierPath.MovePoint(road.PathCreator.bezierPath.NumPoints - 1, intersectionPosition);
            }

            return anchorPoints;
        }

        /// <summary>Creates an intersection at the given position</summary>
        static Intersection CreateIntersectionAtPosition(IntersectionPointData intersectionPointData, RoadSystem roadSystem)
        {
            List<Road> roads = new List<Road>();
            foreach (JunctionEdgeData junctionEdgeData in intersectionPointData.JunctionEdgeDatas)
            {
                if (!roads.Contains(junctionEdgeData.Road))
                    roads.Add(junctionEdgeData.Road);
            }

            // Create a new intersection at the intersection point
            Intersection intersection = roadSystem.AddNewIntersection(intersectionPointData);

            // Sorting the junction edge data by segment index so that we can place the anchor points in the correct order
            var query = from intersectionData in intersectionPointData.JunctionEdgeDatas
            orderby intersectionData.SegmentIndex
            select intersectionData;

            foreach (JunctionEdgeData junctionEdgeData in query.ToList())
                SplitPathOnPoint(junctionEdgeData.Road.PathCreator, junctionEdgeData.AnchorPoint, junctionEdgeData.SegmentIndex);

            DeleteAnchorsInsideIntersectionBounds(intersection);
            foreach (Road road in roads)
            {
                road.OnChange();
            }
            return intersection;
            //intersection.UpdateMesh();
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
            float intersectionBoundLength = intersection.IntersectionLength * Intersection.IntersectionBoundsLengthMultiplier;
            
            // Create a bounding box around the intersection
            Bounds intersectionBounds = new Bounds(intersection.IntersectionPosition, new Vector3(intersectionBoundLength, intersectionBoundHeight, intersectionBoundLength));
            
            foreach (Road road in intersection.GetIntersectionRoads())
            {
                List<Vector3> ignoreAnchors = new List<Vector3>();

                foreach (IntersectionArm arm in intersection.GetArms(road))
                    ignoreAnchors.Add(arm.JunctionEdgePosition);

                // Only ignore the intersection anchor if it is the first or last anchor on the road
                if (road.PathCreator.bezierPath[0] == intersection.IntersectionPosition || road.PathCreator.bezierPath[road.PathCreator.bezierPath.NumPoints - 1] == intersection.IntersectionPosition)
                    ignoreAnchors.Add(intersection.IntersectionPosition);

                DeleteAnchorsInsideBounds(road.PathCreator.bezierPath, intersectionBounds, ignoreAnchors);
            }
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
    }

    public struct JunctionEdgeData
    {
        public Vector3 AnchorPoint;
        public int SegmentIndex;
        public Road Road;
        public JunctionEdgeData(Vector3 anchorPoint, int segmentIndex, Road road)
        {
            AnchorPoint = anchorPoint;
            SegmentIndex = segmentIndex;
            Road = road;
        }
    }

    /// <summary> Data structure for storing information about an intersection point </summary>
    public class IntersectionPointData
    {
        public Vector3 Position;
        public List<JunctionEdgeData> JunctionEdgeDatas;
        public IntersectionType Type;
        public IntersectionPointData(Vector3 position, List<JunctionEdgeData> junctionEdgeData, IntersectionType type)
        {
            Position = position;
            JunctionEdgeDatas = junctionEdgeData;
            Type = type;
        }
    }
}