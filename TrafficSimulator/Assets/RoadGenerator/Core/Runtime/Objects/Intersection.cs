//#define DEBUG_INTERSECTION

using UnityEngine;
using System.Collections.Generic;
using System;
using System.Linq;
using CustomProperties;
using DataModel;

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

    [Serializable]
    public class IntersectionArm
    {
        public Vector3 JunctionEdgePosition;
        public Road Road;
        public NavigationNodeEdge NavigationNodeEdgeOutwards;
        public string ID = System.Guid.NewGuid().ToString();
        public string OppositeArmID;
        public int? FlowControlGroupID;

        public IntersectionArm(JunctionEdgeData junctionEdgeData)
        {
            JunctionEdgePosition = junctionEdgeData.AnchorPoint;
            Road = junctionEdgeData.Road;
            NavigationNodeEdgeOutwards = null; 
        }
    }

    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    [ExecuteInEditMode()]
    [Serializable]
    public class Intersection : MonoBehaviour
    {
        [HideInInspector] public GameObject IntersectionObject;
        [HideInInspector] public RoadSystem RoadSystem;
        [HideInInspector] public Vector3 IntersectionPosition;
        [HideInInspector] public List<IntersectionArm> IntersectionArms = new List<IntersectionArm>();
        [HideInInspector] private Dictionary<string, List<LaneNode>> _laneNodeFromNavigationNodeEdge = new Dictionary<string, List<LaneNode>>();
        [HideInInspector] private Dictionary<string, Section> _intersectionEntrySections = new Dictionary<string, Section>();
        [HideInInspector] private RoadNode _intersectionCenterRoadNode;
        [HideInInspector] public LaneNode _intersectionCenterLaneNode;
        [HideInInspector] private Dictionary<string, Section> _intersectionExitSections = new Dictionary<string, Section>();
        [HideInInspector] private Dictionary<string, RoadNode> _intersectionGuideRoadNodes = new Dictionary<string, RoadNode>();
        [HideInInspector] private Dictionary<(string, string), GuideNode> _intersectionGuidePaths = new Dictionary<(string, string), GuideNode>();

        [HideInInspector] public IntersectionType Type;
        [HideInInspector] public TrafficLightController TrafficLightController;
        [ReadOnly] public string ID;

        [Header("Connections")]
        [SerializeField] private GameObject _guideRoadNodePrefab;
        [SerializeField] private GameObject _guideLaneNodePrefab;

        [Header("Intersection settings")]
        [SerializeField][Range(0, 0.8f)] private float _stretchFactor = 0;
        [SerializeField] public FlowType FlowType = FlowType.TrafficLights;

        [Header ("Material settings")]
        [SerializeField] private Material _material;
        [SerializeField] private Material _bottomMaterial;

        [Header ("Debug settings")]
        [SerializeField] private bool _drawGuideNodes = false;

        private float _thickness;
        private MeshFilter _meshFilter;
        private MeshRenderer _meshRenderer;

        private Mesh _mesh;
        [SerializeField][HideInInspector] private GameObject _guideNodeContainer;
        [HideInInspector] public const float DefaultIntersectionLength = 20f;
        [ReadOnly] public float IntersectionLength = DefaultIntersectionLength;
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

        private struct Section : IEquatable<Section>
        {
            public Road Road;
            public LaneNode JunctionNode;
            public LaneNode Start;
            public LaneNode End;
            private string _edgeID;
            private string _ID;
            public Section(Road road, LaneNode junctionNode, LaneNode start, LaneNode end)
            {
                Road = road;
                JunctionNode = junctionNode;
                Start = start;
                End = end;

                _edgeID = junctionNode.RoadNode.ID;
                _ID = System.Guid.NewGuid().ToString();
            }

            // Used to determine if sections belong to the same edge (legs of the intersecton)
            public string EdgeID => _edgeID;

            public override int GetHashCode() => _ID.GetHashCode();

            public override bool Equals(object other) => other != null && Equals((Section)other);

            public bool Equals(Section other) => other._ID == _ID;

            public static bool operator ==(Section a, Section b) => a.Equals(b);

            public static bool operator !=(Section a, Section b) => !(a == b);
        }

        void Awake()
        {
            IntersectionObject = gameObject;
            TrafficLightController = gameObject.GetComponent<TrafficLightController>();
        }

        public void UpdateMesh()
        {
            AssignMeshComponents();
            AssignMaterials();
            CreateIntersectionMesh();
            ShowGuideNodes();
            gameObject.GetComponent<MeshCollider>().sharedMesh = _mesh;
        }

        public static float CalculateIntersectionLength(Road road1, Road road2)
        {
            return Mathf.Max((int) road1.LaneAmount, (int)road2.LaneAmount) * Intersection.DefaultIntersectionLength;
        }

        public List<Road> GetIntersectionRoads()
        {
            List<Road> roads = new List<Road>();
            foreach(IntersectionArm arm in IntersectionArms)
            {
                if(!roads.Contains(arm.Road))
                    roads.Add(arm.Road);
            }
            return roads;
        }

        private void CreateIntersectionMesh()
        {
            foreach(Road road in GetIntersectionRoads())
            {
                road.UpdateRoadNodes();
                road.UpdateLanes();
                road.PlaceTrafficSigns();
            }

#if DEBUG_INTERSECTION            
            Debug.Log("----------- Road 1 nodes -----------");
            PrintRoadNodes(Road1.StartNode);
            Debug.Log("------------------------------------");

            Debug.Log("----------- Road 2 nodes -----------");
            PrintRoadNodes(Road2.StartNode);
            Debug.Log("------------------------------------");
#endif           

            // The mesh code is based on the vertice layout found at TrafficSimulator/Assets/RoadGenerator/Documentation/IntersectionMeshGeneration   

            List<Vector3> verts = new List<Vector3>();
            List<Vector2> uvs = new List<Vector2>();
            List<Vector3> normals = new List<Vector3>();

            List<int> topTris = new List<int>();

            if(IsThreeWayIntersection())
            {
                // Map out the intersection from the arm without an opposite arm
                IntersectionArm bottomArm = IntersectionArms[0];
                foreach(IntersectionArm arm in IntersectionArms)
                {
                    if(arm.OppositeArmID == null)
                        bottomArm = arm;
                }

                RoadNode bottomArmRoadNode = GetRoadNodeAtIntersectionArm(bottomArm);
                float bottomArmRoadHalfWidth = bottomArm.Road.LaneWidth * (int)bottomArm.Road.LaneAmount;

                Vector3 i5 = bottomArm.JunctionEdgePosition - bottomArmRoadNode.Normal * bottomArmRoadHalfWidth;
                Vector3 i10 = bottomArm.JunctionEdgePosition + bottomArmRoadNode.Normal * bottomArmRoadHalfWidth;

                // If the intersection is at the start of the road, the vertices needs to be swapped as the normal will be in the opposite direction
                if (Type == IntersectionType.ThreeWayIntersectionAtStart)
                    (i5, i10) = (i10, i5);
                
                // Find one of the side arms
                IntersectionArm sideArm = null;
                foreach(IntersectionArm intersectionArm in IntersectionArms)
                {
                    if (intersectionArm != bottomArm)
                    {
                        sideArm = intersectionArm;
                        break;
                    }
                }

                // Find out which side the side arm is on
                TurnDirection turnDirection = GetTurnDirection(bottomArmRoadNode.Position - IntersectionPosition, GetRoadNodeAtIntersectionArm(sideArm).Position - IntersectionPosition);
                IntersectionArm rightArm = turnDirection == TurnDirection.Right ? sideArm : GetArm(sideArm.OppositeArmID);
                RoadNode rightArmRoadNode = GetRoadNodeAtIntersectionArm(rightArm);
                float rightArmRoadHalfWidth = rightArm.Road.LaneWidth * (int)rightArm.Road.LaneAmount;

                Vector3 i9 = rightArm.JunctionEdgePosition - rightArmRoadNode.Normal * rightArmRoadHalfWidth;
                Vector3 i8 = rightArm.JunctionEdgePosition + rightArmRoadNode.Normal * rightArmRoadHalfWidth;

                // Since we don't know the normal direction of the side arm, we need switch if they are in the wrong order
                if (Vector3.Distance(i9, i10) > Vector3.Distance(i8, i10))
                    (i9, i8) = (i8, i9);

                IntersectionArm leftArm = GetArm(rightArm.OppositeArmID);
                RoadNode leftArmRoadNode = GetRoadNodeAtIntersectionArm(leftArm);
                float leftArmRoadHalfWidth = leftArm.Road.LaneWidth * (int)leftArm.Road.LaneAmount;

                Vector3 i7 = leftArm.JunctionEdgePosition - leftArmRoadNode.Normal * leftArmRoadHalfWidth;
                Vector3 i6 = leftArm.JunctionEdgePosition + leftArmRoadNode.Normal * leftArmRoadHalfWidth;

                // Since we don't know the normal direction of the side arm, we need switch if they are in the wrong order
                if (Vector3.Distance(i6, i5) > Vector3.Distance(i7, i5))
                    (i7, i6) = (i6, i7);

                Vector3 road1Dir = (bottomArm.JunctionEdgePosition - IntersectionPosition).normalized;
                Vector3 road2Dir = (leftArm.JunctionEdgePosition - rightArm.JunctionEdgePosition).normalized;

                //
                (Vector3, Vector3) i5RoadLine = (i5, road1Dir);
                (Vector3, Vector3) i10RoadLine = (i10, road1Dir);

                (Vector3, Vector3) i7RoadLine = (i7, road2Dir);
                (Vector3, Vector3) i6RoadLine = (i6, road2Dir);
                (Vector3, Vector3) i8RoadLine = (i8, -road2Dir);
                (Vector3, Vector3) i9RoadLine = (i9, -road2Dir);

                // Mid points
                Vector3 i1 = GetMidPointCorner(i5RoadLine, i6RoadLine);
                Vector3 i2 = GetMidPointCorner(i5RoadLine, i7RoadLine);
                Vector3 i3 = GetMidPointCorner(i10RoadLine, i8RoadLine);
                Vector3 i4 = GetMidPointCorner(i10RoadLine, i9RoadLine);
                
                // Adding unused vertice to make sure the index is correct
                verts.Add(new Vector3(1337,1337,1337));
                verts.AddRange(new List<Vector3>(){ i1, i2, i3, i4, i5, i6, i7, i8, i9, i10 });

                AddTrianglesForRectangle(topTris, 5, 10, 1, 4);
                AddTrianglesForRectangle(topTris, 7, 6, 2, 1);
                AddTrianglesForRectangle(topTris, 9, 8, 4, 3);
                AddTrianglesForRectangle(topTris, 1, 4, 2, 3);
            }
            else if(Type == IntersectionType.FourWayIntersection)
            {
                // Map out the intersection from the first arms perspective
                IntersectionArm bottomArm = IntersectionArms[0];

                RoadNode bottomArmRoadNode = GetRoadNodeAtIntersectionArm(bottomArm);
                float bottomArmRoadHalfWidth = bottomArm.Road.LaneWidth * (int)bottomArm.Road.LaneAmount;

                Vector3 i5 = bottomArm.JunctionEdgePosition - bottomArmRoadNode.Normal * bottomArmRoadHalfWidth;
                Vector3 i12 = bottomArm.JunctionEdgePosition + bottomArmRoadNode.Normal * bottomArmRoadHalfWidth;

                IntersectionArm topArm = GetArm(bottomArm.OppositeArmID);
                RoadNode topArmRoadNode = GetRoadNodeAtIntersectionArm(topArm);
                float topArmRoadHalfWidth = topArm.Road.LaneWidth * (int)topArm.Road.LaneAmount;

                Vector3 i8 = topArm.JunctionEdgePosition - topArmRoadNode.Normal * topArmRoadHalfWidth;
                Vector3 i9 = topArm.JunctionEdgePosition + topArmRoadNode.Normal * topArmRoadHalfWidth;
                
                // Find one of the side arms
                IntersectionArm sideArm = null;
                foreach(IntersectionArm intersectionArm in IntersectionArms)
                {
                    if (intersectionArm != bottomArm && intersectionArm != topArm)
                    {
                        sideArm = intersectionArm;
                        break;
                    }
                }

                // Find out which side the side arm is on
                TurnDirection turnDirection = GetTurnDirection(bottomArmRoadNode.Position - IntersectionPosition, GetRoadNodeAtIntersectionArm(sideArm).Position - IntersectionPosition);
                
                IntersectionArm rightArm = turnDirection == TurnDirection.Right ? sideArm : GetArm(sideArm.OppositeArmID);
                RoadNode rightArmRoadNode = GetRoadNodeAtIntersectionArm(rightArm);
                float rightArmRoadHalfWidth = rightArm.Road.LaneWidth * (int)rightArm.Road.LaneAmount;

                Vector3 i11 = rightArm.JunctionEdgePosition - rightArmRoadNode.Normal * rightArmRoadHalfWidth;
                Vector3 i10 = rightArm.JunctionEdgePosition + rightArmRoadNode.Normal * rightArmRoadHalfWidth;

                // Since we don't know the normal direction of the side arm, we need switch if they are in the wrong order
                if (Vector3.Distance(i11, i12) > Vector3.Distance(i10, i12))
                    (i11, i10) = (i10, i11);

                IntersectionArm leftArm = GetArm(rightArm.OppositeArmID);
                RoadNode leftArmRoadNode = GetRoadNodeAtIntersectionArm(leftArm);
                float leftArmRoadHalfWidth = leftArm.Road.LaneWidth * (int)leftArm.Road.LaneAmount;

                Vector3 i7 = leftArm.JunctionEdgePosition - leftArmRoadNode.Normal * leftArmRoadHalfWidth;
                Vector3 i6 = leftArm.JunctionEdgePosition + leftArmRoadNode.Normal * leftArmRoadHalfWidth;

                if (Vector3.Distance(i6, i5) > Vector3.Distance(i7, i5))
                    (i7, i6) = (i6, i7);

                Vector3 road1Dir = (bottomArm.JunctionEdgePosition - topArm.JunctionEdgePosition).normalized;
                Vector3 road2Dir = (leftArm.JunctionEdgePosition - rightArm.JunctionEdgePosition).normalized;

                (Vector3, Vector3) i5RoadLine = (i5, road1Dir);
                (Vector3, Vector3) i12RoadLine = (i12, road1Dir);
                (Vector3, Vector3) i8RoadLine = (i8, -road1Dir);
                (Vector3, Vector3) i9RoadLine = (i9, -road1Dir);

                (Vector3, Vector3) i7RoadLine = (i7, road2Dir);
                (Vector3, Vector3) i6RoadLine = (i6, road2Dir);
                (Vector3, Vector3) i10RoadLine = (i10, -road2Dir);
                (Vector3, Vector3) i11RoadLine = (i11, -road2Dir);

                // Mid points
                Vector3 i1 = GetMidPointCorner(i5RoadLine, i6RoadLine);
                Vector3 i2 = GetMidPointCorner(i8RoadLine, i7RoadLine);
                Vector3 i3 = GetMidPointCorner(i10RoadLine, i9RoadLine);
                Vector3 i4 = GetMidPointCorner(i11RoadLine, i12RoadLine);
                
                // Adding unused vertice to make sure the index is correct
                verts.Add(new Vector3(1337,1337,1337));
                verts.AddRange(new List<Vector3>(){ i1, i2, i3, i4, i5, i6, i7, i8, i9, i10, i11, i12 });

                AddTrianglesForRectangle(topTris, 5, 12, 1, 4);
                AddTrianglesForRectangle(topTris, 7, 6, 2, 1);
                AddTrianglesForRectangle(topTris, 9, 8, 3, 2);
                AddTrianglesForRectangle(topTris, 11, 10, 4, 3);
                AddTrianglesForRectangle(topTris, 1, 4, 2, 3);
            }

            _mesh.Clear();
            _mesh.vertices = verts.ToArray();
            _mesh.uv = uvs.ToArray();
            _mesh.normals = normals.ToArray();
            _mesh.subMeshCount = 2;

            _mesh.SetTriangles(topTris.ToArray(), 0);
        }

        private void AddTrianglesForRectangle(List<int> tris, int side1Index1, int side1Index2, int side2Index1, int side2Index2)
        {
            tris.Add(side1Index1);
            tris.Add(side2Index1);
            tris.Add(side2Index2);
            
            tris.Add(side2Index2);
            tris.Add(side1Index2);
            tris.Add(side1Index1);
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
                guideNodes.AddRange(_intersectionEntrySections.Values.Select(nodes => nodes.Start));
                guideNodes.Add(_intersectionCenterLaneNode);
                guideNodes.AddRange(_intersectionExitSections.Values.Select(nodes => nodes.Start));

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

        public IntersectionArm? GetIntersectionArmAtJunctionEdge(RoadNode roadNode)
        {   
            foreach(IntersectionArm arm in IntersectionArms)
            {
                if(arm.JunctionEdgePosition == roadNode.Position)
                    return arm;
            }
            return null;
        }

        public IntersectionArm GetArm(string armID)
        {
            foreach(IntersectionArm arm in IntersectionArms)
            {
                if(arm.ID == armID)
                    return arm;
            }
            return null;
        }
        public RoadNode? GetRoadNodeAtIntersectionArm(IntersectionArm arm)
        {
            RoadNode curr = arm.Road.StartNode;
            while(curr != null)
            {
                if(curr.Position == arm.JunctionEdgePosition)
                    return curr;
                curr = curr.Next;
            }
            return null;
        }

        public List<IntersectionArm> GetArms(Road road)
        {
            List<IntersectionArm> arms = new List<IntersectionArm>();
            foreach (IntersectionArm arm in IntersectionArms)
            {
                if (arm.Road == road)
                    arms.Add(arm);
            }
            return arms;
        }

        /// <summary> Maps the navigation for the intersection </summary>
        public void MapIntersectionNavigation()
        {
            // Map the lane node to take in order to get to the navigation node edge
            _laneNodeFromNavigationNodeEdge.Clear();

            _intersectionEntrySections.Clear();
            _intersectionExitSections.Clear();

            _intersectionGuideRoadNodes.Clear();
            _intersectionGuidePaths.Clear();

            List<Lane> lanes = new List<Lane>();
            foreach (Road road in GetIntersectionRoads())
            {
                lanes.AddRange(road.Lanes);
            }
            
            foreach (Lane lane in lanes)
            {
                LaneNode currentNode = lane.StartNode;
                Road road = currentNode.RoadNode.Road;
                while(currentNode != null)
                {
                    if (currentNode.RoadNode.Road != road)
                        break;

                    if (currentNode.Type != RoadNodeType.JunctionEdge)
                    {
                        currentNode = currentNode.Next;
                        continue;
                    }

                    bool isEdgePointingToIntersection = currentNode.GetNavigationEdge().EndNavigationNode.RoadNode.Position == IntersectionPosition;
                    if (!isEdgePointingToIntersection)
                    {
                        IntersectionArm? arm = GetIntersectionArmAtJunctionEdge(currentNode.RoadNode);
                        if (arm != null)
                        {
                            AddLaneNodeFromNavigationNodeEdge(arm?.NavigationNodeEdgeOutwards, currentNode);
                        }
                    }
                        
                    
                    // Since we want to map the nodes that point out of the intersection, we skip nodes that point towards the intersection 
                    if(isEdgePointingToIntersection)
                    {
                        if(currentNode.Intersection == this)
                            CreateEntryIntersectionLaneNodes(road, currentNode, currentNode.Next);
                    }
                    else
                    {
                        // Create exit intersection lane nodes for navigation in the intersection if the node is related to this intersection
                        if(currentNode.Intersection == this)
                            CreateExitIntersectionLaneNodes(road, currentNode, currentNode.Prev);
                    }
                
                    currentNode = currentNode.Next;
                }
            }

            _intersectionCenterRoadNode = new RoadNode(GetIntersectionRoads()[0], IntersectionPosition, new Vector3(0, 0, -1), new Vector3(-1, 0, 0), RoadNodeType.IntersectionGuide, 0, 0);
            _intersectionCenterRoadNode.Intersection = this;
            _intersectionCenterLaneNode = new LaneNode(IntersectionPosition, LaneSide.Primary, 0, _intersectionCenterRoadNode, 0, false);

            // Precompute all the guide paths and store them
            foreach(Section entrySection in _intersectionEntrySections.Values)
            {
                foreach(Section exitSection in _intersectionExitSections.Values)
                {
                    // Do not compute guide paths for U turns
                    if(entrySection.EdgeID == exitSection.EdgeID)
                        continue;
                    _intersectionGuidePaths.Add((entrySection.JunctionNode.ID, exitSection.JunctionNode.ID), CreateGuidePath(entrySection, exitSection, GetYieldToNodes(entrySection, exitSection), GetYieldToBlockingNodes(entrySection, exitSection)));
                }
            }
        }

        /// <summary> Get a list of all nodes a path going between these sections needs to yield to </summary>
        private List<(LaneNode, LaneNode)> GetYieldToNodes(Section entrySection, Section exitSection)
        {
            List<(LaneNode, LaneNode)> yieldNodes = new List<(LaneNode, LaneNode)>();
            
            // Do not yield if you are staying on the same road
            if(entrySection.Road == exitSection.Road)
                return yieldNodes;

            foreach(Section section in _intersectionEntrySections.Values)
            {
                // Do not yield to vehicles in your own entry section
                if(section == entrySection)
                    continue;
                
                yieldNodes.Add((section.End, section.JunctionNode));
            }

            return yieldNodes;
        }

        /// <summary> Get a list of all nodes a path going between these sections needs to yield for blocking vehicles to </summary>
        private Dictionary<string, List<LaneNode>> GetYieldToBlockingNodes(Section entrySection, Section exitSection)
        {
            Dictionary<string, List<LaneNode>> blockingNodes = new Dictionary<string, List<LaneNode>>();
            List<LaneNode> sourceNodes = new List<LaneNode>();

            sourceNodes.Add(entrySection.End);

            foreach(LaneNode source in sourceNodes)
            {
                blockingNodes[source.ID] = new List<LaneNode>();
                foreach(Section section in _intersectionEntrySections.Values)
                {
                    // Do not yield to blocking vehicles on your own road
                    if(section.Road == entrySection.Road)
                        continue;

                    blockingNodes[source.ID].AddRange(GetSectionBlockingNodes(section, source, true));
                }
                

                foreach(Section section in _intersectionExitSections.Values)
                    blockingNodes[source.ID].AddRange(GetSectionBlockingNodes(section, source, false));
            }

            return blockingNodes;
        }

        private List<LaneNode> GetSectionBlockingNodes(Section section, LaneNode source, bool isEntry)
        {
            float maxBlockingDistance = Mathf.Sqrt(2) * section.Road.LaneWidth / 2;
            List<LaneNode> sectionBlockingNodes = new List<LaneNode>();
            LaneNode curr = isEntry ? section.End : section.Start;
            while(curr != null && Vector3.Distance(curr.Position, source.Position) <= maxBlockingDistance)
            {
                sectionBlockingNodes.Add(curr);
                curr = isEntry ? curr.Prev : curr.Next;
            }

            return sectionBlockingNodes;
        }
        
        private void CreateEntryIntersectionLaneNodes(Road road, LaneNode junctionNode, LaneNode intersectionNode)
        {
            RoadNode generatedRoadNodes = FetchOrGenerateRoadNodes(junctionNode.RoadNode, intersectionNode.RoadNode, road);
            Section laneSection = CreateLaneSection(road, junctionNode, junctionNode, generatedRoadNodes, LaneSide.Primary, true);
            _intersectionEntrySections.Add(junctionNode.ID, laneSection);
        }

        private void CreateExitIntersectionLaneNodes(Road road, LaneNode junctionNode, LaneNode intersectionNode)
        {
            RoadNode generatedRoadNodes = FetchOrGenerateRoadNodes(junctionNode.RoadNode, intersectionNode.RoadNode, road);
            Section laneSection = CreateLaneSection(road, junctionNode, intersectionNode, generatedRoadNodes, LaneSide.Secondary, false);
            _intersectionExitSections.Add(junctionNode.ID, laneSection);
        }

        private Section CreateLaneSection(Road road, LaneNode junctionNode, LaneNode start, RoadNode roadNode, LaneSide laneSide, bool isEntry)
        {
            float laneNodeOffset = Vector3.Distance(start.RoadNode.Position, start.Position);
            int laneNodeDirection = laneSide == LaneSide.Primary ? 1 : -1;
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
            
            LaneNode startNode = isEntry ? curr.First : curr.First.Reverse();
            
            return new Section(road, junctionNode, startNode, startNode.Last);
        }

        private RoadNode FetchOrGenerateRoadNodes(RoadNode start, RoadNode end, Road road)
        {
            // Offset the nodes from the intersection so they do not overlap with other section nodes
            float endOffset = 0.8f * road.LaneWidth / 2;
            // Check if this road node has already been generated
            if(_intersectionGuideRoadNodes.ContainsKey(start.ID))
                return _intersectionGuideRoadNodes[start.ID];
            
            const float roadNodeDistance = 2f;
            Vector3 direction = (end.Position - start.Position).normalized;
            
            RoadNode head = CreateEvenlySpacedGuideRoadNodes(road, start.Position, end.Position - direction * endOffset, roadNodeDistance) ?? start;

            head.Intersection = this;
            _intersectionGuideRoadNodes.Add(start.ID, head);

            return head;
        }

        private RoadNode CreateEvenlySpacedGuideRoadNodes(Road road, Vector3 start, Vector3 end, float distance)
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
                
                curr = curr == null ? new RoadNode(road, position, tangent, normal, RoadNodeType.IntersectionGuide, 0, 0) : new RoadNode(road, position, tangent, normal, RoadNodeType.IntersectionGuide, prev, null, 0, distanceToPrev);
                
                // Set the intersection for the road node
                curr.Intersection = this;

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

        /// <summary> Get a random lane node that leads out of the intersection. Returns a tuple on the format (EndNode, NextNode) </summary>
        public (LaneNode, LaneNode) GetRandomLaneNode(LaneNode current, ref TurnDirection turnDirection)
        {
            List<GuideNode> guidePaths = GetGuidePaths(current).Select(x => x.Item3).ToList();
            
            // Pick a random guide path
            System.Random random = new System.Random();
            int randomLaneNodeIndex = random.Next(0, guidePaths.Count);
            GuideNode guidePath = guidePaths[randomLaneNodeIndex];
            LaneNode finalNode = guidePath.Last;

            turnDirection = GetTurnDirection(current, finalNode);
            return (finalNode.Last, guidePath);
        }

        /// <summary> 
        /// Get all guide paths starting at the given entry node.
        /// The return format for each element is (string entryID, string exitID, GuideNode guidePath)
        /// </summary>
        public List<(string, string, GuideNode)> GetGuidePaths(LaneNode entry)
        {
            List<(string, string, GuideNode)> guidePaths = new List<(string, string, GuideNode)>();
            
            // Add all guide paths that start at the current lane node to the list
            foreach((string entryID, string exitID) in _intersectionGuidePaths.Keys)
            {
                if (entryID == entry.ID)
                    guidePaths.Add((entryID, exitID, _intersectionGuidePaths[(entryID, exitID)]));
            }

            return guidePaths;
        }
        
        /// <summary> Get the new end node, and the lane node that leads to the navigation node edge. Returns a tuple on the format (EndNode, NextNode) </summary>
        public (LaneNode, LaneNode) GetNewLaneNode(NavigationNodeEdge navigationNodeEdge, LaneNode current, ref TurnDirection turnDirection)
        {
            if (!_laneNodeFromNavigationNodeEdge.ContainsKey(navigationNodeEdge.ID))
            {
                Debug.LogError("Error, The navigation node edge does not exist in the intersection");
                return (null, null);
            }
            LaneNode finalNode = GetClosestIndexExitNode(_laneNodeFromNavigationNodeEdge[navigationNodeEdge.ID], current.Index);

            if (!_intersectionGuidePaths.ContainsKey((current.ID, finalNode.ID)))
            {
                Debug.LogError("Error, The lane entry node does not exist in the intersection");
                return (null, null);
            }

            
            GuideNode guidePath = _intersectionGuidePaths[(current.ID, finalNode.ID)];
            turnDirection = GetTurnDirection(current, finalNode);
            // Note that the start node is in fact after the next node, but due to the control point only having pointers from it but
            // never to it, once the vehicle passes the control point and reaches the start point, it can never come back to the control point
            // which is then removed by the garbage collector
            return (finalNode.Last, guidePath);
        }

        private GuideNode CreateGuidePath(Section entrySection, Section exitSection, List<(LaneNode, LaneNode)> yieldNodes, Dictionary<string, List<LaneNode>> yieldBlockingNodes)
        {
            LaneNode entryLast = entrySection.End;

            LaneNode currLaneNode = entrySection.Start;
            GuideNode curr = null;
            GuideNode prev = null;
            
            while(currLaneNode != null)
            {
                Vector3 position = currLaneNode.Position;
                curr = new GuideNode(position, currLaneNode, currLaneNode.LaneSide, currLaneNode.Index, currLaneNode.RoadNode, prev, null, prev == null ? 0 : Vector3.Distance(prev.Position, position));

                if(prev != null)
                    prev.Next = curr;
                prev = curr;

                // Set the GuideNode to yield to potential blocking nodes
                if(yieldBlockingNodes.ContainsKey(currLaneNode.ID))
                    curr.YieldBlockingNodes = yieldBlockingNodes[currLaneNode.ID];

                // If we have reached the end of the entry section
                if(currLaneNode == entryLast)
                {
                    // Set the GuideNode at the end of the entry section to yield to the yield nodes
                    curr.YieldNodes = yieldNodes;

                    // Go through the intersection center node for paths passing through the middle of the intersection
                    if(Vector3.Distance(curr.Position, IntersectionPosition) < Vector3.Distance(curr.Position, exitSection.Start.Position))
                    {
                        curr = new GuideNode(IntersectionPosition, _intersectionCenterLaneNode, currLaneNode.LaneSide, currLaneNode.Index, currLaneNode.RoadNode, prev, null, Vector3.Distance(prev.Position, IntersectionPosition));
                        prev.Next = curr;
                        prev = curr;
                    }

                    // Set the current lane node to the exit section
                    currLaneNode = exitSection.Start;
                }
                else
                    currLaneNode = currLaneNode.Next;
            }
            
            curr.Next = exitSection.JunctionNode;
            GuideNode guidePath = (GuideNode)curr.First;

            return guidePath;
        }
        /// <summary> Returns the turn direction for the intersection path. Returns 1, 0 or -1. 1 Is right turn, 0 is straight, -1 is left </summary>
        private TurnDirection GetTurnDirection(LaneNode entry, LaneNode exit)
        {
            // If the entry and exit nodes share the same first node it means that the entry and exit nodes are on the same road
            if (entry.RoadNode.First == exit.RoadNode.First)
                return TurnDirection.Straight;

            Vector3 entryDirection = entry.Position - entry.Next.Position;
            Vector3 exitDirection = exit.Position - exit.Prev.Position;

            Vector3 perp = Vector3.Cross(entryDirection, exitDirection);
            float dir = Vector3.Dot(perp, Vector3.up);
            
            if (dir > 0f)
                return TurnDirection.Left;
            else if (dir < 0)
                return TurnDirection.Right;
            else 
                return TurnDirection.Straight;
        }

        private TurnDirection GetTurnDirection(Vector3 dir1, Vector3 dir2)
        {
            Vector3 perp = Vector3.Cross(dir1, dir2);
            float dir = Vector3.Dot(perp, Vector3.up);
            
            if (dir > 0f)
                return TurnDirection.Left;
            else if (dir < 0)
                return TurnDirection.Right;
            else 
                return TurnDirection.Straight;
        }

        public bool IsThreeWayIntersection()
        {
            return Type == IntersectionType.ThreeWayIntersectionAtStart || Type == IntersectionType.ThreeWayIntersectionAtEnd;
        }

        private List<Vector3> GetJunctionEdgesPositionForRoad(Road road)
        {
            List<Vector3> positions = new List<Vector3>();
            foreach (IntersectionArm arm in IntersectionArms)
            {
                if (arm.Road == road)
                    positions.Add(arm.JunctionEdgePosition);
            }

            return positions;
        }

        /// <summary>Cleans up the intersection and removes the references to it from the road system and roads</summary>
        void OnDestroy()
        {
            // Remove reference to intersection in the road system
            RoadSystem.RemoveIntersection(this);

            Debug.Log("Intersection destroyed");
            // Remove the anchor points for the intersection
            foreach (Road road in GetIntersectionRoads())
            {
                road.PathCreator.bezierPath.RemoveAnchors(GetJunctionEdgesPositionForRoad(road));

                // Remove reference to intersection in the roads
                if (road.HasIntersection(this))
                    road.RemoveIntersection(this);
            }
        }

        public void Reverse(Road road)
        {
            /// When reversing the road these need to be reversed as well.
        //    if (road == Road1)
          //      (Road1AnchorPoint1, Road1AnchorPoint2) = (Road1AnchorPoint2, Road1AnchorPoint1);
          //  else if (road == Road2)
           //     (Road2AnchorPoint1, Road2AnchorPoint2) = (Road2AnchorPoint2, Road2AnchorPoint1);
        }
    }
}
