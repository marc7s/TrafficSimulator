using System.Collections.Generic;
using UnityEngine;
using Extensions;
using System.Linq;

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
            return GraphNode.ID == other.GraphNode.ID;
        }
    }

    public enum NavigationMode
    {
        Disabled,
        Random, // Completely random at every intersection
        RandomNavigationPath, // Generates a random path and follows it
        Path // Follows a specified path, visiting the nodes in order
    }

    public static class Navigation
    {
        private const int MAX_ITERATIONS = 100;
        
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
                    return GetTracedPathFromNode(current);

                // Check all edges from the current node
                foreach (NavigationNodeEdge edge in current.GraphNode.Edges)
                {
                    bool isStartNodeUturn = current == start && edge.EndNavigationNode.RoadNode.Position == startNode.StartNavigationNode.RoadNode.Position;

                    bool isUTurn = isStartNodeUturn || edge.EndNavigationNode.RoadNode.Position == current.PreviousNode?.GraphNode.RoadNode.Position;
                    // If the edge is a u turn, skip it
                    if (isUTurn)
                        continue;

                    double costToNode = current.RouteCost + edge.Cost;
                    string nodeID = edge.EndNavigationNode.ID;
                    
                    // If the cost to the end node is lower than the current cost, update the cost and add the node to the queue
                    // If the node is not in the queue, add it
                    if (!costMap.ContainsKey(nodeID) || costToNode < costMap[nodeID])
                    {
                        // Update the cost
                        costMap[edge.EndNavigationNode.ID] = costToNode;
                        
                        // Calculate the estimated cost to the target node
                        double estimatedCost = costToNode + Heuristic(edge.EndNavigationNode, target.GraphNode);

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
        private static Stack<NavigationNodeEdge> GetTracedPathFromNode(AStarNode node)
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

                // To avoid getting a closed loop as the target node we check if the target node is the first node in a closed loop
                if (targetNode.RoadNode.Road.IsFirstRoadInClosedLoop || targetNode.RoadNode.Road.ConnectedToAtEnd?.Road.IsFirstRoadInClosedLoop == true)
                    continue;

                // To avoid getting an intersection as the target node we check if the target node is an intersection.
                // To check for three way intersections we need to check the next and previous nodes as well
                if (targetNode.RoadNode.IsIntersection() || targetNode.RoadNode.Next?.IsIntersection() == true || targetNode.RoadNode.Prev?.IsIntersection() == true)
                    continue;

                // Avoid getting navigation nodes as the target node
                if (targetNode.RoadNode.Type == RoadNodeType.RoadConnection)
                    continue;

                // Trying to find a path that is not too short
                if (path.Count > 0)
                    return path;
            }
            nodeToFind = null;
            return new Stack<NavigationNodeEdge>();
        }

        /// <summary> Returns a random path from the current edge to a random node in the road graph </summary>
        public static Stack<NavigationNodeEdge> GetPath(RoadSystem roadSystem, NavigationNodeEdge currentEdge, List<NavigationNode> targets, bool logSubPathError, out NavigationNode nodeToFind)
        {
            if(targets.Count > 0)
            {
                // The `nodeToFind` is where the final marker will be placed, so place it at the final target
                nodeToFind = targets[targets.Count - 1];
                NavigationNodeEdge sourceEdge = currentEdge;
                Stack<NavigationNodeEdge> path = new Stack<NavigationNodeEdge>();
                
                // Go through each target in order, appending the path from the previous target to the current target to the path
                foreach(NavigationNode target in targets)
                {
                    // Get the path from the previous target to the current target
                    Stack<NavigationNodeEdge> subPath = GetPathToNode(sourceEdge, target);
                    
                    // Skip the target if no path is found
                    if(subPath == null)
                    {
                        if(logSubPathError)
                        {
                            Debug.LogError("Could not find path to target node " + target.RoadNode.Position + ". Skipping target");
                            DebugUtility.MarkPositions(new Vector3[]{ sourceEdge.StartNavigationNode.RoadNode.Position, sourceEdge.EndNavigationNode.RoadNode.Position });
                        }
                        
                        continue;
                    }

                    if(subPath.Count < 1)
                        continue;
                    
                    // Add the source edge to the path if it is not the first target, to bridge between the targets
                    if(target != targets[0])
                        subPath.Push(sourceEdge);
                    
                    // Append the subpath to the path
                    path.StackBelow(subPath);

                    List<NavigationNodeEdge> subPathList = subPath.ToList();
                    NavigationNodeEdge lastSubEdge = subPathList[subPathList.Count - 1];

                    sourceEdge = target.PrimaryDirectionEdge?.EndNavigationNode == lastSubEdge.StartNavigationNode ? target.SecondaryDirectionEdge : target.PrimaryDirectionEdge;
                }

                return path;
            }

            nodeToFind = null;
            return new Stack<NavigationNodeEdge>();
        }
        
        public static void DrawNewNavigationPath(List<Vector3> positions, NavigationNode nodeToFind, GameObject container, Material pathMaterial, GameObject targetMarker)
        {
            if (container.GetComponent<LineRenderer>() == null)
                container.AddComponent<LineRenderer>();
            LineRenderer lineRenderer = container.GetComponent<LineRenderer>();
            lineRenderer.startWidth = 1f;
            lineRenderer.endWidth = 1f;
            
            lineRenderer.material = pathMaterial;
            lineRenderer.positionCount = positions.Count;
            lineRenderer.SetPositions(positions.ToArray());

            foreach (Transform child in container.transform)
            {
                Object.Destroy(child.gameObject);
            }
            Vector3 position = nodeToFind.RoadNode.Position + Vector3.up * 10f;
            GameObject marker = Object.Instantiate(targetMarker, position, Quaternion.identity);
            marker.transform.parent = container.transform;
        }

        public static void DrawUpdatedNavigationPath(ref List<Vector3> navigationPath, GameObject container)
        {
            if (navigationPath.Count == 0)
                return;

            LineRenderer lineRenderer = container.GetComponent<LineRenderer>();
            lineRenderer.positionCount = navigationPath.Count;
            lineRenderer.SetPositions(navigationPath.ToArray());
        }
    }
}