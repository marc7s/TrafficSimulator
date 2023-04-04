using System;
using UnityEngine;
using System.Collections.Generic;
namespace RoadGenerator
{
    public enum TrafficSignType
    {
        SpeedSignTenKPH,
        SpeedSignTwentyKPH,
        SpeedSignThirtyKPH,
        SpeedSignFortyKPH,
        SpeedSignFiftyKPH,
        SpeedSignSixtyKPH,
        SpeedSignSeventyKPH,
        SpeedSignEightyKPH,
        SpeedSignNinetyKPH,
        SpeedSignOneHundredKPH,
        SpeedSignOneHundredTenKPH,
        SpeedSignOneHundredTwentyKPH,
        SpeedSignOneHundredThirtyKPH,
        StopSign,
        TrafficLight,
        LampPost
    }
    public enum SpeedLimit
    {
        TenKPH = 10,
        TwentyKPH = 20,
        ThirtyKPH = 30,
        FortyKPH = 40,
        FiftyKPH = 50,
        SixtyKPH = 60,
        SeventyKPH = 70,
        EightyKPH = 80,
        NinetyKPH = 90,
        OneHundredKPH = 100,
        OneHundredTenKPH = 110,
        OneHundredTwentyKPH = 120,
        OneHundredThirtyKPH = 130
    }


    public struct RoadNodeData
    {
        public RoadNode RoadNode;
        public float DistanceToStart;
        public float DistanceToEnd;
        public float? DistanceToNextIntersection;
        public float? DistanceToPrevIntersection;
        public bool IntersectionFound;
        public Road Road;
        public Intersection NextIntersection;
        public Intersection PrevIntersection;
        public RoadNodeData(
            RoadNode roadNode, 
            float distanceToStart, 
            float distanceToEnd, 
            bool intersectionFound, 
            Road road, 
            float? distanceToNextIntersection, 
            float? distanceToPrevIntersection, 
            Intersection nextIntersection, 
            Intersection prevIntersection
            )
        {
            this.RoadNode = roadNode;
            DistanceToStart = distanceToStart;
            DistanceToEnd = distanceToEnd;
            IntersectionFound = intersectionFound;
            this.Road = road;
            DistanceToNextIntersection = distanceToNextIntersection;
            DistanceToPrevIntersection = distanceToPrevIntersection;
            this.NextIntersection = nextIntersection;
            this.PrevIntersection = prevIntersection;
        }
    }

    public struct TrafficSignData
    {
        public TrafficSignType TrafficSignType;
        public RoadNode RoadNode;
        public GameObject SignPrefab;
        public bool IsForward;
        public float DistanceFromRoad;

        public TrafficSignData(TrafficSignType type, RoadNode roadNode, GameObject prefab, bool isForward, float distanceFromRoad = 0f)
        {
            TrafficSignType = type;
            this.RoadNode = roadNode;
            this.SignPrefab = prefab;
            this.IsForward = isForward;
            DistanceFromRoad = distanceFromRoad;
        }
    }
    public class TrafficSignAssessor
    {
        private bool HavePlacedSpeedSignAtStart = false;
        private bool HavePlacedSpeedSignAtEnd = false;
        private Dictionary<string, bool> _havePlacedSpeedSignAtStartOfIntersection = new Dictionary<string, bool>();
        private Dictionary<string, bool> _havePlacedSpeedSignAtEndOfIntersection = new Dictionary<string, bool>();
        private float? _distanceToPreviousLampPost = null;
        public List<TrafficSignData> GetSignsThatShouldBePlaced(RoadNodeData data)
        {
            if (_distanceToPreviousLampPost.HasValue)
                _distanceToPreviousLampPost += data.RoadNode.DistanceToPrevNode;

            List<TrafficSignData> signsToBePlaced = new List<TrafficSignData>();
            AssessTrafficLightForRoadNode(data, ref signsToBePlaced);
            AssessStopSignForRoadNode(data, ref signsToBePlaced);
            AssessSpeedSignForRoadNode(data, ref signsToBePlaced);
            AssesLampPostForRoadNode(data, ref signsToBePlaced);
            return signsToBePlaced;
        }

