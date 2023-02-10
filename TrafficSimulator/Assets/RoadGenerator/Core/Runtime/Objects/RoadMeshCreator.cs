using System.Collections.Generic;
using UnityEngine;

namespace RoadGenerator 
{
    public enum LaneAmount 
    {
        One = 1,
        Two = 2,
        Three = 3,
        Four = 4
    }
    public class RoadMeshCreator : PathSceneTool 
    {
        [Header ("Road settings")]
        public float laneWidth = 4f;
        public LaneAmount laneAmount = LaneAmount.One;
        [Range (0, .5f)]
        public float thickness = .15f;
        public bool flattenSurface;

        [Header ("Material settings")]
        public Material laneMaterial;
        public Material bottomMaterial;
        public float textureTilingScale = 100;
        private int laneCount;

        [SerializeField, HideInInspector]
        GameObject meshHolder;

        MeshFilter meshFilter;
        MeshRenderer meshRenderer;
        Mesh mesh;

        protected override void PathUpdated() 
        {
            // Set the lane count to avoid having to cast the enum to an int everytime
            laneCount = (int)laneAmount;
            
            if (pathCreator != null) 
            {
                AssignMeshComponents();
                AssignMaterials();
                CreateRoadMesh();
            }
        }

        void CreateRoadMesh() 
        {
            // The number of vertices required per component of the road
            int edgeVertsPerPoint = 2;
            int laneVertsPerPoint = 2 * laneCount - 1;
            int bottomVertsPerPoint = 2;
            int sideVertsPerPoint = 4;

            // Create the arrays for the vertices, uvs and normals
            Vector3[] verts = new Vector3[path.NumPoints * (edgeVertsPerPoint + laneVertsPerPoint + bottomVertsPerPoint + sideVertsPerPoint)];
            Vector2[] uvs = new Vector2[verts.Length];
            Vector3[] normals = new Vector3[verts.Length];

            // Calculate the number of triangles required for each component of the road
            int laneNumTris = 2 * laneCount * (2 * (path.NumPoints - 1) + (path.isClosedLoop ? 2 : 0));
            int bottomNumTris = 2 * (path.NumPoints - 1) + (path.isClosedLoop ? 2 : 0);
            int sideNumTris = 4 * (path.NumPoints - 1) + 4;

            // Create the arrays for the triangles
            int[,] laneTriangles = new int[2 * laneCount, laneNumTris * 3];
            int[] bottomTriangles = new int[bottomNumTris * 3];
            int[] sideOfRoadTriangles = new int[sideNumTris * 3];

            // Initialise the array indices used for the components
            int vertIndex = 0;
            int laneTriIndex = 0;
            int bottomTriIndex = 0;
            int sideTriIndex = 0;

            // The triangle map for the bottom of the road
            int[] bottomTriangleMap = 
            { 
                0,  2 * laneCount + 7,  1, 
                1,  2 * laneCount + 7,  2 * laneCount + 8
            };

            // The triangle map for the sides of the road
            int[] sidesTriangleMap = 
            {
                0,                  2,                   2 * laneCount + 7,
                2 * laneCount + 7,  2,                   2 * laneCount + 9,
                2 * laneCount + 8,  2 * laneCount + 10,  1,
                1,                  2 * laneCount + 10,  3
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

            bool usePathNormals = !(path.space == PathSpace.xyz && flattenSurface);

            for (int i = 0; i < path.NumPoints; i++) 
            {
                // Store the Y scale to be used for the UVs. It is the path time (0 at the start of the path and 1 at the end)
                float uvYScale = path.times[i];

                // Calculate the local directional vectors
                Vector3 localUp = usePathNormals ? Vector3.Cross(path.GetTangent(i), path.GetNormal(i)) : path.up;
                Vector3 localRight = usePathNormals ? path.GetNormal(i) : Vector3.Cross(localUp, path.GetTangent(i));

                // Find position to left and right of current path vertex
                Vector3 vertCenter = path.GetPoint(i);
                Vector3 vertSideA = vertCenter - localRight * Mathf.Abs(laneWidth) * laneCount;
                Vector3 vertSideB = vertCenter + localRight * Mathf.Abs(laneWidth) * laneCount;


                /*** Add top of road vertices ***/
                verts[vertIndex + 0] = vertSideA;
                verts[vertIndex + 1] = vertSideB;
                verts[vertIndex + 2] = vertCenter;

                // The top of the road has normals pointing up
                normals[vertIndex + 0] = localUp;
                normals[vertIndex + 1] = localUp;
                normals[vertIndex + 2] = localUp;
                
                // The UV x axis is set so that 0 is on the local left and 1 is on the local right. The center will therefore be 0.5
                uvs[vertIndex + 0] = new Vector2 (0, uvYScale);
                uvs[vertIndex + 1] = new Vector2 (1, uvYScale);
                uvs[vertIndex + 2] = new Vector2 (0.5f, uvYScale);


                /*** Add lane vertices ***/
                // Create each lane. For a one lane road, the total two lanes are split along the center so we do not need to create more vertices
                // For roads with more lanes, additional vertices are created to be able to generate each lane
                for(int l = 0, index = 0; l < laneCount - 1; l++, index += 2)
                {
                    // The UV offset is based on the current lane. 
                    // Since the lanes are created in pairs around the center, the offset is calculated with 0.5 as a starting point
                    // Then, it is scaled with the current lane distance from the center
                    float uvOffset = 0.5f * (float)(l + 1) / (float)laneAmount;

                    // The lane vertices are created by offsetting the center vertices with the lane width
                    verts[vertIndex + 3 + index] = vertCenter - localRight * Mathf.Abs(laneWidth) * (l + 1);
                    verts[vertIndex + 4 + index] = vertCenter + localRight * Mathf.Abs(laneWidth) * (l + 1);
                    
                    // The lanes have normals pointing up
                    normals[vertIndex + 3 + index] = localUp;
                    normals[vertIndex + 4 + index] = localUp;
                    
                    // Set the UVs using the calculated UV offset
                    uvs[vertIndex + 3 + index] = new Vector2 (0.5f - uvOffset, uvYScale);
                    uvs[vertIndex + 4 + index] = new Vector2 (0.5f + uvOffset, uvYScale);
                }
                
                // An offset used for simplicity since the amount of lanes that were added determine the current index
                int laneOffset = 2 * (laneCount - 1);
                
                
                /*** Add bottom of road vertices ***/
                verts[laneOffset + vertIndex + 3] = vertSideA - localUp * thickness;
                verts[laneOffset + vertIndex + 4] = vertSideB - localUp * thickness;
                
                // The bottom of the road has normals pointing down
                normals[laneOffset + vertIndex + 3] = -localUp;
                normals[laneOffset + vertIndex + 4] = -localUp;
                
                // The UVs are calculated the same as for the top of the road
                uvs[laneOffset + vertIndex + 3] = new Vector2 (0, uvYScale);
                uvs[laneOffset + vertIndex + 4] = new Vector2 (1, uvYScale);

                
                /*** Add side of road vertices ***/
                // Duplicate vertices to get flat shading for sides of road
                // The vertices are duplicates of the road edge vertices and bottom vertices
                verts[laneOffset + vertIndex + 5] = verts[vertIndex + 0];
                verts[laneOffset + vertIndex + 6] = verts[vertIndex + 1];
                verts[laneOffset + vertIndex + 7] = verts[laneOffset + vertIndex + 3];
                verts[laneOffset + vertIndex + 8] = verts[laneOffset + vertIndex + 4];
                
                // The sides of the road have normals pointing outwards from the road, opposite each other
                normals[laneOffset + vertIndex + 5] = localRight;
                normals[laneOffset + vertIndex + 6] = localRight;
                normals[laneOffset + vertIndex + 7] = -localRight;
                normals[laneOffset + vertIndex + 8] = -localRight;

                uvs[laneOffset + vertIndex + 5] = new Vector2 (0, uvYScale);
                uvs[laneOffset + vertIndex + 6] = new Vector2 (0, uvYScale);
                uvs[laneOffset + vertIndex + 7] = new Vector2 (1, uvYScale);
                uvs[laneOffset + vertIndex + 8] = new Vector2 (1, uvYScale);
                

                /*** Set triangle indices ***/
                // Get the current lane triangle map
                List<int> laneTriangleMap = laneTriangleMaps[laneCount - 1];
                
                if (i < path.NumPoints - 1 || path.isClosedLoop)
                {
                    // Set the lane triangle indices
                    for (int j = 0; j < laneTriangleMap.Count; j++) 
                    {
                        for(int l = 0; l < 2 * laneCount; l++)
                        {
                            laneTriangles[l, laneTriIndex + j] = (vertIndex + laneTriangleMap[j]) % verts.Length;
                        }
                    }
                    
                    // Set the bottom triangle indices
                    int bottomTriangleOffset = 2 * laneCount + 1;
                    for(int j  = 0; j < bottomTriangleMap.Length; j++)
                    {
                        // Reverse triangle map for the bottom so that triangles wind the other way and are visible from underneath
                        bottomTriangles[bottomTriIndex + j] = (vertIndex + bottomTriangleMap[bottomTriangleMap.Length - 1 - j] + bottomTriangleOffset) % verts.Length;
                    }
                    
                    // Set the side triangle indices
                    int sideTriangleOffset = 2 * laneCount + 1 + 2;
                    for (int j = 0; j < sidesTriangleMap.Length; j++) 
                    {
                        sideOfRoadTriangles[sideTriIndex + j] = (vertIndex + sidesTriangleMap[j] + sideTriangleOffset) % verts.Length;
                    }
                }

                vertIndex += 4 + 2 + 2 + 2 * laneCount - 1;
                laneTriIndex += laneTriangleMap.Count;
                bottomTriIndex += bottomTriangleMap.Length;
                sideTriIndex += sidesTriangleMap.Length;
            }

            mesh.Clear();
            mesh.vertices = verts;
            mesh.uv = uvs;
            mesh.normals = normals;
            
            // Two submeshes are created for each lane, so with two directions that makes 2 * laneCount submeshes. 
            // Finally, two more are used for the bottom and sides
            mesh.subMeshCount = 2 + 2 * laneCount;
            
            // Set lane triangles
            for(int l = 0; l < 2 * laneCount; l++)
            {
                // Create a new array for the lane triangles
                int[] laneTrianglesArray = new int[laneTriangles.GetLength(1)];
                
                // Populate the array
                for(int i = 0; i < laneTriangles.GetLength(1); i++)
                {
                    laneTrianglesArray[i] = laneTriangles[l, i];
                }

                // Set the lane triangles for the submesh
                mesh.SetTriangles(laneTrianglesArray, l);
            }

            // Add triangles for side of start and end of road if the road is not a closed loop
            if(!path.isClosedLoop)
            {
                int indexOffset = sideOfRoadTriangles.Length - 12;
                int startOffset = 2 * laneCount + 1 + 2;
                int endOffset = verts.Length - 1;
                
                // Start of road
                sideOfRoadTriangles[indexOffset] = startOffset + 1;
                sideOfRoadTriangles[indexOffset + 1] = startOffset + 3;
                sideOfRoadTriangles[indexOffset + 2] = startOffset + 0;
                
                sideOfRoadTriangles[indexOffset + 3] = startOffset + 0;
                sideOfRoadTriangles[indexOffset + 4] = startOffset + 3;
                sideOfRoadTriangles[indexOffset + 5] = startOffset + 2;

                // End of road
                sideOfRoadTriangles[indexOffset + 6] = endOffset - 3;
                sideOfRoadTriangles[indexOffset + 7] = endOffset - 0;
                sideOfRoadTriangles[indexOffset + 8] = endOffset - 2;
                
                sideOfRoadTriangles[indexOffset + 9] = endOffset - 1;
                sideOfRoadTriangles[indexOffset + 10] = endOffset - 0;
                sideOfRoadTriangles[indexOffset + 11] = endOffset - 3;
            }

            mesh.SetTriangles(bottomTriangles, 2 * laneCount);
            mesh.SetTriangles(sideOfRoadTriangles, 2 * laneCount + 1);
            mesh.RecalculateBounds();
        }

        // Add MeshRenderer and MeshFilter components to this GameObject if not already attached
        void AssignMeshComponents() 
        {
            // Let the road itself hold the mesh
            if (meshHolder == null) 
            {
                meshHolder = gameObject;
            }

            meshHolder.transform.rotation = Quaternion.identity;
            meshHolder.transform.position = Vector3.zero;
            meshHolder.transform.localScale = Vector3.one;

            // Ensure mesh renderer and filter components are assigned
            if (!meshHolder.gameObject.GetComponent<MeshFilter>()) 
            {
                meshHolder.gameObject.AddComponent<MeshFilter>();
            }
            if (!meshHolder.GetComponent<MeshRenderer>()) 
            {
                meshHolder.gameObject.AddComponent<MeshRenderer>();
            }

            meshRenderer = meshHolder.GetComponent<MeshRenderer>();
            meshFilter = meshHolder.GetComponent<MeshFilter>();
            
            // Create a new mesh if one does not already exist
            if (mesh == null) 
            {
                mesh = new Mesh();
            }
            meshFilter.sharedMesh = mesh;
        }

        void AssignMaterials() {
            if (laneMaterial != null && bottomMaterial != null) 
            {
                // Calculate the texture tiling based on the length of the road and the texture height
                float textureTiling = (path.length / laneMaterial.mainTexture.height) * textureTilingScale;
                
                // Create an array of materials for the mesh renderer
                // It will hold a bottom material, a side material and a material for each lane
                Material[] materials = new Material[2 + 2 * laneCount];
                
                // Set the lane materials
                for(int i = 0; i < 2 * laneCount; i++)
                {
                    materials[i] = laneMaterial;
                }
                
                // Set the bottom material
                materials[2 * laneCount] = bottomMaterial;
                
                // Set the side material
                materials[2 * laneCount + 1] = bottomMaterial;
                
                // Assign the materials to the mesh renderer
                meshRenderer.sharedMaterials = materials;
                meshRenderer.sharedMaterials[0].mainTextureScale = new Vector3(2 * laneCount, textureTiling);
            }
        }
    }
}