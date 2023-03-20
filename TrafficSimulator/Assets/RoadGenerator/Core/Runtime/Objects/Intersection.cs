//#define DEBUG_INTERSECTION

using UnityEngine;
using System.Collections.Generic;
using System;
using System.Linq;
using CustomProperties;

namespace RoadGenerator
{
    public enum IntersectionType
    {
        ThreeWayIntersectionAtStart,
        ThreeWayIntersectionAtEnd,
        FourWayIntersection
    }

    public enum FlowType
    {
        TrafficLights,
        StopSigns
    }

    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    [ExecuteInEditMode()]
    [Serializable]
    public class Intersection : MonoBehaviour
    {
        [HideInInspector] public GameObject IntersectionObject;
        [HideInInspector] public RoadSystem RoadSystem;
        [HideInInspector] public Vector3 IntersectionPosition;
        [HideInInspector] public Road Road1;
        [HideInInspector] public Road Road2;
        [HideInInspector] public PathCreator Road1PathCreator;
        [HideInInspector] public PathCreator Road2PathCreator;
        [HideInInspector] public Vector3 Road1AnchorPoint1;
        [HideInInspector] public Vector3 Road1AnchorPoint2;
        [HideInInspector] public Vector3 Road2AnchorPoint1;
        [HideInInspector] public Vector3 Road2AnchorPoint2;
        [HideInInspector] public NavigationNodeEdge Road1AnchorPoint1NavigationEdge;
        [HideInInspector] public NavigationNodeEdge Road1AnchorPoint2NavigationEdge;
        [HideInInspector] public NavigationNodeEdge Road2AnchorPoint1NavigationEdge;
        [HideInInspector] public NavigationNodeEdge Road2AnchorPoint2NavigationEdge;
        [HideInInspector] private Dictionary<string, List<LaneNode>> _laneNodeFromNavigationNodeEdge = new Dictionary<string, List<LaneNode>>();
        [HideInInspector] private Dictionary<string, RoadNode> _intersectionGuideRoadNodes = new Dictionary<string, RoadNode>();
        [HideInInspector] private Dictionary<string, LaneNode> _intersectionEntryNodes = new Dictionary<string, LaneNode>();
        [HideInInspector] private Dictionary<string, LaneNode> _intersectionExitNodes = new Dictionary<string, LaneNode>();
        [HideInInspector] private Dictionary<(string, string), GuideNode> _intersectionGuidePaths = new Dictionary<(string, string), GuideNode>();
        [HideInInspector] public IntersectionType Type;
        [ReadOnly] public string ID;

        [Header("Connections")]
        [SerializeField] private GameObject _guideRoadNodePrefab;
        [SerializeField] private GameObject _guideLaneNodePrefab;

        [Header("Intersection settings")]
        [SerializeField][Range(0, 0.8f)] private float _stretchFactor = 0;
        [SerializeField] private FlowType _flowType = FlowType.TrafficLights;

        [Header ("Material settings")]
        [SerializeField] private Material _material;
        [SerializeField] private Material _bottomMaterial;

        [Header ("Debug settings")]
        [SerializeField] private bool _drawGuideNodes = false;

        private float _thickness;
        private MeshFilter _meshFilter;
        private MeshRenderer _meshRenderer;

        private GameObject _trafficLightController;
        private GameObject _stopSignController;
        private GameObject _flowContainer;

        private List<RoadNode> _twoJunctionNodeRoad = new List<RoadNode>();
        private List<RoadNode> _oneJunctionNodeRoad = new List<RoadNode>();

        private Mesh _mesh;
        [SerializeField][HideInInspector] private GameObject _guideNodeContainer;
        [HideInInspector] public const float IntersectionLength = 20f;
        [HideInInspector] public const float IntersectionBoundsLengthMultiplier = 1.2f;
        private const string GUIDE_NODE_CONTAINER_NAME = "Guide Nodes";

#if DEBUG_INTERSECTION
            public void PrintRoadNodes(RoadNode start)
            {
                RoadNode curr = start;
                int numberOfDefault = 0;
                while(curr != null)
                {
                    if(curr.Type == RoadNodeType.Default)
                        numberOfDefault++;
                    else 
                    {
                        if(numberOfDefault > 0)
                        {
                            Debug.Log("[" + numberOfDefault + "x] Default");
                            numberOfDefault = 0;
                        }
                        Debug.Log(curr.Type + " at " + curr.Position);
                    }
                    
                    curr = curr.Next;
                }
            }
#endif

        void Awake()
        {
            IntersectionObject = gameObject;
            _flowContainer = IntersectionObject.transform.Find("FlowContainer")?.gameObject;
        }

        public void UpdateMesh()
        {
            // Set the thickness of the intersection
            _thickness = Road1.Thickness;
            
            AssignMeshComponents();
            AssignMaterials();
            CreateIntersectionMesh();
            // If intersection doesn't have a container, create one
            if(_flowContainer != null)
                DestroyImmediate(_flowContainer);
            CreateIntersectionContainer();
            if (_flowType == FlowType.TrafficLights)
            {
                CreateTrafficLightController();
                AssignTrafficLights();
            }
            else if (_flowType == FlowType.StopSigns)
            {
                CreateStopSignController();
                AssignStopSigns();
            }
            OffsetSigns();
            ShowGuideNodes();

            gameObject.GetComponent<MeshCollider>().sharedMesh = _mesh;
        }

