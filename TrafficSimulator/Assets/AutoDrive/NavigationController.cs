using RoadGenerator;
using System;

namespace VehicleBrain
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
        public override (NavigationEventType, bool) ShouldActImplementation(ref AutoDriveAgent agent)
        {
            LaneNode curr = agent.Context.CurrentNode;
            ShouldActAtNode(ref agent, curr);
            return (default, false);
        }

        public override Func<LaneNode, (NavigationEventType, bool)> EventAssessor(ref AutoDriveAgent agent, NavigationEventType type)
        {
            switch(type)
            {
                case NavigationEventType.IntersectionEntry:
                    return (LaneNode node) =>
                        { 
                            if(node.Type == RoadNodeType.JunctionEdge && node.Next?.IsIntersection() == true)
                                OnIntersectionEntry?.Invoke(node.Intersection);
                            return (NavigationEventType.IntersectionEntry, false);
                        };
                case NavigationEventType.IntersectionExit:
                    return (LaneNode node) =>
                        { 
                            if(node.Type == RoadNodeType.JunctionEdge && node.Prev?.IsIntersection() == true)
                                OnIntersectionExit?.Invoke(node.Intersection);
                            return (NavigationEventType.IntersectionExit, false);
                        };
                default:
                    return (LaneNode node) => (default, false);
            }
        }
    }
}