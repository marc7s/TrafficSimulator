using UnityEngine;
using RoadGenerator;
using System.Collections.Generic;

namespace POIs
{
    public enum RoadSideParkingType
    {
        Length,
        FullRoad
    }

    public class RoadsideParking : Parking
    {
        [Header("Connections")]
        public Material ParkingMaterial;
        
        [Header("Settings")]
        public float ParkingLength = _parkingSize.y * 3;
        public RoadSideParkingType ParkingType = RoadSideParkingType.Length;
        public bool AllowStartingSmoothEdge = true;
        public bool AllowEndingSmoothEdge = true;

        [HideInInspector] public Mesh _mesh;
        [SerializeField, HideInInspector] private GameObject _meshHolder;
        private MeshFilter _meshFilter;
        private MeshRenderer _meshRenderer;
        private List<RoadNode> _spanNodes;

        private int _sideCoef => (LaneSide == LaneSide.Primary ? -1 : 1) * (int)RoadNode.Road.RoadSystem.DrivingSide;
        private float _smoothEdgeOffset => _parkingSize.y / 2;
        private bool _hasStartingSmoothEdge = true;
        private bool _hasEndingSmoothEdge = true;

        protected override void ParkingSetup()
        {
            _useCustomSize = true;
        }

        protected override Vector3 GetSize()
        {
            return new Vector3(2.15f, 0.1f, ParkingType == RoadSideParkingType.Length || RoadNode == null ? ParkingLength : RoadNode.Road.Length);
        }

        protected override void GenerateParkingSpots()
        {
            if(RoadNode == null)
                return;
            
            CalculateSpanNodes();
            _parkingSpots.Clear();
            
            int parkingSpots = Mathf.FloorToInt(Size.z / _parkingSize.y);
            float forwardOffsetDelta = Size.z / parkingSpots;
            float sideOffset = _parkingSize.x / 2;

            RoadNode startNode = _spanNodes[_hasStartingSmoothEdge ? 1 : 0];
            Vector3 startPos = GetEdgePosition(startNode, sideOffset);
            POINode lastParkingSpot = null;
            
            // Go through all span nodes, ignoring the smooth edge nodes
            for(int i = 1; i < _spanNodes.Count - 1; i++)
            {
                RoadNode curr = _spanNodes[i];

                Vector3 newPosition = GetEdgePosition(curr, sideOffset);
                bool first = lastParkingSpot == null;
                
                if(first || Vector3.Distance(lastParkingSpot.Position, newPosition) >= forwardOffsetDelta)
                {
                    // Do not add the first spot if it is too close to the start
                    if(first && Vector3.Distance(startPos, newPosition) < _parkingSize.y / 2)
                        continue;
                    
                    Quaternion rotation = curr.Rotation * (LaneSide == LaneSide.Secondary ? Quaternion.Euler(0, 180, 0) : Quaternion.identity);
                    POINode parkingSpot = new POINode(newPosition, rotation);
                    _parkingSpots.Add(parkingSpot);
                    lastParkingSpot = parkingSpot;
                }
            }

            UpdateMesh();
            
            if(ParkingType == RoadSideParkingType.FullRoad)
                DistanceAlongRoad = RoadNode.Road.Length / 2;
        }

        private void UpdateMesh()
        {
            AssignMeshComponents();
            AssignMaterials();
            CreateParkingMesh();
        }

        private void AssignMaterials()
        {
            _meshRenderer.sharedMaterial = ParkingMaterial;
        }

        private void CreateParkingMesh()
        {
            List<Vector3> verts = new List<Vector3>();
            List<Vector3> normals = new List<Vector3>();
            List<int> tris = new List<int>();

            for(int i = 0; i < _spanNodes.Count; i++)
            {
                RoadNode vertNode = _spanNodes[i];
                Vector3 roadSide = GetEdgePosition(vertNode);
                Vector3 outside = GetEdgePosition(vertNode, _parkingSize.x);

                bool first = i == 0;
                bool last = i == _spanNodes.Count - 1;
                
                // Add a single inner vertex if it is a smooth edge, otherwise add both
                if((first && _hasStartingSmoothEdge) || (last && _hasEndingSmoothEdge))
                {
                    verts.Add(roadSide);
                }
                else
                {
                    verts.Add(roadSide);
                    verts.Add(outside);
                }
            }

            for(int i = 0; i < verts.Count; i++)
                normals.Add(Vector3.up);

            // Add smooth entry curve
            if(_hasStartingSmoothEdge)
                tris.AddRange(new List<int>{ 0, 1, 2 });

            int startIndex = _hasStartingSmoothEdge ? 1 : 0;
            int endOffset = _hasEndingSmoothEdge ? 1 : 0;
            int vertIndex = startIndex;
            
            for(int i = startIndex; i < _spanNodes.Count - 1 - endOffset; i++, vertIndex += 2)
            {
                tris.AddRange(new List<int>{ vertIndex, vertIndex + 2, vertIndex + 1 });
                tris.AddRange(new List<int>{ vertIndex + 1, vertIndex + 2, vertIndex + 3 });
            }

            int lastLeft = verts.Count - 3;
            
            // Add smooth exit curve
            if(_hasEndingSmoothEdge)
                tris.AddRange(new List<int>{ lastLeft, lastLeft + 2, lastLeft + 1 });

            _mesh.Clear();
            _mesh.vertices = verts.ToArray();
            _mesh.normals = normals.ToArray();
            _mesh.subMeshCount = 1;
            _mesh.SetTriangles(tris, 0);
        }

        /// <summary> Returns the position at the edge of the road </summary>
        private Vector3 GetEdgePosition(RoadNode node, float offset = 0)
        {
            return node.Position + node.Normal * _sideCoef * (offset + node.Road.RoadWidth / 2);
        }