        /// <summary> Returns a list of all RoadNodes that are of type `JunctionEdge` or an intersection. This is because for 3-way intersections, the intersection node are used as an anchor </summary>
        private List<RoadNode> GetJunctionNodes(Road road)
        {
            RoadNode curr = road.StartNode;
            List<RoadNode> junctionNodes = new List<RoadNode>();
            
            while(curr != null)
            {
                if(curr.Type == RoadNodeType.JunctionEdge || curr.IsIntersection())
                    junctionNodes.Add(curr);
                
                curr = curr.Next;
            }
            return junctionNodes;
        }

        // Creates a gameobject container for the intersection to keep track of all assets in the intersection
        private void CreateIntersectionContainer()
        {
            _flowContainer = new GameObject("FlowContainer");
            _flowContainer.transform.parent = IntersectionObject.transform;
        }

        // Creates a gameobject container to control the intersections traffic lights
        private void CreateTrafficLightController()
        {
            _trafficLightController = Instantiate(RoadSystem.DefaultTrafficLightControllerPrefab, Vector3.zero, Quaternion.identity);
            _trafficLightController.transform.parent = _flowContainer.transform;
        }

        private void CreateStopSignController()
        {
            _stopSignController = new GameObject("StopSignController");
            _stopSignController.transform.parent = _flowContainer.transform;
        }

        private TrafficLight SpawnTrafficLight(Vector3 position, Quaternion rotation)
        {
            GameObject trafficLight = Instantiate(RoadSystem.DefaultTrafficLightPrefab, position, rotation);
            trafficLight.transform.parent = _trafficLightController.transform;
            return trafficLight.GetComponent<TrafficLight>();
        }

        private void SpawnStopSign(Vector3 position, Quaternion rotation)
        {
            GameObject stopSign = Instantiate(RoadSystem.DefaultStopSignPrefab, position, rotation);
            stopSign.transform.parent = _stopSignController.transform;
        }

        private void GetIntersectionNodes()
        {
            List<RoadNode> road1IntersectionNodes = new List<RoadNode>();
            List<RoadNode> road2IntersectionNodes = new List<RoadNode>();

            RoadNode road1Node = Road1.StartNode;
            RoadNode road2Node = Road2.StartNode;

            //Find the edge nodes of the roads
            while (road1Node != null)
            {
                if (road1Node.Type == RoadNodeType.JunctionEdge && (road1Node.Position == Road1AnchorPoint1 || road1Node.Position == Road1AnchorPoint2))
                    road1IntersectionNodes.Add(road1Node);
                road1Node = road1Node.Next;
            }
            while (road2Node != null)
            {
                if (road2Node.Type == RoadNodeType.JunctionEdge && (road2Node.Position == Road2AnchorPoint1 || road2Node.Position == Road2AnchorPoint2))
                    road2IntersectionNodes.Add(road2Node);
                road2Node = road2Node.Next;
            }

            if (road1IntersectionNodes.Count > road2IntersectionNodes.Count)
            {
                _twoJunctionNodeRoad = road1IntersectionNodes;
                _oneJunctionNodeRoad = road2IntersectionNodes;
            } else
            {
                _twoJunctionNodeRoad = road2IntersectionNodes;
                _oneJunctionNodeRoad = road1IntersectionNodes;
            }
        }

        private void AssignStopSigns()
        {
            GetIntersectionNodes();

            if (Type == IntersectionType.ThreeWayIntersectionAtStart)
            {
                SpawnStopSign(_twoJunctionNodeRoad[0].Position, _twoJunctionNodeRoad[0].Rotation);
                SpawnStopSign(_twoJunctionNodeRoad[1].Position, _twoJunctionNodeRoad[1].Rotation * Quaternion.Euler(0, 180, 0));

                SpawnStopSign(_oneJunctionNodeRoad[0].Position, _oneJunctionNodeRoad[0].Rotation * Quaternion.Euler(0, 180, 0));
            }
            else if (Type == IntersectionType.ThreeWayIntersectionAtEnd)
            {
                SpawnStopSign(_twoJunctionNodeRoad[0].Position, _twoJunctionNodeRoad[0].Rotation);
                SpawnStopSign(_twoJunctionNodeRoad[1].Position, _twoJunctionNodeRoad[1].Rotation * Quaternion.Euler(0, 180, 0));

                SpawnStopSign(_oneJunctionNodeRoad[0].Position, _oneJunctionNodeRoad[0].Rotation);
            }
            else if (Type == IntersectionType.FourWayIntersection)
            {
                SpawnStopSign(_twoJunctionNodeRoad[0].Position, _twoJunctionNodeRoad[0].Rotation);
                SpawnStopSign(_twoJunctionNodeRoad[1].Position, _twoJunctionNodeRoad[1].Rotation * Quaternion.Euler(0, 180, 0));

                SpawnStopSign(_oneJunctionNodeRoad[0].Position, _oneJunctionNodeRoad[0].Rotation);
                SpawnStopSign(_oneJunctionNodeRoad[1].Position, _oneJunctionNodeRoad[1].Rotation * Quaternion.Euler(0, 180, 0));
            }
        }

