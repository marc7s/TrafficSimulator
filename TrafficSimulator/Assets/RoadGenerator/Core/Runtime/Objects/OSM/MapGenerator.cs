using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Xml;
using System;
using System.Linq;

namespace RoadGenerator
{
    public class Way
    {
        public string ID;
        public string Name;
        public List<Vector3> Points;

        public Way(XmlNode node, List<Vector3> points)
        {
            XmlAttribute IDAttribute = node.Attributes["id"];

            if (IDAttribute != null)
                ID = IDAttribute.Value;

            Points = points;
        }
    }

    public struct BuildingPoints
    {
        public Vector3 BottomPoint;
        public Vector3 TopPoint;
        public BuildingPoints(Vector3 bottomPoint, Vector3 topPoint)
        {
            BottomPoint = bottomPoint;
            TopPoint = topPoint;
        }
    }

    public class BuildingWay : Way
    {
        public float? Height;
        public int? BuildingLevels;
        public bool IsMultiPolygon;
        public string StreetName;
        public string StreetAddress;

        public BuildingWay(XmlNode node, List<Vector3> points) : base(node, points)
        {
            IEnumerator ienum = node.GetEnumerator();

            while (ienum.MoveNext())
            {
                XmlNode currentNode = (XmlNode) ienum.Current;
                if (currentNode.Name != "tag") 
                    continue;

                try
                {
                    switch (currentNode.Attributes["k"].Value)
                    {
                        case "height":
                            Height = float.Parse(currentNode.Attributes["v"].Value.Replace(".", ","));
                            break;
                        case "building:levels":
                            BuildingLevels = int.Parse(currentNode.Attributes["v"].Value);
                            break;
                        case "addr:street":
                            StreetName = currentNode.Attributes["v"].Value;
                            break;
                        case "addr:housenumber":
                            StreetAddress = currentNode.Attributes["v"].Value;
                            break;
                        case "type":
                            if (currentNode.Attributes["v"].Value == "multipolygon")
                                IsMultiPolygon = true;
                            break;
                    }
                }
                catch
                {
                    continue;
                }
            }
        }
    }
    
    public class TerrainWay : Way
    {
        public TerrainType TerrainType;
        public TerrainArea TerrainArea;

        public TerrainWay(XmlNode node, List<Vector3> outerPoints, List<List<Vector3>> innerAreas = null, TerrainType? terrainType = null) : base(node, outerPoints)
        {
            TerrainType = terrainType ?? MapGenerator.GetTerrainType(node);
            TerrainArea = new TerrainArea(TerrainType, outerPoints, innerAreas);
        }
    }

    public struct TerrainArea
    {
        public TerrainType TerrainType;
        public List<Vector3> OuterArea;
        public List<List<Vector3>> InnerAreas;
        public TerrainArea(TerrainType terrainType, List<Vector3> outerArea, List<List<Vector3>> innerAreas = null)
        {
            TerrainType = terrainType;
            OuterArea = outerArea;
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
        private List<TerrainWay> _terrains = new List<TerrainWay>();
        XmlDocument _doc = new XmlDocument();

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

            // Loading the OSM file
            LoadOSMMap();
            // Parsing the Bounds of the map
            AssignMapBounds();
            // Mapping the nodes to dictionaries
            MapNodes();
            // Generating the OSM ways
            GenerateWays();
            // Generating the OSM relations
            GenerateRelations();
            // Generating the intersections
            GenerateIntersections();

            // Adding bus stops
            if (roadSystem.ShouldGenerateBusStops)
                AddBusStops();

            // Generating the terrain
            if (roadSystem.ShouldGenerateTerrain)
            {
                GenerateTerrain();
                AddTrees();
            }

            roadSystem.IsGeneratingOSM = false;

            foreach (Road road in roadSystem.DefaultRoads)
                road.OnChange();
        }

        /// <summary> Generates the OSM ways in the road system. Generates roads, buildings and terrain </summary>
        private void GenerateWays()
        {
            XmlNodeList ways = _doc.GetElementsByTagName("way");
            foreach(XmlNode node in ways)
            {
                Way way = CreateWay(node);

                if (way == null)
                    continue;

                if (way is TerrainWay)
                    _terrains.Add((TerrainWay)way);
                else if (way is RoadWay) 
                    GenerateRoad((RoadWay)way);
                else if (way is BuildingWay)
                    GenerateBuilding((BuildingWay)way);
            }
        }

