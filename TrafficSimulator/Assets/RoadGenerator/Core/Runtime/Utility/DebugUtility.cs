using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;
using System;

namespace RoadGenerator
{
    public static class DebugUtility
    {
        [SerializeField] private static GameObject _container;
        [SerializeField] private static GameObject _positionContainer;
        [SerializeField] private static GameObject _lineContainer;
        [SerializeField] private static LineRenderer _lineRenderer;
        [SerializeField] private static GameObject _endPointPrefab;
        [SerializeField] private static GameObject _markerPrefab;
        private static Vector3 _markerPrefabScale = new Vector3(1.5f, 1f, 1f);
        private static Vector3 _endPointPrefabScale = Vector3.one * 1.3f;
        private static Dictionary<string, (Vector3[], Quaternion[])> _groups = new Dictionary<string, (Vector3[], Quaternion[])>();
        private static bool _nextGroupPressed = false;
        private static int _currentGroup = 0;
        private static bool _isSetup = false;
        private const string _debugUtilityContainer = "Debug Utility";
        private const string _positionContainerName = "Markers";
        private const string _lineContainerName = "Line";
        
        private static void Setup()
        {
            // Remove an existing container if it is found
            GameObject existingContainer = GameObject.Find(_debugUtilityContainer);
            if(existingContainer != null)
                GameObject.DestroyImmediate(existingContainer);

            // Since creating game objects cannot be done without spawning them in the scene,
            // this is used to move the "prefabs" out from visibility
            Vector3 farAway = new Vector3(0, -1000, 0);

            // Create a prefab for markers that are cuboids
            _markerPrefab = GameObject.CreatePrimitive(PrimitiveType.Cube);
            _markerPrefab.GetComponent<BoxCollider>().enabled = false;
            _markerPrefab.transform.localScale = _markerPrefabScale;
            _markerPrefab.transform.position = farAway;

            // Create an empty container for all debug utility objects
            _container = new GameObject(_debugUtilityContainer);
            
            // Create an empty container for all markers
            _positionContainer = new GameObject(_positionContainerName);
            _positionContainer.transform.parent = _container.transform;

            // Create a prefab for line end points that are spheres
            _endPointPrefab = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            _endPointPrefab.GetComponent<SphereCollider>().enabled = false;
            _endPointPrefab.transform.localScale = _endPointPrefabScale;
            _endPointPrefab.transform.position = farAway;
            _endPointPrefab.GetComponent<Renderer>().material = new Material(Shader.Find("Standard"));

            // Create an empty container for the line
            _lineContainer = new GameObject(_lineContainerName);
            _lineRenderer = _lineContainer.AddComponent<LineRenderer>();
            _lineRenderer.positionCount = 0;
            _lineContainer.transform.parent = _container.transform;

            _isSetup = true;
        }

        /// <summary> Draw a line between all positions in a list </summary>
        public static void DrawLine(Vector3[] positions, bool markEndPoints = false)
        {
            if(!_isSetup)
                Setup();
            
            _lineRenderer.positionCount = positions.Length;
            _lineRenderer.SetPositions(positions);


            if(markEndPoints)
            {
                GameObject start = GameObject.Instantiate(_endPointPrefab, positions[0], Quaternion.identity);
                start.name = "Line start";
                start.transform.parent = _lineContainer.transform;
                start.GetComponent<Renderer>().material.SetColor("_Color", Color.green);

                GameObject end = GameObject.Instantiate(_endPointPrefab, positions[positions.Length - 1], Quaternion.identity);
                end.name = "Line end";
                end.transform.parent = _lineContainer.transform;
                end.GetComponent<Renderer>().material.SetColor("_Color", Color.red);
            }
        }

        /// <summary> Draw a line between all nodes in a linked list of nodes </summary>
        // Generic overload for nodes
        public static void DrawLine<T>(T nodes, bool markEndPoints = false) where T : Node<T>
        {
            DrawLine(nodes.GetPositions(), markEndPoints);
        }

