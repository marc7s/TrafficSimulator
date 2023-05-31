using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Xml;
using System;
using System.Linq;

namespace RoadGenerator
{
    public enum WayType
    {
        Residential,
        Path,
        Footway,
        Cycleway,
        Motorway,
        Primary,
        Secondary,
        Trunk,
        Steps,
        RailTrain,
        RailTram,
        Tertiary,
        Service,
        Building,
        Terrain,
        RaceWay,
        Unclassified
    }

    public struct BuildingData
    {
        public bool? IsMultiPolygon;
        public string StreetName;
        public string StreetAddress;
        public int? Height;
        public int? BuildingLevels;
    }

    public enum ServiceType
    {
        DriveWay,
        Alley
    }

    public struct WayData
    {
        public string WayID;
        public WayType WayType;
        // Lane amount for one direction
        public int? LaneAmount;
        public int? MaxSpeed;
        public bool? IsLit;
        public bool? IsOneWay;
        public string Name;
        public SideWalkType? SideWalkType;
        public ParkingType? ParkingType;
        public ServiceType? ServiceType;
        public BuildingData BuildingData;
        public TerrainType? TerrainType;
    }

    public enum SideWalkType
    {
        None,
        Left,
        Right,
        Both
    }

    public enum ParkingType
    {
        None,
        Left,
        Right,
        Both
    }

    public enum TerrainType
    {
        Water = -1,
        Grass = 1,
        Sand = -2,
        Concrete = -3,
        Forest = 3,
        Default = 0
    }

    public struct TerrainArea
    {
        public TerrainType TerrainType;
        public List<Vector3> OuterArea;
        public List<List<Vector3>> InnerAreas;
        public TerrainArea(TerrainType terrainType, List<Vector3> area, List<List<Vector3>> innerAreas = null)
        {
            TerrainType = terrainType;
            OuterArea = area;
            InnerAreas = innerAreas ?? new List<List<Vector3>>();
        }
    }

    public class MapGenerator : MonoBehaviour
    {
        [Header("Connections")]
        public GameObject RoadPrefab;
        public GameObject RailPrefab;
        public GameObject BuildingPrefab;
        public Material BuildingWallMaterial;
        public Material BuildingRoofMaterial;
        private RoadSystem _roadSystem;
        private Dictionary<string, XmlNode> _nodesDict = new Dictionary<string, XmlNode>();
        private Dictionary<string, XmlNode> _wayDict = new Dictionary<string, XmlNode>();
        private Dictionary<Vector3, List<Road>> _roadsAtNode = new Dictionary<Vector3, List<Road>>();
        private Dictionary<Vector3, bool> _trafficLightAtJunction = new Dictionary<Vector3, bool>();
        private List<XmlNode> _busStops = new List<XmlNode>();
        private List<XmlNode> _trees = new List<XmlNode>();
        private float _minLat = 0;
        private float _minLon = 0;
        private float _maxLat = 0;
        private float _maxLon = 0;
        private Terrain _terrain;
        private List<TerrainArea> _terrainAreas = new List<TerrainArea>();

