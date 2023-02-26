using UnityEngine;
using UnityEditor;
using RoadGenerator;

namespace RoadEditor
{
    [CustomEditor(typeof(Road)), CanEditMultipleObjects]
    public class RoadEditor : Editor 
    {
        #region SerializedProperties
            private SerializedProperty _laneAmount;
            private SerializedProperty _laneWidth;
            private SerializedProperty _thickness;
            private SerializedProperty _laneVertexSpacing;
            private SerializedProperty _drawLanes;
        #endregion

        private void OnEnable()
        {
            _laneAmount = serializedObject.FindProperty("LaneAmount");
            _laneWidth = serializedObject.FindProperty("LaneWidth");
            _thickness = serializedObject.FindProperty("Thickness");
            _laneVertexSpacing = serializedObject.FindProperty("LaneVertexSpacing");
            _drawLanes = serializedObject.FindProperty("DrawLanes");
        }
        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            Road road = (Road)target;
            bool changed = false;

            EditorGUILayout.PropertyField(_laneAmount);
            EditorGUILayout.PropertyField(_laneWidth);
            EditorGUILayout.PropertyField(_thickness);
            EditorGUILayout.PropertyField(_laneVertexSpacing);
            EditorGUILayout.PropertyField(_drawLanes);

            if(_laneAmount.intValue != (int)road.LaneAmount)
            {
                changed = true;
                road.LaneAmount = (LaneAmount)_laneAmount.intValue;
            }

            if(_laneWidth.floatValue != road.LaneWidth)
            {
                changed = true;
                road.LaneWidth = _laneWidth.floatValue;
            }

            if(_thickness.floatValue != road.Thickness)
            {
                changed = true;
                road.Thickness = _thickness.floatValue;
            }

            if(_laneVertexSpacing.floatValue != road.LaneVertexSpacing)
            {
                changed = true;
                road.LaneVertexSpacing = _laneVertexSpacing.floatValue;
            }

            if(_drawLanes.boolValue != road.DrawLanes)
            {
                changed = true;
                road.DrawLanes = _drawLanes.boolValue;
            }

            serializedObject.ApplyModifiedProperties();

            if(changed)
                road.OnChange();
        }
    }
}
