using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace RoadGenerator
{
public enum TrafficSignType {SpeedSign, StopSign};
public class TrafficSign : MonoBehaviour
{
    public GameObject Sign;
    public RoadNode RoadNode;
    public Vector3 RoadNodePosition;
    public TrafficSignType TrafficSignType;

}
}