        public void GenerateMap(RoadSystem roadSystem)
        {
            roadSystem.UseOSM = true;
            roadSystem.IsGeneratingOSM = true;
            _terrain = roadSystem.Terrain;
            _nodesDict.Clear();
            _roadsAtNode.Clear();
            _busStops.Clear();
            _minLat = 0;
            _minLon = 0;
            _maxLat = 0;
            _maxLon = 0;

            _roadSystem = roadSystem;

            XmlDocument doc = new XmlDocument();
            LoadOSMMap(doc);
        
            // Finding the bounds of the map and adding all nodes to a dictionary
            foreach(XmlNode node in doc.DocumentElement.ChildNodes)
            {
                switch(node.Name)
                {
                    case "bounds":
                        _minLat = float.Parse(node.Attributes["minlat"].Value.Replace(".", ","));
                        _minLon = float.Parse(node.Attributes["minlon"].Value.Replace(".", ","));
                        _maxLat = float.Parse(node.Attributes["maxlat"].Value.Replace(".", ","));
                        _maxLon = float.Parse(node.Attributes["maxlon"].Value.Replace(".", ","));
                        break;
                    case "node":
                        if (!_nodesDict.ContainsKey(node.Attributes["id"].Value))
                            _nodesDict.Add(node.Attributes["id"].Value, node);
                        break;
                    case "way":
                        if (!_wayDict.ContainsKey(node.Attributes["id"].Value))
                            _wayDict.Add(node.Attributes["id"].Value, node);
                        break;
                }

                foreach(XmlNode childNode in node.ChildNodes)
                {
                    if (childNode.Name == "tag")
                    {
                        switch (childNode.Attributes["k"].Value)
                        {
                            case "highway":
                                if (childNode.Attributes["v"].Value == "bus_stop")
                                    _busStops.Add(node);
                                if (childNode.Attributes["v"].Value == "traffic_signals")
                                    _trafficLightAtJunction[GetNodePosition(node)] = true;
                                break;
                            case "traffic_signals":
                                _trafficLightAtJunction[GetNodePosition(node)] = true;
                                break;
                            case "crossing":
                                if (childNode.Attributes["v"].Value == "traffic_signals")
                                    _trafficLightAtJunction[GetNodePosition(node)] = true;
                                break;
                            case "natural":
                                if (childNode.Attributes["v"].Value == "tree")
                                    _trees.Add(node);
                                break;
                        }
                    }
                }
            }

            foreach(XmlNode node in doc.DocumentElement.ChildNodes)
            {
                if (node.Name == "way") 
                {
                    WayData? wayData = GetWayData(node);
                    IEnumerator ienum = node.GetEnumerator();

                    if (wayData == null)
                        continue;

                    if (wayData?.WayType == WayType.Terrain && roadSystem.ShouldGenerateTerrain)
                        AddTerrain(ienum, wayData.Value);
                    else if (wayData?.WayType != WayType.Building && roadSystem.ShouldGenerateRoads) 
                        GenerateRoad(ienum, wayData.Value);
                    else if (wayData?.WayType == WayType.Building && roadSystem.ShouldGenerateBuildings)
                        GenerateBuilding(ienum, wayData.Value);
                }
                else if (node.Name == "relation")
                {
                    WayData? wayData = GetWayData(node);
                    IEnumerator ienum = node.GetEnumerator();

                    // When the building is multipolygon
                    if (wayData?.BuildingData.IsMultiPolygon == true && wayData?.WayType == WayType.Building && roadSystem.ShouldGenerateBuildings) 
                    {
                        while (ienum.MoveNext())
                        {
                            XmlNode currentNode = (XmlNode) ienum.Current; 
                            if (currentNode.Name == "member" && currentNode.Attributes["type"].Value == "way")
                            {
                                if (_wayDict.ContainsKey(currentNode.Attributes["ref"].Value))
                                {
                                    XmlNode wayNode = _wayDict[currentNode.Attributes["ref"].Value];
                                    GenerateBuilding(wayNode.GetEnumerator(), wayData.Value);
                                }
                            }
                        }
                    }
                    if (wayData?.WayType == WayType.Terrain)
                    {
                        List<Vector3> outerPoints = new List<Vector3>();
                        List<List<Vector3>> innerPoints = new List<List<Vector3>>();
                        while (ienum.MoveNext())
                        {
                            XmlNode currentNode = (XmlNode) ienum.Current; 
                            if (currentNode.Name == "member" && currentNode.Attributes["type"].Value == "way")
                            {
                                if (currentNode.Attributes["role"].Value == "outer" && _wayDict.ContainsKey(currentNode.Attributes["ref"].Value))
                                {
                                    XmlNode wayNode = _wayDict[currentNode.Attributes["ref"].Value];
                                    List<Vector3> wayNodePositions = GetWayNodePositions(wayNode.GetEnumerator());
                                    wayNodePositions.Reverse();
                                    outerPoints.AddRange(wayNodePositions);
                                }
                                else if (currentNode.Attributes["role"].Value == "inner" && _wayDict.ContainsKey(currentNode.Attributes["ref"].Value))
                                {
                                    XmlNode wayNode = _wayDict[currentNode.Attributes["ref"].Value];
                                    innerPoints.Add(GetWayNodePositions(wayNode.GetEnumerator()));
                                }
                            }
                        }

                        AddTerrain(ienum, wayData.Value, outerPoints, innerPoints);
                    }
                }
            }

            AddIntersections();

            if (roadSystem.ShouldGenerateBusStops)
                AddBusStops();

            if (roadSystem.ShouldGenerateTerrain)
            {
                GenerateTerrain();
                AddTrees();
            }

            roadSystem.IsGeneratingOSM = false;

            foreach (Road road in roadSystem.DefaultRoads)
                road.OnChange();
        }

        private void AddTerrain(IEnumerator ienum, WayData wayData, List<Vector3> multiPolygonOuterPoints = null, List<List<Vector3>> multiPolygonInnerPoints = null)
        {
            List<Vector3> points = multiPolygonOuterPoints ?? GetWayNodePositions(ienum);

            if (points.Count < 3)
                return;

            TerrainArea terrainArea = new TerrainArea(wayData.TerrainType.Value, points, multiPolygonInnerPoints);
            _terrainAreas.Add(terrainArea);
        }

        public bool IsPointInPolygon(Vector2 point, Vector2[] polygon)
        {
            if (polygon == null || polygon.Length < 3)
                return false;

            int polygonLength = polygon.Length;
            int i = 0;
            bool inside = false;
            // x, y for tested point.
            float pointX = point.x, pointY = point.y;
            // start / end point for the current polygon segment.
            float startX;
            float startY;
            float endX;
            float endY;
            Vector2 endPoint = polygon[polygonLength - 1];
            endX = endPoint.x;
            endY = endPoint.y;

            while (i < polygonLength)
            {
                startX = endX;
                startY = endY;
                endPoint = polygon[i++];
                endX = endPoint.x;
                endY = endPoint.y;
                inside ^= (endY > pointY ^ startY > pointY) && ((pointX - endX) < (pointY - endY) * (startX - endX) / (startY - endY));
            }

            return inside;
        }

        private List<Vector2> Vector3ToVector2(List<Vector3> points)
        {
            List<Vector2> vector2Points = new List<Vector2>();

            foreach (Vector3 point in points)
                vector2Points.Add(new Vector2(point.x, point.z));

            return vector2Points;
        }

