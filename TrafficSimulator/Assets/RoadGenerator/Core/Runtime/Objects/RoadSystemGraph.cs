using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using RoadGenerator;
using UnityEngine;

namespace RoadGenerator
{
    public class GraphEdge
    {
        public double Distance;
        public double SpeedLimit;
        public double Cost;
        public GraphNode EndNode;
        public GraphEdge(GraphNode endNode, double distance, double speedLimit)
        {
            EndNode = endNode;
            Distance = distance;
            SpeedLimit = speedLimit;
            Cost = distance / speedLimit;
        }
    }
    
    public class GraphNode
    {
        public Vector3 Position;
        public List<GraphEdge> Edges = new List<GraphEdge>();
        public GraphNode(Vector3 position)
        {
            this.Position = position;
        }
    }

    public static class RoadSystemGraph
    {
        private const string GRAPH_NODE_SPHERE_NAME = "Graph Node";
        private const float GRAPH_NODE_SPHERE_SIZE = 15f;
        private const float EDGE_LINE_WIDTH = 4f;
        private const float GRAPH_LIFT = 20f;
        const int SEGMENT_ANCHOR1_INDEX = 0;
        const int SEGMENT_ANCHOR2_INDEX = 3;
        /// <summary> Generates a graph representation for a road system </summary>
        public static Dictionary<string, GraphNode> GenerateRoadGraph(RoadSystem roadSystem)
        {
            // The road system graph, key is the positions string representation
            Dictionary<string, GraphNode> roadSystemGraph = new Dictionary<string, GraphNode>();
            // Loop through all roads in the road system
            foreach(Road road in roadSystem.Roads)
            {   
                // Map the road into the graph
                UpdateGraphForRoad(road, roadSystemGraph, roadSystem);
            }
            return roadSystemGraph;
        }

        /// <summary> Updates a graph for the given road   </summary>
        private static void UpdateGraphForRoad(Road road,  Dictionary<string, GraphNode> roadSystemGraph, RoadSystem roadSystem)
        {
            PathCreator pathCreator = road.GetComponent<PathCreator>();
            float currentCost = 0;
            Vector3 startNodePosition = pathCreator.bezierPath.GetPointsInSegment(0)[SEGMENT_ANCHOR1_INDEX];
            GraphNode startGraphNode = null;
            if (IsNodeIntersection(startNodePosition, roadSystem))
                startGraphNode = new GraphNode(GetIntersectionPositionFromIntersectionAnchor(startNodePosition, roadSystem));
            else
                startGraphNode = new GraphNode(startNodePosition);
            roadSystemGraph.Add(startNodePosition.ToString(), startGraphNode);
            GraphNode previousNode = startGraphNode;


            for (var i = 0; i < pathCreator.bezierPath.NumSegments; i++)
            {
                Vector3 node1Position = pathCreator.bezierPath.GetPointsInSegment(i)[0];
                Vector3 node2Position = pathCreator.bezierPath.GetPointsInSegment(i)[SEGMENT_ANCHOR2_INDEX];
                // If the end node
                if (pathCreator.bezierPath.NumSegments-1 == i)
                    {
                        if (IsNodeIntersection(node2Position, roadSystem))
                        {
                            Vector3 intersectionPosition = GetIntersectionPositionFromIntersectionAnchor(node2Position, roadSystem);
                            AddNodeToGraph(intersectionPosition, previousNode, currentCost, roadSystemGraph);
                            return;
                        }
                        else 
                        {
                            AddNodeToGraph(node2Position, previousNode, currentCost, roadSystemGraph);
                            return;
                        }
                    }

                currentCost += CalculateCostBetweenNodes(i, pathCreator);

                if (IsNodeIntersection(node2Position, roadSystem))
                {
                    Vector3 intersectionPosition = GetIntersectionPositionFromIntersectionAnchor(node2Position, roadSystem);
                    if (roadSystemGraph.ContainsKey(intersectionPosition.ToString()))
                    {
                        // If the intersection node already exists, add the edge to the existing node
                        GraphNode intersectionNode = roadSystemGraph[intersectionPosition.ToString()];
                        intersectionNode.Edges.Add(new GraphEdge(previousNode, currentCost, 1));
                        previousNode.Edges.Add(new GraphEdge(intersectionNode, currentCost, 1));
                        previousNode = intersectionNode;
                        currentCost = 0;
                    }
                    else
                    {
                        // If the intersection node does not exist, create a new node and add the edge to the existing node
                        GraphNode intersectionNode = AddNodeToGraph(intersectionPosition, previousNode, currentCost, roadSystemGraph);
                        previousNode = intersectionNode;
                        currentCost = 0;
                    }
                }

            }

        }
        /// <summary> Calculate the cost from one spline to the other </summary>
        private static float CalculateCostBetweenNodes(int segmentIndex, PathCreator pathCreator)
        {
            // TODO calculate the vertex path instead of just the distance between the two nodes
            // Get the distance between the two nodes
            float distance = Vector3.Distance(pathCreator.bezierPath.GetPointsInSegment(segmentIndex)[0], pathCreator.bezierPath.GetPointsInSegment(segmentIndex)[3]);
            return distance;
        }
        /// <summary> Checks if the given position is an intersection anchor </summary>
        private static bool IsNodeIntersection(Vector3 position, RoadSystem roadSystem)
        {
            foreach (Intersection intersection in roadSystem.Intersections)
            {
                if(IsAnchorPoint(intersection, position))
                    return true;
            }
            return false;
        }
        /// <summary> Adds a node to the graph and returns the node </summary>
        private static GraphNode AddNodeToGraph(Vector3 position, GraphNode previousNode, float currentCost, Dictionary<string, GraphNode> roadSystemGraph)
        {
            GraphNode intersectionNode = new GraphNode(position);
            intersectionNode.Edges.Add(new GraphEdge(previousNode, currentCost, 1));
            roadSystemGraph.Add(intersectionNode.Position.ToString(), intersectionNode);
            previousNode.Edges.Add(new GraphEdge(intersectionNode, currentCost, 1));
            return intersectionNode;
        }
        /// <summary> Checks if the given position is an intersection anchor </summary>     
        private static bool IsAnchorPoint(Intersection intersection, Vector3 position)
        {
            if (intersection.Road1AnchorPoint1 == position || intersection.Road1AnchorPoint2 == position || intersection.Road2AnchorPoint1 == position || intersection.Road2AnchorPoint2 == position)
                return true;
            return false;
        }

