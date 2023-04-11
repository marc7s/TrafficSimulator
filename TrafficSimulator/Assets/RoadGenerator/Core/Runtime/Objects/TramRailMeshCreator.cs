using System.Collections.Generic;
using UnityEngine;
using System;

namespace RoadGenerator 
{
    public struct RailMeshVertices
    {
        public Vector3 OuterTop;
        public Vector3 OuterBottom;
        public Vector3 LeftRailLeftTop;
        public Vector3 LeftRailLeftBottom;
        public Vector3 LeftRailRightTop;
        public Vector3 LeftRailRightBottom;
        public Vector3 RightRailLeftTop;
        public Vector3 RightRailLeftBottom;
        public Vector3 RightRailRightTop;
        public Vector3 RightRailRightBottom;
        public Vector3 Center;
        
        public RailMeshVertices (Vector3 outerTop, Vector3 outerBottom, Vector3 leftRailLeftTop, Vector3 leftRailLeftBottom, Vector3 leftRailRightTop, Vector3 leftRailRightBottom, Vector3 rightRailLeftTop, Vector3 rightRailLeftBottom, Vector3 rightRailRightTop, Vector3 rightRailRightBottom, Vector3 center)
        {
            OuterTop = outerTop;
            OuterBottom = outerBottom;
            LeftRailLeftTop = leftRailLeftTop;
            LeftRailLeftBottom = leftRailLeftBottom;
            LeftRailRightTop = leftRailRightTop;
            LeftRailRightBottom = leftRailRightBottom;
            RightRailLeftTop = rightRailLeftTop;
            RightRailLeftBottom = rightRailLeftBottom;
            RightRailRightTop = rightRailRightTop;
            RightRailRightBottom = rightRailRightBottom;
            Center = center;
        }
    }

    public class MeshBuilder
    {
        /// <summary> The vertices for the mesh at every road node, the values are the order of the vertices in the list </summary>
        enum VerticeType
        {
            OuterTop = 0,
            OuterBottom = 1,
            InnerRailInnerPadding = 2,
            InnerRailInnerTop = 3,
            InnerRailInnerBottom = 4,
            InnerRailOuterTop = 5,
            InnerRailOuterBottom = 6,
            InnerRailOuterPadding = 7,
            OuterRailInnerPadding = 8,
            OuterRailInnerTop = 9,
            OuterRailInnerBottom = 10,
            OuterRailOuterTop = 11,
            OuterRailOuterBottom = 12,
            OuterRailOuterPadding = 13,
            CenterTop = 14,
            CenterBottom = 15
        }

        public enum RailSide
        {
            Left = 0,
            Right = 1
        }

        public List<Vector3> Vertices = new List<Vector3>();
        public List<int> TrianglesMainMesh = new List<int>();
        public List<int> TrianglesRails = new List<int>();
        public List<int> TrianglesBottom = new List<int>();
        public List<Vector2> UVs = new List<Vector2>();
        private TramRail _rail;
        private int _laneCount = 1;
        private bool _flattenSurface = true;
        private VertexPath _path;
        private int _count = 0;
        private int _rightSidePrevIndex;
        private int _leftSidePrevIndex;
        private int _numVerticesPerNode = Enum.GetNames(typeof(VerticeType)).Length;

        public MeshBuilder(VertexPath path, TramRail rail)
        {
            _rail = rail;
            _path = path;
        }

        public void AddNode(RoadNode node)
        {
            AddRailMeshVertices(node, RailSide.Right);
            AddRailMeshVertices(node, RailSide.Left);
            _count++;
        }

