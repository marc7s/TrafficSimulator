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
        
        // Is used to control whether the POI is translated to the side of the road at the RoadNode 
        protected bool _moveToRoadNode = true;
        
        // Is used to control whether the RoadNode is calculated according to the DistanceAlongRoad or set manually
        protected bool _useDistanceAlongRoad = true;

        // Is used to determine if the POI should assign the size itself or use a provided custom size
        protected bool _useCustomSize = false;

        // Getters
        public bool MoveToRoadNode => _moveToRoadNode;
        public bool UseDistanceAlongRoad => _useDistanceAlongRoad;

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

        /// <summary> Is used to manually set the POI RoadNode through scripts. This will override the `DistanceAlongRoad` </summary>
        /// <param> name="roadNode"> The RoadNode the POI is related to </param>
        /// <param name="moveToRoadNode"> Determines if the POI should be translated to the road at the RoadNode. If `false` the POI will not be moved </param>
        public void SetRoadNode(RoadNode roadNode, bool moveToRoadNode)
        {
            if(roadNode == null)
            {
                Debug.LogError("Cannot set POI RoadNode to null");
                return;
            }
            RoadNode = roadNode;
            _moveToRoadNode = moveToRoadNode;
            DistanceAlongRoad = -1;
            _useDistanceAlongRoad = false;
        }

        /// <summary> Assigns a custom size that will not be overwritten by the POI </summary>
        public void SetCustomSize(Vector3 size)
        {
            Size = size;
            _useCustomSize = true;
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