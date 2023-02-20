using System.Collections.Generic;
using UnityEngine;
using RoadGenerator;

namespace RoadGenerator
{
/// <summary> Wrapper class for the GraphNode class.  </summary>
public class AStarNode : System.IComparable<AStarNode>, System.IEquatable<AStarNode>
 {
    public GraphNode GraphNode;
    public AStarNode PreviousNode;
    public double RouteCost;
    public double EstimatedCost;

    public AStarNode(GraphNode node, AStarNode previousNode, double routeCost, double estimatedCost){
        GraphNode = node;
        PreviousNode = previousNode;
        RouteCost = routeCost;
        EstimatedCost = estimatedCost;
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
        public static Stack<string> GetPathToNode(GraphNode startNode, GraphNode endNode)
        {
            AStarNode start = new(startNode, null, 0, 0);
            AStarNode target = new(endNode, null, 0, 0);
            PriorityQueue<AStarNode> toBeSearched = new();
            Dictionary<string, double> costMap = new();
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
                foreach (GraphEdge edge in current.GraphNode.Edges)
                {
                    double costToNode = current.RouteCost + edge.Cost;
                    // If the cost to the end node is lower than the current cost, update the cost and add the node to the queue
                    // If the node is not in the queue, add it
                    if (!costMap.ContainsKey(edge.EndNode.RoadNode.Position.ToString()) || costToNode < costMap[edge.EndNode.RoadNode.Position.ToString()])
                    {
                        // Update the cost
                        costMap[edge.EndNode.RoadNode.Position.ToString()] = costToNode;
                        // Calculate the estimated cost to the target node
                        double estimatesCost = costToNode + Heuristic(edge.EndNode, target.GraphNode);
                        // Add the node to the priority queue
                        AStarNode nextNode = new AStarNode(edge.EndNode, current, costToNode, estimatesCost);
                        toBeSearched.Enqueue(nextNode);
                    }
                }
            }
            // If a path is not found, return null
            return null;
        }
                /// <summary> Heuristic function for A* </summary>
        private static double Heuristic(GraphNode node, GraphNode targetNode)
        {
            return Vector3.Distance(node.RoadNode.Position, targetNode.RoadNode.Position);
        }
        /// <summary> Returns the path from the start node to the target node  </summary>
        private static Stack<string> GetPathToNode(AStarNode node)
        {
            Stack<string> path = new();
            while (node.PreviousNode != null)
            {
                path.Push(node.GraphNode.RoadNode.Position.ToString());
                node = node.PreviousNode;
            }
            return path;
        }
    }
}