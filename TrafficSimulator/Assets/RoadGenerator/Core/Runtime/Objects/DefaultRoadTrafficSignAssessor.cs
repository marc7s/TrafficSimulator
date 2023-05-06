using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace RoadGenerator
{
    public class DefaultRoadTrafficSignAssessor : TrafficSignAssessor
    {
        private bool _havePlacedSpeedSignAtStart = false;
        private bool _havePlacedSpeedSignAtEnd = false;
        private Dictionary<string, bool> _havePlacedSpeedSignAtStartOfIntersection = new Dictionary<string, bool>();
        private Dictionary<string, bool> _havePlacedSpeedSignAtEndOfIntersection = new Dictionary<string, bool>();
        private float? _distanceToPreviousLampPost = null;
        public override List<TrafficSignData> GetSignsThatShouldBePlaced(RoadNodeData data)
        {
            if (_distanceToPreviousLampPost.HasValue)
                _distanceToPreviousLampPost += data.RoadNode.DistanceToPrevNode;

            List<TrafficSignData> signsToBePlaced = new List<TrafficSignData>();
            AssessTrafficLightForRoadNode(data, ref signsToBePlaced);
            AssessStopSignForRoadNode(data, ref signsToBePlaced);
            AssessYieldSignForRoadNode(data, ref signsToBePlaced);
            AssessSpeedSignForRoadNode(data, ref signsToBePlaced);
            AssesLampPostForRoadNode(data, ref signsToBePlaced);
            AssesNoEntryOneDirectionForRoadNode(data, ref signsToBePlaced);
            return signsToBePlaced;
        }

        /// <summary> assesses if a lamp post should be placed at the current road node </summary>
        private void AssessSpeedSignForRoadNode(RoadNodeData data, ref List<TrafficSignData> signsToBePlaced)
        {
            if (!data.Road.GenerateSpeedSigns)
                return;

            DefaultRoad carRoad = data.Road as DefaultRoad;
            if (data.RoadNode.Type == RoadNodeType.JunctionEdge || data.RoadNode.IsIntersection())
                return;
            
            // Don't place a speed sign at an threeway intersectoin
            if (data.RoadNode.Type == RoadNodeType.End && (data.RoadNode.Next?.IsIntersection() == true || data.RoadNode.Prev?.IsIntersection() == true))
                return;

            // Place a speed sign before each intersection
            if (data.DistanceToNextIntersection < data.Road.SpeedSignDistanceFromIntersectionEdge && !_havePlacedSpeedSignAtStartOfIntersection.ContainsKey(data.NextIntersection.ID) && !data.Road.IsOneWay)
             {
                signsToBePlaced.Add(new TrafficSignData(carRoad.GetSpeedSignType(), data.RoadNode, carRoad.GetSpeedSignPrefab(), false, data.Road.DefaultTrafficSignOffset));
                _havePlacedSpeedSignAtStartOfIntersection[data.NextIntersection.ID] = true;
             }

            // Place a speed sign after each intersection
            if (data.DistanceToPrevIntersection > data.Road.SpeedSignDistanceFromIntersectionEdge && !_havePlacedSpeedSignAtEndOfIntersection.ContainsKey(data.PrevIntersection.ID))
            {
                signsToBePlaced.Add(new TrafficSignData(carRoad.GetSpeedSignType(), data.RoadNode, carRoad.GetSpeedSignPrefab(), true, data.Road.DefaultTrafficSignOffset));
                _havePlacedSpeedSignAtEndOfIntersection[data.PrevIntersection.ID] = true;
            }

            // Place a speed sign at the start of the road
            if (!_havePlacedSpeedSignAtStart && data.DistanceToStart > data.Road.SpeedSignDistanceFromRoadEnd && data.PrevIntersection?.Type != IntersectionType.ThreeWayIntersectionAtStart)
            {
                signsToBePlaced.Add(new TrafficSignData(carRoad.GetSpeedSignType(), data.RoadNode, carRoad.GetSpeedSignPrefab(), true, data.Road.DefaultTrafficSignOffset));
                _havePlacedSpeedSignAtStart = true;
            }

            // Place a speed sign at the end of the road
            if (!_havePlacedSpeedSignAtEnd && data.DistanceToEnd < data.Road.SpeedSignDistanceFromRoadEnd && data.NextIntersection?.Type != IntersectionType.ThreeWayIntersectionAtEnd && !data.Road.IsOneWay)
            {
               signsToBePlaced.Add(new TrafficSignData(carRoad.GetSpeedSignType(), data.RoadNode, carRoad.GetSpeedSignPrefab(), false, data.Road.DefaultTrafficSignOffset));
               _havePlacedSpeedSignAtEnd = true;
            }
        }

        /// <summary> Assesses if a stop sign should be placed at the current road node </summary>
        private void AssessStopSignForRoadNode(RoadNodeData data, ref List<TrafficSignData> signsToBePlaced)
        {
            // Place a stop sign at each junction edge
            if (data.RoadNode.Type == RoadNodeType.JunctionEdge && data.RoadNode.Intersection.FlowType == FlowType.StopSigns)
                signsToBePlaced.Add(new TrafficSignData(TrafficSignType.StopSign, data.RoadNode, data.Road.RoadSystem.DefaultStopSignPrefab, data.IntersectionFound, data.Road.DefaultTrafficSignOffset));
        }

        /// <summary> Assesses if a yield sign should be placed at the current road node </summary>
        private void AssessYieldSignForRoadNode(RoadNodeData data, ref List<TrafficSignData> signsToBePlaced)
        {
            // Place a stop sign at each junction edge
            if (data.RoadNode.Type == RoadNodeType.JunctionEdge && data.RoadNode.Intersection.FlowType == FlowType.YieldSigns)
                signsToBePlaced.Add(new TrafficSignData(TrafficSignType.YieldSign, data.RoadNode, data.Road.RoadSystem.DefaultYieldSignPrefab, data.IntersectionFound, data.Road.DefaultTrafficSignOffset));
        }

        /// <summary> Assesses if a traffic light should be placed at the current road node </summary>
        private void AssessTrafficLightForRoadNode(RoadNodeData data, ref List<TrafficSignData> signsToBePlaced)
        {
            // Place a traffic light at each junction edge
            if (data.RoadNode.Type == RoadNodeType.JunctionEdge && data.RoadNode.Intersection.FlowType == FlowType.TrafficLights)
            {
                // Do not spawn traffic lights for the secondary direction if the road is one way
                if (data.Road.IsOneWay && data.RoadNode.Prev.IsIntersection())
                    return;

                signsToBePlaced.Add(new TrafficSignData(TrafficSignType.TrafficLight, data.RoadNode, data.Road.RoadSystem.DefaultTrafficLightPrefab, data.IntersectionFound, data.Road.DefaultTrafficSignOffset));
            }
        }

        /// <summary> assesses if a lamp post should be placed at the current road node </summary>
        private void AssesLampPostForRoadNode(RoadNodeData data, ref List<TrafficSignData> signsToBePlaced)
        {
            DefaultRoad carRoad = data.Road as DefaultRoad;
            if (!data.Road.ShouldSpawnLampPoles)
                return;

            // Don't place a lamp Post at an junction edge or intersection
            if (data.RoadNode.Type == RoadNodeType.JunctionEdge || data.RoadNode.IsIntersection())
                return;

            // Don't place a lamp Post at an threeway intersectoin
            if (data.RoadNode.Type == RoadNodeType.End && (data.RoadNode.Next?.IsIntersection() == true || data.RoadNode.Prev?.IsIntersection() == true))
                return;

            // Place a lamppost all over the road at a certain interval
            if (_distanceToPreviousLampPost == null || _distanceToPreviousLampPost > data.Road.LampPoleIntervalDistance)
            {
                signsToBePlaced.Add(new TrafficSignData(TrafficSignType.LampPost, data.RoadNode, carRoad.LampPostPrefab, true, data.Road.LampPoleSideDistanceOffset));
                _distanceToPreviousLampPost = 0;
            }
        } 

        private void AssesNoEntryOneDirectionForRoadNode(RoadNodeData data, ref List<TrafficSignData> signsToBePlaced)
        {
            if (!data.Road.IsOneWay)
                return;

            if (data.RoadNode.Type == RoadNodeType.JunctionEdge && data.RoadNode.Next?.IsIntersection() == true)
            {
                DefaultRoad road = data.Road as DefaultRoad;
                signsToBePlaced.Add(new TrafficSignData(TrafficSignType.NoEntry, data.RoadNode, road.NoEntryOneDirectionSignPrefab, false, 0));
            }
        }       
    }
}