        private void GenerateTerrain()
        {
            TerrainData terrainData = _terrain.terrainData;
            Vector3 maxSize = LatLonToPosition(_maxLat, _maxLon);
            maxSize.y = 10f;
            terrainData.size = maxSize;
            _terrain.gameObject.SetActive(true);
            _terrain.gameObject.transform.position = new Vector3(0, -10.01f, 0);
            float baseHeight = 10;

            int res = terrainData.heightmapResolution;
            float[,] heights = terrainData.GetHeights(0, 0, res, res);
            float[, ,] splatmapData = new float[terrainData.alphamapWidth, terrainData.alphamapHeight, terrainData.alphamapLayers];

            for (int x = 0; x < res; x++)
            {
                for (int y = 0; y < res; y++)
                {
                    Vector2 basPos2D = Vector2.zero;
                    Vector2 terrainPosition =  basPos2D + new Vector2(x * terrainData.size.x / res, y * terrainData.size.z / res);
                    heights[y, x] = baseHeight;
                    bool isInsideInnerArea = false;

                    foreach (TerrainArea terrainBounds in _terrainAreas)
                    {
                        if (terrainBounds.TerrainType == TerrainType.Water && IsPointInPolygon(terrainPosition, Vector3ToVector2(terrainBounds.OuterArea).ToArray()))
                        {
                            if (terrainBounds.InnerAreas != null && terrainBounds.InnerAreas.Count > 0)
                            {
                                List<Vector3> innerArea = new List<Vector3>();

                                foreach (List<Vector3> innerArea2 in terrainBounds.InnerAreas)
                                    innerArea.AddRange(innerArea2);

                                // The terrain point is inside the terrain type area
                                if (IsPointInPolygon(terrainPosition, Vector3ToVector2(innerArea).ToArray()))
                                {
                                    isInsideInnerArea = true;
                                    break;
                                }
                            }

                            if (isInsideInnerArea)
                                break;

                            heights[y, x] = 0;
                            break;
                        }
                    }
                }
            }

            terrainData.SetHeights(0, 0, heights);

            for (int y = 0; y < terrainData.alphamapHeight; y++)
            {
                for (int x = 0; x < terrainData.alphamapWidth; x++)
                {
                    Vector2 basePos2D = Vector2.zero;
                    Vector2 terrainPosition =  basePos2D + new Vector2(x * terrainData.size.x / terrainData.alphamapWidth, y * terrainData.size.z / terrainData.alphamapHeight);
                    // Setup an array to record the mix of texture weights at this point
                    float[] splatWeights = new float[terrainData.alphamapLayers];

                    bool foundTerrain = false;
                    bool isInsideInnerArea = false;
                    foreach (TerrainArea terrainBounds in _terrainAreas)
                    {
                        // The terrain point is inside the terrain type area
                        if (IsPointInPolygon(terrainPosition, Vector3ToVector2(terrainBounds.OuterArea).ToArray()))
                        {
                            foreach (List<Vector3> innerArea in terrainBounds.InnerAreas)
                            {
                                // The terrain point is inside the terrain type area
                                if (IsPointInPolygon(terrainPosition, Vector3ToVector2(innerArea).ToArray()))
                                {
                                    isInsideInnerArea = true;
                                    break;
                                }
                            }

                            if (isInsideInnerArea)
                                break;

                            if (terrainBounds.TerrainType == TerrainType.Grass)
                                splatWeights[(int)TerrainType.Grass] = 1f;
                            else if (terrainBounds.TerrainType == TerrainType.Forest)
                                splatWeights[(int)TerrainType.Forest] = 1f;
                            else
                                splatWeights[2] = 1f;

                            foundTerrain = true;
                            break;
                        }
                    }

                    if (!foundTerrain)
                        splatWeights[(int)TerrainType.Default] = 1f;

                    // Sum of all textures weights must add to 1, so calculate normalization factor from sum of weights
                    float z = splatWeights.Sum();

                    // Loop through each terrain texture
                    for(int i = 0; i<terrainData.alphamapLayers; i++)
                    {
                        // Normalize so that sum of all texture weights = 1
                        splatWeights[i] /= z;

                        // Assign this point to the splatmap array
                        splatmapData[y, x, i] = splatWeights[i];
                    }
                }
            }
            // Finally assign the new splatmap to the terrainData:
            terrainData.SetAlphamaps(0, 0, splatmapData);
        }

