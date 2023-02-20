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
        public double Cost;
        public GraphNode EndNode;
        public GraphEdge(GraphNode endNode, double cost)
        {
            EndNode = endNode;
            Cost = cost;
        }
    }
    public class GraphNode
    {
        public RoadNode RoadNode;
        public List<GraphEdge> Edges = new List<GraphEdge>();
        public GraphNode(RoadNode roadNode)
        {
            this.RoadNode = roadNode;
        }
    }
    /// <summary> A graph representation of a road </summary>
    public class RoadNavigationGraph
    {
        public Dictionary<string, GraphNode> Graph = new Dictionary<string, GraphNode>();

        // The nodes that should become part of the navigation graph
        private readonly RoadNodeType[] _roadNodeTypesToAdd = 
        {
            RoadNodeType.ThreeWayIntersection,
            RoadNodeType.FourWayIntersection,
            RoadNodeType.End
        };

        private float _currentCost = 0f;
        public RoadNavigationGraph(RoadNode roadNode)
        {
            RoadNode curr = roadNode;
            GraphNode PreviouslyAddedNode = null;
            while (curr != null)
            {
                // The start node is added to the graph
                if (PreviouslyAddedNode == null)
                {
                    PreviouslyAddedNode = new GraphNode(curr);
                    Graph.Add(curr.Position.ToString(), PreviouslyAddedNode);
                    curr = curr.Next;
                    continue;
                }
                _currentCost += CalculateCost(Vector3.Distance(curr.Position, curr.Prev.Position)); 
                if (!_roadNodeTypesToAdd.Contains(curr.Type))
                {
                    curr = curr.Next;
                    continue;
                }
                // Add the node to the graph 
                GraphNode graphNode = new GraphNode(curr);
                // Edges with the current cost are added in both directions
                PreviouslyAddedNode.Edges.Add(new GraphEdge(graphNode, _currentCost));
                graphNode.Edges.Add(new GraphEdge(PreviouslyAddedNode, _currentCost));
                Graph.Add(curr.Position.ToString(), graphNode);
                PreviouslyAddedNode = graphNode;
                _currentCost = 0f;
                curr = curr.Next;
            }

        }
        private float CalculateCost(float distance, float speedLimit = 1f)
        {
            return distance / speedLimit;
        }
    }

    public static class RoadSystemNavigationGraph
    {
        private const string GRAPH_NODE_SPHERE_NAME = "Graph Node";
        private const float GRAPH_NODE_SPHERE_SIZE = 15f;
        private const float EDGE_LINE_WIDTH = 4f;
        private const float GRAPH_LIFT = 20f;

        /// <summary> Generates a graph representation for a road system </summary>
        public static Dictionary<string, GraphNode> GenerateRoadSystemNavigationGraph(RoadSystem roadSystem)
        {
            // The road system graph, key is the positions string representation
            Dictionary<string, GraphNode> roadSystemGraph = new Dictionary<string, GraphNode>();
            // Loop through all roads in the road system
            foreach(Road road in roadSystem.Roads)
            {   
                // Map the road into the graph
                UpdateGraphForRoad(road, roadSystemGraph);

            }
            return roadSystemGraph;
        }

        /// <summary> Updates a graph for the given road   </summary>
        private static void UpdateGraphForRoad(Road road,  Dictionary<string, GraphNode> roadSystemGraph)
        {
            GraphNode[] roadNavigationGraph = new GraphNode[road.NavigationGraph.Graph.Values.Count];
            road.NavigationGraph.Graph.Values.CopyTo(roadNavigationGraph, 0);
            for (var i = 0; i < roadNavigationGraph.Length; i++)
            {
                if (roadSystemGraph.ContainsKey(roadNavigationGraph[i].RoadNode.Position.ToString()))
                {
                    GraphNode node = roadSystemGraph[roadNavigationGraph[i].RoadNode.Position.ToString()];
                    node.Edges.AddRange(roadNavigationGraph[i].Edges);
                    roadNavigationGraph[i] = node;

                    continue;
                }
                roadSystemGraph.Add(roadNavigationGraph[i].RoadNode.Position.ToString(), roadNavigationGraph[i]);
            }
        }
        
         /// <summary> Draws the graph </summary>
        public static void DrawGraph(RoadSystem roadSystem, Dictionary<string, GraphNode> roadGraph)
        {
            foreach (GraphNode node in roadGraph.Values)
            {
                // Create a new graph node sphere
                GameObject nodeObject = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                
                // Name it and place it on the correct location
                nodeObject.name = GRAPH_NODE_SPHERE_NAME;
                nodeObject.transform.parent = roadSystem.Graph.transform;
                nodeObject.transform.position = lift(node.RoadNode.Position);

                // Give it a material and color
                Material mat = new Material(Shader.Find("Standard"));
                mat.SetColor("_Color", Color.red);
                nodeObject.GetComponent<Renderer>().material = mat;

                // Set the scale of the sphere
                nodeObject.transform.localScale = new Vector3(GRAPH_NODE_SPHERE_SIZE, GRAPH_NODE_SPHERE_SIZE, GRAPH_NODE_SPHERE_SIZE);
                
                // Create a list to contain all the graph node positions
                List<Vector3> graphNodePositions = new List<Vector3>(){ lift(node.RoadNode.Position) };
                
                // Add the end node of each edge to the list of positions
                foreach (GraphEdge edge in node.Edges)
                {
                    // To draw the graph, draw the line from the origin node to the target of the edge, and then back to the origin
                    // This is to make sure we only draw lines along the edges
                    graphNodePositions.Add(lift(edge.EndNode.RoadNode.Position));
                    graphNodePositions.Add(lift(node.RoadNode.Position));
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
    
