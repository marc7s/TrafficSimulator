using System.Collections.Generic;
using UnityEngine;

namespace RoadGenerator 
{
    [RequireComponent(typeof(Road))]
    public class RoadMeshCreator : PathSceneTool
    {
        [SerializeField] private bool _flattenSurface;

        [Header ("Material settings")]
        [SerializeField] private Material _laneMaterial;
        [SerializeField] private Material _bottomMaterial;
        [SerializeField] private float _textureTilingScale = 100;
        
        private int _laneCount;
        private float _thickness;
        private Material _laneMaterialCopy;
        private MeshFilter _meshFilter;
        private MeshRenderer _meshRenderer;
        private Mesh _mesh;
        private Road _road;
        [SerializeField, HideInInspector] private GameObject _meshHolder;

        protected override void PathUpdated()
        {
            // Do nothing when the path is updated
            // The road mesh will only be updated when the road requests it
        }

        public void UpdateMesh()
        {
            // Get the road
            if(_road == null)
                _road = GetComponent<Road>();
            
            // Set the lane count to avoid having to cast the enum to an int everytime
            _laneCount = (int)_road.LaneAmount;
            
            // Get the thickness of the road
            _thickness = _road.Thickness;

            // Create a copy of the lane material if it is not set, or the material is updated
            // This makes the material independent from other roads
            if((_laneMaterialCopy == null && _laneMaterial != null) || (_laneMaterialCopy != null && _laneMaterial != null && _laneMaterialCopy.name != _laneMaterial.name))
            {
                _laneMaterialCopy = new Material(_laneMaterial);
            }
                
            
            if (pathCreator != null) 
            {
                AssignMeshComponents();
                AssignMaterials();
                CreateRoadMesh();
            }
        }