        // Finds intersections junction nodes and assigns traffic lights to them
        private void AssignTrafficLights()
        {
            GetIntersectionNodes();

            List<TrafficLight> trafficLightsGroup1 = _trafficLightController.GetComponent<TrafficLightController>().TrafficLightsGroup1;
            List<TrafficLight> trafficLightsGroup2 = _trafficLightController.GetComponent<TrafficLightController>().TrafficLightsGroup2;

            if (Type == IntersectionType.ThreeWayIntersectionAtStart)
            {
                trafficLightsGroup1.Add(SpawnTrafficLight(_twoJunctionNodeRoad[0].Position, _twoJunctionNodeRoad[0].Rotation));
                trafficLightsGroup1.Add(SpawnTrafficLight(_twoJunctionNodeRoad[1].Position, _twoJunctionNodeRoad[1].Rotation * Quaternion.Euler(0, 180, 0)));

                trafficLightsGroup2.Add(SpawnTrafficLight(_oneJunctionNodeRoad[0].Position, _oneJunctionNodeRoad[0].Rotation * Quaternion.Euler(0, 180, 0)));
            }
            else if (Type == IntersectionType.ThreeWayIntersectionAtEnd)
            {
                trafficLightsGroup1.Add(SpawnTrafficLight(_twoJunctionNodeRoad[0].Position, _twoJunctionNodeRoad[0].Rotation));
                trafficLightsGroup1.Add(SpawnTrafficLight(_twoJunctionNodeRoad[1].Position, _twoJunctionNodeRoad[1].Rotation * Quaternion.Euler(0, 180, 0)));

                trafficLightsGroup2.Add(SpawnTrafficLight(_oneJunctionNodeRoad[0].Position, _oneJunctionNodeRoad[0].Rotation));
            }

            // If its a 4-way intersection, spawn a traffic light at each junction node
            else if (Type == IntersectionType.FourWayIntersection)
            {
                trafficLightsGroup1.Add(SpawnTrafficLight(_twoJunctionNodeRoad[0].Position, _twoJunctionNodeRoad[0].Rotation));
                trafficLightsGroup1.Add(SpawnTrafficLight(_twoJunctionNodeRoad[1].Position, _twoJunctionNodeRoad[1].Rotation * Quaternion.Euler(0, 180, 0)));

                trafficLightsGroup2.Add(SpawnTrafficLight(_oneJunctionNodeRoad[0].Position, _oneJunctionNodeRoad[0].Rotation));
                trafficLightsGroup2.Add(SpawnTrafficLight(_oneJunctionNodeRoad[1].Position, _oneJunctionNodeRoad[1].Rotation * Quaternion.Euler(0, 180, 0)));
            }
        }

        private void OffsetSigns()
        {
            if(_flowType == FlowType.TrafficLights)
            {
                foreach (Transform child in _trafficLightController.transform)
                {
                    child.position += (Road1.LaneCount / 2) * child.right * Road1.LaneWidth;
                }
            }
            else if(_flowType == FlowType.StopSigns)
            {
                foreach (Transform child in _stopSignController.transform)
                {
                    child.position += (Road1.LaneCount / 2) * child.right * Road1.LaneWidth;
                }
            }
        }