        /// <summary> Maps the OSM nodes to dictionaries </summary>
        private void MapNodes()
        {
            // Adding all the nodes to a dictionary and mapping bus stops, traffic lights and trees
            foreach(XmlNode node in _doc.DocumentElement.ChildNodes)
            {
                switch(node.Name)
                {
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
        }

        /// <summary> Generates the relational OSM objects </summary>
        private void GenerateRelations()
        {
            XmlNodeList relations = _doc.GetElementsByTagName("relation");
            foreach(XmlNode node in relations)
            {     
                Way way = CreateWay(node);
                IEnumerator ienum = node.GetEnumerator();

                // When the building is multipolygon
                if (way is BuildingWay) 
                {
                    BuildingWay buildingWay = (BuildingWay)way;

                    if (!buildingWay.IsMultiPolygon)
                        return;
                        
                    while (ienum.MoveNext())
                    {
                        XmlNode currentNode = (XmlNode) ienum.Current; 
                        if (currentNode.Name == "member" && currentNode.Attributes["type"].Value == "way")
                        {
                            if (_wayDict.ContainsKey(currentNode.Attributes["ref"].Value))
                            {
                                XmlNode wayNode = _wayDict[currentNode.Attributes["ref"].Value];
                                GenerateBuilding(new BuildingWay(wayNode, GetWayNodePositions(wayNode.GetEnumerator())));
                            }
                        }
                    }
                }
                if (way is TerrainWay)
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
                    TerrainWay terrainWay = (TerrainWay)way;
                    _terrains.Add(new TerrainWay(node, outerPoints, innerPoints, terrainWay.TerrainType));
                }
                
            }
        }
        private void AssignMapBounds()
        {
            XmlNodeList bounds = _doc.GetElementsByTagName("bounds");
            XmlNode boundsNode = bounds[0];
            _minLat = float.Parse(boundsNode.Attributes["minlat"].Value.Replace(".", ","));
            _minLon = float.Parse(boundsNode.Attributes["minlon"].Value.Replace(".", ","));
            _maxLat = float.Parse(boundsNode.Attributes["maxlat"].Value.Replace(".", ","));
            _maxLon = float.Parse(boundsNode.Attributes["maxlon"].Value.Replace(".", ","));
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

                    foreach (TerrainWay terrainWay in _terrains)
                    {
                        if (terrainWay.TerrainType == TerrainType.Water && IsPointInPolygon(terrainPosition, Vector3ToVector2(terrainWay.TerrainArea.OuterArea).ToArray()))
                        {
                            if (terrainWay.TerrainArea.InnerAreas != null && terrainWay.TerrainArea.InnerAreas.Count > 0)
                            {
                                List<Vector3> innerArea = new List<Vector3>();

                                foreach (List<Vector3> innerArea2 in terrainWay.TerrainArea.InnerAreas)
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
                    foreach (TerrainWay terrainWay in _terrains)
                    {
                        // The terrain point is inside the terrain type area
                        if (IsPointInPolygon(terrainPosition, Vector3ToVector2(terrainWay.TerrainArea.OuterArea).ToArray()))
                        {
                            foreach (List<Vector3> innerArea in terrainWay.TerrainArea.InnerAreas)
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

                            if (terrainWay.TerrainType == TerrainType.Grass)
                                splatWeights[(int)TerrainType.Grass] = 1f;
                            else if (terrainWay.TerrainType == TerrainType.Forest)
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

        /// <summary> Generates intersections where the OSM roads intersect </summary>
        private void GenerateIntersections()
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

        private Way CreateWay(XmlNode node)
        {
            XmlNode typeNode;
            WayType? wayType = GetWayType(node, out typeNode);

            if (wayType == null)
                return null;

            if (wayType == WayType.Building && !_roadSystem.ShouldGenerateBuildings)
                return null;
            
            if (wayType == WayType.Terrain && !_roadSystem.ShouldGenerateTerrain)
                return null;

            if (wayType == WayType.Road && !_roadSystem.ShouldGenerateRoads)
                return null;
            
            List<Vector3> points = GetWayNodePositions(node.GetEnumerator());

            switch(wayType)
            {
                case WayType.Road:
                    return new RoadWay(node, points, typeNode);
                case WayType.Building:
                    return new BuildingWay(node, points);
                case WayType.Terrain:
                    return new TerrainWay(typeNode, points);
                case WayType.Water:
                    return new TerrainWay(typeNode, points, null, TerrainType.Water);
                default:
                    return null;
            }
        }

        /// <summary> Returns the way type of a way. Returns null for unsupported ways </summary>
        public WayType? GetWayType(XmlNode node, out XmlNode typeNode)
        {
            IEnumerator ienum = node.GetEnumerator();

            // Search for type of way
            while (ienum.MoveNext())
            {
                XmlNode currentNode = (XmlNode) ienum.Current;
                typeNode = currentNode;

                if (currentNode.Name != "tag") 
                    continue;

                try
                {
                    switch (currentNode.Attributes["k"].Value)
                    {
                        case "highway":
                            return WayType.Road;
                        case "building":
                            return WayType.Building;
                        case "landuse":
                            return WayType.Terrain;
                        case "natural":
                            if (currentNode.Attributes["v"].Value == "water")
                                return WayType.Water;
                            break;
                        case "waterway":
                        case "water":
                            return WayType.Water;

                    }
                }
                catch
                {
                    typeNode = null;
                    return null;
                }
            }
            typeNode = null;
            return null;
        }
        

        // https://wiki.openstreetmap.org/wiki/Key:landuse
        public static TerrainType GetTerrainType(XmlNode node)
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

        private void LoadOSMMap()
        {
            _doc.Load("Assets/OsmMaps/Masthugget.osm");
        }

        private void GenerateFootWay(List <Vector3> points)
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
        void GenerateRoad(RoadWay roadWay) 
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

            if (roadWay.ParkingType2 != null && roadWay.ParkingType2 != ParkingType.None)
            {
                if (roadWay.ParkingType2 == ParkingType.Left || roadWay.ParkingType2 == ParkingType.Both)
                    road.AddFullRoadSideParking(LaneSide.Secondary);
                
                if (roadWay.ParkingType2 == ParkingType.Right || roadWay.ParkingType2 == ParkingType.Both)
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

        public List<Vector3> GetWayNodePositions(IEnumerator ienum)
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

        /// <summary> Calculates the distance between two lat lon points using the Haversine formula </summary>
        private static double Haversine(double lat1, double lat2, double lon1, double lon2)
        {
            const float degToRad = Mathf.PI / 180;
            lat1 *= degToRad;
            lat2 *= degToRad;
            lon1 *= degToRad;
            lon2 *= degToRad;

            const int earthRadius = 6378100;
                    
            double sdlat = Math.Sin((lat2 - lat1) / 2);
            double sdlon = Math.Sin((lon2 - lon1) / 2);
            double q = sdlat * sdlat + Math.Cos(lat1) * Math.Cos(lat2) * sdlon * sdlon;
            double d = 2 * earthRadius * Math.Asin(Math.Sqrt(q));

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

        void GenerateBuilding(BuildingWay buildingWay)
        {
            float defaultBuildingHeight = 25;
            float height = buildingWay.Height ?? defaultBuildingHeight;

            if (buildingWay.Height == null && buildingWay.BuildingLevels != null)
                height = buildingWay.BuildingLevels.Value * 3.5f;

            System.Random random = new System.Random();
            float randomEpsilon = (float)random.NextDouble() * 0.2f;
            height += randomEpsilon;

            List<Vector3> buildingPointsBottom = buildingWay.Points;
            List<BuildingPoints> buildingPoints = new List<BuildingPoints>();
            List<Vector3> buildingPointsTop = new List<Vector3>();

            foreach (Vector3 point in buildingPointsBottom)
                buildingPoints.Add(new BuildingPoints(point, new Vector3(point.x, height, point.z)));

            foreach (BuildingPoints point in buildingPoints)
                buildingPointsTop.Add(point.TopPoint);

            GameObject house = Instantiate(BuildingPrefab, buildingPointsBottom[0], Quaternion.identity);

            house.name = GetBuildingName(buildingWay);

            house.transform.parent = _roadSystem.BuildingContainer.transform;

            BuildingMeshCreator.GenerateBuildingMesh(house, buildingPointsBottom, buildingPointsTop, buildingPoints, BuildingWallMaterial, BuildingRoofMaterial);
        }

        private string GetBuildingName(BuildingWay buildingWay)
        {
            string defaultBuildingName = "Building";

            if (buildingWay.Name != null)
                return buildingWay.Name;
 
            if(buildingWay.StreetName == null || buildingWay.StreetAddress == null)
                return defaultBuildingName;

            return buildingWay.StreetName + " " + buildingWay.StreetAddress;
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

        Road SpawnRoad(List<Vector3> points, RoadWay roadWay)
        {
            GameObject prefab = RoadPrefab;

            // Instantiate a new road prefab
            GameObject roadObj = Instantiate(prefab, points[0], Quaternion.identity);

            // Set the name of the road
            roadObj.name = roadWay.Name;

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
    }
}