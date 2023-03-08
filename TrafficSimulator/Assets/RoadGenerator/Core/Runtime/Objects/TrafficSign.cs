using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace RoadGenerator
{
public enum TrafficSignType {SpeedSign, StopSign};
public struct TrafficSign
{
    public GameObject Sign;
    public RoadNode RoadNode;
    public Vector3 RoadNodePosition;
    public TrafficSignType TrafficSignType;
}
}
