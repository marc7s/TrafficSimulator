#if UNITY_EDITOR
using UnityEngine;
using System.Collections.Generic;

namespace RoadGenerator
{
    public class ValidateIntersectionNavigation : MonoBehaviour
    {
        public Intersection intersection = null;
        void Start()
        {
            if(intersection == null)
            {
                Debug.LogError("Intersection is null");
                return;
            }

            // Update the intersection, forcing all roads to build their road nodes as well
            intersection.UpdateMesh();

            // Create the intersection navigation data
            intersection.MapIntersectionNavigation();

            List<(LaneNode, List<Vector3>)> nodeGroups = new List<(LaneNode, List<Vector3>)>();
            Road[] roads = intersection.GetIntersectionRoads().ToArray();

            // Manually get all entry nodes to the intersection. Do not trust the entry nodes listed in the intersection since
            // that could have bugs as well
            List<LaneNode> entryNodes = new List<LaneNode>();
            Dictionary<string, LaneNode> nodes = new Dictionary<string, LaneNode>();
            foreach(Road road in roads)
            {
                foreach(Lane lane in road.Lanes)
                {
                    LaneNode curr = lane.StartNode;
                    while(curr != null)
                    {
                        // Add all nodes to a dictionary
                        if(!nodes.ContainsKey(curr.ID))
                            nodes.Add(curr.ID, curr);
                        
                        // Add all entry nodes to the list
                        if(curr.Type == RoadNodeType.JunctionEdge && curr.Next != null && curr.Next.IsIntersection())
                            entryNodes.Add(curr);

                        curr = curr.Next;
                    }
                }
            }

            // Go through all found entry nodes and add a node group for each one with the guide paths from that node
            foreach(LaneNode entryNode in entryNodes)
            {
                List<(string, string, GuideNode)> guidePaths = intersection.GetGuidePaths(entryNode);
                foreach((_, string exitID, GuideNode guidePath) in guidePaths)
                {
                    LaneNode exitNode = nodes[exitID];
                    nodeGroups.Add((guidePath, new List<Vector3>(){ entryNode.Position, exitNode.Position }));
                }
            }
            
            DebugUtility.AddMarkGroups(nodeGroups, (LaneNode node) => node.ID);
        }
    }
}
#endif