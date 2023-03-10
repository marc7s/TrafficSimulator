using UnityEngine;

namespace RoadGenerator
{
    public static class DebugUtility
    {
        [SerializeField] private static GameObject _container;
        [SerializeField] private static GameObject _positionContainer;
        [SerializeField] private static GameObject _markerPrefab;
        private static Vector3 _markerPrefabScale = new Vector3(1.5f, 1f, 1f);
        private static bool _isSetup = false;
        private const string _debugUtilityContainer = "Debug Utility";
        private const string _positionContainerName = "Markers";
        
        private static void Setup()
        {
            GameObject existingContainer = GameObject.Find(_debugUtilityContainer);
            if(existingContainer != null)
                GameObject.DestroyImmediate(existingContainer);

            _markerPrefab = GameObject.CreatePrimitive(PrimitiveType.Cube);
            _markerPrefab.GetComponent<BoxCollider>().enabled = false;

            _container = new GameObject(_debugUtilityContainer);
            
            _positionContainer = new GameObject(_positionContainerName);
            _positionContainer.transform.parent = _container.transform;

            _isSetup = true;
        }
        public static void MarkPositions(Vector3[] positions, Quaternion[] rotations = null, float size = 1f)
        {
            ClearMarkers();

            if(!_isSetup)
                Setup();
            
            for (int i = 0; i < positions.Length; i++)
            {
                Quaternion rot = rotations != null ? rotations[i] : Quaternion.identity;
                DebugUtility.MarkPosition(positions[i], rot, size);
            }
        }
        public static void MarkPosition(Vector3 position, Quaternion? rotation = null, float size = 1f)
        {
            if(!_isSetup)
                Setup();

            ClearMarkers();

            _markerPrefab.transform.localScale = _markerPrefabScale * size;
            GameObject marker = GameObject.Instantiate(_markerPrefab, position, rotation ?? Quaternion.identity);
            marker.name = "Marker";
            marker.transform.parent = _positionContainer.transform;
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