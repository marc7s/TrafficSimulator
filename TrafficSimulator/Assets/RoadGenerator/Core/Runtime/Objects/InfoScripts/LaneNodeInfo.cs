using UnityEngine;
using CustomProperties;
using DataModel;

namespace RoadGenerator
{
    public class LaneNodeInfo : InfoScript<LaneNode>
    {
        [SerializeField] [ReadOnly] private RoadNode _roadNode = null;
        [SerializeField] private SNReadOnly<LaneSide> _laneSide = null;
        [SerializeField] [ReadOnly] private Vehicle _vehicle = null;
        [SerializeField] private SNReadOnly<int> _index = null;
        [SerializeField] private SNReadOnly<float> _distanceToPrevNode = null;

        [Header("Related Road Node info")]
        [SerializeField] private SNReadOnly<RoadNodeType> _type = null;

        protected override void SetInfoFromReference(LaneNode _laneNode)
        {
            _roadNode = _laneNode.RoadNode;
            _laneSide = _laneNode.LaneSide;
            _vehicle = _laneNode.Vehicle;
            _index = _laneNode.Index;
            _distanceToPrevNode = _laneNode.DistanceToPrevNode;

            _type = _laneNode.Type;
        }
    }
}