        private void CreateIntersectionMesh()
        {
            Road1.UpdateRoadNodes();
            Road2.UpdateRoadNodes();

#if DEBUG_INTERSECTION            
            Debug.Log("----------- Road 1 nodes -----------");
            PrintRoadNodes(Road1.StartNode);
            Debug.Log("------------------------------------");

            Debug.Log("----------- Road 2 nodes -----------");
            PrintRoadNodes(Road2.StartNode);
            Debug.Log("------------------------------------");
#endif            
            List<Vector3> verts = new List<Vector3>();
            List<Vector2> uvs = new List<Vector2>();
            List<Vector3> normals = new List<Vector3>();

            // The road nodes related to each anchor point
            RoadNode road1Anchor1Node = null;
            RoadNode road1Anchor2Node = null;
            RoadNode road2Anchor1Node = null;
            RoadNode road2Anchor2Node = null;

            // The inaccuracy that determines if an anchor point corresponds to a junction node
            float eps = 3f;

            // Go through all junction nodes in Road1 to find the anchor points
            foreach(RoadNode junctionNode in GetJunctionNodes(Road1))
            {
                Vector3 jpos = junctionNode.Position;
                
                if(Vector3.Distance(jpos, Road1AnchorPoint1) < eps)
                    road1Anchor1Node = junctionNode;
                else if(Vector3.Distance(jpos, Road1AnchorPoint2) < eps)
                    road1Anchor2Node = junctionNode;
            }
            

            // Go through all junction nodes in Road2 to find the anchor points
            foreach(RoadNode junctionNode in GetJunctionNodes(Road2))
            {
                Vector3 jpos = junctionNode.Position;
                
                if(Vector3.Distance(jpos, Road2AnchorPoint1) < eps)
                    road2Anchor1Node = junctionNode;
                else if(Vector3.Distance(jpos, Road2AnchorPoint2) < eps)
                    road2Anchor2Node = junctionNode;
            }


            // Make sure road2Anchor1Node is the only anchor point for road 2 when there is a three way intersection
            if(Type == IntersectionType.ThreeWayIntersectionAtStart || Type == IntersectionType.ThreeWayIntersectionAtEnd)
            {
                if(road2Anchor1Node == null)
                    road2Anchor1Node = road2Anchor2Node;
            }

            // Calculate road directions in order to determine if the anchor points need to be swapped
            Vector3 road1Direction = road1Anchor2Node.Position - road1Anchor1Node.Position;
            Vector3 road1ToRoad2Direction = road2Anchor1Node.Position - road1Anchor1Node.Position;
            float roadAngle = Vector3.SignedAngle(road1Direction, road1ToRoad2Direction, Vector3.up);
            
            // Is used to flip left/right
            int directionCoefficient = 1;
            
            if(Type == IntersectionType.FourWayIntersection)
            {
                // Check the angle between the two roads to determine if the anchor points need to be swapped
                if(roadAngle < 0)
                {
                    // Swap the anchor nodes and extend the mesh to fill in the gap in road 2
                    RoadNode temp = road2Anchor1Node;
                    road2Anchor1Node = road2Anchor2Node;
                    road2Anchor2Node = temp;
                    
                    directionCoefficient = -1;
                }
            }
            else if(Type == IntersectionType.ThreeWayIntersectionAtStart || Type == IntersectionType.ThreeWayIntersectionAtEnd)
            {
                if(roadAngle < 0)
                    directionCoefficient = -1;
            }

            List<int> topTris = new List<int>();

            float road1HalfWidth = Road1.LaneWidth * (int)Road1.LaneAmount;
            float road2HalfWidth = Road2.LaneWidth * (int)Road2.LaneAmount;

            if(Type == IntersectionType.ThreeWayIntersectionAtStart || Type == IntersectionType.ThreeWayIntersectionAtEnd)
            {
                
                // Swap the anchor nodes if the main road is going in the opposite direction
                if(directionCoefficient > 0)
                {
                    RoadNode temp = road1Anchor1Node;
                    road1Anchor1Node = road1Anchor2Node;
                    road1Anchor2Node = temp;
                }
                // Coefficent that is used to flip nodes if the intersection is at the start instead of at the end of the road
                int bottomCoefficient = 1;
                if(Type == IntersectionType.ThreeWayIntersectionAtStart)
                    bottomCoefficient = -1;

                Vector3 road2BottomLeft = road2Anchor1Node.Position - bottomCoefficient * road2Anchor1Node.Normal * road2HalfWidth;
                Vector3 road2BottomRight = road2Anchor1Node.Position + bottomCoefficient * road2Anchor1Node.Normal * road2HalfWidth;
                
                Vector3 road1BottomLeft = road1Anchor2Node.Position + directionCoefficient * road1Anchor2Node.Normal * road1HalfWidth;
                Vector3 road1BottomRight = road1Anchor2Node.Position - directionCoefficient * road1Anchor2Node.Normal * road1HalfWidth;

                Vector3 road1TopLeft = road1Anchor1Node.Position + directionCoefficient * road1Anchor1Node.Normal * road1HalfWidth;
                Vector3 road1TopRight = road1Anchor1Node.Position - directionCoefficient * road1Anchor1Node.Normal * road1HalfWidth;

                // Calculate the direction that the top mid left point should be offset from the bottom left point
                Vector3 tmlDir = (road1TopLeft - road1BottomLeft).normalized;
                Vector3 topMidLeft = road1BottomLeft + tmlDir * road2HalfWidth * 2;
                Vector3 topMidRight = road1TopLeft - tmlDir * road2HalfWidth * 2;

                Vector3 road1Dir = (road1Anchor1Node.Position - road1Anchor2Node.Position).normalized;
                Vector3 road2Dir = bottomCoefficient * (road2Anchor1Node.Position - IntersectionPosition).normalized;

                (Vector3, Vector3) road1BottomRightLine = (road1BottomRight, road1Dir);
                (Vector3, Vector3) road1TopRightLine = (road1TopRight, -road1Dir);
                
                (Vector3, Vector3) road2BottomLeftLine = (road2BottomLeft, road2Dir);
                (Vector3, Vector3) road2BottomRightLine = (road2BottomRight, road2Dir);

                // Mid points
                Vector3 i1 = GetMidPointCorner(road2BottomLeftLine, road1BottomRightLine);
                Vector3 i2 = topMidLeft;
                Vector3 i3 = topMidRight;
                Vector3 i4 = GetMidPointCorner(road2BottomRightLine, road1TopRightLine);
                
                // Road edge points
                Vector3 i5 = road2BottomLeft;
                Vector3 i6 = road1BottomRight;
                Vector3 i7 = road1BottomLeft;
                Vector3 i8 = road1TopLeft;
                Vector3 i9 = road1TopRight;
                Vector3 i10 = road2BottomRight;


                // Mid
                verts.AddRange(GetRectVerts(i1, i2, i3, i4));
                
                // Bottom
                verts.AddRange(GetRectVerts(i5, i1, i4, i10));
                
                // Left
                verts.AddRange(GetRectVerts(i6, i7, i2, i1));

                // Right
                verts.AddRange(GetRectVerts(i4, i3, i8, i9));
            }
            else if(Type == IntersectionType.FourWayIntersection)
            {
                Vector3 road1BottomLeft = road1Anchor2Node.Position - road1Anchor2Node.Normal * road1HalfWidth;
                Vector3 road1BottomRight = road1Anchor2Node.Position + road1Anchor2Node.Normal * road1HalfWidth;

                Vector3 road1TopLeft = road1Anchor1Node.Position - road1Anchor1Node.Normal * road1HalfWidth;
                Vector3 road1TopRight = road1Anchor1Node.Position + road1Anchor1Node.Normal * road1HalfWidth;
                
                Vector3 road2BottomLeft = road2Anchor1Node.Position + directionCoefficient * road2Anchor1Node.Normal * road2HalfWidth;
                Vector3 road2BottomRight = road2Anchor1Node.Position - directionCoefficient * road2Anchor1Node.Normal * road2HalfWidth;

                Vector3 road2TopLeft = road2Anchor2Node.Position + directionCoefficient * road2Anchor2Node.Normal * road2HalfWidth;
                Vector3 road2TopRight = road2Anchor2Node.Position - directionCoefficient * road2Anchor2Node.Normal * road2HalfWidth;

                Vector3 road1Dir = (road1Anchor1Node.Position - road1Anchor2Node.Position).normalized;
                Vector3 road2Dir = (road2Anchor2Node.Position - road2Anchor1Node.Position).normalized;

                (Vector3, Vector3) road1BottomLeftLine = (road1BottomLeft, road1Dir);
                (Vector3, Vector3) road1BottomRightLine = (road1BottomRight, road1Dir);
                (Vector3, Vector3) road1TopLeftLine = (road1TopLeft, -road1Dir);
                (Vector3, Vector3) road1TopRightLine = (road1TopRight, -road1Dir);

                (Vector3, Vector3) road2BottomLeftLine = (road2BottomLeft, road2Dir);
                (Vector3, Vector3) road2BottomRightLine = (road2BottomRight, road2Dir);
                (Vector3, Vector3) road2TopLeftLine = (road2TopLeft, -road2Dir);
                (Vector3, Vector3) road2TopRightLine = (road2TopRight, -road2Dir);

                // Mid points
                Vector3 i1 = GetMidPointCorner(road1BottomLeftLine, road2BottomRightLine);
                Vector3 i2 = GetMidPointCorner(road1TopLeftLine, road2BottomLeftLine);
                Vector3 i3 = GetMidPointCorner(road2TopLeftLine, road1TopRightLine);
                Vector3 i4 = GetMidPointCorner(road2TopRightLine, road1BottomRightLine);

                // Road edge points
                Vector3 i5 = road1BottomLeft;
                Vector3 i6 = road2BottomRight;
                Vector3 i7 = road2BottomLeft;
                Vector3 i8 = road1TopLeft;
                Vector3 i9 = road1TopRight;
                Vector3 i10 = road2TopLeft;
                Vector3 i11 = road2TopRight;
                Vector3 i12 = road1BottomRight;
                
                // Mid
                verts.AddRange(GetRectVerts(i1, i2, i3, i4));

                // Bottom
                verts.AddRange(GetRectVerts(i5, i1, i4, i12));

                // Left
                verts.AddRange(GetRectVerts(i6, i7, i2, i1));

                // Top
                verts.AddRange(GetRectVerts(i2, i8, i9, i3));

                // Right
                verts.AddRange(GetRectVerts(i4, i3, i10, i11)); 
            }

            
            // The vertices are already mapped in the correct order, so we simply create an incrementing list
            for (int i = 0; i < verts.Count; i++)
            {
                topTris.Add(i);
            }

            _mesh.Clear();
            _mesh.vertices = verts.ToArray();
            _mesh.uv = uvs.ToArray();
            _mesh.normals = normals.ToArray();
            _mesh.subMeshCount = 2;

            _mesh.SetTriangles(topTris.ToArray(), 0);
        }

