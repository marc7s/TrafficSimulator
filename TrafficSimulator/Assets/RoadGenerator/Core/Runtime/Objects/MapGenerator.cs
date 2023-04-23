using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Xml;

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
    public string? StreetName;
    public string? StreetAddress;
    public int? Height;
}

public enum ServiceType
{
    DriveWay,
    Alley
}

public struct WayData
{
    public WayType WayType;
    // Lane amount for one direction
    public int? LaneAmount;
    public int? MaxSpeed;
    public bool? IsLit;
    public string? Name;
    public SideWalkType? SideWalkType;
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

public class MapGenerator : MonoBehaviour
{
    public GameObject roadPrefab;
    public GameObject BuildingPrefab;
    private RoadSystem roadSystem;
    public Material BuildingMaterial;
    Dictionary<string, XmlNode> nodesDict = new Dictionary<string, XmlNode>();
    Dictionary<Vector3, List<Road>> roadsAtNode = new Dictionary<Vector3, List<Road>>();
    List<XmlNode> busStops = new List<XmlNode>();
    double minLat = 0;
    double minLon = 0;
    public void GenerateMap(RoadSystem roadSystem)
    {
        nodesDict.Clear();
        roadsAtNode.Clear();
        busStops.Clear();
        minLat = 0;
        minLon = 0;

        this.roadSystem = roadSystem;

        XmlDocument doc = new XmlDocument();
        LoadOSMMap(doc);
    
        // Finding the bounds of the map and adding all nodes to a dictionary
        foreach(XmlNode node in doc.DocumentElement.ChildNodes)
        {
            if (node.Name == "bounds") 
            {
                minLat = double.Parse(node.Attributes["minlat"].Value.Replace(".", ","));
                minLon = double.Parse(node.Attributes["minlon"].Value.Replace(".", ","));
            }
            if (node.Name == "node") 
            {
                if (!nodesDict.ContainsKey(node.Attributes["id"].Value))
                    nodesDict.Add(node.Attributes["id"].Value, node);
            }
            foreach(XmlNode childNode in node.ChildNodes)
            {
                if (childNode.Name == "tag")
                {
                    switch (childNode.Attributes["k"].Value)
                    {
                        case "highway":
                            if (childNode.Attributes["v"].Value == "bus_stop")
                            {
                                busStops.Add(node);
                            }
                            break;
                    }
                }
            }


        }

        int count = 0;
        foreach(XmlNode node in doc.DocumentElement.ChildNodes){
            if (node.Name == "way") {
                WayData? wayData = GetWayData(node);
                IEnumerator ienum = node.GetEnumerator();
                if (wayData == null)
                    continue;
                if (wayData?.WayType != WayType.Building) {
                    GenerateRoad(ienum, wayData.Value);
                    count++;
                }
                if (wayData?.WayType == WayType.Building) {
                    GenerateBuilding(ienum, wayData.Value);
                }
            }
        }

        AddRoads();
        AddBusStops();

    }

