using System;
using UnityEditor;
using UnityEngine;

namespace RoadGenerator
{
    // Remove [CreateAssetMenu] when you've created an instance, because you don't need more than one.
    //[CreateAssetMenu(menuName = "RoadGenerator/PrefabManager")]
    public class PrefabManager : ScriptableObject
    {
    #if UNITY_EDITOR
        public GameObject RoadSystem;
        public RoadSigns Signs;

        [Serializable]
        public class RoadSigns
        {
        public GameObject StopSign;
        public GameObject YieldSign;
        }
    #endif
    }

    #if UNITY_EDITOR
    [CustomEditor(typeof(PrefabManager))]
    public class PrefabManagerEditor : Editor
    {
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        EditorGUILayout.HelpBox("If you move this file somewhere else, also change the path in RoadGeneratorMenu! ", MessageType.Info);
    }
    }
    #endif
}