        private void CreateRoadMesh()
        {
            // The number of vertices required per component of the road
            int edgeVertsPerPoint = 2;
            int laneVertsPerPoint = 2 * _laneCount - 1;
            int bottomVertsPerPoint = 2;
            int sideVertsPerPoint = 4;

            // Create the arrays for the vertices, uvs and normals
            int numPoints = _road.StartNode.CountNonIntersections;
            int vertsLength = numPoints * (edgeVertsPerPoint + laneVertsPerPoint + bottomVertsPerPoint + sideVertsPerPoint);
            List<Vector3> verts = new List<Vector3>();
            List<Vector2> uvs = new List<Vector2>();
            List<Vector3> normals = new List<Vector3>();

            // Calculate the number of triangles required for each component of the road
            int laneNumTris = 2 * _laneCount * (2 * (numPoints - 1) + (path.isClosedLoop ? 2 : 0));
            int bottomNumTris = 2 * (numPoints - 1) + (path.isClosedLoop ? 2 : 0);
            int sideNumTris = 4 * (numPoints - 1) + 4;
            
            // A 2D list where each list will hold the triangles for a lane
            List<List<int>> laneTriangles = new List<List<int>>();

            // Since it will be indexed it needs to be initialised with the correct size
            for(int i = 0; i < 2 * _laneCount; i++)
                laneTriangles.Add(new List<int>(new int[laneNumTris * 3]));
            
            // The triangles for the bottom and side of the road
            List<int> bottomTriangles = new List<int>();
            List<int> sideOfRoadTriangles = new List<int>();

            // Initialise the array indices used for the components
            int vertIndex = 0;
            int laneTriIndex = 0;

            // The triangle map for the bottom of the road
            int[] bottomTriangleMap = 
            { 
                0,  2 * _laneCount + 7,  1, 
                1,  2 * _laneCount + 7,  2 * _laneCount + 8
            };

            // The triangle map for the sides of the road
            int[] sidesTriangleMap = 
            {
                0,                  2,                   2 * _laneCount + 7,
                2 * _laneCount + 7,  2,                   2 * _laneCount + 9,
                2 * _laneCount + 8,  2 * _laneCount + 10,  1,
                1,                  2 * _laneCount + 10,  3
            };
            

            /*
                -- Original documentation: --
                
                Vertices for the top of the road are layed out:
                0  1
                8  9
                and so on... So the triangle map 0,8,1 for example, defines a triangle from top left to bottom left to bottom right.

                -- Current documentation: --
                Each lane is a submesh of the road mesh. Vertices for the top of the road are layed out:
                0                      ...laneAVertices        2                       ...laneBVertices        1
                (2 * laneAmount + 1)   ...laneAVertices       (2 * laneAmount + 3)     ...laneBVertices       (2 * laneAmount + 2)

                See the image and graph in the documentation folder for a visual representation as well as the order of the lane vertices
            */

            // Triangle map for the top of the road. It holds the different triangle maps for each lane amount
            List<List<int>> laneTriangleMaps = new List<List<int>>();
            
            // One lane
            laneTriangleMaps.Add(new List<int> 
            {
                0, 9, 2, 
                2, 9, 11,
                2, 11, 1,
                1, 11, 10
            });

            // Two lanes
            laneTriangleMaps.Add(new List<int> 
            {
                0, 11, 4,
                4, 11, 15,
                4, 15, 2,
                2, 15, 13,
                2, 13, 3,
                3, 13, 14,
                3, 14, 1,
                1, 14, 12
            });

            // Three lanes
            laneTriangleMaps.Add(new List<int> 
            {
                0, 13, 5,
                5, 13, 18,
                5, 18, 3,
                3, 18, 16,
                3, 16, 2,
                2, 16, 15,
                2, 15, 4,
                4, 15, 17,
                4, 17, 6,
                6, 17, 19,
                6, 19, 1,
                1, 19, 14
            });

            // Four lanes
            laneTriangleMaps.Add(new List<int> 
            {
                0, 15, 7,
                7, 15, 22,
                7, 22, 5,
                5, 22, 20,
                5, 20, 3,
                3, 20, 18,
                3, 18, 2,
                2, 18, 17,
                2, 17, 4,
                4, 17, 19,
                4, 19, 6,
                6, 19, 21,
                6, 21, 8,
                8, 21, 23,
                8, 23, 1,
                1, 23, 16
            });

            bool usePathNormals = !(path.space == PathSpace.xyz && _flattenSurface);
            RoadNode curr = _road.StartNode;
            
            while(curr != null)
            {
                vertIndex = verts.Count == 0 ? 0 : verts.Count;

                // Skip the start node if the next node is a three way intersection
                bool skipFirst = curr.Type == RoadNodeType.End && curr.Next != null && curr.Next.Type == RoadNodeType.ThreeWayIntersection;

                // Skip the end node if the previous node is a three way intersection
                bool skipLast = curr.Type == RoadNodeType.End && curr.Prev != null && curr.Prev.Type == RoadNodeType.ThreeWayIntersection;
                
                if(curr.IsIntersection() || skipFirst || skipLast)
                {
                    curr = curr.Next;
                    continue;
                }

                // Store the Y scale to be used for the UVs. It is the path time (0 at the start of the path and 1 at the end)
                float uvYScale = curr.Time;

                // Calculate the local directional vectors
                Vector3 localUp = usePathNormals ? Vector3.Cross(curr.Tangent, curr.Normal) : path.up;
                Vector3 localRight = usePathNormals ? curr.Normal : Vector3.Cross(localUp, curr.Tangent);

                // Find position to left and right of current path vertex
                Vector3 vertCenter = curr.Position;
                Vector3 vertSideA = vertCenter - localRight * Mathf.Abs(_road.LaneWidth) * _laneCount;
                Vector3 vertSideB = vertCenter + localRight * Mathf.Abs(_road.LaneWidth) * _laneCount;

                /*** Add top of road vertices ***/
                verts.Add(vertSideA);
                verts.Add(vertSideB);
                verts.Add(vertCenter);

                // The top of the road has normals pointing up
                normals.Add(localUp);
                normals.Add(localUp);
                normals.Add(localUp);
                
                // The UV x axis is set so that 0 is on the local left and 1 is on the local right. The center will therefore be 0.5
                uvs.Add(new Vector2 (0, uvYScale));
                uvs.Add(new Vector2 (1, uvYScale));
                uvs.Add(new Vector2 (0.5f, uvYScale));


                /*** Add lane vertices ***/
                // Create each lane. For a one lane road, the total two lanes are split along the center so we do not need to create more vertices
                // For roads with more lanes, additional vertices are created to be able to generate each lane
                for(int l = 0, index = 0; l < _laneCount - 1; l++, index += 2)
                {
                    // The UV offset is based on the current lane. 
                    // Since the lanes are created in pairs around the center, the offset is calculated with 0.5 as a starting point
                    // Then, it is scaled with the current lane distance from the center
                    float uvOffset = 0.5f * (float)(l + 1) / (float)_laneCount;

                    // The lane vertices are created by offsetting the center vertices with the lane width
                    verts.Add(vertCenter - localRight * Mathf.Abs(_road.LaneWidth) * (l + 1));
                    verts.Add(vertCenter + localRight * Mathf.Abs(_road.LaneWidth) * (l + 1));
                    
                    // The lanes have normals pointing up
                    normals.Add(localUp);
                    normals.Add(localUp);
                    
                    // Set the UVs using the calculated UV offset
                    uvs.Add(new Vector2 (0.5f - uvOffset, uvYScale));
                    uvs.Add(new Vector2 (0.5f + uvOffset, uvYScale));
                }
                
                // An offset used for simplicity since the amount of lanes that were added determine the current index
                int laneOffset = 2 * (_laneCount - 1);
                
                
                /*** Add bottom of road vertices ***/
                verts.Add(vertSideA - localUp * _thickness);
                verts.Add(vertSideB - localUp * _thickness);
                
                // The bottom of the road has normals pointing down
                normals.Add(-localUp);
                normals.Add(-localUp);
                
                // The UVs are calculated the same as for the top of the road
                uvs.Add(new Vector2 (0, uvYScale));
                uvs.Add(new Vector2 (1, uvYScale));

                
                /*** Add side of road vertices ***/
                // Duplicate vertices to get flat shading for sides of road
                // The vertices are duplicates of the road edge vertices and bottom vertices
                verts.Add(verts[vertIndex + 0]);
                verts.Add(verts[vertIndex + 1]);
                verts.Add(verts[laneOffset + vertIndex + 3]);
                verts.Add(verts[laneOffset + vertIndex + 4]);
                
                // The sides of the road have normals pointing outwards from the road, opposite each other
                normals.Add(localRight);
                normals.Add(localRight);
                normals.Add(-localRight);
                normals.Add(-localRight);

                // The UVs are calculated the same as for the top of the road
                uvs.Add(new Vector2 (0, uvYScale));
                uvs.Add(new Vector2 (0, uvYScale));
                uvs.Add(new Vector2 (1, uvYScale));
                uvs.Add(new Vector2 (1, uvYScale));
                

                /*** Set triangle indices ***/
                // Get the current lane triangle map
                List<int> laneTriangleMap = laneTriangleMaps[_laneCount - 1];
                
                if((curr.Next != null && !curr.Next.IsIntersection()) || path.isClosedLoop)
                {
                    // Set the lane triangle indices
                    for (int j = 0; j < laneTriangleMap.Count; j++) 
                    {
                        for(int l = 0; l < 2 * _laneCount; l++)
                        {
                            laneTriangles[l][laneTriIndex + j] = (vertIndex + laneTriangleMap[j]) % vertsLength;
                        }
                    }
                    
                    // Set the bottom triangle indices
                    int bottomTriangleOffset = 2 * _laneCount + 1;
                    for(int j  = 0; j < bottomTriangleMap.Length; j++)
                    {
                        // Reverse triangle map for the bottom so that triangles wind the other way and are visible from underneath
                        bottomTriangles.Add((vertIndex + bottomTriangleMap[bottomTriangleMap.Length - 1 - j] + bottomTriangleOffset) % vertsLength);
                    }
                    
                    // Set the side triangle indices
                    int sideTriangleOffset = 2 * _laneCount + 1 + 2;
                    for (int j = 0; j < sidesTriangleMap.Length; j++) 
                    {
                        sideOfRoadTriangles.Add((vertIndex + sidesTriangleMap[j] + sideTriangleOffset) % vertsLength);
                    }
                }

                laneTriIndex += laneTriangleMap.Count;

                curr = curr.Next;
            }

            _mesh.Clear();
            _mesh.vertices = verts.ToArray();
            _mesh.uv = uvs.ToArray();
            _mesh.normals = normals.ToArray();
            
            // Two submeshes are created for each lane, so with two directions that makes 2 * laneCount submeshes. 
            // Finally, two more are used for the bottom and sides
            _mesh.subMeshCount = 2 + 2 * _laneCount;
            
            // Set lane triangles
            for(int i = 0; i < laneTriangles.Count; i++)
            {
                _mesh.SetTriangles(laneTriangles[i].ToArray(), i);
            }

            // Add triangles for side of start and end of road if the road is not a closed loop
            if(!path.isClosedLoop)
            {
                int indexOffset = sideOfRoadTriangles.Count - 12;
                int startOffset = 2 * _laneCount + 1 + 2;
                int endOffset = verts.Count - 1;
                
                // Start of road
                sideOfRoadTriangles.Add(startOffset + 1);
                sideOfRoadTriangles.Add(startOffset + 3);
                sideOfRoadTriangles.Add(startOffset + 0);
                
                sideOfRoadTriangles.Add(startOffset + 0);
                sideOfRoadTriangles.Add(startOffset + 3);
                sideOfRoadTriangles.Add(startOffset + 2);

                // End of road
                sideOfRoadTriangles.Add(endOffset - 3);
                sideOfRoadTriangles.Add(endOffset - 0);
                sideOfRoadTriangles.Add(endOffset - 2);
                
                sideOfRoadTriangles.Add(endOffset - 1);
                sideOfRoadTriangles.Add(endOffset - 0);
                sideOfRoadTriangles.Add(endOffset - 3);
            }

            _mesh.SetTriangles(bottomTriangles, 2 * _laneCount);
            _mesh.SetTriangles(sideOfRoadTriangles, 2 * _laneCount + 1);
            _mesh.RecalculateBounds();
        }