        /// <summary> Returns the position of a mid point corner used for creating the intersections. Uses line intersection with a fallback for lerps </summary>
        private Vector3 GetMidPointCorner((Vector3 position, Vector3 direction) main, (Vector3 position, Vector3 direction) projectionTarget)
        {
            // Normalize the directions. It should not matter for the algorithm,
            // but if these vectors would be abnormally large the intersection point accuracy could be compromised, so this is mostly here for sanity
            main.direction = main.direction.normalized;
            projectionTarget.direction = projectionTarget.direction.normalized;

            // Project the a direction onto the 2D plane as the intersection algorithm works in 2D
            Vector2 a2Ddir = new Vector2(main.direction.x, main.direction.z);

            // Project the b position and direction onto the 2D plane as the intersection algorithm works in 2D
            Vector2 b2DPos = new Vector2(projectionTarget.position.x, projectionTarget.position.z);
            Vector2 b2Ddir = new Vector2(projectionTarget.direction.x, projectionTarget.direction.z);

            // Calculate the intersection coefficient for b, see the documentation for an explanation
            float coefB = (main.direction.x * (projectionTarget.position.z - main.position.z) - main.direction.z * (projectionTarget.position.x - main.position.x)) 
                / (main.direction.z * projectionTarget.direction.x - main.direction.x * projectionTarget.direction.z);

            // Calculate the intersection point as the b point offset by the intersection coefficient for b in the direction of b
            Vector2 intersectPos2D = b2DPos + coefB * b2Ddir;

            // If there is no intersection (directions are parallell), or the intersection is too far away, use a default method instead
            if(a2Ddir == b2Ddir || Vector2.Distance(intersectPos2D, b2DPos) > Vector3.Distance(main.position, projectionTarget.position))
            {
                Vector3 midPoint = Vector3.Lerp(main.position, projectionTarget.position, 0.5f);
                return Vector3.Lerp(midPoint, IntersectionPosition, _stretchFactor);
            }
            else
            {
                // Project the intersection position back to the 3D plane, with the y coordinate set to the average y coordinate of the two points
                Vector3 intersectPos = new Vector3(intersectPos2D.x, Vector3.Lerp(main.position, projectionTarget.position, 0.5f).y, intersectPos2D.y);
                
                // Return the intersection position, offset in the projection target position by the stretch factor to apply the stretch
                return Vector3.Lerp(intersectPos, projectionTarget.position, _stretchFactor);
            }
        }