    private void AddRoads()
    {
        foreach (var roads in roadsAtNode) {
            Vector3 position = roads.Key;

            // Find nodes that are shared by more than one road
            // This will mean it will either be an intersection or a road connection
            if (roads.Value.Count > 1)
            {

                if (roads.Value.Count == 2)
                {
                   // continue;
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
                       // Debug.Log("Intersection position" + position);
                        IntersectionCreator.CreateIntersectionAtPosition(position, road1, road2);
                    }
                }
                else if (roads.Value.Count == 3)
                {
                    continue;

                    Road road1 = roads.Value[0];
                    Road road2 = roads.Value[1];
                    Road road3 = roads.Value[2];

                    PathCreator pathCreator1 = road1.PathCreator;
                    PathCreator pathCreator2 = road2.PathCreator;
                    PathCreator pathCreator3 = road3.PathCreator;

                    Debug.Log("Road 1 length: " + pathCreator1.path.length);
                    Debug.Log("Road 2 length: " + pathCreator2.path.length);
                    Debug.Log("Road 3 length: " + pathCreator3.path.length);
                    if (pathCreator1.path.length < 15 || pathCreator2.path.length < 15 || pathCreator3.path.length < 15)
                    {
                        continue;
                    }
                    Debug.Log(position + "gdgf");

                    bool isNodeAtEndPointRoad1 = pathCreator1.path.GetPoint(pathCreator1.path.NumPoints - 1) == roads.Key || pathCreator1.path.GetPoint(0) == roads.Key;
                    bool isNodeAtEndPointRoad2 = pathCreator2.path.GetPoint(pathCreator2.path.NumPoints - 1) == roads.Key || pathCreator2.path.GetPoint(0) == roads.Key;
                    bool isNodeAtEndPointRoad3 = pathCreator3.path.GetPoint(pathCreator3.path.NumPoints - 1) == roads.Key || pathCreator3.path.GetPoint(0) == roads.Key;
                    
                    // If it is an intersection
                    if (!(isNodeAtEndPointRoad1 && isNodeAtEndPointRoad2 && isNodeAtEndPointRoad3))
                    {
                        // Temporary hack, TODO fix
                        position = position + new Vector3(0, 0, 0.0001f);
                        Debug.Log("Intersection position" + position);
                        Debug.Log("road1 not null" + road1);
                        Debug.Log("road2 not null" + road2);
                        Debug.Log("road3 not null" + road3);
                        IntersectionCreator.CreateIntersectionAtPositionMultipleRoads(position, new List<Road> { road1, road2, road3 });
                    }
                }
            }
        }        
    }

