using UnityEngine;
using UnityEditor;
using RoadGenerator;

namespace RoadSystemGenerator
{
    [CustomEditor(typeof(RoadSystem))]
    public class RoadSystemEditor : Editor 
    {
        private SerializedProperty showRoadSystemGraphButton;
        public void OnEnable()
        {
            showRoadSystemGraphButton = serializedObject.FindProperty("ShowGraph");
        }
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            RoadSystem roadSystem = (RoadSystem)target;
            showRoadSystemGraphButton.boolValue = EditorGUILayout.Toggle("Show Road System Graph", roadSystem.ShowGraph);
            // Set the ShowGraph property and update the road system graph if the button does not correspond to the ShowGraph property
            if (showRoadSystemGraphButton.boolValue != roadSystem.ShowGraph)
            {
                roadSystem.ShowGraph = showRoadSystemGraphButton.boolValue;
                roadSystem.UpdateRoadSystemGraph();
            }

            if(GUILayout.Button("Add new road"))
            {
                roadSystem.AddNewRoad();
            }


        }
    }
}
