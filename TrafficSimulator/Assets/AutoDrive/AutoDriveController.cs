using RoadGenerator;
using System;

namespace Car
{
    public interface IAutoDriveController<T> where T : System.Enum
    {
        abstract bool ShouldAct(ref AutoDriveAgent agent);
        abstract Func<LaneNode, bool> EventAssessor(ref AutoDriveAgent agent, T type);
    }
    public abstract class AutoDriveController<T> : IAutoDriveController<T> where T : System.Enum
    {
        public abstract bool ShouldAct(ref AutoDriveAgent agent);

        public abstract Func<LaneNode, bool> EventAssessor(ref AutoDriveAgent agent, T type);
        public Action OnNavigationUpdate { get; set; }
        
        protected bool ShouldActAtNode(ref AutoDriveAgent agent, LaneNode node)
        {
            // Check all events and return true if any of them returns true
            foreach(T type in Enum.GetValues(typeof(T)))
            {
                if(EventAssessor(ref agent, type)(node))
                    return true;
            }
            return false;
        }
    }
}