        private static Vector3 GetIntersectionPositionFromIntersectionAnchor(Vector3 anchorPosition, RoadSystem roadSystem)
        {
            // TODO MAKE OOPTIONAL
            foreach (Intersection intersection in roadSystem.Intersections)
            {
                if(IsAnchorPoint(intersection, anchorPosition))
                    return intersection.IntersectionPosition;
            }
            return anchorPosition;
        }
        
         /// <summary> Draws the graph </summary>
        public static void DrawGraph(RoadSystem roadSystem, Dictionary<string, GraphNode> roadGraph)
        {
            foreach (GraphNode node in roadGraph.Values)
            {
                Debug.Log(node.Position);
                // Create a new graph node sphere
                GameObject nodeObject = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                
                // Name it and place it on the correct location
                nodeObject.name = GRAPH_NODE_SPHERE_NAME;
                nodeObject.transform.parent = roadSystem.Graph.transform;
                nodeObject.transform.position = lift(node.Position);

                // Give it a material and color
                Material mat = new Material(Shader.Find("Standard"));
                mat.SetColor("_Color", Color.red);
                nodeObject.GetComponent<Renderer>().material = mat;

                // Set the scale of the sphere
                nodeObject.transform.localScale = new Vector3(GRAPH_NODE_SPHERE_SIZE, GRAPH_NODE_SPHERE_SIZE, GRAPH_NODE_SPHERE_SIZE);
                
                // Create a list to contain all the graph node positions
                List<Vector3> graphNodePositions = new List<Vector3>(){ lift(node.Position) };
                
                // Add the end node of each edge to the list of positions
                foreach (GraphEdge edge in node.Edges)
                {
                    // To draw the graph, draw the line from the origin node to the target of the edge, and then back to the origin
                    // This is to make sure we only draw lines along the edges
                    graphNodePositions.Add(lift(edge.EndNode.Position));
                    graphNodePositions.Add(lift(node.Position));
                }

                // Draw the lines between the graph nodes
                DrawDebugLine(graphNodePositions, color: Color.blue, width: EDGE_LINE_WIDTH, parent: nodeObject.gameObject);
            }
        }
        /// <summary>Return a vertically transposed vector for creating the graph above the road system</summary>
        private static Vector3 lift(Vector3 vector)
        {
            return vector + Vector3.up * GRAPH_LIFT;
        }
                /// <summary>Helper function that performs the drawing of a lane's path</summary>
                      /// <summary>Helper function that performs the drawing of a lane's path</summary>
        private static void DrawLanePath(GameObject line, List<Vector3> lane, Color color, float width = 0.5f)
        {
            // Get the line renderer
            LineRenderer lr = line.GetComponent<LineRenderer>();

            // Give it a material
            lr.sharedMaterial = new Material(Shader.Find("Standard"));

            // Give it a color
            lr.sharedMaterial.SetColor("_Color", color);
            
            // Give it a width
            lr.startWidth = width;
            lr.endWidth = width;
            
            // Set the positions
            lr.positionCount = lane.Count;
            lr.SetPositions(lane.ToArray());
        }

        #nullable enable
        /// <summary>Draws a line, used for debugging</summary>
        public static void DrawDebugLine(List<Vector3> line, Color? color = null, float width = 0.5f, GameObject? parent = null)
        {
            if(line.Count < 1) return;
            
            // Create the line object
            GameObject lineObject = new GameObject();

            // Set the parent game object if one was passed
            if(parent != null) {
                lineObject.transform.parent = parent.transform;
            }

            // Add a line renderer to the lane
            lineObject.AddComponent<LineRenderer>();
            
            // Draw the lane path
            DrawLanePath(lineObject, line, color: color ?? Color.red, width: width);
        }
        #nullable disable
    }
    }
    
