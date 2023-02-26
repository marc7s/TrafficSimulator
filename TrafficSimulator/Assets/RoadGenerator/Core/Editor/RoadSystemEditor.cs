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
        #endregion
        public void OnEnable()
        {
            _showGraph = serializedObject.FindProperty("ShowGraph");
            _spawnRoadsAtOrigin = serializedObject.FindProperty("SpawnRoadsAtOrigin");
            _drivingSide = serializedObject.FindProperty("DrivingSide");

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
            
            if(_drivingSide.intValue != (int)roadSystem.DrivingSide)
            {
                roadSystem.DrivingSide = (DrivingSide)_drivingSide.intValue;
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

            if(GUILayout.Button("Add new road"))
            {
                roadSystem.AddNewRoad();
            }

            serializedObject.ApplyModifiedProperties();

            if(changed)
            {
                roadSystem.UpdateRoadSystemGraph();
            }
        }
    }
}
