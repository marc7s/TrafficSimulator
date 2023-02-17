using UnityEngine;
using UnityEditor;
using RoadGenerator;

namespace RoadSystemGenerator
{
    [CustomEditor(typeof(RoadSystem))]
    public class RoadSystemEditor : Editor 
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            RoadSystem roadSystem = (RoadSystem)target;

            if(GUILayout.Button("Add new road"))
            {
                roadSystem.AddNewRoad();
            }
        }
    }
}
