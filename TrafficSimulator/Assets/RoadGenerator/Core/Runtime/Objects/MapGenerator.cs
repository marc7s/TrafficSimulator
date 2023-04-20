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
    Building,
    Unclassified
}

public struct WayData
{
    public WayType WayType;
    // Lane amount for one direction
    public int LaneAmount;
    public int Maxspeed;
    public bool IsLit;
    public string Name;
    public SideWalkType SideWalkType;
    public int Height;
    //
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
    public GameObject HousePrefab;
    private RoadSystem roadSystem;
    Dictionary<string, XmlNode> nodesDict = new Dictionary<string, XmlNode>();
    Dictionary<Vector3, List<Road>> roadsAtNode = new Dictionary<Vector3, List<Road>>();
    double minLat = 0;
    double minLon = 0;
    int test = 0;
    public void GenerateMap(RoadSystem roadSystem)
    {
        this.roadSystem = roadSystem;


        

        XmlDocument doc = new XmlDocument();
        LoadOSMMap(doc);
    
        // Finding the bounds of the map and adding all nodes to a dictionary
        foreach(XmlNode node in doc.DocumentElement.ChildNodes){
            if (node.Name == "bounds") {
                minLat = double.Parse(node.Attributes["minlat"].Value.Replace(".", ","));
                minLon = double.Parse(node.Attributes["minlon"].Value.Replace(".", ","));
            }
            if (node.Name == "node") {
                nodesDict.Add(node.Attributes["id"].Value, node);
            }
        }

        int count = 0;
        foreach(XmlNode node in doc.DocumentElement.ChildNodes){
            if (node.Name == "way") {
                WayData? wayData = GetWayData(node);
                IEnumerator ienum = node.GetEnumerator();
                if (wayData != null && wayData?.WayType != WayType.Building) {
                    GenerateRoad(ienum, wayData.Value);
                    count++;

                }
            }
        }

        foreach (var roads in roadsAtNode) {
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
                       // Debug.Log("Intersection position" + position);
                        IntersectionCreator.CreateIntersectionAtPosition(position, road1, road2);
                    }
                }
                else if (roads.Value.Count == 3 && false)
                {

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

    private WayData? GetWayData(XmlNode node)
    {
        IEnumerator ienum = node.GetEnumerator();
        bool isRoad = false;
        bool isBuilding = false;
        WayType? wayType = null;
        float height = 10f;
        string name = "";
        WayData wayData = new WayData();
        // search for type of way
        while (ienum.MoveNext())
        {
            XmlNode currentNode = (XmlNode) ienum.Current;
            if (currentNode.Name != "tag") 
                continue;

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
                    wayData.Maxspeed = int.Parse(currentNode.Attributes["v"].Value);
                    break;
                case "junction":
                    if (currentNode.Attributes["v"].Value == "roundabout")
                        return null;
                    break;
                case "height":
                    wayData.Height = int.Parse(currentNode.Attributes["v"].Value);
                    break;
            }
        }
        if (wayType == null)
            return null;

        wayData.WayType = wayType.Value;        
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
            case "unclassified":
                return WayType.Unclassified;
            
            default:
                return null;
        }
    }
    private void LoadOSMMap(XmlDocument document)
    {
        document.Load("Assets/map4.osm");
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
                const int scale = 111000;
                // monka line
                Vector3 nodePosition = new Vector3((float)(double.Parse(nodesDict[currentNode.Attributes["ref"].Value].Attributes["lon"].Value.Replace(".", ",")) - minLon)*scale, 0, (float)(double.Parse(nodesDict[currentNode.Attributes["ref"].Value].Attributes["lat"].Value.Replace(".", ",")) - minLat)*scale);
                nodePositions.Add(nodePosition);
                
            }
        }
        return nodePositions;
    }

    void GenerateBuilding()
    {

    }

    void DrawRoad(Vector3 point1, Vector3 point2) {
        double distance = Vector3.Distance(point1, point2);
        Vector3 distance3 = point2 - point1;
        roadPrefab.transform.localScale = new Vector3(5f, 0.01f, (float)distance);
        Vector3 halfwayCordinates = (point1 + point2) / 2;
        //roadPrefab.transform.rotation = Quaternion.LookRotation(distance3, Vector3.up);
        Quaternion rotation = Quaternion.LookRotation(distance3, Vector3.up);
        roadPrefab.transform.rotation = Quaternion.identity;
        Instantiate(roadPrefab, halfwayCordinates, Quaternion.LookRotation(distance3, Vector3.up));
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

            // Set the road pointers
            road.RoadObject = roadObj;
            road.RoadSystem = roadSystem;
            
            // Update the road to display it
            road.OnChange();
            roadSystem.AddRoad(road);
            
            return road;
    }

    void DrawBuildingWall(Vector3 point1, Vector3 point2, float height = 10f) {
        double distance = Vector3.Distance(point1, point2);
        Vector3 distance3 = point2 - point1;
        roadPrefab.transform.localScale = new Vector3(0.1f, height, (float)distance);
        Vector3 halfwayCordinates = (point1 + point2) / 2;
        //roadPrefab.transform.rotation = Quaternion.LookRotation(distance3, Vector3.up);
        Quaternion rotation = Quaternion.LookRotation(distance3, Vector3.up);
        roadPrefab.transform.rotation = Quaternion.identity;
        Instantiate(roadPrefab, halfwayCordinates, Quaternion.LookRotation(distance3, Vector3.up));
    }
}
}