        /// <summary>Returns a list of vertices that make up a rectangle</summary>
        private List<Vector3> GetRectVerts(Vector3 bottomLeft, Vector3 topLeft, Vector3 topRight, Vector3 bottomRight)
        {
            return new List<Vector3>(){
                bottomLeft, topLeft, topRight,
                bottomLeft, topRight, bottomRight
            };
        }
        
        private void AssignMeshComponents() 
        {
            // Let the road itself hold the mesh
            if (IntersectionObject == null) 
            {
                IntersectionObject = gameObject;
            }

            IntersectionObject.transform.rotation = Quaternion.identity;
            IntersectionObject.transform.position = Vector3.zero;
            IntersectionObject.transform.localScale = Vector3.one;

            // Ensure mesh renderer and filter components are assigned
            if (!IntersectionObject.gameObject.GetComponent<MeshFilter>()) 
            {
                IntersectionObject.gameObject.AddComponent<MeshFilter>();
            }
            if (!IntersectionObject.GetComponent<MeshRenderer>()) 
            {
                IntersectionObject.gameObject.AddComponent<MeshRenderer>();
            }

            _meshRenderer = IntersectionObject.GetComponent<MeshRenderer>();
            _meshFilter = IntersectionObject.GetComponent<MeshFilter>();
            
            // Create a new mesh if one does not already exist
            if (_mesh == null) 
            {
                _mesh = new Mesh();
            }
            _meshFilter.sharedMesh = _mesh;
        }

        private void AssignMaterials() {
            if (_material != null && _bottomMaterial != null) 
            {              
                // Create an array of materials for the mesh renderer
                // It will hold a bottom material, a side material and a material for each lane
                Material[] materials = new Material[]{ _material, _bottomMaterial };
                
                // Assign the materials to the mesh renderer
                _meshRenderer.sharedMaterials = materials;
            }
        }

        public void ShowGuideNodes()
        {
            if(_guideNodeContainer == null)
            {
                // Try to find the lane container if it has already been created
                foreach(Transform child in transform)
                {
                    if(child.name == GUIDE_NODE_CONTAINER_NAME)
                    {
                        _guideNodeContainer = child.gameObject;
                        break;
                    }
                }
            }

            // Destroy the lane container, and with it all the previous lanes
            if(_guideNodeContainer != null)
                DestroyImmediate(_guideNodeContainer);

            // Create a new empty lane container
            _guideNodeContainer = new GameObject(GUIDE_NODE_CONTAINER_NAME);
            _guideNodeContainer.transform.parent = transform;

            // Draw the lines if the setting is enabled
            if(_drawGuideNodes)
            {
                List<LaneNode> guideNodes = new List<LaneNode>();
                guideNodes.AddRange(_intersectionEntryNodes.Values);
                guideNodes.AddRange(_intersectionExitNodes.Values);

                // Draw the lane nodes
                foreach(LaneNode start in guideNodes)
                {
                    LaneNode curr = start;
                    int i = 0;
                    while(curr != null && curr.Type == RoadNodeType.IntersectionGuide)
                    {
                        GameObject roadNodeObject = Instantiate(_guideLaneNodePrefab, curr.Position, curr.Rotation, _guideNodeContainer.transform);
                        roadNodeObject.name = i + " " + curr.Type;

                        curr = curr.Next;
                        i++;
                    }
                }

                // Draw the road nodes
                foreach(RoadNode start in _intersectionGuideRoadNodes.Values)
                {
                    RoadNode curr = start;
                    int i = 0;
                    while(curr != null && curr.Type == RoadNodeType.IntersectionGuide)
                    {
                        GameObject roadNodeObject = Instantiate(_guideRoadNodePrefab, curr.Position, curr.Rotation, _guideNodeContainer.transform);
                        roadNodeObject.name = i + " " + curr.Type;

                        curr = curr.Next;
                        i++;
                    }
                }
            }
        }

        // If it is the first node, add a list with the node. Otherwise, append the node to the existing list
        private void AddLaneNodeFromNavigationNodeEdge(NavigationNodeEdge nodeEdge, LaneNode laneNode)
        {
            if(!_laneNodeFromNavigationNodeEdge.ContainsKey(nodeEdge.ID))
                _laneNodeFromNavigationNodeEdge.Add(nodeEdge.ID, new List<LaneNode>(new LaneNode[]{ laneNode }));
            else
                _laneNodeFromNavigationNodeEdge[nodeEdge.ID].Add(laneNode);
        }

