using System.Collections.Generic;
using UnityEngine;

namespace RoadGenerator
{
/// <summary> A* wrapper class for the GraphNode class.  </summary>
    public class AStarNode : System.IComparable<AStarNode>, System.IEquatable<AStarNode>
    {
        public NavigationNode GraphNode;
        public AStarNode PreviousNode;
        public double RouteCost;
        public double EstimatedCost;
        
        public NavigationNodeEdge NavigationEdge;
        public AStarNode(NavigationNode node, AStarNode previousNode, NavigationNodeEdge navigationNodeEdge, double routeCost, double estimatedCost){
            GraphNode = node;
            PreviousNode = previousNode;
            RouteCost = routeCost;
            EstimatedCost = estimatedCost;
            NavigationEdge = navigationNodeEdge;
        }

        public int CompareTo(AStarNode other)
        {
            return EstimatedCost.CompareTo(other.EstimatedCost);
        }

        public bool Equals(AStarNode other) {
            return GraphNode.RoadNode.Position.ToString() == other.GraphNode.RoadNode.Position.ToString();
        }
    }

    public enum NavigationMode
    {
        Disabled,
        Random, // Completely random at every intersection
        RandomNavigationPath // Generates a random path and follows it
    }

    public static class Navigation
    {
        private const int MAX_ITERATIONS = 100;
        private static List<Vector3> _navigationPath = new List<Vector3>();
        /// <summary> Finds the shortest path between two nodes in the road graph using A* algorithm </summary>
        public static Stack<NavigationNodeEdge> GetPathToNode(NavigationNodeEdge startNode, NavigationNode endNode)
        {
            AStarNode start = new AStarNode(startNode.EndNavigationNode, null, null, 0, 0);
            AStarNode target = new AStarNode(endNode, null, null, 0, 0);
            
            Dictionary<string, double> costMap = new Dictionary<string, double>();
            
            PriorityQueue<AStarNode> toBeSearched = new PriorityQueue<AStarNode>();
            toBeSearched.Enqueue(start);

            while (toBeSearched.Count > 0)
            {
                // Get the node with the lowest estimated cost to the target
                AStarNode current = toBeSearched.Dequeue();
                
                if (current.Equals(target))
                    return GetPathToNode(current);

                // Check all edges from the current node
                foreach (NavigationNodeEdge edge in current.GraphNode.Edges)
                {
                    bool isStartNodeUturn = current == start && edge.EndNavigationNode.RoadNode.Position == startNode.StartNavigationNode.RoadNode.Position;

                    bool isUTurn = isStartNodeUturn || edge.EndNavigationNode.RoadNode.Position == current.PreviousNode?.GraphNode.RoadNode.Position;
                    // If the edge is a u turn, skip it
                    if (isUTurn)
                        continue;

                    double costToNode = current.RouteCost + edge.Cost;
                    string pos = edge.EndNavigationNode.RoadNode.Position.ToString();
                    // If the cost to the end node is lower than the current cost, update the cost and add the node to the queue
                    // If the node is not in the queue, add it
                    if (!costMap.ContainsKey(pos) || costToNode < costMap[pos])
                    {
                        // Update the cost
                        costMap[edge.EndNavigationNode.RoadNode.Position.ToString()] = costToNode;
                        // Calculate the estimated cost to the target node
                        double estimatedCost = costToNode + Heuristic(edge.EndNavigationNode, target.GraphNode);
                        // Debug.Log(edge.EndNavigationNode.RoadNode.Position);
                        // Add the node to the priority queue
                        AStarNode nextNode = new AStarNode(edge.EndNavigationNode, current, edge, costToNode, estimatedCost);
                        toBeSearched.Enqueue(nextNode);
                    }
                }
            }
            // If a path is not found, return null
            return null;
        }
                /// <summary> Heuristic function for A* </summary>
        private static double Heuristic(NavigationNode node, NavigationNode targetNode)
        {
            return Vector3.Distance(node.RoadNode.Position, targetNode.RoadNode.Position);
        }
        /// <summary> Returns the path from the start node to the target node  </summary>
        private static Stack<NavigationNodeEdge> GetPathToNode(AStarNode node)
        {
            Stack<NavigationNodeEdge> path = new Stack<NavigationNodeEdge>();
            while (node.NavigationEdge != null)
            {
                path.Push(node.NavigationEdge);
                node = node.PreviousNode;
            }
            return path;
        }
        /// <summary> Returns a random path from the current edge to a random node in the road graph </summary>
        public static Stack<NavigationNodeEdge> GetRandomPath(RoadSystem roadSystem, NavigationNodeEdge currentEdge, out NavigationNode nodeToFind)
        {
            List<NavigationNode> nodeList = roadSystem.RoadSystemGraph;
            for (int i = 0; i < MAX_ITERATIONS; i++)
            {
                System.Random random = new System.Random();
                int randomIndex = random.Next(0, nodeList.Count);
                NavigationNode targetNode = nodeList[randomIndex];
                nodeToFind = targetNode;
                Stack<NavigationNodeEdge> path = GetPathToNode(currentEdge, targetNode);
                if (path == null)
                    continue;
                // Trying to find a path that is not too short
                if (path.Count > (MAX_ITERATIONS < MAX_ITERATIONS / 2 ? 1 : 0))
                    return path;
            }
            nodeToFind = null;
            return new Stack<NavigationNodeEdge>();
        }
        
