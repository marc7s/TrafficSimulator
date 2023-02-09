using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(TrafficLight))]

public class TrafficLightEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        
        var trafficLight = target as TrafficLight;

        if (GUILayout.Button("Switch To Green"))
        {
            trafficLight.Go();
        }
        if (GUILayout.Button("Switch To Red"))
        {
            trafficLight.Stop();
        }
    }
}