        /// <summary> Maps the navigation for the intersection </summary>
        public void MapIntersectionNavigation()
        {
            // Map the lane node to take in order to get to the navigation node edge
            _laneNodeFromNavigationNodeEdge.Clear();
            List<Lane> lanes = new List<Lane>();
            
            List<LaneNode> entryNodes = new List<LaneNode>();
            List<LaneNode> exitNodes = new List<LaneNode>();

            _intersectionEntryNodes.Clear();
            _intersectionExitNodes.Clear();
            _intersectionGuideRoadNodes.Clear();
            
            lanes.AddRange(Road1.Lanes);
            lanes.AddRange(Road2.Lanes);
            foreach (Lane lane in lanes)
            {
                LaneNode currentNode = lane.StartNode;
                while(currentNode != null)
                {
                    if (currentNode.Type != RoadNodeType.JunctionEdge)
                    {
                        currentNode = currentNode.Next;
                        continue;
                    }

                    bool isEdgePointingToIntersection = currentNode.GetNavigationEdge().EndNavigationNode.RoadNode.Position == IntersectionPosition;
                    // Since we want to map the nodes that point out of the intersection, we skip nodes that point towards the intersection 
                    if (isEdgePointingToIntersection)
                    {
                        // Add entry nodes if the current node is related to this intersection
                        if(currentNode.RoadNode.Intersection == this)
                        {
                            // Add the node to the list of entry nodes
                            entryNodes.Add(currentNode);

                            // Create entry intersection lane nodes for navigation in the intersection
                            CreateEntryIntersectionLaneNodes(currentNode, currentNode.Next);
                        }
                            
                        currentNode = currentNode.Next;
                        continue;
                    }

                    // Add exit nodes if the current node is related to this intersection
                    if(currentNode.RoadNode.Intersection == this)
                    {
                        // Since it was not an entry node, it must be an exit node, so add it to the exit node list
                        exitNodes.Add(currentNode);

                        // Create exit intersection lane nodes for navigation in the intersection
                        CreateExitIntersectionLaneNodes(currentNode, currentNode.Prev);
                    }

                    // If the node is an anchor point, we map the edge going out of the intersection to the node
                    if (currentNode.RoadNode.Position == Road1AnchorPoint1)
                        AddLaneNodeFromNavigationNodeEdge(Road1AnchorPoint1NavigationEdge, currentNode);
                    if (currentNode.RoadNode.Position == Road1AnchorPoint2)
                        AddLaneNodeFromNavigationNodeEdge(Road1AnchorPoint2NavigationEdge, currentNode);
                    if (currentNode.RoadNode.Position == Road2AnchorPoint1)
                        AddLaneNodeFromNavigationNodeEdge(Road2AnchorPoint1NavigationEdge, currentNode);
                    
                    // If the intersection is a three way intersection, the second anchor point does not exist
                    if (!IsThreeWayIntersection() && currentNode.RoadNode.Position == Road2AnchorPoint2)
                        AddLaneNodeFromNavigationNodeEdge(Road2AnchorPoint2NavigationEdge, currentNode);

                    currentNode = currentNode.Next;
                }
            }

            // Precompute all the guide paths and store them
            foreach(LaneNode entry in entryNodes)
            {
                foreach(LaneNode exit in exitNodes)
                {
                    if(_intersectionEntryNodes.ContainsKey(entry.ID) && _intersectionExitNodes.ContainsKey(exit.ID))
                        _intersectionGuidePaths.Add((entry.ID, exit.ID), GetGuidePath(entry, exit));
                }
            }
        }
        
        private void CreateEntryIntersectionLaneNodes(LaneNode start, LaneNode intersectionNode)
        {
            RoadNode generatedRoadNodes = FetchOrGenerateRoadNodes(start.RoadNode, intersectionNode.RoadNode);

            LaneNode entry = CreateLaneNodes(start, generatedRoadNodes, true);
            
            _intersectionEntryNodes.Add(start.ID, entry);
        }

        private void CreateExitIntersectionLaneNodes(LaneNode node, LaneNode intersectionNode)
        {
            RoadNode generatedRoadNodes = FetchOrGenerateRoadNodes(node.RoadNode, intersectionNode.RoadNode);
            
            LaneNode exit = CreateLaneNodes(intersectionNode, generatedRoadNodes, false);
            
            _intersectionExitNodes.Add(node.ID, exit.Reverse());
        }

        private LaneNode CreateLaneNodes(LaneNode start, RoadNode roadNode, bool isPrimary)
        {
            float laneNodeOffset = Vector3.Distance(start.RoadNode.Position, start.Position);
            int laneNodeDirection = isPrimary ? 1 : -1;
            LaneSide laneSide = start.LaneSide;
            // Important: In order to get the correct direction of the lane nodes, we need to reverse the road nodes if the lane is secondary
            // Since reverse creates copies of the nodes, this means that logic that relies on the road nodes being the same instance between entry and exit will not work
            RoadNode currRoadNode = roadNode;
            
            LaneNode curr = null;
            LaneNode prev = null;
            while(currRoadNode != null)
            {
                Vector3 position = currRoadNode.Position + currRoadNode.Normal * laneNodeOffset * laneNodeDirection;
                
                curr = curr == null ? new LaneNode(position, laneSide, start.Index, currRoadNode, 0) : new LaneNode(position, laneSide, start.Index, currRoadNode, prev, null, Vector3.Distance(prev.Position, position));
                
                if(prev != null)
                    prev.Next = curr;
                prev = curr;
                currRoadNode = currRoadNode.Next;
            }
            
            return curr.First;
        }

        private RoadNode FetchOrGenerateRoadNodes(RoadNode start, RoadNode end)
        {
            // Check if this road node has already been generated
            if(_intersectionGuideRoadNodes.ContainsKey(start.ID))
                return _intersectionGuideRoadNodes[start.ID];
            
            const float roadNodeDistance = 3f;
            
            RoadNode head = CreateEvenlySpacedGuideRoadNodes(start.Position, end.Position, roadNodeDistance) ?? start;

            head.Intersection = this;
            _intersectionGuideRoadNodes.Add(start.ID, head);

            return head;
        }

