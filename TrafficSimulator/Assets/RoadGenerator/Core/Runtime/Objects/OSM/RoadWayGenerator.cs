using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Xml;
using UnityEngine;

namespace RoadGenerator
{
    public class RoadWayGenerator
    {
        private RoadSystem _roadSystem;
        private Dictionary<Vector3, bool> _trafficLightAtJunction;
        private Dictionary<Vector3, List<Road>> _roadsAtNode = new Dictionary<Vector3, List<Road>>();
        public RoadWayGenerator(RoadSystem roadSystem, Dictionary<Vector3, bool> trafficLightAtJunction)
        {
            _roadSystem = roadSystem;
            _trafficLightAtJunction = trafficLightAtJunction;
        }
        private Road SpawnRoad(List<Vector3> points, RoadWay roadWay)
        {
            Road road = _roadSystem.AddNewRoad(PathType.Road);

            // Set the name of the road
            road.gameObject.name = roadWay.Name;

            // Uncomment for testing purposes
            // road.gameObject.name = wayData.WayID;

            // Move the road to the spawn point
            PathCreator pathCreator = road.PathCreator;
            pathCreator.bezierPath = new BezierPath(points, false, PathSpace.xz);
            pathCreator.bezierPath.autoControlLength = 0.1f;

            // Set the road pointers

            // Update the road to display it
            road.OnChange();

            return road;
        }
        public void GenerateRoads(List<RoadWay> roadWays)
        {
            foreach (RoadWay roadWay in roadWays)
                GenerateRoad(roadWay);
        }
        // https://wiki.openstreetmap.org/wiki/Map_features#Highway
        private void GenerateRoad(RoadWay roadWay) 
        {
            List <Vector3> roadPoints = roadWay.Points;

            if (roadWay.RoadType == null)
                return;

            if (roadWay.RoadType == RoadWayType.Footway || roadWay.RoadType == RoadWayType.Path)
            {
                //GenerateFootWay(roadPoints, wayData);
                return;
            }

            if (roadWay.Name == null && roadWay.RoadType != RoadWayType.RaceWay)
                return;

            // Total Length of road
            float totalLength = 0;
            
            for (int i = 0; i < roadPoints.Count - 1; i++)
                totalLength += Vector3.Distance(roadPoints[i], roadPoints[i + 1]);
            
            if (totalLength < 10)
                return;

            // Currently get error when roads have same start and end point, TODO fix
            if (roadPoints[0] == roadPoints[roadPoints.Count - 1])
                return;
            
            List<Vector3> roadPoints2 = new List<Vector3>();
            roadPoints2.Add(roadPoints[0]);
            roadPoints2.Add(roadPoints[1]);
            Road road = SpawnRoad(roadPoints2, (RoadWay)roadWay);

            road.RoadType = roadWay.RoadType.Value;

            if (roadWay.IsOneWay == true)
            {
                road.IsOneWay = true;
                road.LaneWidth /= 2;
            }

            if (roadWay.RoadType == RoadWayType.RaceWay)
                road.LaneWidth = 5;

            if (roadWay.ParkingType != null && roadWay.ParkingType != ParkingType.None)
            {
                if (roadWay.ParkingType == ParkingType.Left || roadWay.ParkingType == ParkingType.Both)
                    road.AddFullRoadSideParking(LaneSide.Secondary);
                
                if (roadWay.ParkingType == ParkingType.Right || roadWay.ParkingType == ParkingType.Both)
                    road.AddFullRoadSideParking(LaneSide.Primary);
                // Avoid spawning lamppoles if there is parking on the road
                roadWay.IsLit = false;
            }

            if (roadWay.MaxSpeed != null)
                road.SpeedLimit = (SpeedLimit)roadWay.MaxSpeed;

            // When the speedlimit is not known in residential areas, set it to 30km/h
            if (roadWay.RoadType == RoadWayType.Residential && roadWay.MaxSpeed == null)
                road.SpeedLimit = SpeedLimit.ThirtyKPH;

            road.ShouldSpawnLampPoles = !roadWay.IsLit;

            PathCreator pathCreator = road.GetComponent<PathCreator>();

            // Roads with only two points will not render properly, this is a hack to render them
            // TODO update the roads correctly
            // Move point to the same place to rerender the road
            if (roadPoints.Count == 2)
                pathCreator.bezierPath.MovePoint(0, roadPoints[0]);

            for (int i = 2; i < roadPoints.Count; i++)
                pathCreator.bezierPath.AddSegmentToEnd(roadPoints[i]);

            foreach (Vector3 point in roadPoints) 
            {
                if (!_roadsAtNode.ContainsKey(point))
                    _roadsAtNode.Add(point, new List<Road> { road });
                else
                    _roadsAtNode[point].Add(road);
            }
        
            road.OnChange();
        }
        /// <summary> Generates intersections where the OSM roads intersect </summary>
        public void GenerateIntersections()
        {
            foreach (KeyValuePair<Vector3, List<Road>> roads in _roadsAtNode)
            {
                Vector3 position = roads.Key;

                // Find nodes that are shared by more than one road
                // This will mean it will either be an intersection or a road connection
                if (roads.Value.Count < 2)
                    continue;

                if (roads.Value.Count == 2)
                {
                    Road road1 = roads.Value[0];
                    Road road2 = roads.Value[1];

                    PathCreator pathCreator1 = road1.PathCreator;
                    PathCreator pathCreator2 = road2.PathCreator;

                    // Currently skipping roads that are shorter than 15m, When the issues with intersections between short roads is fixed this can be removed
                    if (pathCreator1.path.length < 15 || pathCreator2.path.length < 15)
                        continue;

                    bool isNodeAtEndPointRoad1 = pathCreator1.path.GetPoint(pathCreator1.path.NumPoints - 1) == roads.Key || pathCreator1.path.GetPoint(0) == roads.Key;
                    bool isNodeAtEndPointRoad2 = pathCreator2.path.GetPoint(pathCreator2.path.NumPoints - 1) == roads.Key || pathCreator2.path.GetPoint(0) == roads.Key;

                    // If it is an intersection
                    if (!(isNodeAtEndPointRoad1 && isNodeAtEndPointRoad2))
                    {
                        Intersection intersection = IntersectionCreator.CreateIntersectionAtPosition(position, road1, road2);

                        if (intersection != null)
                            intersection.FlowType = _trafficLightAtJunction.ContainsKey(position) && _trafficLightAtJunction[position] ? FlowType.TrafficLights : FlowType.YieldSigns;
                    }
                }
                else if (roads.Value.Count == 3)
                {
                    Road road1 = roads.Value[0];
                    Road road2 = roads.Value[1];
                    Road road3 = roads.Value[2];

                    PathCreator pathCreator1 = road1.PathCreator;
                    PathCreator pathCreator2 = road2.PathCreator;
                    PathCreator pathCreator3 = road3.PathCreator;

                    if (pathCreator1.path.length < 10 || pathCreator2.path.length < 10 || pathCreator3.path.length < 10)
                    {
                        Debug.Log("Road too short");
                        continue;
                    }

                    IntersectionCreator.CreateIntersectionAtPositionMultipleRoads(position, new List<Road> { road1, road2, road3 });
                }
                else if (roads.Value.Count == 4)
                {
                    Road road1 = roads.Value[0];
                    Road road2 = roads.Value[1];
                    Road road3 = roads.Value[2];
                    Road road4 = roads.Value[3];

                    PathCreator pathCreator1 = road1.PathCreator;
                    PathCreator pathCreator2 = road2.PathCreator;
                    PathCreator pathCreator3 = road3.PathCreator;
                    PathCreator pathCreator4 = road4.PathCreator;

                    if (pathCreator1.path.length < 10 || pathCreator2.path.length < 10 || pathCreator3.path.length < 10 || pathCreator4.path.length < 10)
                    {
                        Debug.Log("Road too short");
                        continue;
                    }

                    IntersectionCreator.CreateIntersectionAtPositionMultipleRoads(position, new List<Road> { road1, road2, road3, road4 });
                }
            
            }

            foreach (KeyValuePair<Vector3, List<Road>> roads in _roadsAtNode) 
            {
                if (roads.Value.Count == 2)
                {
                    Road road1 = roads.Value[0];
                    Road road2 = roads.Value[1];
                    PathCreator pathCreator1 = road1.PathCreator;
                    PathCreator pathCreator2 = road2.PathCreator;

                    bool isNodeAtEndPointRoad1 = pathCreator1.path.GetPoint(pathCreator1.path.NumPoints - 1) == roads.Key || pathCreator1.path.GetPoint(0) == roads.Key;
                    bool isNodeAtEndPointRoad2 = pathCreator2.path.GetPoint(pathCreator2.path.NumPoints - 1) == roads.Key || pathCreator2.path.GetPoint(0) == roads.Key;

                    if (isNodeAtEndPointRoad1 && isNodeAtEndPointRoad2 && road1.ConnectedToAtStart == null && road1.ConnectedToAtEnd == null)
                        road1.ConnectRoadIfEndPointsAreClose();
                }
            }
        }
        public void AddBusStops(List<XmlNode> _busStops, MapGenerator mapGenerator)
        {
            if (_busStops.Count == 0)
                return;

            GameObject busStopPrefab = _roadSystem.DefaultBusStopPrefab;
            string name = "";
            string refName = "";

            foreach (XmlNode busStopNode in _busStops)
            {
                foreach (XmlNode tagNode in busStopNode.ChildNodes)
                {
                    if (tagNode.Name == "tag")
                    {
                        switch (tagNode.Attributes["k"].Value)
                        {
                            case "name":
                                name = tagNode.Attributes["v"].Value;
                                break;
                            case "ref":
                                refName = tagNode.Attributes["v"].Value;
                                break;
                        }
                    }
                }

                name = name + " " + refName;
                Vector3 position = mapGenerator.GetNodePosition(busStopNode);

                if (_roadsAtNode.ContainsKey(position) == false)
                    continue;

                List<Road> roads = _roadsAtNode[position];

                if (roads.Count == 0)
                    continue;

                Road road = roads[0];
                RoadNode curr = road.StartRoadNode;
                float minDistanceToBusStop = float.MaxValue;
                RoadNode closestRoadNode = null;

                while (curr != null)
                {
                    float distanceToBusStop = Vector3.Distance(curr.Position, position);

                    if (distanceToBusStop < minDistanceToBusStop)
                    {
                        minDistanceToBusStop = distanceToBusStop;
                        closestRoadNode = curr;
                    }

                    curr = curr.Next;
                }

                bool isForward = refName == "A";
                road.SpawnBusStop(closestRoadNode, isForward, busStopPrefab, name);
            }
        }
    }
}