        private void AddIntersections()
        {
            foreach (KeyValuePair<Vector3, List<Road>> roads in _roadsAtNode)
            {
                Vector3 position = roads.Key;

                // Find nodes that are shared by more than one road
                // This will mean it will either be an intersection or a road connection
                if (roads.Value.Count > 1)
                {
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

        public void AddBusStops()
        {
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
                Vector3 position = GetNodePosition(busStopNode);

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

        private void AddTrees()
        {
            GameObject treePrefab = _roadSystem.DefaultTreePrefab;

            foreach (XmlNode treeNode in _trees)
            {
                Vector3 position = GetNodePosition(treeNode);
                GameObject tree = Instantiate(treePrefab, position, Quaternion.identity);
                tree.transform.parent = _roadSystem.NatureContainer.transform;
                tree.name = "Tree";
            }
        }

        private WayData? GetWayData(XmlNode node)
        {
            IEnumerator ienum = node.GetEnumerator();
            WayType? wayType = null;
            BuildingData buildingData = new BuildingData();
            WayData wayData = new WayData();
            ParkingType parkingType = ParkingType.None;
            wayData.WayID = node.Attributes["id"].Value;
            TerrainType? terrainType = null;
            
            // Search for type of way
            while (ienum.MoveNext())
            {
                XmlNode currentNode = (XmlNode) ienum.Current;
                if (currentNode.Name != "tag") 
                    continue;

                try
                {
                    switch (currentNode.Attributes["k"].Value)
                    {
                        case "highway":
                            wayType = GetRoadType(currentNode);
                            break;
                        case "building":
                            wayType = WayType.Building;
                            break;
                        case "water":
                            if (currentNode.Attributes["v"].Value == "lake")
                            {
                                terrainType = TerrainType.Water;
                                wayType = WayType.Terrain;
                            }

                            break;
                        case "natural":
                            if (currentNode.Attributes["v"].Value == "water")
                            {
                                terrainType = TerrainType.Water;
                                wayType = WayType.Terrain;
                            }
                            break;
                        case "waterway":
                            terrainType = TerrainType.Water;
                            wayType = WayType.Terrain;
                            break;
                        case "name":
                            wayData.Name = currentNode.Attributes["v"].Value;
                            break;
                        case "landuse":
                            terrainType = GetTerrainType(currentNode);
                            wayType = WayType.Terrain;
                            break;
                        case "maxspeed":
                            wayData.MaxSpeed = int.Parse(currentNode.Attributes["v"].Value);
                            break;
                        case "junction":
                            if (currentNode.Attributes["v"].Value == "roundabout")
                                return null;
                            break;
                        case "height":
                            buildingData.Height = int.Parse(currentNode.Attributes["v"].Value);
                            break;
                        case "addr:housenumber":
                            buildingData.StreetAddress = currentNode.Attributes["v"].Value;
                            break;
                        case "addr:street":
                            buildingData.StreetName = currentNode.Attributes["v"].Value;
                            break;
                        case "service":
                            if (currentNode.Attributes["v"].Value == "driveway")
                                wayData.ServiceType = ServiceType.DriveWay;
                            break;
                        case "lit":
                            if (currentNode.Attributes["v"].Value == "yes")
                                wayData.IsLit = true;
                            else 
                                wayData.IsLit = false;
                            break;
                        case "railway":
                            wayType = WayType.RailTram;
                            break;
                        case "building:levels":
                            buildingData.BuildingLevels = int.Parse(currentNode.Attributes["v"].Value);
                            break;
                        case "type":
                            if (currentNode.Attributes["v"].Value == "multipolygon")
                                buildingData.IsMultiPolygon = true;
                            break;
                        case "oneway":
                            wayData.IsOneWay = currentNode.Attributes["v"].Value == "yes";
                            break;
                        case "parking:right":
                            if (currentNode.Attributes["v"].Value == "lane")
                                parkingType = ParkingType.Right;
                            else if (currentNode.Attributes["v"].Value == "street_side")
                                parkingType = ParkingType.Right;
                            break;
                        case "parking:left":
                            if (currentNode.Attributes["v"].Value == "lane")
                                parkingType = ParkingType.Left;
                            else if (currentNode.Attributes["v"].Value == "street_side")
                                parkingType = ParkingType.Left;
                            break;
                        case "parking:both":
                            if (currentNode.Attributes["v"].Value == "lane")
                                parkingType = ParkingType.Both;
                            else if (currentNode.Attributes["v"].Value == "street_side")
                                parkingType = ParkingType.Both;
                            break;
                    }
                }
                catch
                {
                    Debug.Log("Error parsing way data");
                }
            }

            if (wayType == null)
                return null;

            wayData.WayType = wayType.Value;
            wayData.BuildingData = buildingData;
            wayData.ParkingType = parkingType;
            wayData.TerrainType = terrainType;

            return wayData;
        }

        //https://wiki.openstreetmap.org/wiki/Map_features#Highway
        private WayType? GetRoadType(XmlNode node)
        {
            switch (node.Attributes["v"].Value)
            {
                case "motorway":
                    return WayType.Motorway;
                case "residential":
                    return WayType.Residential;
                case "tertiary":
                    return WayType.Tertiary;
                case "secondary":
                    return WayType.Secondary;
                case "primary":
                    return WayType.Primary;
                case "trunk":
                    return WayType.Trunk;
                case "service":
                    return WayType.Service;
                case "footway":
                    return WayType.Footway;
                case "path":
                    return WayType.Path;
                case "unclassified":
                    return WayType.Unclassified;
                case "raceway":
                    return WayType.RaceWay;
                default:
                    return null;
            }
        }

        // https://wiki.openstreetmap.org/wiki/Key:landuse
        private TerrainType GetTerrainType(XmlNode node)
        {
            switch (node.Attributes["v"].Value)
            {
                case "grass":
                    return TerrainType.Grass;
                case "sand":
                    return TerrainType.Sand;
                case "forest":
                    return TerrainType.Forest;
                default:
                    return TerrainType.Default;
            }
        }

        private void LoadOSMMap(XmlDocument document)
        {
            document.Load("Assets/OsmMaps/Masthugget.osm");
        }

        private void GenerateFootWay(List <Vector3> points, WayData wayData)
        {
            return;
            /*Debug.Log("Generating footway");
            FootWayMeshGenerator footWayMeshGenerator = new FootWayMeshGenerator();
            Mesh footWay = footWayMeshGenerator.GenerateMesh(points, 4, 0.1f);
            GameObject footWayObject = new GameObject();
            footWayObject.transform.parent = _roadSystem.RoadContainer.transform;
            MeshFilter meshFilter = footWayObject.AddComponent<MeshFilter>();
            meshFilter.mesh = footWay;
            MeshRenderer meshRenderer = footWayObject.AddComponent<MeshRenderer>();
            //meshRenderer.material = roadSystem.DefaultRoadMaterial;
            footWayObject.name = "FootWay";*/
        }

        // https://wiki.openstreetmap.org/wiki/Map_features#Highway
        void GenerateRoad(IEnumerator ienum, WayData wayData) 
        {
            List <Vector3> roadPoints = GetWayNodePositions(ienum);

            if (wayData.WayType == WayType.Footway || wayData.WayType == WayType.Path)
            {
                GenerateFootWay(roadPoints, wayData);
                return;
            }

            if (wayData.Name == null && wayData.WayType != WayType.RailTram && wayData.WayType != WayType.RaceWay)
                return;

            if (wayData.WayType == WayType.RailTram)
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
            Road road = SpawnRoad(roadPoints2, wayData);

            road.RoadType = wayData.WayType;

            if (wayData.IsOneWay == true || wayData.WayType == WayType.RailTram)
            {
                road.IsOneWay = true;
                road.LaneWidth /= 2;
            }

            if (wayData.WayType == WayType.RaceWay)
                road.LaneWidth = 5;

            if (wayData.ParkingType != null && wayData.ParkingType != ParkingType.None)
            {
                if (wayData.ParkingType == ParkingType.Left || wayData.ParkingType == ParkingType.Both)
                    road.AddFullRoadSideParking(LaneSide.Secondary);
                
                if (wayData.ParkingType == ParkingType.Right || wayData.ParkingType == ParkingType.Both)
                    road.AddFullRoadSideParking(LaneSide.Primary);
                // Avoid spawning lamppoles if there is parking on the road
                wayData.IsLit = false;
            }

            if (wayData.MaxSpeed != null)
                road.SpeedLimit = (SpeedLimit)wayData.MaxSpeed;

            // When the speedlimit is not known in residential areas, set it to 30km/h
            if (wayData.WayType == WayType.Residential && wayData.MaxSpeed == null)
                road.SpeedLimit = SpeedLimit.ThirtyKPH;

            road.ShouldSpawnLampPoles = wayData.IsLit != null ? wayData.IsLit.Value : false;

            PathCreator pathCreator = road.GetComponent<PathCreator>();

            // Roads with only two points will not render properly, this is a hack to render them
            // TODO update the roads correctly
            // Move point to the same place to rerender the road
            if (roadPoints.Count == 2)
                pathCreator.bezierPath.MovePoint(0, roadPoints[0]);

            for (int i = 2; i < roadPoints.Count; i++)
                pathCreator.bezierPath.AddSegmentToEnd(roadPoints[i]);
            
            if (wayData.WayType != WayType.RailTram)
            {
                foreach (Vector3 point in roadPoints) 
                {
                    if (!_roadsAtNode.ContainsKey(point))
                        _roadsAtNode.Add(point, new List<Road> { road });
                    else
                        _roadsAtNode[point].Add(road);
                }
            }

            road.OnChange();
        }

        List<Vector3> GetWayNodePositions(IEnumerator ienum)
        {
            List <Vector3> nodePositions = new List<Vector3>();
            while (ienum.MoveNext())
            {
                XmlNode currentNode = (XmlNode) ienum.Current; 

                if (currentNode.Name == "nd" && _nodesDict.ContainsKey(currentNode.Attributes["ref"].Value)) 
                { 
                    Vector3 nodePosition = GetNodePosition(_nodesDict[currentNode.Attributes["ref"].Value]);
                    nodePositions.Add(nodePosition);
                }
            }

            return nodePositions;
        }

        private Vector3 GetNodePosition(XmlNode node)
        {
            try
            {
                float lat = (float)(double.Parse(node.Attributes["lat"].Value.Replace(".", ",")));
                float lon = (float)(double.Parse(node.Attributes["lon"].Value.Replace(".", ",")));

                return LatLonToPosition(lat, lon);
            }
            catch
            {
                Debug.Log("Error parsing node position");
                return new Vector3(0, 0, 0);
            }
        }

        private static double Haversine(double lat1, double lat2, double lon1, double lon2)
        {
            float defToRad = Mathf.PI / 180;
            lat1 *= defToRad;
            lat2 *= defToRad;
            lon1 *= defToRad;
            lon2 *= defToRad;

            const double r = 6378100; // meters
                    
            var sdlat = Math.Sin((lat2 - lat1) / 2);
            var sdlon = Math.Sin((lon2 - lon1) / 2);
            var q = sdlat * sdlat + Math.Cos(lat1) * Math.Cos(lat2) * sdlon * sdlon;
            var d = 2 * r * Math.Asin(Math.Sqrt(q));

            return d;
        }

        private Vector3 LatLonToPosition(float lat, float lon)
        {
            float x = (float)Haversine((double)lat, (double)lat, (double)_minLon, (double)lon);
            float z = (float)Haversine((double)_minLat, (double)lat, (double)lon, (double)lon);

            // If the lat is less than the min lat, the x value should be negative
            if (lon < _minLon)
                x *= -1;
            
            // If the lon is less than the min lon, the z value should be negative
            if (lat < _minLat)
                z *= -1;

            return new Vector3(x, 0, z);
        }

        void GenerateBuilding(IEnumerator ienum, WayData wayData)
        {
            float defaultBuildingHeight = 25;
            float height = wayData.BuildingData.Height ?? defaultBuildingHeight;

            if (wayData.BuildingData.Height == null && wayData.BuildingData.BuildingLevels != null)
                height = wayData.BuildingData.BuildingLevels.Value * 3.5f;

            System.Random random = new System.Random();
            float randomEpsilon = (float)random.NextDouble() * 0.2f;
            height += randomEpsilon;

            List<Vector3> buildingPointsBottom = GetWayNodePositions(ienum);
            List<BuildingPoints> buildingPoints = new List<BuildingPoints>();
            List<Vector3> buildingPointsTop = new List<Vector3>();

            foreach (Vector3 point in buildingPointsBottom)
                buildingPoints.Add(new BuildingPoints(point, new Vector3(point.x, height, point.z)));

            foreach (BuildingPoints point in buildingPoints)
                buildingPointsTop.Add(point.TopPoint);

            GameObject house = Instantiate(BuildingPrefab, buildingPointsBottom[0], Quaternion.identity);

            house.name = GetBuildingName(wayData);

            house.transform.parent = _roadSystem.BuildingContainer.transform;
            Mesh buildingMesh = AssignMeshComponents(house);

            // Create roofs for buildings
            List<Triangle> triangles = new List<Triangle>();

            try
            {
                triangles.AddRange(TriangulateConcavePolygon(buildingPointsTop));
            }
            catch (Exception){}

            buildingPointsTop.Reverse();

            try
            {
                triangles.AddRange(TriangulateConcavePolygon(buildingPointsTop));
            }
            catch (Exception){}

            CreateBuildingMesh(buildingMesh, buildingPoints, triangles);
        }

        private string GetBuildingName(WayData wayData)
        {
            string defaultBuildingName = "Building";

            if (wayData.Name != null)
                return wayData.Name;
 
            if(wayData.BuildingData.StreetName == null || wayData.BuildingData.StreetAddress == null)
                return defaultBuildingName;

            return wayData.BuildingData.StreetName + " " + wayData.BuildingData.StreetAddress;
        }

        private struct BuildingPoints
        {
            public Vector3 BottomPoint;
            public Vector3 TopPoint;
            public BuildingPoints(Vector3 bottomPoint, Vector3 topPoint)
            {
                BottomPoint = bottomPoint;
                TopPoint = topPoint;
            }
        }

        private void CreateBuildingMesh(Mesh buildingMesh, List<BuildingPoints> buildingPoints, List<Triangle> triangles)
        {
            List<Vector3> bottomPoints = new List<Vector3>();
            Dictionary<Vector3, int> positionToIndex = new Dictionary<Vector3, int>();
            List<Vector3> verts = new List<Vector3>();
            List<int> wallTris = new List<int>();
            List<int> roofTris = new List<int>();

            foreach (BuildingPoints buildingPoint in buildingPoints)
            {
                verts.Add(buildingPoint.BottomPoint);

                if (!positionToIndex.ContainsKey(buildingPoint.BottomPoint))
                {
                    positionToIndex.Add(buildingPoint.BottomPoint, verts.Count - 1);
                    bottomPoints.Add(buildingPoint.BottomPoint);
                }

                verts.Add(buildingPoint.TopPoint);

                if (!positionToIndex.ContainsKey(buildingPoint.TopPoint))
                    positionToIndex.Add(buildingPoint.TopPoint, verts.Count - 1);
            }

            bool isBuildingClockwise = IsBuildingClockWise(bottomPoints);
            int index = -1;
            bool isFirstIteration = true;
            
            foreach (BuildingPoints buildingPoint in buildingPoints)
            {
                if (isFirstIteration)
                {
                    isFirstIteration = false;
                    index += 2;
                    continue;
                }
                
                index += 2;
                AddBuildingWall(index, index -1, index - 2, index - 3, wallTris, isBuildingClockwise);
            }

            foreach (Triangle triangle in triangles)
            {
                if (positionToIndex.ContainsKey(triangle.Vertex1.Position) && positionToIndex.ContainsKey(triangle.Vertex2.Position) && positionToIndex.ContainsKey(triangle.Vertex3.Position))
                {
                    roofTris.Add(positionToIndex[triangle.Vertex1.Position]);
                    roofTris.Add(positionToIndex[triangle.Vertex2.Position]);
                    roofTris.Add(positionToIndex[triangle.Vertex3.Position]);
                }
            }

            buildingMesh.Clear();
            buildingMesh.vertices = verts.ToArray();
            buildingMesh.subMeshCount = 2;
            buildingMesh.SetTriangles(wallTris.ToArray(), 0);
            buildingMesh.SetTriangles(roofTris.ToArray(), 1);
            buildingMesh.RecalculateBounds();
        }

        private bool IsBuildingClockWise(List<Vector3> buildingPoints)
        {
            float sum = 0;

            for (int i = 0; i < buildingPoints.Count; i++)
            {
                Vector3 currentPoint = buildingPoints[i];
                Vector3 nextPoint = buildingPoints[(i + 1) % buildingPoints.Count];

                sum += (nextPoint.x - currentPoint.x) * (nextPoint.z + currentPoint.z);
            }

            return sum > 0;
        }

        private void AddBuildingWall(int currentSideTopIndex, int currentSideBottomIndex, int prevSideTopIndex, int prevSideBottomIndex, List<int> triangles, bool isClockWise)
        {
            if (isClockWise)
            {
                triangles.Add(prevSideBottomIndex);
                triangles.Add(currentSideTopIndex);
                triangles.Add(prevSideTopIndex);

                triangles.Add(currentSideTopIndex);
                triangles.Add(prevSideBottomIndex);
                triangles.Add(currentSideBottomIndex);
            }
            else
            {
                triangles.Add(prevSideTopIndex);
                triangles.Add(currentSideTopIndex);
                triangles.Add(prevSideBottomIndex);

                triangles.Add(currentSideBottomIndex);
                triangles.Add(prevSideBottomIndex);
                triangles.Add(currentSideTopIndex);
            }
        }

        Mesh AssignMeshComponents(GameObject buildingObject)
        {
            buildingObject.transform.rotation = Quaternion.identity;
            buildingObject.transform.position = Vector3.zero;
            buildingObject.transform.localScale = Vector3.one;

            // Ensure mesh renderer and filter components are assigned
            if (!buildingObject.gameObject.GetComponent<MeshFilter>()) 
                buildingObject.gameObject.AddComponent<MeshFilter>();

            if (!buildingObject.GetComponent<MeshRenderer>()) 
                buildingObject.gameObject.AddComponent<MeshRenderer>();

            MeshRenderer _meshRenderer = buildingObject.GetComponent<MeshRenderer>();
            _meshRenderer.sharedMaterials = new Material[]{ BuildingWallMaterial, BuildingRoofMaterial };
            MeshFilter _meshFilter = buildingObject.GetComponent<MeshFilter>();

            Mesh mesh = new Mesh();

            _meshFilter.sharedMesh = mesh;
            return mesh;
        }

        Road SpawnRoad(List<Vector3> points, WayData wayData)
        {
            GameObject prefab = wayData.WayType == WayType.RailTram ? RailPrefab : RoadPrefab;

            // Instantiate a new road prefab
            GameObject roadObj = Instantiate(prefab, points[0], Quaternion.identity);

            // Set the name of the road
            roadObj.name = wayData.Name;

            // Uncomment for testing purposes
            // roadObj.name = wayData.WayID;

            roadObj.transform.parent = _roadSystem.RoadContainer.transform;

            // Get the road from the prefab
            Road road = roadObj.GetComponent<Road>();

            // Move the road to the spawn point
            PathCreator pathCreator = roadObj.GetComponent<PathCreator>();
            pathCreator.bezierPath = new BezierPath(points, false, PathSpace.xz);
            pathCreator.bezierPath.autoControlLength = 0.1f;

            // Set the road pointers
            road.RoadObject = roadObj;
            road.RoadSystem = _roadSystem;

            // Update the road to display it
            road.OnChange();
            _roadSystem.AddRoad(road as DefaultRoad);

            return road;
        }

        public static List<Triangle> TriangulateConcavePolygon(List<Vector3> points)
        {
            // The list with triangles the method returns
            List<Triangle> triangles = new List<Triangle>();

            // If we just have three points, then we dont have to do all calculations
            if (points.Count == 3)
            {
                triangles.Add(new Triangle(points[0], points[1], points[2]));
                return triangles;
            }

            // Step 1. Store the vertices in a list and we also need to know the next and prev vertex
            List<Vertex> vertices = new List<Vertex>();

            for (int i = 0; i < points.Count; i++)
                vertices.Add(new Vertex(points[i]));

            // Find the next and previous vertex
            for (int i = 0; i < vertices.Count; i++)
            {
                int nextPos = ClampListIndex(i + 1, vertices.Count);
                int prevPos = ClampListIndex(i - 1, vertices.Count);

                vertices[i].PrevVertex = vertices[prevPos];
                vertices[i].NextVertex = vertices[nextPos];
            }

            // Step 2. Find the reflex (concave) and convex vertices, and ear vertices
            for (int i = 0; i < vertices.Count; i++)
                CheckIfReflexOrConvex(vertices[i]);

            // Have to find the ears after we have found if the vertex is reflex or convex
            List<Vertex> earVertices = new List<Vertex>();
            
            for (int i = 0; i < vertices.Count; i++)
                IsVertexEar(vertices[i], vertices, earVertices);

            // Step 3. Triangulate!
            while (true)
            {
                // This means we have just one triangle left
                if (vertices.Count == 3)
                {
                    // The final triangle
                    triangles.Add(new Triangle(vertices[0], vertices[0].PrevVertex, vertices[0].NextVertex));
                    break;
                }

                // Make a triangle of the first ear
                Vertex earVertex = earVertices[0];

                Vertex earVertexPrev = earVertex.PrevVertex;
                Vertex earVertexNext = earVertex.NextVertex;

                Triangle newTriangle = new Triangle(earVertex, earVertexPrev, earVertexNext);

                triangles.Add(newTriangle);

                // Remove the vertex from the lists
                earVertices.Remove(earVertex);

                vertices.Remove(earVertex);

                // Update the previous vertex and next vertex
                earVertexPrev.NextVertex = earVertexNext;
                earVertexNext.PrevVertex = earVertexPrev;

                //...see if we have found a new ear by investigating the two vertices that was part of the ear
                CheckIfReflexOrConvex(earVertexPrev);
                CheckIfReflexOrConvex(earVertexNext);

                earVertices.Remove(earVertexPrev);
                earVertices.Remove(earVertexNext);

                IsVertexEar(earVertexPrev, vertices, earVertices);
                IsVertexEar(earVertexNext, vertices, earVertices);
            }

            return triangles;
        }

        public static int ClampListIndex(int index, int listSize)
        {
            index = ((index % listSize) + listSize) % listSize;
            return index;
        }

        public static bool IsTriangleOrientedClockwise(Vector2 p1, Vector2 p2, Vector2 p3)
        {
            bool isClockWise = true;
            float determinant = p1.x * p2.y + p3.x * p1.y + p2.x * p3.y - p1.x * p3.y - p3.x * p2.y - p2.x * p1.y;

            if (determinant > 0f)
                isClockWise = false;

            return isClockWise;
        }

        public static bool IsPointInTriangle(Vector2 p1, Vector2 p2, Vector2 p3, Vector2 p)
        {
            bool isWithinTriangle = false;

            //Based on Barycentric coordinates
            float denominator = ((p2.y - p3.y) * (p1.x - p3.x) + (p3.x - p2.x) * (p1.y - p3.y));

            float a = ((p2.y - p3.y) * (p.x - p3.x) + (p3.x - p2.x) * (p.y - p3.y)) / denominator;
            float b = ((p3.y - p1.y) * (p.x - p3.x) + (p1.x - p3.x) * (p.y - p3.y)) / denominator;
            float c = 1 - a - b;

            //The point is within the triangle or on the border if 0 <= a <= 1 and 0 <= b <= 1 and 0 <= c <= 1
            //if (a >= 0f && a <= 1f && b >= 0f && b <= 1f && c >= 0f && c <= 1f)
            //{
            //    isWithinTriangle = true;
            //}

            //The point is within the triangle
            if (a > 0f && a < 1f && b > 0f && b < 1f && c > 0f && c < 1f)
                isWithinTriangle = true;

            return isWithinTriangle;
        }

        // Check if a vertex if reflex or convex, and add to appropriate list
        private static void CheckIfReflexOrConvex(Vertex v)
        {
            v.IsReflex = false;
            v.IsConvex = false;

            // This is a reflex vertex if its triangle is oriented clockwise
            Vector2 a = v.PrevVertex.GetPos2D_XZ();
            Vector2 b = v.GetPos2D_XZ();
            Vector2 c = v.NextVertex.GetPos2D_XZ();

            if (IsTriangleOrientedClockwise(a, b, c))
                v.IsReflex = true;
            else
                v.IsConvex = true;
        }

        // Check if a vertex is an ear
        private static void IsVertexEar(Vertex v, List<Vertex> vertices, List<Vertex> earVertices)
        {
            // A reflex vertex cant be an ear!
            if (v.IsReflex)
                return;

            // This triangle to check point in triangle
            Vector2 a = v.PrevVertex.GetPos2D_XZ();
            Vector2 b = v.GetPos2D_XZ();
            Vector2 c = v.NextVertex.GetPos2D_XZ();

            bool hasPointInside = false;

            for (int i = 0; i < vertices.Count; i++)
            {
                // We only need to check if a reflex vertex is inside of the triangle
                if (vertices[i].IsReflex)
                {
                    Vector2 p = vertices[i].GetPos2D_XZ();

                    // This means inside and not on the hull
                    if (IsPointInTriangle(a, b, c, p))
                    {
                        hasPointInside = true;
                        break;
                    }
                }
            }

            if (!hasPointInside)
                earVertices.Add(v);
        }

        public class Vertex
        {
            public Vector3 Position;

            // The outgoing halfedge (a halfedge that starts at this vertex)
            // Doesnt matter which edge we connect to it
            public HalfEdge HalfEdge;

            // Which triangle is this vertex a part of?
            public Triangle Triangle;

            // The previous and next vertex this vertex is attached to
            public Vertex PrevVertex;
            public Vertex NextVertex;

            // Properties this vertex may have
            // Reflex is concave
            public bool IsReflex; 
            public bool IsConvex;
            public bool IsEar;

            public Vertex(Vector3 position)
            {
                this.Position = position;
            }

            // Get 2d pos of this vertex
            public Vector2 GetPos2D_XZ()
            {
                Vector2 pos_2d_xz = new Vector2(Position.x, Position.z);
                return pos_2d_xz;
            }
        }

        public class HalfEdge
        {
            // The vertex the edge points to
            public Vertex Vertex;

            // The face this edge is a part of
            public Triangle Triangle;

            // The next edge
            public HalfEdge NextEdge;
            
            // The previous
            public HalfEdge PrevEdge;
            
            // The edge going in the opposite direction
            public HalfEdge OppositeEdge;

            // This structure assumes we have a vertex class with a reference to a half edge going from that vertex
            // and a face (triangle) class with a reference to a half edge which is a part of this face 
            public HalfEdge(Vertex vertex)
            {
                this.Vertex = vertex;
            }
        }

        public class Triangle
        {
            // Corners
            public Vertex Vertex1;
            public Vertex Vertex2;
            public Vertex Vertex3;

            // If we are using the half edge mesh structure, we just need one half edge
            public HalfEdge HalfEdge;

            public Triangle(Vertex v1, Vertex v2, Vertex v3)
            {
                Vertex1 = v1;
                Vertex2 = v2;
                Vertex3 = v3;
            }

            public Triangle(Vector3 v1, Vector3 v2, Vector3 v3)
            {
                Vertex1 = new Vertex(v1);
                Vertex2 = new Vertex(v2);
                Vertex3 = new Vertex(v3);
            }

            public Triangle(HalfEdge halfEdge)
            {
                this.HalfEdge = halfEdge;
            }

            // Change orientation of triangle from cw -> ccw or ccw -> cw
            public void ChangeOrientation()
            {
                Vertex temp = Vertex1;
                Vertex1 = Vertex2;
                Vertex2 = temp;
            }
        }

        public class Edge
        {
            public Vertex Vertex1;
            public Vertex Vertex2;

            // Is this edge intersecting with another edge?
            public bool IsIntersecting = false;

            public Edge(Vertex v1, Vertex v2)
            {
                Vertex1 = v1;
                Vertex2 = v2;
            }

            public Edge(Vector3 v1, Vector3 v2)
            {
                Vertex1 = new Vertex(v1);
                Vertex2 = new Vertex(v2);
            }

            // Get vertex in 2d space (assuming x, z)
            public Vector2 GetVertex2D(Vertex v)
            {
                return new Vector2(v.Position.x, v.Position.z);
            }

            // Flip edge
            public void FlipEdge()
            {
                Vertex temp = Vertex1;
                Vertex1 = Vertex2;
                Vertex2 = temp;
            }
        }

        public class Plane
        { 
            public Vector3 Position;

            public Vector3 Normal;

            public Plane(Vector3 position, Vector3 normal)
            {
                Position = position;
                Normal = normal;
            }
        }
    }
}