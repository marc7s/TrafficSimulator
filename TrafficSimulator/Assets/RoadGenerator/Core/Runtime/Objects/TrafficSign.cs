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
            float distanceToEnd, bool 
            intersectionFound, 
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
        private float _speedSignDistanceFromIntersectionEdge;
        private float _lampPoleDistanceOffset;
        private float _speedSignDistanceFromEndPoint;
        private float _defaultTrafficSignOffset;
        private bool HavePlacesSpeedSignAtStart = false;
        private bool HavePlacesSpeedSignAtEnd = false;
        private Dictionary<String, bool> _havePlacedSpeedSignAtStartOfIntersection = new Dictionary<string, bool>();
        private Dictionary<String, bool> _havePlacedSpeedSignAtEndOfIntersection = new Dictionary<string, bool>();
        public TrafficSignCreator(TrafficSignSettings trafficSignSettings)
        {
            _speedSignDistanceFromIntersectionEdge = trafficSignSettings.SpeedSignDistanceFromIntersectionEdge;
            _lampPoleDistanceOffset = trafficSignSettings.LampPoleDistanceOffset;
            _speedSignDistanceFromEndPoint = trafficSignSettings.SpeedSignDistanceFromRoadEnd;
            _defaultTrafficSignOffset = trafficSignSettings.DefaultTrafficSignOffset;
        }

        private float? _distanceToPreviousLampPost = null;
        public List<TrafficSignData> SignThatShouldPlace(RoadNodeData data)
        {
            
            if (_distanceToPreviousLampPost.HasValue)
                _distanceToPreviousLampPost += data.roadNode.DistanceToPrevNode;

            List<TrafficSignData> signs = new List<TrafficSignData>();
            ShouldPlaceTrafficLight(data, ref signs);
            ShouldPlaceStopSign(data, ref signs);
            ShouldPlaceSpeedSign(data, ref signs);
            ShouldPlaceLampPost(data, ref signs);
            return signs;
        }
        private void ShouldPlaceSpeedSign(RoadNodeData data, ref List<TrafficSignData> signs)
        {
             if (data.DistanceToNextIntersection.HasValue && data.DistanceToNextIntersection.Value < _speedSignDistanceFromIntersectionEdge && !_havePlacedSpeedSignAtStartOfIntersection.ContainsKey(data.nextIntersection.ID))
             {
                signs.Add(new TrafficSignData(data.road.GetSpeedSignType(), data.roadNode, data.road.GetSpeedSignPrefab(), false, _defaultTrafficSignOffset));
                _havePlacedSpeedSignAtStartOfIntersection[data.nextIntersection.ID] = true;
             }

            if (data.DistanceToPrevIntersection.HasValue && data.DistanceToPrevIntersection.Value > _speedSignDistanceFromIntersectionEdge && !_havePlacedSpeedSignAtEndOfIntersection.ContainsKey(data.prevIntersection.ID))
            {
                signs.Add(new TrafficSignData(data.road.GetSpeedSignType(), data.roadNode, data.road.GetSpeedSignPrefab(), true, _defaultTrafficSignOffset));
                _havePlacedSpeedSignAtEndOfIntersection[data.prevIntersection.ID] = true;
            }

            if (!HavePlacesSpeedSignAtStart && data.DistanceToStart > _speedSignDistanceFromEndPoint)
            {
                signs.Add(new TrafficSignData(data.road.GetSpeedSignType(), data.roadNode, data.road.GetSpeedSignPrefab(), true, _defaultTrafficSignOffset));
                HavePlacesSpeedSignAtStart = true;
            }

            if (!HavePlacesSpeedSignAtEnd && data.DistanceToEnd < _speedSignDistanceFromEndPoint)
            {
               signs.Add(new TrafficSignData(data.road.GetSpeedSignType(), data.roadNode, data.road.GetSpeedSignPrefab(), false, _defaultTrafficSignOffset));
               HavePlacesSpeedSignAtEnd = true;
            }
        }
        private void ShouldPlaceStopSign(RoadNodeData data, ref List<TrafficSignData> signs)
        {
            if (data.roadNode.Type == RoadNodeType.JunctionEdge && data.roadNode.Intersection.FlowType == FlowType.StopSigns)
            {
                signs.Add(new TrafficSignData(TrafficSignType.StopSign, data.roadNode, data.road.RoadSystem.DefaultStopSignPrefab, data.IntersectionFound, _defaultTrafficSignOffset));
            }
        }
        private void ShouldPlaceTrafficLight(RoadNodeData data, ref List<TrafficSignData> signs)
        {
            if (data.roadNode.Type == RoadNodeType.JunctionEdge && data.roadNode.Intersection.FlowType == FlowType.TrafficLights)
            {
                signs.Add(new TrafficSignData(TrafficSignType.TrafficLight, data.roadNode, data.road.RoadSystem.DefaultTrafficLightPrefab, data.IntersectionFound, _defaultTrafficSignOffset));
            }
        }

        private void ShouldPlaceLampPost(RoadNodeData data, ref List<TrafficSignData> signs)
        {
            float LampDistanceOffset = 1;
            if (data.roadNode.Type == RoadNodeType.JunctionEdge || data.roadNode.IsIntersection())
                return;
            if (_distanceToPreviousLampPost == null || _distanceToPreviousLampPost > _lampPoleDistanceOffset)
            {
                signs.Add(new TrafficSignData(TrafficSignType.LampPost, data.roadNode, data.road.LampPostPrefab, true, LampDistanceOffset));
                _distanceToPreviousLampPost = 0;
            }
        }
    }
}