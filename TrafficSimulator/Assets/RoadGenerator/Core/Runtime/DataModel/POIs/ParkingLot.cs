using UnityEngine;
using RoadGenerator;
using System.Collections.Generic;

namespace POIs
{
    public class ParkingLot : Parking
    {
        protected override void ParkingSetup()
        {
            _useCustomSize = true;
        }
        protected override Vector3 GetSize()
        {
            return new Vector3(20, 0.1f, 20);
        }

        protected override void GenerateParkingSpots()
        {
            _parkingSpots.Clear();
            float forwardOffset = Size.z / 2;
            Vector3 startPos = Position - transform.right * forwardOffset;
            
            int parkingSpotsPerSide = Mathf.FloorToInt(Size.x / _parkingSize.x);
            float sideOffsetDelta = Size.x / parkingSpotsPerSide;

            for(int side = 0; side < 2; side++)
            {
                float sideOffset = _parkingSize.x / 2;
                for(int i = 0; i < parkingSpotsPerSide; i++, sideOffset += sideOffsetDelta)
                {
                    int sideCoef = side == 0 ? -1 : 1;
                    
                    // The amount to offset from the rear edge
                    Vector3 forwardOffsetVector = transform.forward * (forwardOffset + sideCoef * _parkingSize.y / 2);
                    
                    // The amount to offset from the right edge
                    Vector3 sideOffsetVector = transform.right * sideOffset;
                    
                    Quaternion rotation = transform.rotation * (side == 0 ? Quaternion.Euler(Vector3.up * 180) : Quaternion.identity);
                    POINode parkingSpot = new POINode(startPos + forwardOffsetVector + sideOffsetVector, rotation);
                    _parkingSpots.Add(parkingSpot);
                }
                forwardOffset *= -1;
            }
        }

        protected override void CreateParkingMesh()
        {
            List<Vector3> verts = new List<Vector3>();
            List<Vector2> uvs = new List<Vector2>();
            List<Vector3> normals = new List<Vector3>();
            List<int> tris = new List<int>();

            Vector3 up = Rotation * Vector3.forward * Size.x / 2;
            Vector3 right = Rotation * Vector3.right * Size.z / 2;
            Vector3 pos = Position;

            // Add the four corner vertices
            verts.AddRange(new List<Vector3>{ pos - up - right, pos + up - right, pos + up + right, pos - up + right });

            // Add the four corner uvs
            uvs.AddRange(new List<Vector2>{ new Vector2(0, 0), new Vector2(0, 1), new Vector2(1, 1), new Vector2(1, 0) });

            // Add the four corner uvs
            normals.AddRange(new List<Vector3>{ Vector3.up, Vector3.up, Vector3.up, Vector3.up });
            
            // Add the top left triangle
            tris.AddRange(new List<int>{ 0, 1, 2 });

            // Add the bottom right triangle
            tris.AddRange(new List<int>{ 0, 2, 3 });

            _mesh.Clear();
            _mesh.vertices = verts.ToArray();
            _mesh.normals = normals.ToArray();
            _mesh.uv = uvs.ToArray();
            _mesh.subMeshCount = 2;
            _mesh.SetTriangles(tris, 0);
        }
    }
}