        // Add MeshRenderer and MeshFilter components to this GameObject if not already attached
        private void AssignMeshComponents() 
        {
            // Let the road itself hold the mesh
            if (_meshHolder == null) 
            {
                _meshHolder = gameObject;
            }

            _meshHolder.transform.rotation = Quaternion.identity;
            _meshHolder.transform.position = Vector3.zero;
            _meshHolder.transform.localScale = Vector3.one;

            // Ensure mesh renderer and filter components are assigned
            if (!_meshHolder.gameObject.GetComponent<MeshFilter>()) 
            {
                _meshHolder.gameObject.AddComponent<MeshFilter>();
            }
            if (!_meshHolder.GetComponent<MeshRenderer>()) 
            {
                _meshHolder.gameObject.AddComponent<MeshRenderer>();
            }

            _meshRenderer = _meshHolder.GetComponent<MeshRenderer>();
            _meshFilter = _meshHolder.GetComponent<MeshFilter>();
            
            // Create a new mesh if one does not already exist
            if (_mesh == null) 
            {
                _mesh = new Mesh();
            }
            _meshFilter.sharedMesh = _mesh;
        }

        private void AssignMaterials() {
            if (_laneMaterialCopy != null && _bottomMaterial != null) 
            {
                // Calculate the texture tiling based on the length of the road and the texture height
                float textureTiling = (_road.Length / _laneMaterial.mainTexture.height) * _textureTilingScale;
                
                // Create an array of materials for the mesh renderer
                // It will hold a bottom material, a side material and a material for each lane
                Material[] materials = new Material[2 + 2 * _laneCount];
                
                // Set the lane materials
                for(int i = 0; i < 2 * _laneCount; i++)
                {
                    materials[i] = _laneMaterialCopy;
                }
                
                // Set the bottom material
                materials[2 * _laneCount] = _bottomMaterial;
                
                // Set the side material
                materials[2 * _laneCount + 1] = _bottomMaterial;
                
                // Assign the materials to the mesh renderer
                _meshRenderer.sharedMaterials = materials;
                _meshRenderer.sharedMaterials[0].mainTextureScale = new Vector3(2 * _laneCount, textureTiling);
            }
        }
    }
}