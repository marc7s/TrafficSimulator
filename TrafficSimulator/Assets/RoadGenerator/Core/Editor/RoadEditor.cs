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
            SerializedProperty thickness;
            SerializedProperty laneVertexSpacing;
            SerializedProperty drawLanes;
        #endregion

        private void OnEnable()
        {
            laneAmount = serializedObject.FindProperty("LaneAmount");
            laneWidth = serializedObject.FindProperty("LaneWidth");
            thickness = serializedObject.FindProperty("Thickness");
            laneVertexSpacing = serializedObject.FindProperty("LaneVertexSpacing");
            drawLanes = serializedObject.FindProperty("DrawLanes");
        }
        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            Road road = (Road)target;
            bool changed = false;

            EditorGUILayout.PropertyField(laneAmount);
            EditorGUILayout.PropertyField(laneWidth);
            EditorGUILayout.PropertyField(thickness);
            EditorGUILayout.PropertyField(laneVertexSpacing);
            EditorGUILayout.PropertyField(drawLanes);

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

            if(thickness.floatValue != road.Thickness)
            {
                changed = true;
                road.Thickness = thickness.floatValue;
            }

            if(laneVertexSpacing.floatValue != road.LaneVertexSpacing)
            {
                changed = true;
                road.LaneVertexSpacing = laneVertexSpacing.floatValue;
            }

            if(drawLanes.boolValue != road.DrawLanes)
            {
                changed = true;
                road.DrawLanes = drawLanes.boolValue;
            }

            if(changed)
                road.OnChange();

            serializedObject.ApplyModifiedProperties();
        }
    }
}
