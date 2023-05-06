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
            _parkingSpots.Clear();
            
            int parkingSpots = Mathf.FloorToInt(Size.z / _parkingSize.y);
            float forwardOffsetDelta = Size.z / parkingSpots;

            float sideOffset = _parkingSize.x / 2;
            float forwardOffset = 0;

            Vector3 startPos = transform.position - transform.forward * (parkingSpots * forwardOffsetDelta) / 2;
            
            for(int i = 0; i < parkingSpots; i++, forwardOffset += forwardOffsetDelta)
            {
                // The amount to offset from the rear parking
                Vector3 forwardOffsetVector = transform.forward * (forwardOffset + _parkingSize.y / 2);
                
                // The amount to offset from the road side
                Vector3 sideOffsetVector = transform.right * sideOffset;
                
                Quaternion rotation = transform.rotation * Quaternion.Euler(Vector3.up * 180);
                POINode parkingSpot = new POINode(startPos + forwardOffsetVector + sideOffsetVector, rotation);
                _parkingSpots.Add(parkingSpot);
            }

            UpdateMesh();
        }

        private void UpdateMesh()
        {
            if(RoadNode == null)
                return;
            
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

            int sideCoef = LaneSide == LaneSide.Primary ? 1 : -1;

            for(int i = 0; i < _parkingSpots.Count; i++)
            {
                POINode parkingSpot = _parkingSpots[i];
                Vector3 pos = parkingSpot.Position;

                bool first = i == 0;
                bool last = i == _parkingSpots.Count - 1;

                if(first)
                    pos += sideCoef * RoadNode.Tangent * _parkingSize.y / 2;
                else if(last)
                    pos -= sideCoef * RoadNode.Tangent * _parkingSize.y / 2;

                Vector3 left = pos + sideCoef * RoadNode.Normal * _parkingSize.x / 2;
                Vector3 right = pos - sideCoef * RoadNode.Normal * _parkingSize.x / 2;
                
                if(first)
                    verts.Add(right + sideCoef * RoadNode.Tangent * _parkingSize.y / 2);

                verts.Add(left);
                verts.Add(right);

                if(last)
                    verts.Add(right - sideCoef * RoadNode.Tangent * _parkingSize.y / 2);
            }

            for(int i = 0; i < verts.Count; i++)
                normals.Add(Vector3.up);

            // Add smooth entry curve
            tris.AddRange(new List<int>{ 0, 1, 2 });

            int vertIndex = 1;
            for(int i = 0; i < _parkingSpots.Count - 1; i++, vertIndex += 2)
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