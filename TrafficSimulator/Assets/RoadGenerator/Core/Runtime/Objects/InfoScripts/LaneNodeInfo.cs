using UnityEngine;
using CustomProperties;
using DataModel;

namespace RoadGenerator
{
    public class LaneNodeInfo : InfoScript<LaneNode>
    {
        [SerializeField] private SNReadOnly<int> _index = null;
        [SerializeField] [ReadOnly] private RoadNode _roadNode = null;
        [SerializeField] private SNReadOnly<LaneSide> _laneSide = null;
        [SerializeField] [ReadOnly] private Vehicle _vehicle = null;
        [SerializeField] private SNReadOnly<int> _laneIndex = null;
        [SerializeField] private SNReadOnly<float> _distanceToPrevNode = null;

        [Header("Related Road Node info")]
        [SerializeField] private SNReadOnly<RoadNodeType> _type = null;
        [SerializeField] [ReadOnly] private Intersection _intersection = null;
        [SerializeField] [ReadOnly] private TrafficLight _trafficLight = null;
        [SerializeField] private SNReadOnly<Vector3> _edgeEndPosition = null;

        protected override void SetInfoFromReference(LaneNode _laneNode)
        {
            _index = _laneNode.Index;
            _roadNode = _laneNode.RoadNode;
            _laneSide = _laneNode.LaneSide;
            _vehicle = _laneNode.Vehicle;
            _laneIndex = _laneNode.LaneIndex;
            _distanceToPrevNode = _laneNode.DistanceToPrevNode;

            _type = _laneNode.Type;
            _intersection = _roadNode.Intersection;
            _trafficLight = _roadNode.TrafficLight;
            _edgeEndPosition = _laneNode.GetNavigationEdge()?.EndNavigationNode.RoadNode.Position;
        }
    }
}