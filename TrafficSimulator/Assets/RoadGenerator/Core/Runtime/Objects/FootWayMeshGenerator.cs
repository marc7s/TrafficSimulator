using System.Collections.Generic;
using UnityEngine;

namespace RoadGenerator
{
    public class FootWayMeshBuilder
    {
        enum VerticeType
        {
            BottomLeft = 0,
            TopLeft = 1,
            BottomRight = 2,
            TopRight = 3
        }

        public List<Vector3> Vertices = new List<Vector3>();
        public List<int> Triangles = new List<int>();
        public List<Vector2> UVS = new List<Vector2>();
        public List<Vector3> Normals = new List<Vector3>();
        private float _height;

        public FootWayMeshBuilder(float height)
        {
            _height = height;
        }

        public void AddNode((Vector3, Vector3) InnerOuterPoints)
        {
            bool isFirst = Vertices.Count == 0;
            int startIndex = Vertices.Count;
            Vector3 BottomLeft = InnerOuterPoints.Item1;
            Vector3 TopLeft = InnerOuterPoints.Item1 + Vector3.up * _height;
            Vector3 BottomRight = InnerOuterPoints.Item2;
            Vector3 TopRight = InnerOuterPoints.Item2 + Vector3.up * _height;
            
            Vertices.Add(BottomLeft);
            Vertices.Add(TopLeft);
            Vertices.Add(BottomRight);
            Vertices.Add(TopRight);

            if (isFirst)
                return;

            int prevStartIndex = startIndex - 4;
            int prevBottomLeftIndex = prevStartIndex + (int)VerticeType.BottomLeft;
            int prevTopLeftIndex = prevStartIndex + (int)VerticeType.TopLeft;
            int prevBottomRightIndex = prevStartIndex + (int)VerticeType.BottomRight;
            int prevTopRightIndex = prevStartIndex + (int)VerticeType.TopRight;
            
            int bottomLeftIndex = startIndex + (int)VerticeType.BottomLeft;
            int topLeftIndex = startIndex + (int)VerticeType.TopLeft;
            int bottomRightIndex = startIndex + (int)VerticeType.BottomRight;
            int topRightIndex = startIndex + (int)VerticeType.TopRight;

            AddSide(bottomLeftIndex, topLeftIndex, prevBottomLeftIndex, prevTopLeftIndex);
            AddSide(topLeftIndex, topRightIndex, prevTopLeftIndex, prevTopRightIndex);
            AddSide(topRightIndex, bottomRightIndex, prevTopRightIndex, prevBottomRightIndex);   
        }

        private void AddSide(int sidePoint1Index, int sidePoint2Index, int prevSidePoint1Index, int prevSidePoint2Index)
        {
            Triangles.Add(prevSidePoint1Index);
            Triangles.Add(prevSidePoint2Index);
            Triangles.Add(sidePoint1Index);
            
            Triangles.Add(prevSidePoint2Index);
            Triangles.Add(sidePoint2Index);
            Triangles.Add(sidePoint1Index);
        }
    }

    public class FootWayMeshGenerator
    {
        List<Vector3> _vertices = new List<Vector3>();
        List<int> _triangles = new List<int>();
        List<Vector2> _uvs = new List<Vector2>();
        List<Vector3> _normals = new List<Vector3>();

        public Mesh GenerateMesh(List<(Vector3, Vector3)> InnerOuterPoints, float height)
        {
            _vertices.Clear();
            _triangles.Clear();
            _uvs.Clear();
            _normals.Clear();

            FootWayMeshBuilder builder = new FootWayMeshBuilder(height);

            foreach (var innerOuterPoints in InnerOuterPoints)
                builder.AddNode(innerOuterPoints);

            Mesh mesh = new Mesh();
            mesh.vertices = builder.Vertices.ToArray();
            mesh.triangles = builder.Triangles.ToArray();
            mesh.uv = builder.UVS.ToArray();
            mesh.normals = builder.Normals.ToArray();
            
            return mesh;
        }

        public Mesh GenerateMesh(List<Vector3> midPoints, float width, float height)
        {
            _vertices.Clear();
            _triangles.Clear();
            _uvs.Clear();
            _normals.Clear();

            FootWayMeshBuilder builder = new FootWayMeshBuilder(height);

            for (int i = 0; i < midPoints.Count - 1; i++)
            {
                Vector3 point1 = midPoints[i];
                Vector3 point2 = midPoints[i + 1];
                Vector3 direction = (point2 - point1).normalized;
                Vector3 perpendicular = Vector3.Cross(direction, Vector3.up);
                Vector3 bottomLeft = point1 - perpendicular * width / 2;
                Vector3 bottomRight = point1 + perpendicular * width / 2;
                builder.AddNode((bottomLeft, bottomRight));
            }

            Mesh mesh = new Mesh();
            mesh.vertices = builder.Vertices.ToArray();
            mesh.triangles = builder.Triangles.ToArray();
            mesh.uv = builder.UVS.ToArray();
            mesh.normals = builder.Normals.ToArray();
            
            return mesh;
        }
    }
}