using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;

namespace RoadGenerator
{
    public class BuildingMeshCreator
    {
        public static void GenerateBuildingMesh(GameObject building, List<Vector3> buildingPointsBottom, List<Vector3> buildingPointsTop, List<BuildingPoints> buildingPoints, Material buildingWallMaterial, Material buildingRoofMaterial)
        {
            Mesh buildingMesh = AssignMeshComponents(building, buildingWallMaterial, buildingRoofMaterial);

            // Create roofs for buildings
            List<Triangle> triangles = new List<Triangle>();

            try
            {
                triangles.AddRange(TriangulateConcavePolygon(buildingPointsTop));
            }
            catch (Exception){}

            buildingPointsTop.Reverse();

            try
            {
                triangles.AddRange(TriangulateConcavePolygon(buildingPointsTop));
            }
            catch (Exception){}

            CreateBuildingMesh(buildingMesh, buildingPoints, triangles);
        }

        private static void CreateBuildingMesh(Mesh buildingMesh, List<BuildingPoints> buildingPoints, List<Triangle> triangles)
        {
            List<Vector3> bottomPoints = new List<Vector3>();
            Dictionary<Vector3, int> positionToIndex = new Dictionary<Vector3, int>();
            List<Vector3> verts = new List<Vector3>();
            List<int> wallTris = new List<int>();
            List<int> roofTris = new List<int>();

            foreach (BuildingPoints buildingPoint in buildingPoints)
            {
                verts.Add(buildingPoint.BottomPoint);

                if (!positionToIndex.ContainsKey(buildingPoint.BottomPoint))
                {
                    positionToIndex.Add(buildingPoint.BottomPoint, verts.Count - 1);
                    bottomPoints.Add(buildingPoint.BottomPoint);
                }

                verts.Add(buildingPoint.TopPoint);

                if (!positionToIndex.ContainsKey(buildingPoint.TopPoint))
                    positionToIndex.Add(buildingPoint.TopPoint, verts.Count - 1);
            }

            bool isBuildingClockwise = IsBuildingClockWise(bottomPoints);
            int index = -1;
            bool isFirstIteration = true;
            
            foreach (BuildingPoints buildingPoint in buildingPoints)
            {
                if (isFirstIteration)
                {
                    isFirstIteration = false;
                    index += 2;
                    continue;
                }
                
                index += 2;
                AddBuildingWall(index, index -1, index - 2, index - 3, wallTris, isBuildingClockwise);
            }

            foreach (Triangle triangle in triangles)
            {
                if (positionToIndex.ContainsKey(triangle.Vertex1.Position) && positionToIndex.ContainsKey(triangle.Vertex2.Position) && positionToIndex.ContainsKey(triangle.Vertex3.Position))
                {
                    roofTris.Add(positionToIndex[triangle.Vertex1.Position]);
                    roofTris.Add(positionToIndex[triangle.Vertex2.Position]);
                    roofTris.Add(positionToIndex[triangle.Vertex3.Position]);
                }
            }

            buildingMesh.Clear();
            buildingMesh.vertices = verts.ToArray();
            buildingMesh.subMeshCount = 2;
            buildingMesh.SetTriangles(wallTris.ToArray(), 0);
            buildingMesh.SetTriangles(roofTris.ToArray(), 1);
            buildingMesh.RecalculateBounds();
        }

        private static bool IsBuildingClockWise(List<Vector3> buildingPoints)
        {
            float sum = 0;

            for (int i = 0; i < buildingPoints.Count; i++)
            {
                Vector3 currentPoint = buildingPoints[i];
                Vector3 nextPoint = buildingPoints[(i + 1) % buildingPoints.Count];

                sum += (nextPoint.x - currentPoint.x) * (nextPoint.z + currentPoint.z);
            }

            return sum > 0;
        }

        private static void AddBuildingWall(int currentSideTopIndex, int currentSideBottomIndex, int prevSideTopIndex, int prevSideBottomIndex, List<int> triangles, bool isClockWise)
        {
            if (isClockWise)
            {
                triangles.Add(prevSideBottomIndex);
                triangles.Add(currentSideTopIndex);
                triangles.Add(prevSideTopIndex);

                triangles.Add(currentSideTopIndex);
                triangles.Add(prevSideBottomIndex);
                triangles.Add(currentSideBottomIndex);
            }
            else
            {
                triangles.Add(prevSideTopIndex);
                triangles.Add(currentSideTopIndex);
                triangles.Add(prevSideBottomIndex);

                triangles.Add(currentSideBottomIndex);
                triangles.Add(prevSideBottomIndex);
                triangles.Add(currentSideTopIndex);
            }
        }