    public void AddBusStops()
    {
        GameObject busStopPrefab = roadSystem.DefaultBusStopPrefab;
        string name = "";
        string refName = "";
        foreach (XmlNode busStopNode in busStops)
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
            if (roadsAtNode.ContainsKey(position) == false)
                continue;
            List<Road> roads = roadsAtNode[position];
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
            Debug.Log("Bus stop position: " + closestRoadNode.Position);
            bool isForward = refName == "A";
            road.SpawnBusStop(closestRoadNode, isForward, busStopPrefab, name);

            //GameObject busStopObject = Instantiate(busStopPrefab, position, Quaternion.identity);
            //busStopObject.transform.parent = roadSystem.transform;
        }
    }

    private WayData? GetWayData(XmlNode node)
    {
        IEnumerator ienum = node.GetEnumerator();
        WayType? wayType = null;
        BuildingData buildingData = new BuildingData();
        WayData wayData = new WayData();
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
            case "unclassified":
                return WayType.Unclassified;
            
            default:
                return null;
        }
    }
    private void LoadOSMMap(XmlDocument document)
    {
        document.Load("Assets/map5.osm");
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

    // https://wiki.openstreetmap.org/wiki/Map_features#Highway
    void GenerateRoad(IEnumerator ienum, WayData wayData) {
        List <Vector3> roadPoints = GetWayNodePositions(ienum);

        // Currently get error when roads have same start and end point, TODO fix
        if (roadPoints[0] == roadPoints[roadPoints.Count - 1])
        {
            return;
        }
        List<Vector3> roadPoints2 = new List<Vector3>();
        roadPoints2.Add(roadPoints[0]);
        roadPoints2.Add(roadPoints[1]);
        Road road = spawnRoad(roadPoints2, wayData.Name);

        if (wayData.MaxSpeed != null)
            road.SpeedLimit = (SpeedLimit)wayData.MaxSpeed;

        // When the speedlimit is not known in residential areas, set it to 30km/h
        if (wayData.WayType == WayType.Residential && wayData.MaxSpeed == null)
            road.SpeedLimit = SpeedLimit.ThirtyKPH;

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
        
        foreach (Vector3 point in roadPoints) {
            if (!roadsAtNode.ContainsKey(point))
                roadsAtNode.Add(point, new List<Road> { road });
            else
                roadsAtNode[point].Add(road);
        }

        road.OnChange();
    }

    List<Vector3> GetWayNodePositions(IEnumerator ienum)
    {
        List <Vector3> nodePositions = new List<Vector3>();
        while (ienum.MoveNext())
        {
            XmlNode currentNode = (XmlNode) ienum.Current; 

            if (currentNode.Name == "nd" && nodesDict.ContainsKey(currentNode.Attributes["ref"].Value)) 
            { 

                Vector3 nodePosition = GetNodePosition(nodesDict[currentNode.Attributes["ref"].Value]);
                nodePositions.Add(nodePosition);
                
            }
        }
        return nodePositions;
    }

    private Vector3 GetNodePosition(XmlNode node)
    {
        const int scale = 111000;
        float xPos = (float)(double.Parse(node.Attributes["lon"].Value.Replace(".", ",")) - minLon)*scale;
        float zPos = (float)(double.Parse(node.Attributes["lat"].Value.Replace(".", ",")) - minLat)*scale;
        return new Vector3(xPos, 0, zPos);
    }
    void GenerateBuilding(IEnumerator ienum, WayData wayData)
    {
        float defaultBuildingHeight = 10;
        float height = wayData.BuildingData.Height ?? defaultBuildingHeight;

        List<Vector3> buildingPointsBottom = GetWayNodePositions(ienum);
        List<BuildingPoints> buildingPoints = new List<BuildingPoints>();
        foreach (Vector3 point in buildingPointsBottom)
            buildingPoints.Add(new BuildingPoints(point, new Vector3(point.x, height, point.z)));

        GameObject house = Instantiate(BuildingPrefab, buildingPointsBottom[0], Quaternion.identity);


        house.name = GetBuildingName(wayData);

        house.transform.parent = roadSystem.BuildingContainer.transform;
        Mesh buildingMesh = AssignMeshComponents(house);
        CreateBuildingMesh(buildingMesh, buildingPoints);
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
    private void CreateBuildingMesh(Mesh buildingMesh, List<BuildingPoints> buildingPoints)
    {
        List<Vector3> verts = new List<Vector3>();
        List<int> tris = new List<int>();

        foreach (BuildingPoints buildingPoint in buildingPoints)
        {
            verts.Add(buildingPoint.BottomPoint);
            verts.Add(buildingPoint.TopPoint);
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
            AddBuildingWall(index, index -1, index - 2, index - 3, tris);
        }

        buildingMesh.Clear();
        buildingMesh.vertices = verts.ToArray();
        buildingMesh.subMeshCount = 1;
        buildingMesh.SetTriangles(tris.ToArray(), 0);
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
        _meshRenderer.sharedMaterial = BuildingMaterial;
        MeshFilter _meshFilter = buildingObject.GetComponent<MeshFilter>();
        

        Mesh mesh = new Mesh();
    
        _meshFilter.sharedMesh = mesh;
        return mesh;
    }
    Road spawnRoad(List<Vector3> points, string roadName)
    {
          // Instantiate a new road prefab
            GameObject roadObj = Instantiate(roadPrefab, points[0], Quaternion.identity);
            
            // Set the name of the road
            roadObj.name = roadName;
            
            roadObj.transform.parent = roadSystem.RoadContainer.transform;
            // Get the road from the prefab
            Road road = roadObj.GetComponent<Road>();

            // Move the road to the spawn point
            PathCreator pathCreator = roadObj.GetComponent<PathCreator>();
            pathCreator.bezierPath = new BezierPath(points, false, PathSpace.xz);
            pathCreator.bezierPath.autoControlLength = 0.1f;

            // Set the road pointers
            road.RoadObject = roadObj;
            road.RoadSystem = roadSystem;
            
            // Update the road to display it
            road.OnChange();
            roadSystem.AddRoad(road);
            
            return road;
    }
}
}