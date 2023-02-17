using UnityEngine;
using UnityEditor;
using RoadGenerator;

namespace RoadEditor
{
    [CustomEditor(typeof(Road)), CanEditMultipleObjects]
    public class RoadEditor : Editor 
    {
        #region SerializedProperties
            SerializedProperty laneAmount;
            SerializedProperty laneWidth;
        #endregion

        private void OnEnable()
        {
            laneAmount = serializedObject.FindProperty("LaneAmount");
            laneWidth = serializedObject.FindProperty("LaneWidth");
        }
        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            Road road = (Road)target;
            bool changed = false;

            EditorGUILayout.PropertyField(laneAmount);
            EditorGUILayout.PropertyField(laneWidth);

            if(laneAmount.intValue != (int)road.LaneAmount)
            {
                changed = true;
                road.LaneAmount = (LaneAmount)laneAmount.intValue;
            }

            if(laneWidth.floatValue != road.LaneWidth)
            {
                changed = true;
                road.LaneWidth = laneWidth.floatValue;
            }

            if(changed)
                road.OnChange();

            serializedObject.ApplyModifiedProperties();
        }
    }
}
