using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace RoadGenerator
{
    public class TramRailTrafficSignAssessor : TrafficSignAssessor
    {
        public override List<TrafficSignData> GetSignsThatShouldBePlaced(RoadNodeData data)
        {
            return new List<TrafficSignData>();
        }
    }
}