        /// <summary> assesses if a lamp post should be placed at the current road node </summary>
        private void AssessSpeedSignForRoadNode(RoadNodeData data, ref List<TrafficSignData> signsToBePlaced)
        {
            if (data.RoadNode.Type == RoadNodeType.JunctionEdge || data.RoadNode.IsIntersection())
                return;
            
            // Don't place a speed sign at an threeway intersectoin
            if (data.RoadNode.Type == RoadNodeType.End && (data.RoadNode.Next?.IsIntersection() == true || data.RoadNode.Prev?.IsIntersection() == true))
                return;

            // Place a speed sign before each intersection
            if (data.DistanceToNextIntersection < data.Road.SpeedSignDistanceFromIntersectionEdge && !_havePlacedSpeedSignAtStartOfIntersection.ContainsKey(data.NextIntersection.ID))
             {
                signsToBePlaced.Add(new TrafficSignData(data.Road.GetSpeedSignType(), data.RoadNode, data.Road.GetSpeedSignPrefab(), false, data.Road.DefaultTrafficSignOffset));
                _havePlacedSpeedSignAtStartOfIntersection[data.NextIntersection.ID] = true;
             }

            // Place a speed sign after each intersection
            if (data.DistanceToPrevIntersection > data.Road.SpeedSignDistanceFromIntersectionEdge && !_havePlacedSpeedSignAtEndOfIntersection.ContainsKey(data.PrevIntersection.ID))
            {
                signsToBePlaced.Add(new TrafficSignData(data.Road.GetSpeedSignType(), data.RoadNode, data.Road.GetSpeedSignPrefab(), true, data.Road.DefaultTrafficSignOffset));
                _havePlacedSpeedSignAtEndOfIntersection[data.PrevIntersection.ID] = true;
            }

            // Place a speed sign at the start of the road
            if (!HavePlacedSpeedSignAtStart && data.DistanceToStart > data.Road.SpeedSignDistanceFromRoadEnd && data.PrevIntersection?.Type != IntersectionType.ThreeWayIntersectionAtStart)
            {
                signsToBePlaced.Add(new TrafficSignData(data.Road.GetSpeedSignType(), data.RoadNode, data.Road.GetSpeedSignPrefab(), true, data.Road.DefaultTrafficSignOffset));
                HavePlacedSpeedSignAtStart = true;
            }

            // Place a speed sign at the end of the road
            if (!HavePlacedSpeedSignAtEnd && data.DistanceToEnd < data.Road.SpeedSignDistanceFromRoadEnd && data.NextIntersection?.Type != IntersectionType.ThreeWayIntersectionAtEnd)
            {
               signsToBePlaced.Add(new TrafficSignData(data.Road.GetSpeedSignType(), data.RoadNode, data.Road.GetSpeedSignPrefab(), false, data.Road.DefaultTrafficSignOffset));
               HavePlacedSpeedSignAtEnd = true;
            }
        }

        /// <summary> assesses if a lamp post should be placed at the current road node </summary>
        private void AssessStopSignForRoadNode(RoadNodeData data, ref List<TrafficSignData> signsToBePlaced)
        {
            // Place a stop sign at each junction edge
            if (data.RoadNode.Type == RoadNodeType.JunctionEdge && data.RoadNode.Intersection.FlowType == FlowType.StopSigns)
            {
                signsToBePlaced.Add(new TrafficSignData(TrafficSignType.StopSign, data.RoadNode, data.Road.RoadSystem.DefaultStopSignPrefab, data.IntersectionFound, data.Road.DefaultTrafficSignOffset));
            }
        }

        /// <summary> assesses if a trafficd light should be placed at the current road node </summary>
        private void AssessTrafficLightForRoadNode(RoadNodeData data, ref List<TrafficSignData> signsToBePlaced)
        {
            // Place a traffic light at each junction edge
            if (data.RoadNode.Type == RoadNodeType.JunctionEdge && data.RoadNode.Intersection.FlowType == FlowType.TrafficLights)
            {
                signsToBePlaced.Add(new TrafficSignData(TrafficSignType.TrafficLight, data.RoadNode, data.Road.RoadSystem.DefaultTrafficLightPrefab, data.IntersectionFound, data.Road.DefaultTrafficSignOffset));
            }
        }

        /// <summary> assesses if a lamp post should be placed at the current road node </summary>
        private void AssesLampPostForRoadNode(RoadNodeData data, ref List<TrafficSignData> signsToBePlaced)
        {
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
                signsToBePlaced.Add(new TrafficSignData(TrafficSignType.LampPost, data.RoadNode, data.Road.LampPostPrefab, true, data.Road.LampPoleSideDistanceOffset));
                _distanceToPreviousLampPost = 0;
            }
        }
    }
}