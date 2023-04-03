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
        public RoadNode roadNode;
        public float DistanceToStart;
        public float DistanceToEnd;
        public float? DistanceToNextIntersection;
        public float? DistanceToPrevIntersection;
        public bool IntersectionFound;
        public Road road;
        public Intersection nextIntersection;
        public Intersection prevIntersection;
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
            this.roadNode = roadNode;
            DistanceToStart = distanceToStart;
            DistanceToEnd = distanceToEnd;
            IntersectionFound = intersectionFound;
            this.road = road;
            DistanceToNextIntersection = distanceToNextIntersection;
            DistanceToPrevIntersection = distanceToPrevIntersection;
            this.nextIntersection = nextIntersection;
            this.prevIntersection = prevIntersection;
        }
    }

    public struct TrafficSignData
    {
        public TrafficSignType TrafficSignType;
        public RoadNode RoadNode;
        public GameObject SignPrefab;
        public bool isForward;
        public float DistanceFromRoad;

        public TrafficSignData(TrafficSignType type, RoadNode roadNode, GameObject prefab, bool isForward, float distanceFromRoad = 0f)
        {
            TrafficSignType = type;
            this.RoadNode = roadNode;
            this.SignPrefab = prefab;
            this.isForward = isForward;
            DistanceFromRoad = distanceFromRoad;
        }
    }
    public class TrafficSignCreator
    {
        private bool HavePlacedSpeedSignAtStart = false;
        private bool HavePlacedSpeedSignAtEnd = false;
        private Dictionary<String, bool> _havePlacedSpeedSignAtStartOfIntersection = new Dictionary<string, bool>();
        private Dictionary<String, bool> _havePlacedSpeedSignAtEndOfIntersection = new Dictionary<string, bool>();
        private float? _distanceToPreviousLampPost = null;
        public List<TrafficSignData> GetSignsThatShouldBePlaced(RoadNodeData data)
        {
            
            if (_distanceToPreviousLampPost.HasValue)
                _distanceToPreviousLampPost += data.roadNode.DistanceToPrevNode;

            List<TrafficSignData> signs = new List<TrafficSignData>();
            ShouldPlaceTrafficLight(data, ref signs);
            ShouldPlaceStopSign(data, ref signs);
            ShouldSpeedSignBePlaced(data, ref signs);
            ShouldPlaceLampPost(data, ref signs);
            return signs;
        }

        private void ShouldSpeedSignBePlaced(RoadNodeData data, ref List<TrafficSignData> signs)
        {
            if (data.roadNode.Type == RoadNodeType.JunctionEdge || data.roadNode.IsIntersection())
                return;
            
            // Don't place a speed sign at an threeway intersectoin
            if (data.roadNode.Type == RoadNodeType.End && (data.roadNode.Next?.IsIntersection() == true || data.roadNode.Prev?.IsIntersection() == true))
                return;

            // Place a speed sign before each intersection
            if (data.DistanceToNextIntersection < data.road.SpeedSignDistanceFromIntersectionEdge && !_havePlacedSpeedSignAtStartOfIntersection.ContainsKey(data.nextIntersection.ID))
             {
                signs.Add(new TrafficSignData(data.road.GetSpeedSignType(), data.roadNode, data.road.GetSpeedSignPrefab(), false, data.road.DefaultTrafficSignOffset));
                _havePlacedSpeedSignAtStartOfIntersection[data.nextIntersection.ID] = true;
             }

            // Place a speed sign after each intersection
            if (data.DistanceToPrevIntersection > data.road.SpeedSignDistanceFromIntersectionEdge && !_havePlacedSpeedSignAtEndOfIntersection.ContainsKey(data.prevIntersection.ID))
            {
                signs.Add(new TrafficSignData(data.road.GetSpeedSignType(), data.roadNode, data.road.GetSpeedSignPrefab(), true, data.road.DefaultTrafficSignOffset));
                _havePlacedSpeedSignAtEndOfIntersection[data.prevIntersection.ID] = true;
            }

            // Place a speed sign at the start of the road
            if (!HavePlacedSpeedSignAtStart && data.DistanceToStart > data.road.SpeedSignDistanceFromRoadEnd && data.prevIntersection?.Type != IntersectionType.ThreeWayIntersectionAtStart)
            {
                signs.Add(new TrafficSignData(data.road.GetSpeedSignType(), data.roadNode, data.road.GetSpeedSignPrefab(), true, data.road.DefaultTrafficSignOffset));
                HavePlacedSpeedSignAtStart = true;
            }

            // Place a speed sign at the end of the road
            if (!HavePlacedSpeedSignAtEnd && data.DistanceToEnd < data.road.SpeedSignDistanceFromRoadEnd && data.nextIntersection?.Type != IntersectionType.ThreeWayIntersectionAtEnd)
            {
               signs.Add(new TrafficSignData(data.road.GetSpeedSignType(), data.roadNode, data.road.GetSpeedSignPrefab(), false, data.road.DefaultTrafficSignOffset));
               HavePlacedSpeedSignAtEnd = true;
            }
        }
        private void ShouldPlaceStopSign(RoadNodeData data, ref List<TrafficSignData> signs)
        {
            // Place a stop sign at each junction edge
            if (data.roadNode.Type == RoadNodeType.JunctionEdge && data.roadNode.Intersection.FlowType == FlowType.StopSigns)
            {
                signs.Add(new TrafficSignData(TrafficSignType.StopSign, data.roadNode, data.road.RoadSystem.DefaultStopSignPrefab, data.IntersectionFound, data.road.DefaultTrafficSignOffset));
            }
        }
        private void ShouldPlaceTrafficLight(RoadNodeData data, ref List<TrafficSignData> signs)
        {
            // Place a traffic light at each junction edge
            if (data.roadNode.Type == RoadNodeType.JunctionEdge && data.roadNode.Intersection.FlowType == FlowType.TrafficLights)
            {
                signs.Add(new TrafficSignData(TrafficSignType.TrafficLight, data.roadNode, data.road.RoadSystem.DefaultTrafficLightPrefab, data.IntersectionFound, data.road.DefaultTrafficSignOffset));
            }
        }

        private void ShouldPlaceLampPost(RoadNodeData data, ref List<TrafficSignData> signs)
        {
            if (!data.road.ShouldSpawnLampPoles)
                return;

            // Don't place a lamp Post at an junction edge or intersection
            if (data.roadNode.Type == RoadNodeType.JunctionEdge || data.roadNode.IsIntersection())
                return;

            // Don't place a lamp Post at an threeway intersectoin
            if (data.roadNode.Type == RoadNodeType.End && (data.roadNode.Next?.IsIntersection() == true || data.roadNode.Prev?.IsIntersection() == true))
                return;

            // Place a lamppost all over the road at a certain interval
            if (_distanceToPreviousLampPost == null || _distanceToPreviousLampPost > data.road.LampPoleDistanceOffset)
            {
                signs.Add(new TrafficSignData(TrafficSignType.LampPost, data.roadNode, data.road.LampPostPrefab, true, data.road.LampPoleSideDistanceOffset));
                _distanceToPreviousLampPost = 0;
            }
        }
    }
}