    public void AddRailMeshVertices(RoadNode node, RailSide railSide)
    {
        bool isFirst = Vertices.Count == 0;
        bool usePathNormals = !(_path.space == PathSpace.xyz && _flattenSurface);
        Vector3 localUp = usePathNormals ? Vector3.Cross(node.Tangent, node.Normal) : _path.up;
        Vector3 localRight = usePathNormals ? node.Normal : Vector3.Cross(localUp, node.Tangent);
        Vector3 localLeft = -localRight;

        Vector3 side = railSide == RailSide.Right ? localRight : localLeft;
        Vector3 otherSide = railSide == RailSide.Right ? localLeft : localRight;

        Vector3 vertCenterTop = node.Position;
        Vector3 vertCenterBottom = vertCenterTop - localUp * _rail.Thickness;
        Vector3 vertOuterTop = vertCenterTop + side * Mathf.Abs(_rail.LaneWidth) * _laneCount;
        Vector3 vertOuterBottom = vertOuterTop - localUp * _rail.Thickness;

        Vector3 vertInnerRailOuterTop = vertCenterTop + side * _rail.LaneWidth / 2 + otherSide * _rail.RailSpacing;
        Vector3 vertInnerRailOuterBottom = vertInnerRailOuterTop - localUp * _rail.RailDepth;
        Vector3 vertInnerRailInnerTop = vertInnerRailOuterTop + otherSide * _rail.RailWidth;
        Vector3 vertInnerRailInnerPadding = vertInnerRailInnerTop + otherSide * _rail.RailPadding;
        Vector3 vertInnerRailOuterPadding = vertInnerRailOuterTop + side * _rail.RailPadding;
        Vector3 vertInnerRailInnerBottom = vertInnerRailInnerTop - localUp * _rail.RailDepth;

        Vector3 vertOuterRailInnerTop =  vertCenterTop + side * _rail.LaneWidth / 2 + side * _rail.RailSpacing;
        Vector3 vertOuterRailInnerPadding = vertOuterRailInnerTop + otherSide * _rail.RailPadding;
        Vector3 vertOuterRailInnerBottom = vertOuterRailInnerTop - localUp * _rail.RailDepth;
        Vector3 vertOuterRailOuterTop = vertOuterRailInnerTop + side * _rail.RailWidth;
        Vector3 vertOuterRailOuterPadding = vertOuterRailOuterTop + side * _rail.RailPadding;
        Vector3 vertOuterRailOuterBottom = vertOuterRailOuterTop - localUp * _rail.RailDepth;

        bool isEven = _count % 2 == 0;
        float vValue = isEven ? 1 : 0; 
        Vertices.Add(vertOuterTop);
        UVs.Add(new Vector2(0.99f, vValue));
        Vertices.Add(vertOuterBottom);
        UVs.Add(new Vector2(1, vValue));

        Vertices.Add(vertInnerRailInnerPadding);
        UVs.Add(new Vector2(0, vValue));
        Vertices.Add(vertInnerRailInnerTop);
        UVs.Add(new Vector2(0.3f, vValue));
        Vertices.Add(vertInnerRailInnerBottom);
        UVs.Add(new Vector2(0.45f, vValue));
        Vertices.Add(vertInnerRailOuterTop);
        UVs.Add(new Vector2(0.65f, vValue));
        Vertices.Add(vertInnerRailOuterBottom);
        UVs.Add(new Vector2(0.55f, vValue));
        Vertices.Add(vertInnerRailOuterPadding);
        UVs.Add(new Vector2(1f, vValue));

        Vertices.Add(vertOuterRailInnerPadding);
        UVs.Add(new Vector2(0f, vValue));
        Vertices.Add(vertOuterRailInnerTop);
        UVs.Add(new Vector2(0.3f, vValue));
        Vertices.Add(vertOuterRailInnerBottom);
        UVs.Add(new Vector2(0.45f, vValue));
        Vertices.Add(vertOuterRailOuterTop);
        UVs.Add(new Vector2(0.65f, vValue));
        Vertices.Add(vertOuterRailOuterBottom);
        UVs.Add(new Vector2(0.55f, vValue));
        Vertices.Add(vertOuterRailOuterPadding);
        UVs.Add(new Vector2(1, vValue));
        Vertices.Add(vertCenterTop);
        UVs.Add(new Vector2(0, vValue));
        Vertices.Add(vertCenterBottom);
        UVs.Add(new Vector2(1, vValue));

        if (isFirst)
        {
            if (RailSide.Left == railSide)
                _leftSidePrevIndex = Vertices.Count - _numVerticesPerNode;
            else
                _rightSidePrevIndex = Vertices.Count - _numVerticesPerNode;
            return;
        }

        int vertIndex;
        if (RailSide.Left == railSide)
        {
            vertIndex = _leftSidePrevIndex;
            _leftSidePrevIndex = Vertices.Count - _numVerticesPerNode;
        }
        else
        {
            vertIndex = _rightSidePrevIndex;
            _rightSidePrevIndex = Vertices.Count - _numVerticesPerNode;
        }

        int prevVertOuterTopIndex = vertIndex + (int)VerticeType.OuterTop;
        int prevVertOuterBottomIndex = vertIndex + (int)VerticeType.OuterBottom;
        int prevVertInnerRailInnerTopIndex = vertIndex + (int)VerticeType.InnerRailInnerTop;
        int prevVertInnerRailInnerBottomIndex = vertIndex + (int)VerticeType.InnerRailInnerBottom;
        int prevVertInnerRailOuterTopIndex = vertIndex + (int)VerticeType.InnerRailOuterTop;
        int prevVertInnerRailOuterBottomIndex = vertIndex + (int)VerticeType.InnerRailOuterBottom;

        int prevVertOuterRailInnerTopIndex = vertIndex + (int)VerticeType.OuterRailInnerTop;
        int prevVertOuterRailIndexBottomIndex = vertIndex + (int)VerticeType.OuterRailInnerBottom;
        int prevVertOuterRailOuterTopIndex = vertIndex + (int)VerticeType.OuterRailOuterTop;
        int prevVertOuterRailOuterBottomIndex = vertIndex + (int)VerticeType.OuterRailOuterBottom;

        int prevVertInnerRailInnerPaddingIndex = vertIndex + (int)VerticeType.InnerRailInnerPadding;
        int prevVertInnerRailOuterPaddingIndex = vertIndex + (int)VerticeType.InnerRailOuterPadding;
        int prevVertOuterRailInnerPaddingIndex = vertIndex + (int)VerticeType.OuterRailInnerPadding;
        int prevVertOuterRailOuterPaddingIndex = vertIndex + (int)VerticeType.OuterRailOuterPadding;

        int prevVertCenterTopIndex = vertIndex + (int)VerticeType.CenterTop;
        int prevVertCenterBottomIndex = vertIndex + (int)VerticeType.CenterBottom;

        int vertOuterTopIndex = Vertices.Count - _numVerticesPerNode + (int)VerticeType.OuterTop;
        int vertOuterBottomIndex = Vertices.Count - _numVerticesPerNode + (int)VerticeType.OuterBottom;
        int vertInnerRailInnerTopIndex = Vertices.Count - _numVerticesPerNode + (int)VerticeType.InnerRailInnerTop;
        int vertInnerRailInnerBottomIndex = Vertices.Count -  _numVerticesPerNode + (int)VerticeType.InnerRailInnerBottom;
        int vertInnerRailOuterTopIndex = Vertices.Count -  _numVerticesPerNode + (int)VerticeType.InnerRailOuterTop;
        int vertInnerRailOuterBottomIndex = Vertices.Count -  _numVerticesPerNode + (int)VerticeType.InnerRailOuterBottom;
        int vertOuterRailInnerTopIndex = Vertices.Count -  _numVerticesPerNode + (int)VerticeType.OuterRailInnerTop;
        int vertOuterRailInnerBottomIndex = Vertices.Count -  _numVerticesPerNode + (int)VerticeType.OuterRailInnerBottom;
        int vertOuterRailOuterTopIndex = Vertices.Count -  _numVerticesPerNode + (int)VerticeType.OuterRailOuterTop;
        int vertOuterRailOuterBottomIndex = Vertices.Count -  _numVerticesPerNode + (int)VerticeType.OuterRailOuterBottom;

        int vertInnerRailInnerPaddingIndex = Vertices.Count -  _numVerticesPerNode + (int)VerticeType.InnerRailInnerPadding;
        int vertInnerRailOuterPaddingIndex = Vertices.Count -  _numVerticesPerNode + (int)VerticeType.InnerRailOuterPadding;
        int vertOuterRailInnerPaddingIndex = Vertices.Count -  _numVerticesPerNode + (int)VerticeType.OuterRailInnerPadding;
        int vertOuterRailOuterPaddingIndex = Vertices.Count -  _numVerticesPerNode + (int)VerticeType.OuterRailOuterPadding;

        int vertCenterTopIndex = Vertices.Count -  _numVerticesPerNode + (int)VerticeType.CenterTop;
        int vertCenterBottomIndex = Vertices.Count -  _numVerticesPerNode + (int)VerticeType.CenterBottom;
        
        // Rectangle between the center and to the inner rail
        AddRectangle(vertCenterTopIndex, vertInnerRailInnerPaddingIndex, prevVertCenterTopIndex, prevVertInnerRailInnerPaddingIndex, railSide, TrianglesMainMesh);

        // Inner Rail mesh
        AddRectangle(vertInnerRailInnerPaddingIndex, vertInnerRailInnerTopIndex, prevVertInnerRailInnerPaddingIndex, prevVertInnerRailInnerTopIndex , railSide, TrianglesRails);
        AddRectangle(vertInnerRailInnerTopIndex, vertInnerRailInnerBottomIndex, prevVertInnerRailInnerTopIndex, prevVertInnerRailInnerBottomIndex, railSide, TrianglesRails);
        AddRectangle(vertInnerRailInnerBottomIndex, vertInnerRailOuterBottomIndex, prevVertInnerRailInnerBottomIndex, prevVertInnerRailOuterBottomIndex, railSide, TrianglesRails);
        AddRectangle(vertInnerRailOuterBottomIndex, vertInnerRailOuterTopIndex, prevVertInnerRailOuterBottomIndex, prevVertInnerRailOuterTopIndex, railSide, TrianglesRails);
        AddRectangle(vertInnerRailOuterTopIndex, vertInnerRailOuterPaddingIndex, prevVertInnerRailOuterTopIndex, prevVertInnerRailOuterPaddingIndex, railSide, TrianglesRails);
        
        // Middle Rectangle between the inner and outer rail
        AddRectangle(vertInnerRailOuterPaddingIndex, vertOuterRailInnerPaddingIndex, prevVertInnerRailOuterPaddingIndex, prevVertOuterRailInnerPaddingIndex, railSide, TrianglesMainMesh);
        
        // Outer Rail mesh
        AddRectangle(vertOuterRailInnerPaddingIndex, vertOuterRailInnerTopIndex, prevVertOuterRailInnerPaddingIndex, prevVertOuterRailInnerTopIndex, railSide, TrianglesRails);
        AddRectangle(vertOuterRailInnerTopIndex, vertOuterRailInnerBottomIndex, prevVertOuterRailInnerTopIndex, prevVertOuterRailIndexBottomIndex, railSide, TrianglesRails);
        AddRectangle(vertOuterRailInnerBottomIndex, vertOuterRailOuterBottomIndex, prevVertOuterRailIndexBottomIndex, prevVertOuterRailOuterBottomIndex, railSide, TrianglesRails);
        AddRectangle(vertOuterRailOuterBottomIndex, vertOuterRailOuterTopIndex, prevVertOuterRailOuterBottomIndex, prevVertOuterRailOuterTopIndex, railSide, TrianglesRails);
        AddRectangle(vertOuterRailOuterTopIndex, vertOuterRailOuterPaddingIndex, prevVertOuterRailOuterTopIndex, prevVertOuterRailOuterPaddingIndex, railSide, TrianglesRails);

        // Rectangle between the outer rail and the outer side of the road
        AddRectangle(vertOuterRailOuterPaddingIndex, vertOuterTopIndex, prevVertOuterRailOuterPaddingIndex, prevVertOuterTopIndex, railSide, TrianglesMainMesh);

        // The side of the road mesh
        AddRectangle(vertOuterTopIndex, vertOuterBottomIndex, prevVertOuterTopIndex, prevVertOuterBottomIndex, railSide, TrianglesMainMesh);
        
        // Bottom of the road mesh
        AddRectangle(vertOuterBottomIndex, vertCenterBottomIndex, prevVertOuterBottomIndex, prevVertCenterBottomIndex, railSide, TrianglesBottom);
    }