        public static void DrawNavigationPath(NavigationNode nodeToFind, Stack<NavigationNodeEdge> path, LaneNode startNode, GameObject container, Material pathMaterial, Vector3? prevIntersectionPosition)
        {
            if(nodeToFind == null)
                return;
            LaneNode current = startNode;
            var clonedStack = new Stack<NavigationNodeEdge>(new Stack<NavigationNodeEdge>(path));
            if (container.GetComponent<LineRenderer>() == null)
                container.AddComponent<LineRenderer>();
            LineRenderer lineRenderer = container.GetComponent<LineRenderer>();
            lineRenderer.startWidth = 1f;
            lineRenderer.endWidth = 1f;
            List<Vector3> positions = new List<Vector3>();

            while (current != null)
            {
                positions.Add(current.Position);
                if (current.RoadNode == nodeToFind.RoadNode)
                 {
                    break;
                 }   

                if (clonedStack.Count == 0)
                {
                    current = current.Next;
                    continue;
                }

                bool isNonIntersectionNavigationNode = current.RoadNode.IsNavigationNode && !current.IsIntersection() && current.Type != RoadNodeType.JunctionEdge;
                if (isNonIntersectionNavigationNode && clonedStack.Count != 0)
                {
                    clonedStack.Pop();
                    prevIntersectionPosition = Vector3.zero; 
                }
                if (current.Type == RoadNodeType.JunctionEdge && prevIntersectionPosition != current.RoadNode.Intersection.IntersectionPosition)
                {

                    (_, _, current) = current.RoadNode.Intersection.GetNewLaneNode(clonedStack.Pop(), current);
                    prevIntersectionPosition = current.RoadNode.Intersection.IntersectionPosition;
                    continue;
                }

                current = current.Next;
            }
            _navigationPath = positions;
            lineRenderer.material = pathMaterial;
            lineRenderer.positionCount = positions.Count;
            lineRenderer.SetPositions(positions.ToArray());
        }

        public static void DrawPathRemoveOldestPoint(GameObject container, Material pathMaterial)
        {
            if (_navigationPath.Count == 0)
                return;
            _navigationPath.RemoveAt(0);
            LineRenderer lineRenderer = container.GetComponent<LineRenderer>();
            lineRenderer.positionCount = _navigationPath.Count;
            lineRenderer.SetPositions(_navigationPath.ToArray());
        }
    }
}