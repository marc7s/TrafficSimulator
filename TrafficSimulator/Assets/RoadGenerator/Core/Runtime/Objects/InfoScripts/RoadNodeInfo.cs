using UnityEngine;
using CustomProperties;

namespace RoadGenerator
{
    public class RoadNodeInfo : InfoScript<RoadNode>
    {
        [SerializeField] private SNReadOnly<TrafficSignType> _trafficSignType = null;
        [SerializeField] [ReadOnly] private TrafficLight _trafficLight = null;
        [SerializeField] [ReadOnly] private Intersection _intersection = null;
        [SerializeField] [ReadOnly] private NavigationNodeEdge _primaryNavigationNodeEdge = null;
        [SerializeField] [ReadOnly] private NavigationNodeEdge _secondaryNavigationNodeEdge = null;
        [SerializeField] [ReadOnly] private bool _isNavigationNode = false;
        [SerializeField] private SNReadOnly<Vector3> _tangent = null;
        [SerializeField] private SNReadOnly<Vector3> _normal = null;
        [SerializeField] private SNReadOnly<RoadNodeType> _type = null;
        [SerializeField] private SNReadOnly<float> _time = null;
        [SerializeField] private SNReadOnly<float> _distanceToPrevNode = null;
        [SerializeField] private SNReadOnly<Vector3> _primaryDirectionEdgePos = null;
        [SerializeField] private SNReadOnly<Vector3> _secondaryDirectionEdgePos = null;
        protected override void SetInfoFromReference(RoadNode _roadNode)
        {
            _trafficSignType = _roadNode.TrafficSignType;
            _trafficLight = _roadNode.TrafficLight;
            _intersection = _roadNode.Intersection;
            _primaryNavigationNodeEdge = _roadNode.PrimaryNavigationNodeEdge;
            _secondaryNavigationNodeEdge = _roadNode.SecondaryNavigationNodeEdge;
            _isNavigationNode = _roadNode.IsNavigationNode;
            _tangent = _roadNode.Tangent;
            _normal = _roadNode.Normal;
            _type = _roadNode.Type;
            _time = _roadNode.Time;
            _distanceToPrevNode = _roadNode.DistanceToPrevNode;
            _primaryDirectionEdgePos = _roadNode.PrimaryNavigationNodeEdge?.EndNavigationNode.RoadNode.Position;
            _secondaryDirectionEdgePos = _roadNode.SecondaryNavigationNodeEdge?.EndNavigationNode.RoadNode.Position;
        }
    }
}