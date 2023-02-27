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
            private SerializedProperty _drawRoadNodes;
            private SerializedProperty _drawLaneNodes;
            private SerializedProperty _drawLaneNodePointers;
        #endregion

        private void OnEnable()
        {
            _laneAmount = serializedObject.FindProperty("LaneAmount");
            _laneWidth = serializedObject.FindProperty("LaneWidth");
            _thickness = serializedObject.FindProperty("Thickness");
            _laneVertexSpacing = serializedObject.FindProperty("LaneVertexSpacing");
            _drawLanes = serializedObject.FindProperty("DrawLanes");
            _drawRoadNodes = serializedObject.FindProperty("DrawRoadNodes");
            _drawLaneNodes = serializedObject.FindProperty("DrawLaneNodes");
            _drawLaneNodePointers = serializedObject.FindProperty("DrawLaneNodePointers");
        }
        public override void OnInspectorGUI()
        {
            // Uncomment this to change connections
            //DrawDefaultInspector();
            
            
            serializedObject.Update();
            Road road = (Road)target;
            bool changed = false;

            EditorGUILayout.PropertyField(_laneAmount);
            EditorGUILayout.PropertyField(_laneWidth);
            EditorGUILayout.PropertyField(_thickness);
            EditorGUILayout.PropertyField(_laneVertexSpacing);
            EditorGUILayout.PropertyField(_drawLanes);
            EditorGUILayout.PropertyField(_drawRoadNodes);
            EditorGUILayout.PropertyField(_drawLaneNodes);
            
            // Only show the Draw Lane Pointers option if we are drawing lane nodes
            if(_drawLaneNodes.boolValue)
                EditorGUILayout.PropertyField(_drawLaneNodePointers);

            
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

            if(_drawRoadNodes.boolValue != road.DrawRoadNodes)
            {
                changed = true;
                road.DrawRoadNodes = _drawRoadNodes.boolValue;
            }

            if(_drawLaneNodes.boolValue != road.DrawLaneNodes)
            {
                changed = true;
                road.DrawLaneNodes = _drawLaneNodes.boolValue;
                
                // If we have disabled drawing lane nodes, we also disable drawing the pointers
                if(!road.DrawLaneNodes)
                    _drawLaneNodePointers.boolValue = false;
            }

            if(_drawLaneNodePointers.boolValue != road.DrawLaneNodePointers)
            {
                changed = true;
                road.DrawLaneNodePointers = _drawLaneNodePointers.boolValue;
            }
            

            serializedObject.ApplyModifiedProperties();

            if(changed)
                road.OnChange();
        }
    }
}
