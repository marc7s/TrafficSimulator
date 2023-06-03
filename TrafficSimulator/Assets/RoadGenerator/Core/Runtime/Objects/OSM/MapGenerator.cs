using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Xml;
using System;

namespace RoadGenerator
{
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
        private Dictionary<Vector3, bool> _trafficLightAtJunction = new Dictionary<Vector3, bool>();
        private List<XmlNode> _busStops = new List<XmlNode>();
        private List<XmlNode> _trees = new List<XmlNode>();
        private float _minLat;
        private float _minLon;
        private float _maxLat;
        private float _maxLon;
        private Terrain _terrain;
        private List<TerrainWay> _terrains = new List<TerrainWay>();
        private XmlDocument _doc = new XmlDocument();
        private List<RoadWay> _roadWays = new List<RoadWay>();
        private List<BuildingWay> _buildingWays = new List<BuildingWay>();
        public void GenerateMap(RoadSystem roadSystem)
        {
            Setup(roadSystem);

            RoadWayGenerator roadWayGenerator = new RoadWayGenerator(roadSystem, _trafficLightAtJunction);
            BuildingGenerator buildingGenerator = new BuildingGenerator();
            TerrainGenerator terrainGenerator = new TerrainGenerator();

            // Loading the OSM file
            LoadOSMMap();
            // Parsing the Bounds of the map
            AssignMapBounds();
            // Mapping the nodes to dictionaries
            ParseNodes();
            // Generating the OSM ways
            ParseWays();
            // Parsing the OSM relations
            ParseRelations();

            // Generating the roads
            roadWayGenerator.GenerateRoads(_roadWays);

            // Generating the intersections
            roadWayGenerator.GenerateIntersections();

            buildingGenerator.GenerateBuildings(_buildingWays, BuildingPrefab, BuildingWallMaterial, BuildingRoofMaterial, roadSystem.BuildingContainer.transform);

            // Adding bus stops
            roadWayGenerator.AddBusStops(_busStops, this);

            // Generating the terrain
            if (roadSystem.ShouldGenerateTerrain)
            {
                Vector3 terrainSize = LatLonToPosition(_maxLat, _maxLon);
                terrainGenerator.GenerateTerrain(_terrain, _terrains, terrainSize);
                AddTrees();
            }

            roadSystem.IsGeneratingOSM = false;

            foreach (Road road in roadSystem.DefaultRoads)
                road.OnChange();
        }

        private void Setup(RoadSystem roadSystem)
        {
            roadSystem.UseOSM = true;
            roadSystem.IsGeneratingOSM = true;
            _terrain = roadSystem.Terrain;
            _nodesDict.Clear();
            _busStops.Clear();
            _trees.Clear();
            _roadWays.Clear();
            _buildingWays.Clear();
            _minLat = 0;
            _minLon = 0;
            _maxLat = 0;
            _maxLon = 0;
            _roadSystem = roadSystem;
        }

        /// <summary> Parses the OSM ways </summary>
        private void ParseWays()
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
                    _roadWays.Add((RoadWay)way);
                else if (way is BuildingWay)
                    _buildingWays.Add((BuildingWay)way);
            }
        }

        /// <summary> Maps the OSM nodes to dictionaries </summary>
        private void ParseNodes()
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
                                if (_roadSystem.ShouldGenerateBusStops && childNode.Attributes["v"].Value == "bus_stop")
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
        // https://wiki.openstreetmap.org/wiki/Relation
        private void ParseRelations()
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
                                _buildingWays.Add(new BuildingWay(wayNode, GetWayNodePositions(wayNode.GetEnumerator())));
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

        private void LoadOSMMap()
        {
            _doc.Load("Assets/OsmMaps/Masthugget.osm");
        }

        private void GenerateFootWay(List <Vector3> points)
        {
            FootWayMeshGenerator footWayMeshGenerator = new FootWayMeshGenerator();
            Mesh footWay = footWayMeshGenerator.GenerateMesh(points, 4, 0.1f);
            GameObject footWayObject = new GameObject();
            footWayObject.transform.parent = _roadSystem.RoadContainer.transform;
            MeshFilter meshFilter = footWayObject.AddComponent<MeshFilter>();
            meshFilter.mesh = footWay;
            MeshRenderer meshRenderer = footWayObject.AddComponent<MeshRenderer>();;
            footWayObject.name = "FootWay";
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

        public Vector3 GetNodePosition(XmlNode node)
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
    }
}