    private void AddRectangle(int currentSideIndex1, int currentSideIndex2, int prevSideIndex1, int prevSideIndex2, RailSide railSide, List<int> triangles)
    {
        if (railSide == RailSide.Right)
        {
            triangles.Add(currentSideIndex1);
            triangles.Add(currentSideIndex2);
            triangles.Add(prevSideIndex1);

            triangles.Add(prevSideIndex1);
            triangles.Add(currentSideIndex2);
            triangles.Add(prevSideIndex2);
        }
        else
        {
            triangles.Add(currentSideIndex1);
            triangles.Add(prevSideIndex1);
            triangles.Add(currentSideIndex2);

            triangles.Add(prevSideIndex1);
            triangles.Add(prevSideIndex2);
            triangles.Add(currentSideIndex2);
        }

    }
    public void AddShortSideRectangles()
    {
        int vertOuterRightTopIndex = _rightSidePrevIndex + (int)VerticeType.OuterTop;
        int vertOuterRightBottomIndex = _rightSidePrevIndex + (int)VerticeType.OuterBottom;
        int vertOuterLeftTopIndex = _leftSidePrevIndex + (int)VerticeType.OuterTop;
        int vertOuterLeftBottomIndex = _leftSidePrevIndex + (int)VerticeType.OuterBottom;
        AddRectangle(vertOuterRightTopIndex, vertOuterRightBottomIndex, vertOuterLeftTopIndex, vertOuterLeftBottomIndex, RailSide.Left, TrianglesMainMesh);
    }
    }

