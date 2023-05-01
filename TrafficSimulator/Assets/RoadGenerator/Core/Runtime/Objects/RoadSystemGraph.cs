using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace RoadGenerator
{
    public class NavigationNodeEdge
    {
        public NavigationNode StartNavigationNode;
        public NavigationNode EndNavigationNode;
        private string _id;
        public double Cost;
        public NavigationNodeEdge(NavigationNode startNode, NavigationNode endNode, double cost)
        {
            _id = System.Guid.NewGuid().ToString();
            EndNavigationNode = endNode;
            StartNavigationNode = startNode;
            Cost = cost;
        }
        public string ID
        {
            get => _id;
        }
    }
    public class NavigationNode
    {
        public RoadNode RoadNode;
        public List<NavigationNodeEdge> Edges = new List<NavigationNodeEdge>();

        public NavigationNodeEdge PrimaryDirectionEdge;

        public NavigationNodeEdge SecondaryDirectionEdge;
        public NavigationNode(RoadNode roadNode)
        {
            this.RoadNode = roadNode;
        }
    }
    
    public class NavigationGraphBuilder
    {
        public NavigationNode PrevPrimaryDirectionNode;
        public NavigationNode PrevSecondaryDirectionNode;
        public List<NavigationNode> Nodes = new List<NavigationNode>();

        public void AddNode(RoadNode roadNode, float cost)
        {
            if(Nodes.FindAll(x => x.RoadNode.Position == roadNode.Position).Count != 0)
                return;

            NavigationNode node = new NavigationNode(roadNode);
            Nodes.Add(node);
            AddEdges(node, cost);
            PrevPrimaryDirectionNode = node;
            PrevSecondaryDirectionNode = node;
        }
        private void AddEdges(NavigationNode newNode, float cost)
        {
            if (PrevPrimaryDirectionNode != null)
            {
                NavigationNodeEdge edge = new NavigationNodeEdge(PrevPrimaryDirectionNode, newNode, cost);
                PrevPrimaryDirectionNode.Edges.Add(edge);
                PrevPrimaryDirectionNode.PrimaryDirectionEdge = edge;
            }
            if (PrevSecondaryDirectionNode != null && newNode.RoadNode.Road.IsOneWay == false)
            {
                NavigationNodeEdge edge = new NavigationNodeEdge(newNode, PrevSecondaryDirectionNode, cost);
                newNode.Edges.Add(edge);
                newNode.SecondaryDirectionEdge = edge;
            }
        }
    }
    /// <summary> A graph representation of a road </summary>
    public class RoadNavigationGraph
    {
        public NavigationNode StartNavigationNode;
        public NavigationNode EndNavigationNode;
        public List<NavigationNode> Graph = new List<NavigationNode>();

        // The nodes that should become part of the navigation graph
        private readonly RoadNodeType[] _roadNodeTypesToAdd = 
        {
            RoadNodeType.ThreeWayIntersection,
            RoadNodeType.FourWayIntersection,
            RoadNodeType.RoadConnection,
            RoadNodeType.End
        };

        private float _currentCost = 0f;
        public RoadNavigationGraph(RoadNode roadNode)
        {
            RoadNode curr = roadNode;
            NavigationGraphBuilder builder = new NavigationGraphBuilder();
            while (curr != null)
            {
                // If the current node is not part of the same road, we stop
                if (curr.Road != roadNode.Road)
                    break;
                // Increase the current cost if the current node is not the starting node
                if (curr.Prev != null)
                    _currentCost += CalculateCost(curr.DistanceToPrevNode);
                // If the current node is not a node that should be added to the graph, we skip it
                if (!_roadNodeTypesToAdd.Contains(curr.Type) && !curr.IsNavigationNode)
                {
                    curr = curr.Next;
                    continue;
                }

                if (curr.Type == RoadNodeType.End && (curr.Next?.Type == RoadNodeType.ThreeWayIntersection || curr.Prev?.Type == RoadNodeType.ThreeWayIntersection))
                {
                    curr = curr.Next;
                    continue;
                }

                builder.AddNode(curr, _currentCost);
                _currentCost = 0f;
                curr = curr.Next;
            }
            EndNavigationNode = builder.Nodes.Last();
            StartNavigationNode = builder.Nodes.First();
            Graph = builder.Nodes;
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
        public static List<NavigationNode> GenerateRoadSystemNavigationGraph(RoadSystem roadSystem)
        {
            // The road system graph, key is the positions string representation
            List<NavigationNode> roadSystemGraph = new List<NavigationNode>();
            foreach (Road road in roadSystem.DefaultRoads)
            {
                // Map the road into the graph
                road.UpdateRoadNoGraphUpdate();
            }
            // Loop through all roads in the road system
            foreach(Road road in roadSystem.DefaultRoads)
            {   
                // Map the road into the graph
                UpdateGraphForRoad(road, roadSystemGraph);
            }
            foreach (Intersection intersection in roadSystem.Intersections)
            {
                // Map the intersections navigation
                intersection.MapIntersectionNavigation(); 
            }

            return roadSystemGraph;
        }

        /// <summary> Updates a graph for the given road  </summary>
        private static void UpdateGraphForRoad(Road road,  List<NavigationNode> roadSystemGraph)
        {
            List<NavigationNode> roadNavigationGraph = road.NavigationGraph.Graph;
            List<NavigationNode> addedFromThisRoad = new List<NavigationNode>();
            for (int i = 0; i < roadNavigationGraph.Count; i++)
            {
                bool doesNodeExistInGraph = roadSystemGraph.FindAll(x => x.RoadNode.Position == roadNavigationGraph[i].RoadNode.Position).Count != 0;
                if(doesNodeExistInGraph)
                {
                    NavigationNode node = roadSystemGraph.Find(x => x.RoadNode.Position == roadNavigationGraph[i].RoadNode.Position);
                    
                    // If a node with the same position has already been added from this road, we don't want to add it again
                    if (addedFromThisRoad.Find(x => x.RoadNode.Position == node.RoadNode.Position) != null)
                        continue;
                    
                    node.Edges.AddRange(roadNavigationGraph[i].Edges);
                    UpdateEdgeEndNode(roadNavigationGraph[i], node.RoadNode.Position, node);
                    roadNavigationGraph[i] = node;

                    continue;
                }
                addedFromThisRoad.AddRange(roadNavigationGraph.FindAll(x => x.RoadNode.Position == roadNavigationGraph[i].RoadNode.Position));
                roadSystemGraph.AddRange(roadNavigationGraph.FindAll(x => x.RoadNode.Position == roadNavigationGraph[i].RoadNode.Position));
            }
        }

        private static void UpdateEdgeEndNode(NavigationNode navigationNodeToUpdate, Vector3 oldNodePosition, NavigationNode newNode)
        {
            foreach (NavigationNodeEdge edge1 in navigationNodeToUpdate.Edges)
            {
                // Finding the edges with the old node as the end node and updating them to the new node
                foreach (NavigationNodeEdge edge2 in edge1.EndNavigationNode.Edges)
                {
                    if (edge2.EndNavigationNode.RoadNode.Position == oldNodePosition)
                        edge2.EndNavigationNode = newNode;
                }
            }
        }
        
        /// <summary> Draws the graph </summary>
        public static void DrawGraph(RoadSystem roadSystem, List<NavigationNode> roadGraph, GameObject graphNodePrefab)
        {
            foreach (NavigationNode node in roadGraph)
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
                LineDrawer.DrawDebugLine(graphNodePositions, color: Color.blue, width: EDGE_LINE_WIDTH, parent: nodeObject.gameObject);
            }
        }
        
        /// <summary>Return a vertically transposed vector for creating the graph above the road system</summary>
        private static Vector3 lift(Vector3 vector)
        {
            return vector + Vector3.up * GRAPH_LIFT;
        }
    }
    public static class LineDrawer
    {
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
    }
}
    