using System.Collections.Generic;
using System.Linq;

namespace RoadGenerator
{
    public enum RoadNodeType 
    {
        Default = 0,
        ThreeWayIntersection,
        FourWayIntersection,
        Roundabout,
        End
    }

	public class RoadNode
	{
        private RoadNode _next;
        private RoadNode _prev;
        private RoadNodeType _type;
        public RoadNode(RoadNodeType type) : this(type, null, null){}
        public RoadNode(RoadNodeType type, RoadNode next, RoadNode prev)
        {
            _type = type;
            _next = next;
            _prev = prev;
        }
        public bool IsIntersection() => new RoadNodeType[]{ RoadNodeType.ThreeWayIntersection, RoadNodeType.FourWayIntersection, RoadNodeType.Roundabout }.Contains(_type);

        public RoadNodeType Type
        {
            get => _type;
        }
        public RoadNode Next
        {
            get => _next;
        }
        public RoadNode Prev
        {
            get => _prev;
        }
    }
}