//#define DEBUG_INTERSECTION

using UnityEngine;
using System.Collections.Generic;
using System;
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
        StopSigns,
    }

    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    [ExecuteInEditMode()]
    [Serializable]
    public class Intersection : MonoBehaviour
    {
        [HideInInspector] public GameObject IntersectionObject;

        [HideInInspector] public GameObject FlowContainer;
        [HideInInspector] public GameObject TrafficLightController;

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
        [HideInInspector] private Dictionary<string, LaneNode> _laneNodeFromNavigationNodeEdge;
        [HideInInspector] public IntersectionType Type;
        [ReadOnly] public string ID;

        [Header("Intersection settings")]
        [SerializeField][Range(0, 0.8f)] float _stretchFactor = 0.4f;

        [Header ("Material settings")]
        [SerializeField] private Material _material;
        [SerializeField] private Material _bottomMaterial;
        private float _thickness;
        private MeshFilter _meshFilter;
        private MeshRenderer _meshRenderer;

        [SerializeField] private GameObject _trafficLightPrefab;

        private Mesh _mesh;
        public const float IntersectionLength = 20f;
        public const float IntersectionBoundsLengthMultiplier = 1.2f;

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

        public void UpdateMesh()
        {
            // Set the thickness of the intersection
            _thickness = Road1.Thickness;
            
            AssignMeshComponents();
            AssignMaterials();
            CreateIntersectionMesh();
            // If intersection doesn't have a container, create one
            if(FlowContainer == null)
            {
                CreateIntersectionContainer();
            }
            //AssignTrafficLights();
            AssignTrafficLights2();
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

        private void CreateIntersectionContainer()
        {
            FlowContainer = new GameObject("FlowContainer");
            FlowContainer.transform.parent = IntersectionObject.transform;
            FlowContainer.transform.localPosition = Vector3.zero;
            FlowContainer.transform.localRotation = Quaternion.identity;
            FlowContainer.transform.localScale = Vector3.one;

            CreateTrafficLightController();
        }

        private void CreateTrafficLightController()
        {
            TrafficLightController = new GameObject("TrafficLightController");
            TrafficLightController.transform.parent = FlowContainer.transform;
            TrafficLightController.transform.localPosition = Vector3.zero;
            TrafficLightController.transform.localRotation = Quaternion.identity;
            TrafficLightController.transform.localScale = Vector3.one;
        }

        private void SpawnTrafficLight(Vector3 position, Quaternion rotation)
        {
            GameObject trafficLight = Instantiate(_trafficLightPrefab, position, rotation);
            trafficLight.transform.parent = TrafficLightController.transform;
        }

        private void AssignTrafficLights2()
        {
            List<RoadNode> intersectionNodes = new List<RoadNode>();
            RoadNode road1Node = Road1.StartNode;
            RoadNode road2Node = Road2.StartNode;

            //Find the edge nodes of the roads
            while (road1Node != null)
            {
                if (road1Node.Type == RoadNodeType.JunctionEdge)
                {
                    intersectionNodes.Add(road1Node);
                }
                road1Node = road1Node.Next;
            }
            while (road2Node != null)
            {
                if (road2Node.Type == RoadNodeType.JunctionEdge)
                {
                    intersectionNodes.Add(road2Node);
                }
                road2Node = road2Node.Next;
            }

            Debug.Log("Total junc nodes: " + intersectionNodes.Count);
            int trafficLightCounter = 0;

            // Spawn a traffic light at each junction node
            if (Type == IntersectionType.ThreeWayIntersectionAtStart || Type == IntersectionType.ThreeWayIntersectionAtEnd)
            {
                foreach (RoadNode junctionNode in intersectionNodes)
                {
                    if(trafficLightCounter % 2 == 0)
                    {
                        SpawnTrafficLight(junctionNode.Position, junctionNode.Rotation);
                    } else
                    {
                        SpawnTrafficLight(junctionNode.Position, junctionNode.Rotation * Quaternion.Euler(0, 180, 0));
                    }
                    trafficLightCounter++;
                }
            }
            // If its a 4-way intersection, spawn a traffic light at each junction node
            else if (Type == IntersectionType.FourWayIntersection)
            {
                foreach (RoadNode junctionNode in intersectionNodes)
                {   
                    if(trafficLightCounter % 2 == 0)
                    {
                        SpawnTrafficLight(junctionNode.Position, junctionNode.Rotation);
                    } else
                    {
                        SpawnTrafficLight(junctionNode.Position, junctionNode.Rotation * Quaternion.Euler(0, 180, 0));
                    }        
                    trafficLightCounter++;
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

                // Helper points in the middle between the road edges
                Vector3 bottomLeftMid = Vector3.Lerp(road2BottomLeft, road1BottomRight, _stretchFactor);
                Vector3 bottomRightMid = Vector3.Lerp(road2BottomRight, road1TopRight, _stretchFactor);

                // Mid points
                Vector3 i1 = Vector3.Lerp(bottomLeftMid, IntersectionPosition, _stretchFactor);
                Vector3 i2 = topMidLeft;
                Vector3 i3 = topMidRight;
                Vector3 i4 = Vector3.Lerp(bottomRightMid, IntersectionPosition, _stretchFactor);
                
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
                
                // Helper points in the middle between the road edges
                Vector3 bottomLeftMid = Vector3.Lerp(road1BottomLeft, road2BottomRight, 0.5f);
                Vector3 bottomRightMid = Vector3.Lerp(road1BottomRight, road2TopRight, 0.5f);
                Vector3 topLeftMid = Vector3.Lerp(road1TopLeft, road2BottomLeft, 0.5f);
                Vector3 topRightMid = Vector3.Lerp(road1TopRight, road2TopLeft, 0.5f);
                

                // Mid points
                Vector3 i1 = Vector3.Lerp(bottomLeftMid, IntersectionPosition, _stretchFactor);
                Vector3 i2 = Vector3.Lerp(topLeftMid, IntersectionPosition, _stretchFactor);
                Vector3 i3 = Vector3.Lerp(topRightMid, IntersectionPosition, _stretchFactor);
                Vector3 i4 = Vector3.Lerp(bottomRightMid, IntersectionPosition, _stretchFactor);

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
        /// <summary> Maps the navigation for the intersection </summary>
        public void MapIntersectionNavigation()
        {
            // Map the lane node to take in order to get to the navigation node edge
            _laneNodeFromNavigationNodeEdge = new Dictionary<string, LaneNode>();
            List<Lane> lanes = new List<Lane>();
            
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
                        currentNode = currentNode.Next;
                        continue;
                    }

                    // If the node is an anchor point, we map the edge going out of the intersection to the node
                    if (currentNode.RoadNode.Position == Road1AnchorPoint1)
                        _laneNodeFromNavigationNodeEdge.Add(Road1AnchorPoint1NavigationEdge.ID, currentNode);
                    if (currentNode.RoadNode.Position == Road1AnchorPoint2)
                        _laneNodeFromNavigationNodeEdge.Add(Road1AnchorPoint2NavigationEdge.ID, currentNode);
                    if (currentNode.RoadNode.Position == Road2AnchorPoint1)
                        _laneNodeFromNavigationNodeEdge.Add(Road2AnchorPoint1NavigationEdge.ID, currentNode);
                    // If the intersection is a three way intersection, the second anchor point does not exist
                    if (!IsThreeWayIntersection() && currentNode.RoadNode.Position == Road2AnchorPoint2)
                        _laneNodeFromNavigationNodeEdge.Add(Road2AnchorPoint2NavigationEdge.ID, currentNode);
                    currentNode = currentNode.Next;
                }
            }     
        }
        /// <summary> Get a random lane node that leads out of the intersection </summary>
        public LaneNode GetRandomLaneNode()
        {
            List<LaneNode> laneNodes = new List<LaneNode>(_laneNodeFromNavigationNodeEdge.Values);
            System.Random random = new System.Random();
            int randomLaneNodeIndex = random.Next(0, laneNodes.Count);
            return laneNodes[randomLaneNodeIndex];
        }
        /// <summary> Get the lane node that leads to the navigation node edge </summary>
        public LaneNode GetNewLaneNode(NavigationNodeEdge navigationNodeEdge)
        {
            if (!_laneNodeFromNavigationNodeEdge.ContainsKey(navigationNodeEdge.ID))
            {
                Debug.LogError("Error, The navigation node edge does not exist in the intersection");
                return null;
            }  
            return _laneNodeFromNavigationNodeEdge[navigationNodeEdge.ID];
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
