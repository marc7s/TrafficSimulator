using UnityEngine;
using RoadGenerator;
using System.Collections.Generic;

namespace POIs
{
    public class RoadsideParking : Parking
    {
        [Header("Connections")]
        public Material ParkingMaterial;
        
        [Header("Settings")]
        public float ParkingLength = _parkingSize.y * 3;

        [HideInInspector] public Mesh _mesh;
        [SerializeField, HideInInspector] private GameObject _meshHolder;
        private MeshFilter _meshFilter;
        private MeshRenderer _meshRenderer;
        private List<RoadNode> _spanNodes;

        private int _sideCoef => (LaneSide == LaneSide.Primary ? -1 : 1) * (int)RoadNode.Road.RoadSystem.DrivingSide;
        private float _smoothEdgeOffset => _parkingSize.y / 2f;

        protected override void ParkingSetup()
        {
            _useCustomSize = true;
        }
        protected override Vector3 GetSize()
        {
            return new Vector3(2.15f, 0.1f, ParkingLength);
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

            RoadNode startNode = _spanNodes[1];
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
                    
                    Quaternion rotation = curr.Rotation;
                    POINode parkingSpot = new POINode(newPosition, rotation);
                    _parkingSpots.Add(parkingSpot);
                    lastParkingSpot = parkingSpot;
                }
            }

            UpdateMesh();
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
                
                if(first || last)
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
            tris.AddRange(new List<int>{ 0, 1, 2 });

            int vertIndex = 1;
            for(int i = 1; i < _spanNodes.Count - 2; i++, vertIndex += 2)
            {
                tris.AddRange(new List<int>{ vertIndex, vertIndex + 2, vertIndex + 1 });
                tris.AddRange(new List<int>{ vertIndex + 1, vertIndex + 2, vertIndex + 3 });
            }

            int lastLeft = verts.Count - 3;
            
            // Add smooth exit curve
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

            // Move back until the smooth edge node
            while(curr != null && distance <= Size.z / 2 + offset + _smoothEdgeOffset)
            {
                distance += GetEdgeDistance(curr, prev);
                prev = curr;
                curr = curr.Prev;
            }
            
            // Add the smooth edge node
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

            // Move forward until the smooth edge node
            while(curr != null && distance <= Size.z / 2 + offset + _smoothEdgeOffset)
            {
                distance += GetEdgeDistance(curr, prev);
                prev = curr;
                curr = curr.Next;
            }

            // Add the smooth edge node
            nodes.Add(curr);

            _spanNodes = nodes;
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