        private static Mesh AssignMeshComponents(GameObject buildingObject, Material buildingWallMaterial, Material buildingRoofMaterial)
        {
            buildingObject.transform.rotation = Quaternion.identity;
            buildingObject.transform.position = Vector3.zero;
            buildingObject.transform.localScale = Vector3.one;

            // Ensure mesh renderer and filter components are assigned
            if (!buildingObject.gameObject.GetComponent<MeshFilter>()) 
                buildingObject.gameObject.AddComponent<MeshFilter>();

            if (!buildingObject.GetComponent<MeshRenderer>()) 
                buildingObject.gameObject.AddComponent<MeshRenderer>();

            MeshRenderer _meshRenderer = buildingObject.GetComponent<MeshRenderer>();
            _meshRenderer.sharedMaterials = new Material[]{ buildingWallMaterial, buildingRoofMaterial };
            MeshFilter _meshFilter = buildingObject.GetComponent<MeshFilter>();

            Mesh mesh = new Mesh();

            _meshFilter.sharedMesh = mesh;
            return mesh;
        }
        public static List<Triangle> TriangulateConcavePolygon(List<Vector3> points)
        {
            // The list with triangles the method returns
            List<Triangle> triangles = new List<Triangle>();

            // If we just have three points, then we dont have to do all calculations
            if (points.Count == 3)
            {
                triangles.Add(new Triangle(points[0], points[1], points[2]));
                return triangles;
            }

            // Step 1. Store the vertices in a list and we also need to know the next and prev vertex
            List<Vertex> vertices = new List<Vertex>();

            for (int i = 0; i < points.Count; i++)
                vertices.Add(new Vertex(points[i]));

            // Find the next and previous vertex
            for (int i = 0; i < vertices.Count; i++)
            {
                int nextPos = ClampListIndex(i + 1, vertices.Count);
                int prevPos = ClampListIndex(i - 1, vertices.Count);

                vertices[i].PrevVertex = vertices[prevPos];
                vertices[i].NextVertex = vertices[nextPos];
            }

            // Step 2. Find the reflex (concave) and convex vertices, and ear vertices
            for (int i = 0; i < vertices.Count; i++)
                CheckIfReflexOrConvex(vertices[i]);

            // Have to find the ears after we have found if the vertex is reflex or convex
            List<Vertex> earVertices = new List<Vertex>();
            
            for (int i = 0; i < vertices.Count; i++)
                IsVertexEar(vertices[i], vertices, earVertices);

            // Step 3. Triangulate!
            while (true)
            {
                // This means we have just one triangle left
                if (vertices.Count == 3)
                {
                    // The final triangle
                    triangles.Add(new Triangle(vertices[0], vertices[0].PrevVertex, vertices[0].NextVertex));
                    break;
                }

                // Make a triangle of the first ear
                Vertex earVertex = earVertices[0];

                Vertex earVertexPrev = earVertex.PrevVertex;
                Vertex earVertexNext = earVertex.NextVertex;

                Triangle newTriangle = new Triangle(earVertex, earVertexPrev, earVertexNext);

                triangles.Add(newTriangle);

                // Remove the vertex from the lists
                earVertices.Remove(earVertex);

                vertices.Remove(earVertex);

                // Update the previous vertex and next vertex
                earVertexPrev.NextVertex = earVertexNext;
                earVertexNext.PrevVertex = earVertexPrev;

                //...see if we have found a new ear by investigating the two vertices that was part of the ear
                CheckIfReflexOrConvex(earVertexPrev);
                CheckIfReflexOrConvex(earVertexNext);

                earVertices.Remove(earVertexPrev);
                earVertices.Remove(earVertexNext);

                IsVertexEar(earVertexPrev, vertices, earVertices);
                IsVertexEar(earVertexNext, vertices, earVertices);
            }

            return triangles;
        }

        public static int ClampListIndex(int index, int listSize)
        {
            index = ((index % listSize) + listSize) % listSize;
            return index;
        }

        public static bool IsTriangleOrientedClockwise(Vector2 p1, Vector2 p2, Vector2 p3)
        {
            bool isClockWise = true;
            float determinant = p1.x * p2.y + p3.x * p1.y + p2.x * p3.y - p1.x * p3.y - p3.x * p2.y - p2.x * p1.y;

            if (determinant > 0f)
                isClockWise = false;

            return isClockWise;
        }

        public static bool IsPointInTriangle(Vector2 p1, Vector2 p2, Vector2 p3, Vector2 p)
        {
            bool isWithinTriangle = false;

            //Based on Barycentric coordinates
            float denominator = ((p2.y - p3.y) * (p1.x - p3.x) + (p3.x - p2.x) * (p1.y - p3.y));

            float a = ((p2.y - p3.y) * (p.x - p3.x) + (p3.x - p2.x) * (p.y - p3.y)) / denominator;
            float b = ((p3.y - p1.y) * (p.x - p3.x) + (p1.x - p3.x) * (p.y - p3.y)) / denominator;
            float c = 1 - a - b;

            //The point is within the triangle or on the border if 0 <= a <= 1 and 0 <= b <= 1 and 0 <= c <= 1
            //if (a >= 0f && a <= 1f && b >= 0f && b <= 1f && c >= 0f && c <= 1f)
            //{
            //    isWithinTriangle = true;
            //}

            //The point is within the triangle
            if (a > 0f && a < 1f && b > 0f && b < 1f && c > 0f && c < 1f)
                isWithinTriangle = true;

            return isWithinTriangle;
        }