    [RequireComponent(typeof(TramRail))]
    public class TramRailMeshCreator : PathSceneTool
    {
        [Header ("Material settings")]
        [SerializeField] private Material _laneMaterial;
        [SerializeField] private Material _bottomMaterial;
        private Material _laneMaterialCopy;
        private MeshFilter _meshFilter;
        private MeshRenderer _meshRenderer;
        public Mesh _mesh;
        private Road _road;
        public Material railMaterial;
        public Material roadMaterial;
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
                _road.gameObject.GetComponent<MeshCollider>().sharedMesh = _mesh;
            }
        }

        private void CreateRoadMesh()
        {
            RoadNode curr = _road.StartNode;
            MeshBuilder meshBuilder = new MeshBuilder(pathCreator.path, _road as TramRail);
            while (curr != null)
            {
                meshBuilder.AddNode(curr);
                curr = curr.Next;
            }

            meshBuilder.AddShortSideRectangles();
            _mesh.Clear();
            _mesh.vertices = meshBuilder.Vertices.ToArray();
            _mesh.subMeshCount = 3;
            _mesh.SetTriangles(meshBuilder.TrianglesMainMesh.ToArray(), 0);
            _mesh.SetTriangles(meshBuilder.TrianglesRails.ToArray(), 1);
            _mesh.SetTriangles(meshBuilder.TrianglesBottom.ToArray(), 2);
            _mesh.uv = meshBuilder.UVs.ToArray();
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
            Material[] materials = new Material[3];
            materials[0] = roadMaterial;
            materials[1] = railMaterial;
            materials[2] = _bottomMaterial;
            _meshRenderer.sharedMaterials = materials;
        }
    }
}