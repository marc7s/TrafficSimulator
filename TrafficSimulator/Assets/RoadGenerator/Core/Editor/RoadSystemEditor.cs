using UnityEngine;
using UnityEditor;
using RoadGenerator;

namespace RoadSystemGenerator
{
    [CustomEditor(typeof(RoadSystem))]
    public class RoadSystemEditor : Editor 
    {
        #region SerializedProperties
            private SerializedProperty _drivingSide;
            private SerializedProperty _spawnRoadsAtOrigin;
            private SerializedProperty _showGraph;
            private SerializedProperty _useOSM;
            private SerializedProperty _defaultTrafficLight;
            private SerializedProperty _defaultTrafficLightController;
            private SerializedProperty _defaultStopSign;
            private SerializedProperty _defaultYieldSign;
            private SerializedProperty _shouldGenerateBuildings;
            private SerializedProperty _shouldGenerateBusStops;
            private SerializedProperty _shouldGenerateRoads;
            private SerializedProperty _shouldGenerateTerrain;
        #endregion
        public void OnEnable()
        {
            _showGraph = serializedObject.FindProperty("ShowGraph");
            _useOSM = serializedObject.FindProperty("UseOSM");
            _spawnRoadsAtOrigin = serializedObject.FindProperty("SpawnRoadsAtOrigin");
            _drivingSide = serializedObject.FindProperty("DrivingSide");
            _defaultTrafficLight = serializedObject.FindProperty("DefaultTrafficLightPrefab");
            _defaultTrafficLightController = serializedObject.FindProperty("DefaultTrafficLightControllerPrefab");
            _defaultStopSign = serializedObject.FindProperty("DefaultStopSignPrefab");
            _defaultYieldSign = serializedObject.FindProperty("DefaultYieldSignPrefab");
            _shouldGenerateBuildings = serializedObject.FindProperty("ShouldGenerateBuildings");
            _shouldGenerateBusStops = serializedObject.FindProperty("ShouldGenerateBusStops");
            _shouldGenerateRoads = serializedObject.FindProperty("ShouldGenerateRoads");
            _shouldGenerateTerrain = serializedObject.FindProperty("ShouldGenerateTerrain");
        }
        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            
            // Uncomment this if you want to change the connections to containers and prefabs
            //DrawDefaultInspector();
            
            RoadSystem roadSystem = (RoadSystem)target;
            
            bool changed = false;

            EditorGUILayout.PropertyField(_drivingSide);
            EditorGUILayout.PropertyField(_spawnRoadsAtOrigin);
            EditorGUILayout.PropertyField(_showGraph);
            EditorGUILayout.PropertyField(_useOSM);
            EditorGUILayout.PropertyField(_defaultTrafficLight);
            EditorGUILayout.PropertyField(_defaultTrafficLightController);
            EditorGUILayout.PropertyField(_defaultStopSign);
            EditorGUILayout.PropertyField(_defaultYieldSign);
            EditorGUILayout.PropertyField(_shouldGenerateBuildings);
            EditorGUILayout.PropertyField(_shouldGenerateBusStops);
            EditorGUILayout.PropertyField(_shouldGenerateRoads);
            EditorGUILayout.PropertyField(_shouldGenerateTerrain);
            
            if(_drivingSide.intValue != (int)roadSystem.DrivingSide)
            {
                roadSystem.DrivingSide = (DrivingSide)_drivingSide.intValue;
                roadSystem.UpdateRoads();
            }

            if(_spawnRoadsAtOrigin.boolValue != roadSystem.SpawnRoadsAtOrigin)
                roadSystem.SpawnRoadsAtOrigin = _spawnRoadsAtOrigin.boolValue;

            if(_showGraph.boolValue != roadSystem.ShowGraph)
            {
                changed = true;
                roadSystem.ShowGraph = _showGraph.boolValue;
            }

            if(_useOSM.boolValue != roadSystem.UseOSM)
                roadSystem.UseOSM = _useOSM.boolValue;

            if(_defaultTrafficLight.objectReferenceValue != (GameObject)roadSystem.DefaultTrafficLightPrefab)
                roadSystem.DefaultTrafficLightPrefab = (GameObject)_defaultTrafficLight.objectReferenceValue;

            if(_defaultTrafficLightController.objectReferenceValue != (GameObject)roadSystem.DefaultTrafficLightControllerPrefab)
                roadSystem.DefaultTrafficLightControllerPrefab = (GameObject)_defaultTrafficLightController.objectReferenceValue;

            if(_defaultStopSign.objectReferenceValue != (GameObject)roadSystem.DefaultStopSignPrefab)
                roadSystem.DefaultStopSignPrefab = (GameObject)_defaultStopSign.objectReferenceValue;

            if(_defaultYieldSign.objectReferenceValue != (GameObject)roadSystem.DefaultYieldSignPrefab)
                roadSystem.DefaultYieldSignPrefab = (GameObject)_defaultYieldSign.objectReferenceValue;


            if(!_useOSM.boolValue)
            {
                EditorGUILayout.LabelField("Actions", EditorStyles.boldLabel);

                if(GUILayout.Button("Add new road"))
                    roadSystem.AddNewRoad(PathType.Road);

                if(GUILayout.Button("Add new rail"))
                    roadSystem.AddNewRoad(PathType.Rail);

                if(GUILayout.Button("Update POIs"))
                    roadSystem.UpdatePOIs();
            }
            else
            {
                EditorGUILayout.LabelField("OSM Actions", EditorStyles.boldLabel);
                
                if(GUILayout.Button("Generate OSM roads"))
                    roadSystem.GenerateOSMRoads();

                if(GUILayout.Button("Spawn bus stops"))
                    roadSystem.SpawnBusStops();

                if(GUILayout.Button("Delete all roads"))
                    roadSystem.DeleteAllRoads();

                if(GUILayout.Button("Delete all buildings"))
                    roadSystem.DeleteAllBuildings();
            }

            serializedObject.ApplyModifiedProperties();

            if(changed)
                roadSystem.UpdateRoadSystemGraph();
        }
    }
}