        // Check if a vertex if reflex or convex, and add to appropriate list
        private static void CheckIfReflexOrConvex(Vertex v)
        {
            v.IsReflex = false;
            v.IsConvex = false;

            // This is a reflex vertex if its triangle is oriented clockwise
            Vector2 a = v.PrevVertex.GetPos2D_XZ();
            Vector2 b = v.GetPos2D_XZ();
            Vector2 c = v.NextVertex.GetPos2D_XZ();

            if (IsTriangleOrientedClockwise(a, b, c))
                v.IsReflex = true;
            else
                v.IsConvex = true;
        }

        // Check if a vertex is an ear
        private static void IsVertexEar(Vertex v, List<Vertex> vertices, List<Vertex> earVertices)
        {
            // A reflex vertex cant be an ear!
            if (v.IsReflex)
                return;

            // This triangle to check point in triangle
            Vector2 a = v.PrevVertex.GetPos2D_XZ();
            Vector2 b = v.GetPos2D_XZ();
            Vector2 c = v.NextVertex.GetPos2D_XZ();

            bool hasPointInside = false;

            for (int i = 0; i < vertices.Count; i++)
            {
                // We only need to check if a reflex vertex is inside of the triangle
                if (vertices[i].IsReflex)
                {
                    Vector2 p = vertices[i].GetPos2D_XZ();

                    // This means inside and not on the hull
                    if (IsPointInTriangle(a, b, c, p))
                    {
                        hasPointInside = true;
                        break;
                    }
                }
            }

            if (!hasPointInside)
                earVertices.Add(v);
        }

        public class Vertex
        {
            public Vector3 Position;

            // The outgoing halfedge (a halfedge that starts at this vertex)
            // Doesnt matter which edge we connect to it
            public HalfEdge HalfEdge;

            // Which triangle is this vertex a part of?
            public Triangle Triangle;

            // The previous and next vertex this vertex is attached to
            public Vertex PrevVertex;
            public Vertex NextVertex;

            // Properties this vertex may have
            // Reflex is concave
            public bool IsReflex; 
            public bool IsConvex;
            public bool IsEar;

            public Vertex(Vector3 position)
            {
                this.Position = position;
            }

            // Get 2d pos of this vertex
            public Vector2 GetPos2D_XZ()
            {
                Vector2 pos_2d_xz = new Vector2(Position.x, Position.z);
                return pos_2d_xz;
            }
        }

        public class HalfEdge
        {
            // The vertex the edge points to
            public Vertex Vertex;

            // The face this edge is a part of
            public Triangle Triangle;

            // The next edge
            public HalfEdge NextEdge;
            
            // The previous
            public HalfEdge PrevEdge;
            
            // The edge going in the opposite direction
            public HalfEdge OppositeEdge;

            // This structure assumes we have a vertex class with a reference to a half edge going from that vertex
            // and a face (triangle) class with a reference to a half edge which is a part of this face 
            public HalfEdge(Vertex vertex)
            {
                this.Vertex = vertex;
            }
        }

        public class Triangle
        {
            // Corners
            public Vertex Vertex1;
            public Vertex Vertex2;
            public Vertex Vertex3;

            // If we are using the half edge mesh structure, we just need one half edge
            public HalfEdge HalfEdge;

            public Triangle(Vertex v1, Vertex v2, Vertex v3)
            {
                Vertex1 = v1;
                Vertex2 = v2;
                Vertex3 = v3;
            }

            public Triangle(Vector3 v1, Vector3 v2, Vector3 v3)
            {
                Vertex1 = new Vertex(v1);
                Vertex2 = new Vertex(v2);
                Vertex3 = new Vertex(v3);
            }

            public Triangle(HalfEdge halfEdge)
            {
                this.HalfEdge = halfEdge;
            }

            // Change orientation of triangle from cw -> ccw or ccw -> cw
            public void ChangeOrientation()
            {
                Vertex temp = Vertex1;
                Vertex1 = Vertex2;
                Vertex2 = temp;
            }
        }

        public class Edge
        {
            public Vertex Vertex1;
            public Vertex Vertex2;

            // Is this edge intersecting with another edge?
            public bool IsIntersecting = false;

            public Edge(Vertex v1, Vertex v2)
            {
                Vertex1 = v1;
                Vertex2 = v2;
            }

            public Edge(Vector3 v1, Vector3 v2)
            {
                Vertex1 = new Vertex(v1);
                Vertex2 = new Vertex(v2);
            }

            // Get vertex in 2d space (assuming x, z)
            public Vector2 GetVertex2D(Vertex v)
            {
                return new Vector2(v.Position.x, v.Position.z);
            }

            // Flip edge
            public void FlipEdge()
            {
                Vertex temp = Vertex1;
                Vertex1 = Vertex2;
                Vertex2 = temp;
            }
        }

        public class Plane
        { 
            public Vector3 Position;

            public Vector3 Normal;

            public Plane(Vector3 position, Vector3 normal)
            {
                Position = position;
                Normal = normal;
            }
        }
    }
}