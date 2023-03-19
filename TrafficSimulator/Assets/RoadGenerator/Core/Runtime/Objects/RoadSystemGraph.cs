using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace RoadGenerator
{
    public class NavigationNodeEdge
    {
        public NavigationNode EndNavigationNode;
        public NavigationNode StartNavigationNode;
        private string _id;
        public double Cost;
        public NavigationNodeEdge(NavigationNode endNode, NavigationNode startNode, double cost)
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
        public RoadNavigationGraph(RoadNode roadNode, bool isClosed)
        {
            RoadNode curr = roadNode;
            
            NavigationNode PreviouslyAddedNode = null;
            
            NavigationNode closedStartNodePrimaryDirection = null;
            NavigationNode closedStartNodeSecondaryDirection = null;
            
            NavigationNode prevNonIntersectionNodePrimary = null;
            NavigationNode prevIntersectionNodeSecondary = null;
            
            bool isPreviousNodeNotIntersection = false;
            
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
                    break;   
            
                // If the current node is the first node to be added
                if (PreviouslyAddedNode == null)
                {
                    PreviouslyAddedNode = new NavigationNode(curr);
                    StartNavigationNode = PreviouslyAddedNode;
                    Graph.Add(PreviouslyAddedNode);
                    if (isClosed)
                    {
                        closedStartNodePrimaryDirection = PreviouslyAddedNode;
                        prevNonIntersectionNodePrimary = closedStartNodePrimaryDirection;
                        closedStartNodeSecondaryDirection = new NavigationNode(curr);
                        prevIntersectionNodeSecondary = closedStartNodeSecondaryDirection;
                        Graph.Add(closedStartNodeSecondaryDirection);
                        isPreviousNodeNotIntersection = true;
                        curr.IsNavigationNode = true;
                    }
                        
                    curr = curr.Next;
                    continue;
                }
                
                if(Graph.FindAll(x => x.RoadNode.Position == curr.Position).Count == 0)
                {
                    NavigationNode graphNode = new NavigationNode(curr);
                    Graph.Add(graphNode);

                    // When a non intersection node is added, we need to one node for the primary direction and one for the secondary direction
                    if (isPreviousNodeNotIntersection)
                    {
                        // Edges with the current cost are added in both directions
                        NavigationNodeEdge edgeSecondaryDirection = new NavigationNodeEdge(prevIntersectionNodeSecondary, graphNode, _currentCost);
                        graphNode.Edges.Add(edgeSecondaryDirection);
                        graphNode.SecondaryDirectionEdge = edgeSecondaryDirection;
                        NavigationNodeEdge edgePrimaryDirection = new NavigationNodeEdge(graphNode, prevNonIntersectionNodePrimary, _currentCost);
                        prevNonIntersectionNodePrimary.Edges.Add(edgePrimaryDirection);
                        prevIntersectionNodeSecondary.PrimaryDirectionEdge = edgePrimaryDirection;
                        prevNonIntersectionNodePrimary.PrimaryDirectionEdge = edgePrimaryDirection;

                        PreviouslyAddedNode = graphNode;
                        _currentCost = 0f;
                        isPreviousNodeNotIntersection = false;
                        continue;
                    }

                    // Edges with the current cost are added in both directions
                    AddEdgeInBothDirections(PreviouslyAddedNode, graphNode, _currentCost);

                    PreviouslyAddedNode = graphNode;
                    _currentCost = 0f;
                }
                curr = curr.Next;
            }
            EndNavigationNode = PreviouslyAddedNode;
            
            // If the road is closed we need to add the edges between the start and end node
            if (isClosed)
            {
                NavigationNodeEdge edgePrimary = new NavigationNodeEdge(closedStartNodeSecondaryDirection, EndNavigationNode, _currentCost);
                EndNavigationNode.PrimaryDirectionEdge = edgePrimary;
                EndNavigationNode.Edges.Add(edgePrimary);
                NavigationNodeEdge edgeSecondary = new NavigationNodeEdge(EndNavigationNode, closedStartNodeSecondaryDirection, _currentCost);
                closedStartNodeSecondaryDirection.Edges.Add(edgeSecondary);
                closedStartNodeSecondaryDirection.SecondaryDirectionEdge = edgeSecondary;
                closedStartNodePrimaryDirection.SecondaryDirectionEdge = edgeSecondary;   
            }
        }
        
        private void AddEdgeInBothDirections(NavigationNode prevNode, NavigationNode newNode, double cost)
        {
            NavigationNodeEdge edgePrimary = new NavigationNodeEdge(newNode, prevNode, cost);
            prevNode.Edges.Add(edgePrimary);
            prevNode.PrimaryDirectionEdge = edgePrimary;
            NavigationNodeEdge edgeSecondary = new NavigationNodeEdge(prevNode, newNode, cost);
            newNode.Edges.Add(edgeSecondary);
            newNode.SecondaryDirectionEdge = edgeSecondary;
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
            foreach (Road road in roadSystem.Roads)
            {
                // Map the road into the graph
                road.UpdateRoadNoGraphUpdate();
            }
            // Loop through all roads in the road system
            foreach(Road road in roadSystem.Roads)
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
    