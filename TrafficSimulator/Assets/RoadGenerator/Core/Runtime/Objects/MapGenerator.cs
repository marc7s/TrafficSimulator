using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Xml;
using System.Linq;
using System;

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
        Unclassified
    }

    public struct BuildingData
    {
        public bool? IsMultiPolygon;
        public string? StreetName;
        public string? StreetAddress;
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
        public string? Name;
        public SideWalkType? SideWalkType;
        public ParkingType? ParkingType;
        public ServiceType? ServiceType;
        public BuildingData BuildingData;
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
        private double _minLat = 0;
        private double _minLon = 0;
        
        public void GenerateMap(RoadSystem roadSystem)
        {
            _nodesDict.Clear();
            _roadsAtNode.Clear();
            _busStops.Clear();
            _minLat = 0;
            _minLon = 0;

            _roadSystem = roadSystem;

            XmlDocument doc = new XmlDocument();
            LoadOSMMap(doc);
        
            // Finding the bounds of the map and adding all nodes to a dictionary
            foreach(XmlNode node in doc.DocumentElement.ChildNodes)
            {
                switch(node.Name)
                {
                    case "bounds":
                        _minLat = double.Parse(node.Attributes["minlat"].Value.Replace(".", ","));
                        _minLon = double.Parse(node.Attributes["minlon"].Value.Replace(".", ","));
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

            int count = 0;
            foreach(XmlNode node in doc.DocumentElement.ChildNodes){
                if (node.Name == "way") 
                {
                    WayData? wayData = GetWayData(node);
                    IEnumerator ienum = node.GetEnumerator();
                    if (wayData == null)
                        continue;
                    if (wayData?.WayType != WayType.Building) {
                        GenerateRoad(ienum, wayData.Value);
                        count++;
                    }
                    if (wayData?.WayType == WayType.Building && roadSystem.ShouldGenerateBuildings) {
                        GenerateBuilding(ienum, wayData.Value);
                    }
                }

                if (node.Name == "relation")
                {
                    WayData? wayData = GetWayData(node);
                    IEnumerator ienum = node.GetEnumerator();

                    // When the building is multipolygon
                    if (wayData?.BuildingData.IsMultiPolygon == true && wayData?.WayType == WayType.Building && roadSystem.ShouldGenerateBuildings) 
                    {
                        while (ienum.MoveNext())
                        {
                            XmlNode currentNode = (XmlNode) ienum.Current; 
                            if (currentNode.Name == "member")
                            {
                                if (currentNode.Attributes["type"].Value == "way")
                                {
                                    if (_wayDict.ContainsKey(currentNode.Attributes["ref"].Value))
                                    {
                                        XmlNode wayNode = _wayDict[currentNode.Attributes["ref"].Value];
                                        GenerateBuilding(wayNode.GetEnumerator(), wayData.Value);
                                    }
                                }
                            }
                        }
                    }
                }
            }

            AddRoads();

            if (roadSystem.ShouldGenerateBusStops)
                AddBusStops();

            if (roadSystem.ShouldGenerateTrees)
                AddTrees();

        foreach (Road road in roadSystem.DefaultRoads)
                road.IsGeneratingOSM = false;
        }

        private void AddRoads()
        {
            foreach (var roads in _roadsAtNode) {
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

                        if (pathCreator1.path.length < 15 || pathCreator2.path.length < 15)
                        {
                            continue;
                        }

                        bool isNodeAtEndPointRoad1 = pathCreator1.path.GetPoint(pathCreator1.path.NumPoints - 1) == roads.Key || pathCreator1.path.GetPoint(0) == roads.Key;
                        bool isNodeAtEndPointRoad2 = pathCreator2.path.GetPoint(pathCreator2.path.NumPoints - 1) == roads.Key || pathCreator2.path.GetPoint(0) == roads.Key;
                        
                        // If it is an intersection
                        if (!(isNodeAtEndPointRoad1 && isNodeAtEndPointRoad2))
                        {
                            position = position + new Vector3(0, 0, 0.01f);
                            Intersection intersection = IntersectionCreator.CreateIntersectionAtPosition(position, road1, road2);
                            if (intersection != null)
                            {
                                if (_trafficLightAtJunction.ContainsKey(position) && _trafficLightAtJunction[position])
                                    intersection.FlowType = FlowType.TrafficLights;
                                else
                                    intersection.FlowType = FlowType.YieldSigns;
                            }
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


                        // Temporary hack, TODO fix
                        position = position + new Vector3(0, 0, 0.0001f);

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

                        // Temporary hack, TODO fix
                        position = position + new Vector3(0, 0, 0.0001f);

                        IntersectionCreator.CreateIntersectionAtPositionMultipleRoads(position, new List<Road> { road1, road2, road3, road4 });
                    }
                }
            }

            foreach (var roads in _roadsAtNode) 
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
                    {
                        road1.ConnectRoadIfEndPointsAreClose();
                    }
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

                //GameObject busStopObject = Instantiate(busStopPrefab, position, Quaternion.identity);
                //busStopObject.transform.parent = roadSystem.transform;
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
            // search for type of way
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
                            case "name":
                                wayData.Name = currentNode.Attributes["v"].Value;
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
            return wayData;
        }

        //https://wiki.openstreetmap.org/wiki/Map_features#Highway
        // TODO add support for more road types
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
                default:
                    return null;
            }
        }
        private void LoadOSMMap(XmlDocument document)
        {
            document.Load("Assets/MastHugget.osm");
        }

        private bool IsTagKeyName(XmlNode node, string value)
        {
            return node.Name == "tag" && node.Attributes["k"].Value == value;
        }

        double LatitudeToY (double latitude)
        {
            return System.Math.Log(System.Math.Tan(
                (latitude + 90) / 360 * System.Math.PI
            )) / System.Math.PI * 180;
        }

        Vector3 LatLonVector3ToVector3 (Vector3 latLonVector3)
        {
            return new Vector3(
                latLonVector3.x,
                0,
                latLonVector3.z
            );
        }

        private void GenerateFootWay(List <Vector3> points, WayData wayData)
        {
            return;
            Debug.Log("Generating footway");
            FootWayMeshGenerator footWayMeshGenerator = new FootWayMeshGenerator();
            Mesh footWay = footWayMeshGenerator.GenerateMesh(points, 4, 0.1f);
            GameObject footWayObject = new GameObject();
            footWayObject.transform.parent = _roadSystem.RoadContainer.transform;
            MeshFilter meshFilter = footWayObject.AddComponent<MeshFilter>();
            meshFilter.mesh = footWay;
            MeshRenderer meshRenderer = footWayObject.AddComponent<MeshRenderer>();
            //meshRenderer.material = roadSystem.DefaultRoadMaterial;
            footWayObject.name = "FootWay";
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

            if (wayData.Name == null && wayData.WayType != WayType.RailTram)
                return;

            if (wayData.WayType == WayType.RailTram)
                return;

            // TotatlLenght of road
            float totalLength = 0;
            for (int i = 0; i < roadPoints.Count - 1; i++)
            {
                totalLength += Vector3.Distance(roadPoints[i], roadPoints[i + 1]);
            }
            if (totalLength < 10)
                return;

            // Currently get error when roads have same start and end point, TODO fix
            if (roadPoints[0] == roadPoints[roadPoints.Count - 1])
            {
                return;
            }
            List<Vector3> roadPoints2 = new List<Vector3>();
            roadPoints2.Add(roadPoints[0]);
            roadPoints2.Add(roadPoints[1]);
            Road road = spawnRoad(roadPoints2, wayData);
            //road.IsGeneratingOSM = true;

            road.RoadType = wayData.WayType;

            if (wayData.IsOneWay == true || wayData.WayType == WayType.RailTram)
            {
                road.IsOneWay = true;
                road.LaneWidth /= 2;
            }

            if (wayData.ParkingType != null && wayData.ParkingType != ParkingType.None)
            {
                Debug.Log("Adding parking" + road);
                if (wayData.ParkingType == ParkingType.Left || wayData.ParkingType == ParkingType.Both)
                {
                    road.AddLaneParkingPOI(LaneSide.Secondary);
                }
                if (wayData.ParkingType == ParkingType.Right || wayData.ParkingType == ParkingType.Both)
                {
                    road.AddLaneParkingPOI(LaneSide.Primary);
                }
            }

            if (wayData.MaxSpeed != null)
                road.SpeedLimit = (SpeedLimit)wayData.MaxSpeed;

            // When the speedlimit is not known in residential areas, set it to 30km/h
            if (wayData.WayType == WayType.Residential && wayData.MaxSpeed == null)
                road.SpeedLimit = SpeedLimit.ThirtyKPH;

            if (wayData.IsLit != null)
                road.ShouldSpawnLampPoles = wayData.IsLit.Value;
            else
                road.ShouldSpawnLampPoles = false;

            if (wayData.WayType == WayType.Residential)
            {
        //     road.LaneWidth = 4f;
            }

            PathCreator pathCreator = road.GetComponent<PathCreator>();
            // Roads with only two points will not render properly, this is a hack to render them
            // TODO update the roads correctly
            if(roadPoints.Count == 2) {
                // Move point to the same place to rerender the road
                pathCreator.bezierPath.MovePoint(0, roadPoints[0]);
            }

            for (int i = 2; i < roadPoints.Count; i++) {
                pathCreator.bezierPath.AddSegmentToEnd(roadPoints[i]);
            }
            
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
            const int scale = 111000;
            float xPos = (float)(double.Parse(node.Attributes["lon"].Value.Replace(".", ",")) - _minLon)*scale;
            float zPos = (float)(double.Parse(node.Attributes["lat"].Value.Replace(".", ",")) - _minLat)*scale;
            return new Vector3(xPos, 0, zPos);
        }

        void GenerateBuilding(IEnumerator ienum, WayData wayData)
        {
            float defaultBuildingHeight = 25;
            float height = wayData.BuildingData.Height ?? defaultBuildingHeight;
            if (wayData.BuildingData.Height == null && wayData.BuildingData.BuildingLevels != null)
            {
                height = wayData.BuildingData.BuildingLevels.Value * 3.5f;
            //  Debug.Log("Building height: " + height);
            }
                

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
            catch (Exception e){}

            buildingPointsTop.Reverse();

            try
            {
                triangles.AddRange(TriangulateConcavePolygon(buildingPointsTop));
            }
            catch (Exception e){}

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
            Dictionary<Vector3, int> positionToIndex = new Dictionary<Vector3, int>();
            List<Vector3> verts = new List<Vector3>();
            List<int> wallTris = new List<int>();
            List<int> roofTris = new List<int>();

            foreach (BuildingPoints buildingPoint in buildingPoints)
            {
                verts.Add(buildingPoint.BottomPoint);
                if (!positionToIndex.ContainsKey(buildingPoint.BottomPoint))
                    positionToIndex.Add(buildingPoint.BottomPoint, verts.Count - 1);
                verts.Add(buildingPoint.TopPoint);
                if (!positionToIndex.ContainsKey(buildingPoint.TopPoint))
                    positionToIndex.Add(buildingPoint.TopPoint, verts.Count - 1);
            }

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
                AddBuildingWall(index, index -1, index - 2, index - 3, wallTris);
            }

            foreach (Triangle triangle in triangles)
            {
                if (positionToIndex.ContainsKey(triangle.v1.position) && positionToIndex.ContainsKey(triangle.v2.position) && positionToIndex.ContainsKey(triangle.v3.position))
                {
                    roofTris.Add(positionToIndex[triangle.v1.position]);
                    roofTris.Add(positionToIndex[triangle.v2.position]);
                    roofTris.Add(positionToIndex[triangle.v3.position]);
                }
            }

            buildingMesh.Clear();
            buildingMesh.vertices = verts.ToArray();
            buildingMesh.subMeshCount = 2;
            buildingMesh.SetTriangles(wallTris.ToArray(), 0);
            buildingMesh.SetTriangles(roofTris.ToArray(), 1);
            buildingMesh.RecalculateBounds();

        }

        private void AddBuildingWall(int currentSideTopIndex, int currentSideBottomIndex, int prevSideTopIndex, int prevSideBottomIndex, List<int> triangles)
        {
            triangles.Add(prevSideTopIndex);
            triangles.Add(currentSideTopIndex);
            triangles.Add(prevSideBottomIndex);

            // Adding double sided triangles until the roof is done
            // Remove this when roof is done
            triangles.Add(prevSideBottomIndex);
            triangles.Add(currentSideTopIndex);
            triangles.Add(prevSideTopIndex);

            triangles.Add(currentSideBottomIndex);
            triangles.Add(prevSideBottomIndex);
            triangles.Add(currentSideTopIndex);

            // Adding double sided triangles until the roof is done
            // Remove this when roof is done
            triangles.Add(currentSideTopIndex);
            triangles.Add(prevSideBottomIndex);
            triangles.Add(currentSideBottomIndex);
        }

        Mesh AssignMeshComponents(GameObject buildingObject)
        {
            buildingObject.transform.rotation = Quaternion.identity;
            buildingObject.transform.position = Vector3.zero;
            buildingObject.transform.localScale = Vector3.one;

            // Ensure mesh renderer and filter components are assigned
            if (!buildingObject.gameObject.GetComponent<MeshFilter>()) 
            {
                buildingObject.gameObject.AddComponent<MeshFilter>();
            }
            if (!buildingObject.GetComponent<MeshRenderer>()) 
            {
                buildingObject.gameObject.AddComponent<MeshRenderer>();
            }

            MeshRenderer _meshRenderer = buildingObject.GetComponent<MeshRenderer>();
            _meshRenderer.sharedMaterials = new Material[]{ BuildingWallMaterial, BuildingRoofMaterial };
            MeshFilter _meshFilter = buildingObject.GetComponent<MeshFilter>();
            

            Mesh mesh = new Mesh();
        
            _meshFilter.sharedMesh = mesh;
            return mesh;
        }

        Road spawnRoad(List<Vector3> points, WayData wayData)
        {
                GameObject prefab = wayData.WayType == WayType.RailTram ? RailPrefab : RoadPrefab;
            // Instantiate a new road prefab
                GameObject roadObj = Instantiate(prefab, points[0], Quaternion.identity);
                
                // Set the name of the road
                roadObj.name = wayData.Name;

                roadObj.name = wayData.WayID;
                
                roadObj.transform.parent = _roadSystem.RoadContainer.transform;
                // Get the road from the prefab
                Road road = roadObj.GetComponent<Road>();
                road.IsGeneratingOSM = true;
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
            //The list with triangles the method returns
            List<Triangle> triangles = new List<Triangle>();

            //If we just have three points, then we dont have to do all calculations
            if (points.Count == 3)
            {
                triangles.Add(new Triangle(points[0], points[1], points[2]));

                return triangles;
            }



            //Step 1. Store the vertices in a list and we also need to know the next and prev vertex
            List<Vertex> vertices = new List<Vertex>();

            for (int i = 0; i < points.Count; i++)
                vertices.Add(new Vertex(points[i]));

            //Find the next and previous vertex
            for (int i = 0; i < vertices.Count; i++)
            {
                int nextPos = ClampListIndex(i + 1, vertices.Count);
                int prevPos = ClampListIndex(i - 1, vertices.Count);

                vertices[i].prevVertex = vertices[prevPos];
                vertices[i].nextVertex = vertices[nextPos];
            }

            //Step 2. Find the reflex (concave) and convex vertices, and ear vertices
            for (int i = 0; i < vertices.Count; i++)
                CheckIfReflexOrConvex(vertices[i]);

            //Have to find the ears after we have found if the vertex is reflex or convex
            List<Vertex> earVertices = new List<Vertex>();
            
            for (int i = 0; i < vertices.Count; i++)
                IsVertexEar(vertices[i], vertices, earVertices);

            //Step 3. Triangulate!
            while (true)
            {
                //This means we have just one triangle left
                if (vertices.Count == 3)
                {
                    //The final triangle
                    triangles.Add(new Triangle(vertices[0], vertices[0].prevVertex, vertices[0].nextVertex));
                    break;
                }

                //Make a triangle of the first ear
                Vertex earVertex = earVertices[0];

                Vertex earVertexPrev = earVertex.prevVertex;
                Vertex earVertexNext = earVertex.nextVertex;

                Triangle newTriangle = new Triangle(earVertex, earVertexPrev, earVertexNext);

                triangles.Add(newTriangle);

                //Remove the vertex from the lists
                earVertices.Remove(earVertex);

                vertices.Remove(earVertex);

                //Update the previous vertex and next vertex
                earVertexPrev.nextVertex = earVertexNext;
                earVertexNext.prevVertex = earVertexPrev;

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

        //Check if a vertex if reflex or convex, and add to appropriate list
        private static void CheckIfReflexOrConvex(Vertex v)
        {
            v.isReflex = false;
            v.isConvex = false;

            //This is a reflex vertex if its triangle is oriented clockwise
            Vector2 a = v.prevVertex.GetPos2D_XZ();
            Vector2 b = v.GetPos2D_XZ();
            Vector2 c = v.nextVertex.GetPos2D_XZ();

            if (IsTriangleOrientedClockwise(a, b, c))
                v.isReflex = true;
            else
                v.isConvex = true;
        }

        //Check if a vertex is an ear
        private static void IsVertexEar(Vertex v, List<Vertex> vertices, List<Vertex> earVertices)
        {

            //A reflex vertex cant be an ear!
            if (v.isReflex)
                return;

            //This triangle to check point in triangle
            Vector2 a = v.prevVertex.GetPos2D_XZ();
            Vector2 b = v.GetPos2D_XZ();
            Vector2 c = v.nextVertex.GetPos2D_XZ();

            bool hasPointInside = false;

            for (int i = 0; i < vertices.Count; i++)
            {
                //We only need to check if a reflex vertex is inside of the triangle
                if (vertices[i].isReflex)
                {
                    Vector2 p = vertices[i].GetPos2D_XZ();

                    //This means inside and not on the hull
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
            public Vector3 position;

            //The outgoing halfedge (a halfedge that starts at this vertex)
            //Doesnt matter which edge we connect to it
            public HalfEdge halfEdge;

            //Which triangle is this vertex a part of?
            public Triangle triangle;

            //The previous and next vertex this vertex is attached to
            public Vertex prevVertex;
            public Vertex nextVertex;

            //Properties this vertex may have
            //Reflex is concave
            public bool isReflex; 
            public bool isConvex;
            public bool isEar;

            public Vertex(Vector3 position)
            {
                this.position = position;
            }

            //Get 2d pos of this vertex
            public Vector2 GetPos2D_XZ()
            {
                Vector2 pos_2d_xz = new Vector2(position.x, position.z);
                return pos_2d_xz;
            }
        }

        public class HalfEdge
        {
            //The vertex the edge points to
            public Vertex v;

            //The face this edge is a part of
            public Triangle t;

            //The next edge
            public HalfEdge nextEdge;
            //The previous
            public HalfEdge prevEdge;
            //The edge going in the opposite direction
            public HalfEdge oppositeEdge;

            //This structure assumes we have a vertex class with a reference to a half edge going from that vertex
            //and a face (triangle) class with a reference to a half edge which is a part of this face 
            public HalfEdge(Vertex v)
            {
                this.v = v;
            }
        }

        public class Triangle
        {
            //Corners
            public Vertex v1;
            public Vertex v2;
            public Vertex v3;

            //If we are using the half edge mesh structure, we just need one half edge
            public HalfEdge halfEdge;

            public Triangle(Vertex v1, Vertex v2, Vertex v3)
            {
                this.v1 = v1;
                this.v2 = v2;
                this.v3 = v3;
            }

            public Triangle(Vector3 v1, Vector3 v2, Vector3 v3)
            {
                this.v1 = new Vertex(v1);
                this.v2 = new Vertex(v2);
                this.v3 = new Vertex(v3);
            }

            public Triangle(HalfEdge halfEdge)
            {
                this.halfEdge = halfEdge;
            }

            //Change orientation of triangle from cw -> ccw or ccw -> cw
            public void ChangeOrientation()
            {
                Vertex temp = this.v1;

                this.v1 = this.v2;
                this.v2 = temp;
            }
        }

        public class Edge
        {
            public Vertex v1;
            public Vertex v2;

            //Is this edge intersecting with another edge?
            public bool isIntersecting = false;

            public Edge(Vertex v1, Vertex v2)
            {
                this.v1 = v1;
                this.v2 = v2;
            }

            public Edge(Vector3 v1, Vector3 v2)
            {
                this.v1 = new Vertex(v1);
                this.v2 = new Vertex(v2);
            }

            //Get vertex in 2d space (assuming x, z)
            public Vector2 GetVertex2D(Vertex v)
            {
                return new Vector2(v.position.x, v.position.z);
            }

            //Flip edge
            public void FlipEdge()
            {
                Vertex temp = v1;

                v1 = v2;

                v2 = temp;
            }
        }

        public class Plane
        { 
            public Vector3 pos;

            public Vector3 normal;

            public Plane(Vector3 pos, Vector3 normal)
            {
                this.pos = pos;

                this.normal = normal;
            }
        }
    }
}