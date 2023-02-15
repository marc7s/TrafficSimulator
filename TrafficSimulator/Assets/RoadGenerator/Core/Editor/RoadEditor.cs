using UnityEngine;
using UnityEditor;
using RoadGenerator;

namespace RoadSystemGenerator
{
    [CustomEditor(typeof(Road))]
    public class RoadEditor : Editor 
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();
            Road road = (Road)target;

            road.Update();
        }
    }
}
