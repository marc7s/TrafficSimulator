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
    public abstract class TrafficSignAssessor
    {
        public abstract List<TrafficSignData> GetSignsThatShouldBePlaced(RoadNodeData data);
    }
}