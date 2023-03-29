using RoadGenerator;
using System;

namespace Car
{
    public enum NavigationEventType
    { 
        IntersectionEntry,
        IntersectionExit
    }

    public class NavigationController : AutoDriveController<NavigationEventType>
    {
        public Action<Intersection> OnIntersectionEntry;
        public Action<Intersection> OnIntersectionExit;
        public override bool ShouldAct(ref AutoDriveAgent agent)
        {
            LaneNode curr = agent.Context.CurrentNode;
            ShouldActAtNode(ref agent, curr);
            return false;
        }

        public override Func<LaneNode, bool> EventAssessor(ref AutoDriveAgent agent, NavigationEventType type)
        {
            switch(type)
            {
                case NavigationEventType.IntersectionEntry:
                    return (LaneNode node) =>
                        { 
                            if(node.Type == RoadNodeType.JunctionEdge && node.Next?.IsIntersection() == true)
                                OnIntersectionEntry?.Invoke(node.Intersection);
                            return false;
                        };
                case NavigationEventType.IntersectionExit:
                    return (LaneNode node) =>
                        { 
                            if(node.Type == RoadNodeType.JunctionEdge && node.Prev?.IsIntersection() == true)
                                OnIntersectionExit?.Invoke(node.Intersection);
                            return false;
                        };
                default:
                    return (LaneNode node) => false;
            }
        }
    }
}