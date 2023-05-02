using UnityEngine;
using RoadGenerator;

namespace POIs
{
    abstract public class POI : MonoBehaviour
    {
        [HideInInspector] public Road Road;
        [HideInInspector] public RoadNode RoadNode;
        public float DistanceAlongRoad;
        public LaneSide LaneSide = LaneSide.Primary;
        [HideInInspector] public Vector3 Size;
        
        [Header("Debug Settings")]
        public bool DrawRelatedRoadNode = false;
        [SerializeField][HideInInspector] protected GameObject _roadNodeContainer;
        protected const string ROAD_NODE_CONTAINER_NAME = "Road Node";

        public void Setup()
        {
            if(Road != null)
            {
                if(!Road.POIs.Contains(this))
                    Road.POIs.Add(this);
            }
            // Try to find the road node container if it has already been created
            foreach(Transform child in transform)
            {
                if(child.name == ROAD_NODE_CONTAINER_NAME)
                {
                    _roadNodeContainer = child.gameObject;
                    break;
                }
            }
            
            // Destroy the lane container, and with it all the previous lanes
            if(_roadNodeContainer != null)
                DestroyImmediate(_roadNodeContainer);

            // Create a new empty road node container
            _roadNodeContainer = new GameObject(ROAD_NODE_CONTAINER_NAME);
            _roadNodeContainer.AddComponent<LineRenderer>();
            _roadNodeContainer.transform.parent = transform;

            if(RoadNode != null && DrawRelatedRoadNode)
                DrawRelatedRoadNodeLine();
            
            CustomSetup();
        }

        private void DrawRelatedRoadNodeLine()
        {
            LineRenderer lineRenderer = _roadNodeContainer.GetComponent<LineRenderer>();
            lineRenderer.positionCount = 2;
            lineRenderer.SetPositions(new Vector3[] { transform.position, RoadNode.Position });
        }

        protected abstract void CustomSetup();
    }
}