        private float GetEdgeDistance(RoadNode node1, RoadNode node2)
        {
            return Vector3.Distance(GetEdgePosition(node1), GetEdgePosition(node2));
        }

        private void CalculateSpanNodes()
        {
            if(ParkingType == RoadSideParkingType.FullRoad)
            {
                _spanNodes = GetFullRoadSpanNodes();
                return;
            }

            List<RoadNode> nodes = new List<RoadNode>();
            RoadNode curr = RoadNode.Prev;
            RoadNode prev = RoadNode;
            float distance = 0;
            const float buffer = 1.5f;
            float offset = _parkingSize.x / 2 + buffer;

            // Add the nodes behind
            while(curr != null && distance <= Size.z / 2 + offset)
            {
                nodes.Add(curr);
                distance += GetEdgeDistance(curr, prev);
                prev = curr;
                curr = curr.Prev;
            }

            float smoothEdgeDistance = 0;
            
            // Move back until the smooth edge node
            while(curr != null && smoothEdgeDistance <= _smoothEdgeOffset)
            {
                smoothEdgeDistance += GetEdgeDistance(curr, prev);
                prev = curr;
                curr = curr.Prev;
            }
            
            // Set the starting smooth edge depending on if we have enough distance in this direction
            _hasStartingSmoothEdge = AllowStartingSmoothEdge && smoothEdgeDistance >= _smoothEdgeOffset && curr != null;
            
            // Add the smooth edge node
            if(_hasStartingSmoothEdge)
                nodes.Add(curr);
            
            nodes.Reverse();
            
            // Add the center node
            nodes.Add(RoadNode);

            curr = RoadNode.Next;
            prev = RoadNode;
            distance = 0;
            
            // Add the nodes in front
            while(curr != null && distance <= Size.z / 2 + offset)
            {
                nodes.Add(curr);
                distance += GetEdgeDistance(curr, prev);
                prev = curr;
                curr = curr.Next;
            }

            smoothEdgeDistance = 0;

            // Move forward until the smooth edge node
            while(curr != null && smoothEdgeDistance <= _smoothEdgeOffset)
            {
                smoothEdgeDistance += GetEdgeDistance(curr, prev);
                prev = curr;
                curr = curr.Next;
            }

            // Set the starting smooth edge depending on if we have enough distance in this direction
            _hasEndingSmoothEdge = AllowEndingSmoothEdge && smoothEdgeDistance >= _smoothEdgeOffset && curr != null;

            // Add the smooth edge node
            if(_hasEndingSmoothEdge)
                nodes.Add(curr);

            if(LaneSide == LaneSide.Secondary)
                nodes.Reverse();

            _spanNodes = nodes;
        }

        private List<RoadNode> GetFullRoadSpanNodes()
        {
            List<RoadNode> nodes = new List<RoadNode>();
            RoadNode prev = RoadNode.First;
            RoadNode curr = prev.Next;

            RoadNode lastBeforeSmoothEdge = null;

            bool roadTooShortForSmoothEdges = RoadNode.Road.Length < _smoothEdgeOffset * 2 + _parkingSize.y * 2;
            _hasStartingSmoothEdge = AllowStartingSmoothEdge && !roadTooShortForSmoothEdges;
            _hasEndingSmoothEdge = AllowEndingSmoothEdge && !roadTooShortForSmoothEdges;

            float distance = 0;

            if(_hasEndingSmoothEdge)
            {
                prev = RoadNode.Last;
                curr = prev.Prev;

                // Find the last node before the ending smooth edge
                distance = 0;
                
                while(curr != null && distance <= _smoothEdgeOffset)
                {
                    distance += GetEdgeDistance(curr, prev);
                    prev = curr;
                    curr = curr.Prev;
                }
                
                lastBeforeSmoothEdge = curr;
            }

            prev = RoadNode.First;
            curr = prev;

            if(_hasStartingSmoothEdge)
            {
                // Add the first smooth edge node
                nodes.Add(RoadNode.First);

                prev = RoadNode.First;
                curr = prev.Next;
                
                // Move the curr node forward until the smooth edge is over
                distance = 0;
                while(curr != null && distance <= _smoothEdgeOffset)
                {
                    distance += GetEdgeDistance(curr, prev);
                    prev = curr;
                    curr = curr.Next;
                }
            }

            // Add all nodes between the smooth edges
            while(curr != null && curr != lastBeforeSmoothEdge)
            {
                nodes.Add(curr);
                curr = curr.Next;
            }

            // Add the last smooth edge node
            if(_hasEndingSmoothEdge)
                nodes.Add(RoadNode.Last);

            if(LaneSide == LaneSide.Secondary)
                nodes.Reverse();
            
            return nodes;
        }

        private void AssignMeshComponents() 
        {
            // Let the parking itself hold the mesh
            if (_meshHolder == null) 
                _meshHolder = gameObject;

            _meshHolder.transform.rotation = Quaternion.identity;
            _meshHolder.transform.position = Vector3.zero;
            _meshHolder.transform.localScale = Vector3.one;

            // Ensure mesh renderer and filter components are assigned
            if (!_meshHolder.gameObject.GetComponent<MeshFilter>()) 
                _meshHolder.gameObject.AddComponent<MeshFilter>();

            if (!_meshHolder.GetComponent<MeshRenderer>()) 
                _meshHolder.gameObject.AddComponent<MeshRenderer>();

            _meshRenderer = _meshHolder.GetComponent<MeshRenderer>();
            _meshFilter = _meshHolder.GetComponent<MeshFilter>();
            
            // Create a new mesh if one does not already exist
            if (_mesh == null) 
                _mesh = new Mesh();

            _meshFilter.sharedMesh = _mesh;
        }
    }
}