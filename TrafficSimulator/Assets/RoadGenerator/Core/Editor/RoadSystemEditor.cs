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
            private SerializedProperty _defaultTrafficLight;
            private SerializedProperty _defaultTrafficLightController;
            private SerializedProperty _defaultStopSign;
        #endregion
        public void OnEnable()
        {
            _showGraph = serializedObject.FindProperty("ShowGraph");
            _spawnRoadsAtOrigin = serializedObject.FindProperty("SpawnRoadsAtOrigin");
            _drivingSide = serializedObject.FindProperty("DrivingSide");
            _defaultTrafficLight = serializedObject.FindProperty("DefaultTrafficLightPrefab");
            _defaultTrafficLightController = serializedObject.FindProperty("DefaultTrafficLightControllerPrefab");
            _defaultStopSign = serializedObject.FindProperty("DefaultStopSignPrefab");

        }
        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            
            // Uncomment this if you want to change the connections to containers and prefabs
            DrawDefaultInspector();
            
            RoadSystem roadSystem = (RoadSystem)target;
            
            bool changed = false;

            EditorGUILayout.PropertyField(_drivingSide);
            EditorGUILayout.PropertyField(_spawnRoadsAtOrigin);
            EditorGUILayout.PropertyField(_showGraph);
            EditorGUILayout.PropertyField(_defaultTrafficLight);
            EditorGUILayout.PropertyField(_defaultTrafficLightController);
            EditorGUILayout.PropertyField(_defaultStopSign);
            
            if(_drivingSide.intValue != (int)roadSystem.DrivingSide)
            {
                roadSystem.DrivingSide = (DrivingSide)_drivingSide.intValue;
                roadSystem.UpdateRoads();
            }

            if(_spawnRoadsAtOrigin.boolValue != roadSystem.SpawnRoadsAtOrigin)
            {
                roadSystem.SpawnRoadsAtOrigin = _spawnRoadsAtOrigin.boolValue;
            }

            if(_showGraph.boolValue != roadSystem.ShowGraph)
            {
                changed = true;
                roadSystem.ShowGraph = _showGraph.boolValue;
            }

            if(_defaultTrafficLight.objectReferenceValue != (GameObject)roadSystem.DefaultTrafficLightPrefab)
            {
                roadSystem.DefaultTrafficLightPrefab = (GameObject)_defaultTrafficLight.objectReferenceValue;
            }

            if(_defaultTrafficLightController.objectReferenceValue != (GameObject)roadSystem.DefaultTrafficLightControllerPrefab)
            {
                roadSystem.DefaultTrafficLightControllerPrefab = (GameObject)_defaultTrafficLightController.objectReferenceValue;
            }

            if(_defaultStopSign.objectReferenceValue != (GameObject)roadSystem.DefaultStopSignPrefab)
            {
                roadSystem.DefaultStopSignPrefab = (GameObject)_defaultStopSign.objectReferenceValue;
            }

            if(GUILayout.Button("Add new road"))
            {
                roadSystem.AddNewRoad(PathType.Road);
            }
            if(GUILayout.Button("Add new rail"))
            {
                roadSystem.AddNewRoad(PathType.Rail);
            }
            if(GUILayout.Button("Generate OSM roads"))
            {
                roadSystem.GenerateOSMRoads();
            }
            if(GUILayout.Button("Delete all roads"))
            {
                roadSystem.DeleteAllRoads();
            }
            if(GUILayout.Button("Delete all buildings"))
            {
                roadSystem.DeleteAllBuildings();
            }

            serializedObject.ApplyModifiedProperties();

            if(changed)
            {
                roadSystem.UpdateRoadSystemGraph();
            }
        }
    }
}
