using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using RoadGenerator;
using UnityEngine;

namespace RoadGenerator
{
    public class NavigationNodeEdge
    {
        public NavigationNode EndNavigationNode;
        public NavigationNode StartNavigationNode;
        public string ID;
        public double Cost;
        public NavigationNodeEdge(NavigationNode endNode, NavigationNode startNode, double cost)
        {
            ID = System.Guid.NewGuid().ToString();
            EndNavigationNode = endNode;
            StartNavigationNode = startNode;
            Cost = cost;
        }
    }
    public class NavigationNode
    {
        public RoadNode RoadNode;
        public List<NavigationNodeEdge> Edges = new List<NavigationNodeEdge>();
        public NavigationNode(RoadNode roadNode)
        {
            this.RoadNode = roadNode;
        }
    }
    /// <summary> A graph representation of a road </summary>
    public class RoadNavigationGraph
    {
        private Road _road;
        public NavigationNode StartNavigationNode;
        public NavigationNode EndNavigationNode;
        public Dictionary<string, NavigationNode> Graph = new Dictionary<string, NavigationNode>();

        // The nodes that should become part of the navigation graph
        private readonly RoadNodeType[] _roadNodeTypesToAdd = 
        {
            RoadNodeType.ThreeWayIntersection,
            RoadNodeType.FourWayIntersection,
            RoadNodeType.End
        };

        private float _currentCost = 0f;
        public RoadNavigationGraph(RoadNode roadNode, Road road, bool isClosed)
        {
            _road = road;
            RoadNode start = roadNode;
            RoadNode curr = roadNode;
            NavigationNode PreviouslyAddedNode = null;

            while (curr != null)
            {
                // Increase the current cost if the current node is not the starting node
                if (curr.Prev != null)
                    _currentCost += CalculateCost(curr.DistanceToPrevNode);
                // If the current node is not a node that should be added to the graph, we skip it
                if (!_roadNodeTypesToAdd.Contains(curr.Type))
                {
                    curr = curr.Next;
                    continue;
                }
                // In a closed loop we never want to add the end node, so we skip it
                if (curr.Type == RoadNodeType.End && isClosed && PreviouslyAddedNode != null)
                {
                    PreviouslyAddedNode.Edges.Add(new NavigationNodeEdge(StartNavigationNode, PreviouslyAddedNode, _currentCost));
                    StartNavigationNode.Edges.Add(new NavigationNodeEdge(PreviouslyAddedNode, StartNavigationNode, _currentCost));
                    break;
                }
            
                // If the current node is the first node to be added
                if (PreviouslyAddedNode == null)
                {
                    PreviouslyAddedNode = new NavigationNode(curr);
                    StartNavigationNode = PreviouslyAddedNode;
                    Graph.Add(curr.Position.ToString(), PreviouslyAddedNode);
                    if (isClosed)
                    {
                        curr.IsNavigationNode = true;
                    }
                        
                    curr = curr.Next;
                    continue;
                }
                string key = curr.Position.ToString();
                if(!Graph.ContainsKey(key))
                {
                    NavigationNode graphNode = new NavigationNode(curr);
                    Graph.Add(key, graphNode);
                    // Edges with the current cost are added in both directions
                    PreviouslyAddedNode.Edges.Add(new NavigationNodeEdge(graphNode, PreviouslyAddedNode, _currentCost));
                    graphNode.Edges.Add(new NavigationNodeEdge(PreviouslyAddedNode, graphNode, _currentCost));
                    PreviouslyAddedNode = graphNode;
                    _currentCost = 0f;
                }
                curr = curr.Next;
            }
            EndNavigationNode = PreviouslyAddedNode;
            
        }
        private float CalculateCost(float distance, float speedLimit = 1f)
        {
            return distance / speedLimit;
        }

    }

    public static class RoadSystemNavigationGraph
    {
        private const float EDGE_LINE_WIDTH = 4f;
        private const float GRAPH_LIFT = 20f;

        /// <summary> Generates a graph representation for a road system </summary>
        public static Dictionary<string, NavigationNode> GenerateRoadSystemNavigationGraph(RoadSystem roadSystem)
        {
            // The road system graph, key is the positions string representation
            Dictionary<string, NavigationNode> roadSystemGraph = new Dictionary<string, NavigationNode>();
            foreach (Road road in roadSystem.Roads)
            {
                // Map the road into the graph
                road.UpdateRoad2();
            }
            // Loop through all roads in the road system
            foreach(Road road in roadSystem.Roads)
            {   
                // Map the road into the graph
                UpdateGraphForRoad(road, roadSystemGraph);
            }
            foreach (Intersection intersection in roadSystem.Intersections)
            {
                intersection.MapIntersectionNavigation(); 
            }
            return roadSystemGraph;
        }

        /// <summary> Updates a graph for the given road  </summary>
        private static void UpdateGraphForRoad(Road road,  Dictionary<string, NavigationNode> roadSystemGraph)
        {
            NavigationNode[] roadNavigationGraph = new NavigationNode[road.NavigationGraph.Graph.Values.Count];
            road.NavigationGraph.Graph.Values.CopyTo(roadNavigationGraph, 0);
            for (int i = 0; i < roadNavigationGraph.Length; i++)
            {
                if (roadSystemGraph.ContainsKey(roadNavigationGraph[i].RoadNode.Position.ToString()))
                {
                    NavigationNode node = roadSystemGraph[roadNavigationGraph[i].RoadNode.Position.ToString()];
                    node.Edges.AddRange(roadNavigationGraph[i].Edges);
                    UpdateEdgeEndNode(roadNavigationGraph[i], node.RoadNode.Position, node);
                    roadNavigationGraph[i] = node;

                    continue;
                }
                roadSystemGraph.Add(roadNavigationGraph[i].RoadNode.Position.ToString(), roadNavigationGraph[i]);
            }
        }
        private static void UpdateEdgeEndNode(NavigationNode navigationNodeToUpdate, Vector3 oldNodePosition, NavigationNode newNode)
        {
            foreach (NavigationNodeEdge edge in navigationNodeToUpdate.Edges)
            {
                foreach (NavigationNodeEdge edge2 in edge.EndNavigationNode.Edges)
                {
                    if (edge2.EndNavigationNode.RoadNode.Position == oldNodePosition)
                    {
                        edge2.EndNavigationNode = newNode;
                    }
                }
            }
        }
         /// <summary> Draws the graph </summary>
        public static void DrawGraph(RoadSystem roadSystem, Dictionary<string, NavigationNode> roadGraph, GameObject graphNodePrefab)
        {
            foreach (NavigationNode node in roadGraph.Values)
            {
                // Spawn a new graph node sphere
                GameObject nodeObject = GameObject.Instantiate(graphNodePrefab);
                
                // Place it in the correct location
                nodeObject.transform.parent = roadSystem.GraphContainer.transform;
                nodeObject.transform.position = lift(node.RoadNode.Position);
                
                // Create a list to contain all the graph node positions
                List<Vector3> graphNodePositions = new List<Vector3>(){ lift(node.RoadNode.Position) };
                
                // Add the end node of each edge to the list of positions
                foreach (NavigationNodeEdge edge in node.Edges)
                {
                    // To draw the graph, draw the line from the origin node to the target of the edge, and then back to the origin
                    // This is to make sure we only draw lines along the edges
                    graphNodePositions.Add(lift(edge.EndNavigationNode.RoadNode.Position));
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
    