        /// <summary> Draw a line between all positions in a list </summary>
        // Explicit overload for GuideNodes since GuideNode inherits from LaneNode, which in turn inherits from Node<LaneNode>
        public static void DrawLine(GuideNode nodes, bool markEndPoints = false)
        {
            DrawLine(nodes.GetPositions(), markEndPoints);
        }

        public static void ClearLine()
        {
            _lineRenderer.SetPositions(new Vector3[]{});
            _lineRenderer.positionCount = 0;
        }

        /// <summary> Mark all nodes in a linked list of nodes </summary>
        public static void MarkPositions<T>(T nodes, float size = 1f) where T : Node<T>
        {
            Vector3[] positions = nodes.GetPositions();
            Quaternion[] rotations = nodes.GetRotations();
            MarkPositions(positions, rotations, size);
        }
        
        /// <summary> Mark all positions in a list </summary>
        public static void MarkPositions(Vector3[] positions, Quaternion[] rotations = null, float size = 1f)
        {
            ClearMarkers();

            if(!_isSetup)
                Setup();
            
            for (int i = 0; i < positions.Length; i++)
            {
                Quaternion? rot = rotations == null ? null : rotations[i];
                DebugUtility.MarkPosition(positions[i], rot, size, false);
            }
        }

        /// <summary> Mark a single position </summary>
        public static void MarkPosition(Vector3 position, Quaternion? rotation = null, float size = 1f, bool clearMarkers = true)
        {
            if(!_isSetup)
                Setup();

            if(clearMarkers)
                ClearMarkers();

            // If rotation was passed, rotate the marker and make it longer in the rotation direction. Otherwise, make it a cube
            _markerPrefab.transform.localScale = rotation == null ? Vector3.one : _markerPrefabScale * size;
            GameObject marker = GameObject.Instantiate(_markerPrefab, position, rotation ?? Quaternion.identity);
            marker.name = "Marker";
            marker.transform.parent = _positionContainer.transform;
        }

        /// <summary> 
        /// Adds several groups of positions to display at separate times. Press the `N` key to cycle through the groups. 
        /// Only the positions for the current group is displayed. The key press is only detected in Play mode, in the Game view
        /// For the easiest use, split the Game and Scene view so both are visible at the same time, and press the `N` key in the Game view window
        /// and the markers will be visible in both views
        /// </summary>
        public static void AddMarkGroups(Dictionary<string, (Vector3[], Quaternion[])> groups)
        {          
            _groups = groups;
            EditorApplication.update += UpdateDisplayedMarkGroups;
        }

        public static void AddMarkGroups<T>(List<T> nodes, Func<T, string> name) where T : Node<T>
        {
            _groups.Clear();
            
            foreach (T node in nodes)
            {
                _groups.Add(name(node), (node.GetPositions(), node.GetRotations()));
            }
            
            EditorApplication.update += UpdateDisplayedMarkGroups;
        }

        public static void RemoveMarkGroups()
        {          
            _groups.Clear();
            EditorApplication.update -= UpdateDisplayedMarkGroups;
        }

        private static void UpdateDisplayedMarkGroups()
        {
            bool nextKeyPressed = Input.GetKey(KeyCode.N);
            
            // If the key is still being pressed, return
            if(nextKeyPressed && _nextGroupPressed)
                return;

            // Update the key state
            _nextGroupPressed = nextKeyPressed;

            // If the key is not pressed, return
            if(!_nextGroupPressed)
                return;
            
            // Check that there are groups to display
            if(_groups.Count > 0)
            {
                // Increment the current group
                _currentGroup = (_currentGroup + 1) % _groups.Count;

                // Get the new group name
                string group = _groups.Keys.ToList()[_currentGroup];
                
                // Log the new group being displayed and mark all its positions
                (Vector3[] positions, Quaternion[] rotations) = _groups[group];
                Debug.Log($"[Group {_currentGroup}, {positions.Length} positions]: {group}");
                MarkPositions(positions,  rotations);
            }
        }

        public static void ClearMarkers()
        {
            if(!_isSetup)
                Setup();
            
            if(_positionContainer != null)
                GameObject.DestroyImmediate(_positionContainer);

            _positionContainer = new GameObject(_positionContainerName);
            _positionContainer.transform.parent = _container.transform;
        }
    }
}