        private RoadNode CreateEvenlySpacedGuideRoadNodes(Vector3 start, Vector3 end, float distance)
        {
            int nodesToCreate = Mathf.FloorToInt(Vector3.Distance(start, end) / distance);
            Vector3 path = end - start;

            RoadNode curr = null;
            RoadNode prev = null;
            for(int i = 0; i < nodesToCreate; i++)
            {
                Vector3 position = Vector3.Lerp(start, end, (float)(i + 1) / (nodesToCreate + 1));
                Vector3 tangent = path.normalized;
                Vector3 normal = Quaternion.Euler(0, 90, 0) * tangent;
                float distanceToPrev = prev == null ? 0 : Vector3.Distance(prev.Position, position);
                
                curr = curr == null ? new RoadNode(position, tangent, normal, RoadNodeType.IntersectionGuide, 0, 0) : new RoadNode(position, tangent, normal, RoadNodeType.IntersectionGuide, prev, null, 0, distanceToPrev);
                
                if(prev != null)
                    prev.Next = curr;
                prev = curr;
            }
            return curr?.First;
        }

        /// <summary> Returns the lane node out of a list that has the closest lane index. This maps entry and exit nodes of lanes with differing lane counts </summary>
        private LaneNode GetClosestIndexExitNode(List<LaneNode> exitNodes, int index)
        {
            int closestLaneIndex = exitNodes.Aggregate((x, y) => Math.Abs(x.Index - index) < Math.Abs(y.Index - index) ? x : y).Index;
            return exitNodes.Find(x => x.Index == closestLaneIndex);
        }

        /// <summary> Get a random lane node that leads out of the intersection. Returns a tuple on the format (StartNode, EndNode, NextNode) </summary>
        public (LaneNode, LaneNode, LaneNode) GetRandomLaneNode(LaneNode current)
        {
            List<GuideNode> guidePaths = new List<GuideNode>();
            // Add all guide paths that start at the current lane node to the list
            foreach((string entry, string exit) in _intersectionGuidePaths.Keys)
            {
                if (entry == current.ID)
                    guidePaths.Add(_intersectionGuidePaths[(entry, exit)]);
            }
            
            // Pick a random guide path
            System.Random random = new System.Random();
            int randomLaneNodeIndex = random.Next(0, guidePaths.Count);
            GuideNode guidePath = guidePaths[randomLaneNodeIndex];
            LaneNode finalNode = guidePath.Last;

            return (finalNode.First, finalNode.Last, guidePath);
        }
        /// <summary> Get the new start node, and the lane node that leads to the navigation node edge. Returns a tuple on the format (StartNode, EndNode, NextNode) </summary>
        public (LaneNode, LaneNode, LaneNode) GetNewLaneNode(NavigationNodeEdge navigationNodeEdge, LaneNode current)
        {
            if (!_laneNodeFromNavigationNodeEdge.ContainsKey(navigationNodeEdge.ID))
            {
                Debug.LogError("Error, The navigation node edge does not exist in the intersection");
                return (null, null, null);
            }

            LaneNode finalNode = GetClosestIndexExitNode(_laneNodeFromNavigationNodeEdge[navigationNodeEdge.ID], current.Index);

            if (!_intersectionGuidePaths.ContainsKey((current.ID, finalNode.ID)))
            {
                Debug.LogError("Error, The lane entry node does not exist in the intersection");
                return (null, null, null);
            }

            
            GuideNode guidePath = _intersectionGuidePaths[(current.ID, finalNode.ID)];
            
            // Note that the start node is in fact after the next node, but due to the control point only having pointers from it but
            // never to it, once the vehicle passes the control point and reaches the start point, it can never come back to the control point
            // which is then removed by the garbage collector
            return (finalNode.First, finalNode.Last, guidePath);
        }

        private GuideNode GetGuidePath(LaneNode start, LaneNode end)
        {
            LaneNode entrySection = _intersectionEntryNodes[start.ID];
            LaneNode exitSection = _intersectionExitNodes[end.ID];

            LaneNode entryLast = entrySection.Last;

            LaneNode currLaneNode = entrySection;
            GuideNode curr = null;
            GuideNode prev = null;
            while(currLaneNode != null)
            {
                Vector3 position = currLaneNode.Position;
                curr = new GuideNode(position, currLaneNode, currLaneNode.LaneSide, currLaneNode.Index, currLaneNode.RoadNode, prev, null, prev == null ? 0 : Vector3.Distance(prev.Position, position));
                
                // Set the intersection for the road node
                currLaneNode.RoadNode.Intersection = this;

                if(prev != null)
                    prev.Next = curr;
                prev = curr;
                currLaneNode = currLaneNode.Next;

                if(currLaneNode == entryLast)
                    currLaneNode = exitSection;
            }
            
            curr.Next = end;
            GuideNode guidePath = (GuideNode)curr.First;

            return guidePath;
        }
        private bool IsThreeWayIntersection()
        {
            return Type == IntersectionType.ThreeWayIntersectionAtStart || Type == IntersectionType.ThreeWayIntersectionAtEnd;
        }
        /// <summary>Cleans up the intersection and removes the references to it from the road system and roads</summary>
        void OnDestroy()
        {
            // Remove reference to intersection in the road system
            RoadSystem.RemoveIntersection(this);

            // Remove the anchor points for the intersection
            Road1PathCreator.bezierPath.RemoveAnchors(new List<Vector3>{ Road1AnchorPoint1, Road1AnchorPoint2 });
            Road2PathCreator.bezierPath.RemoveAnchors(new List<Vector3>{ Road2AnchorPoint1, Road2AnchorPoint2 });
            
            // Remove reference to intersection in the roads
            if (Road1?.HasIntersection(this) == true)
                Road1.RemoveIntersection(this);
            if (Road2?.HasIntersection(this) == true)
                Road2.RemoveIntersection(this);
        }
    }
}
