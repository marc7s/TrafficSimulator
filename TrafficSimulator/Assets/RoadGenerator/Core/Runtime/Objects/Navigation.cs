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
        return string.Equals(GraphNode.RoadNode.Position.ToString(), other.GraphNode.RoadNode.Position.ToString());
    }
}

    public static class Navigation
    {
        private const int MAX_ITERATIONS = 100;
        /// <summary> Finds the shortest path between two nodes in the road graph using A* algorithm </summary>
        public static Stack<NavigationNodeEdge> GetPathToNode(NavigationNode startNode, NavigationNode endNode)
        {
            AStarNode start = new AStarNode(startNode, null, null, 0, 0);
            AStarNode target = new AStarNode(endNode, null, null, 0, 0);
            PriorityQueue<AStarNode> toBeSearched = new PriorityQueue<AStarNode>();
            Dictionary<string, double> costMap = new Dictionary<string, double>();
            toBeSearched.Enqueue(start);

            while (toBeSearched.Count > 0)
            {
                // Get the node with the lowest estimated cost to the target
                AStarNode current = toBeSearched.Dequeue();
                if (current.Equals(target))
                {
                    return GetPathToNode(current);
                }
                // Check all edges from the current node
                foreach (NavigationNodeEdge edge in current.GraphNode.Edges)
                {
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
        public static Stack<NavigationNodeEdge> GetRandomPath(RoadSystem roadSystem, NavigationNodeEdge currentEdge, out NavigationNode nodeToFind, int iterationCount = 0)
        {
            // If a path is not found, return null
            if (iterationCount > MAX_ITERATIONS)
            {
                nodeToFind = null;
                return null;
            }
            List<NavigationNode> nodeList = roadSystem.RoadSystemGraph;
            System.Random random = new System.Random();
            int randomIndex = random.Next(0, nodeList.Count);
            NavigationNode targetNode = nodeList[randomIndex];
            nodeToFind = targetNode;
            Stack<NavigationNodeEdge> path = GetPathToNode(currentEdge.EndNavigationNode, targetNode);
            if (path.Count < 2)
                return GetRandomPath(roadSystem, currentEdge, out nodeToFind, iterationCount + 1);
            return path;
        }
        
        public static void DrawNavigationPath(NavigationNode nodeToFind, GameObject container)
        {
            // TODO DRAW ACTUAL LANE PATH, CURRENTLY ONLY DRAWS A CUBE AT THE DESTINATION

            foreach (Transform child in container.transform)
            {
                Object.Destroy(child.gameObject);
            }
            GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.transform.parent = container.transform;
            cube.transform.position = nodeToFind.RoadNode.Position;
            cube.transform.position = new Vector3(cube.transform.position.x, cube.transform.position.y + 10f, cube.transform.position.z);
            cube.transform.localScale = new Vector3(5f, 5f, 5f);
            Renderer cubeRenderer = cube.GetComponent<Renderer>();
            cubeRenderer.material.SetColor("_Color", Color.red);
        }
    }
}