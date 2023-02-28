using System.Collections.Generic;
using UnityEngine;
using RoadGenerator;

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
            Debug.Log("No path found");
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

        public static Stack<NavigationNodeEdge> GetRandomPath(RoadSystem roadSystem, NavigationNodeEdge currentEdge)
        {
            //roadSystem.Setup();
            List<NavigationNode> nodeList = new List<NavigationNode>();
            nodeList.AddRange(roadSystem.RoadSystemGraph.Values);
            System.Random random = new System.Random();
            NavigationNode targetNode = currentEdge.EndNavigationNode;
            //while(targetNode == currentEdge.EndNavigationNode)
            //{
                int randomIndex = random.Next(0, nodeList.Count-1);
                targetNode = nodeList[randomIndex];
            //}
            GameObject cube2 = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube2.transform.position = targetNode.RoadNode.Position;
            cube2.transform.position = new Vector3(cube2.transform.position.x, cube2.transform.position.y + 10f, cube2.transform.position.z);
            cube2.transform.localScale = new Vector3(5f, 5f, 5f);
            var cubeRenderer = cube2.GetComponent<Renderer>();
//            cubeRenderer.material.SetColor("_Color", Color.red);
            Debug.Log("Start Node" + currentEdge.EndNavigationNode.RoadNode.Position +   "Target node: " + targetNode.RoadNode.Position);
            return GetPathToNode(currentEdge.EndNavigationNode, targetNode);
        }
    }
}