using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Xml;

namespace RoadGenerator
{


public class MapGenerator : MonoBehaviour
{
    public GameObject roadPrefab;
    public GameObject HousePrefab;
    public RoadSystem roadSystem;
    Dictionary<Vector3, List<Road>> roadsAtNode = new Dictionary<Vector3, List<Road>>();
    int test = 0;
    void Start()
    {
        roadSystem.Setup();
        double minLat = 0;
        double minLon = 0;
        Dictionary<string, XmlNode> nodesDict = new Dictionary<string, XmlNode>();

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
                IEnumerator ienum = node.GetEnumerator();
                bool isRoad = false;
                bool isBuilding = false;

                float height = 10f;
                string name = "";
                // search for type of way
                while (ienum.MoveNext())
                {
                    XmlNode currentNode = (XmlNode) ienum.Current;
                    if (IsTagKeyName(currentNode, "highway") &&  currentNode.Attributes["v"].Value != "path") {
                        //isRoad = true;
                    }

                    if (IsTagKeyName(currentNode, "junction") && currentNode.Attributes["v"].Value == "roundabout")
                    {
                        // TODO: Add when roundabouts are supported
                        isRoad = false;
                        break;
                    }

                    if (IsTagKeyName(currentNode, "name")) {
                        name = currentNode.Attributes["v"].Value;
                    }

                    if (IsTagKeyName(currentNode, "maxspeed")) {
                        isRoad = true;
                    }

                    if (IsTagKeyName(currentNode, "building")) {
                        isBuilding = true;
                    }

                    if (IsTagKeyName(currentNode, "height")) {
                        height = float.Parse(currentNode.Attributes["v"].Value);
                    }

                }

                ienum = node.GetEnumerator();
                if (isRoad) {
                    GenerateRoad(ienum, nodesDict, name, minLat, minLon);
                    count++;

                }
            }
        }
        //return;
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

                    Debug.Log("Road 1 length: " + pathCreator1.path.length);
                    Debug.Log("Road 2 length: " + pathCreator2.path.length);
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
                        Debug.Log("Intersection position" + position);
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
    private void LoadOSMMap(XmlDocument document)
    {
        document.Load("Assets/map2.osm");
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
    void GenerateRoad(IEnumerator ienum, Dictionary<string, XmlNode> nodesDict, string roadName, double minLat = 0, double minLon = 0) {
        bool firstNode = true;
        Vector3 previousPoint = new Vector3(0,0,0);
        List <Vector3> roadPoints = new List<Vector3>();
        while (ienum.MoveNext())
        {
            XmlNode currentNode = (XmlNode) ienum.Current; 

            if (currentNode.Name == "nd" && nodesDict.ContainsKey(currentNode.Attributes["ref"].Value)) 
            { 
                const int scale = 111000;
                // monka line
                Vector3 nodePosition = new Vector3((float)(double.Parse(nodesDict[currentNode.Attributes["ref"].Value].Attributes["lon"].Value.Replace(".", ",")) - minLon)*scale, 0, (float)(double.Parse(nodesDict[currentNode.Attributes["ref"].Value].Attributes["lat"].Value.Replace(".", ",")) - minLat)*scale);
                if (firstNode) 
                {
                    firstNode = false;
                    previousPoint = LatLonVector3ToVector3(nodePosition);
                    roadPoints.Add(nodePosition);

                } 
                else 
                {
                    //DrawRoad(previousPoint, LatLonVector3ToVector3(nodePosition));
                    previousPoint = LatLonVector3ToVector3(nodePosition);
                    roadPoints.Add(nodePosition);
                }
            }
        }

        List<Vector3> roadPoints2 = new List<Vector3>();
        roadPoints2.Add(roadPoints[0]);
        roadPoints2.Add(roadPoints[1]);
        Road road = spawnRoad(roadPoints2, roadName);
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
            foreach (Vector3 point in points) {
                Debug.Log(point);
            }
            roadSystem.Setup();
          // Instantiate a new road prefab
            GameObject roadObj = Instantiate(roadPrefab, points[0], Quaternion.identity);
            
            // Set the name of the road
            roadObj.name = roadName;
            
            //roadObj.transform.parent = roadSystem.RoadContainer.transform;
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