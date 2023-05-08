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
            private SerializedProperty _maxAngleError;
            private SerializedProperty _minVertexDistance;
            private SerializedProperty _maxRoadNodeDistance;
            private SerializedProperty _drawLanes;
            private SerializedProperty _drawRoadNodes;
            private SerializedProperty _drawLaneNodes;
            private SerializedProperty _drawLaneNodePointers;
            private SerializedProperty _generateSpeedSigns;
            private SerializedProperty _connectionDistanceThreshold;
            private SerializedProperty _speedLimit;
            private SerializedProperty _speedSignDistanceFromIntersectionEdge;
            private SerializedProperty _speedSignDistanceFromRoadEnd;
            private SerializedProperty _shouldSpawnLampPoles;
            private SerializedProperty _lampPoleIntervalDistance;
            private SerializedProperty _lampPoleSideDistanceOffset;
            private SerializedProperty _defaultTrafficSignOffset;
            private SerializedProperty _isOneWay;
            private SerializedProperty _isGeneratingOSM;
        #endregion

        private void OnEnable()
        {
            _laneAmount = serializedObject.FindProperty("LaneAmount");
            _laneWidth = serializedObject.FindProperty("LaneWidth");
            _thickness = serializedObject.FindProperty("Thickness");
            _maxAngleError = serializedObject.FindProperty("MaxAngleError");
            _minVertexDistance = serializedObject.FindProperty("MinVertexDistance");
            _maxRoadNodeDistance = serializedObject.FindProperty("MaxRoadNodeDistance");
            _drawLanes = serializedObject.FindProperty("DrawLanes");
            _drawRoadNodes = serializedObject.FindProperty("DrawRoadNodes");
            _drawLaneNodes = serializedObject.FindProperty("DrawLaneNodes");
            _drawLaneNodePointers = serializedObject.FindProperty("DrawLaneNodePointers");
            _generateSpeedSigns = serializedObject.FindProperty("GenerateSpeedSigns");
            _connectionDistanceThreshold = serializedObject.FindProperty("ConnectionDistanceThreshold");
            _speedLimit = serializedObject.FindProperty("SpeedLimit");
            _speedSignDistanceFromIntersectionEdge = serializedObject.FindProperty("SpeedSignDistanceFromIntersectionEdge");
            _speedSignDistanceFromRoadEnd = serializedObject.FindProperty("SpeedSignDistanceFromRoadEnd");
            _shouldSpawnLampPoles = serializedObject.FindProperty("ShouldSpawnLampPoles");
            _lampPoleIntervalDistance = serializedObject.FindProperty("LampPoleIntervalDistance");
            _lampPoleSideDistanceOffset = serializedObject.FindProperty("LampPoleSideDistanceOffset");
            _defaultTrafficSignOffset = serializedObject.FindProperty("DefaultTrafficSignOffset");
            _isOneWay = serializedObject.FindProperty("IsOneWay");
            _isGeneratingOSM = serializedObject.FindProperty("IsGeneratingOSM");
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
            EditorGUILayout.PropertyField(_maxAngleError);
            EditorGUILayout.PropertyField(_minVertexDistance);
            EditorGUILayout.PropertyField(_maxRoadNodeDistance);
            EditorGUILayout.PropertyField(_generateSpeedSigns);
            EditorGUILayout.PropertyField(_connectionDistanceThreshold);
            EditorGUILayout.PropertyField(_speedLimit);
            EditorGUILayout.PropertyField(_speedSignDistanceFromIntersectionEdge);
            EditorGUILayout.PropertyField(_speedSignDistanceFromRoadEnd);
            EditorGUILayout.PropertyField(_shouldSpawnLampPoles);
            EditorGUILayout.PropertyField(_lampPoleIntervalDistance);
            EditorGUILayout.PropertyField(_lampPoleSideDistanceOffset);
            EditorGUILayout.PropertyField(_defaultTrafficSignOffset);
            EditorGUILayout.PropertyField(_drawLanes);
            EditorGUILayout.PropertyField(_drawRoadNodes);
            EditorGUILayout.PropertyField(_drawLaneNodes);
            EditorGUILayout.PropertyField(_isOneWay);
            EditorGUILayout.PropertyField(_isGeneratingOSM);

            
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

            if(_maxAngleError.floatValue != road.MaxAngleError)
            {
                changed = true;
                road.MaxAngleError = _maxAngleError.floatValue;
            }

            if(_minVertexDistance.floatValue != road.MinVertexDistance)
            {
                changed = true;
                road.MinVertexDistance = _minVertexDistance.floatValue;
            }

            if(_maxRoadNodeDistance.floatValue != road.MaxRoadNodeDistance)
            {
                changed = true;
                road.MaxRoadNodeDistance = _maxRoadNodeDistance.floatValue;
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
            
            if (_generateSpeedSigns.boolValue != road.GenerateSpeedSigns)
            {
                changed = true;
                road.GenerateSpeedSigns = _generateSpeedSigns.boolValue;
            }
            
            if (_connectionDistanceThreshold.floatValue != road.ConnectionDistanceThreshold)
            {
                changed = true;
                road.ConnectionDistanceThreshold = _connectionDistanceThreshold.floatValue;
            }
            
            if (_speedLimit.intValue != (int)road.SpeedLimit)
            {
                changed = true;
                road.SpeedLimit = (SpeedLimit)_speedLimit.intValue;
            }
            
            if (_speedSignDistanceFromIntersectionEdge.floatValue != road.SpeedSignDistanceFromIntersectionEdge)
            {
                changed = true;
                road.SpeedSignDistanceFromIntersectionEdge = _speedSignDistanceFromIntersectionEdge.floatValue;
            }
            
            if (_speedSignDistanceFromRoadEnd.floatValue != road.SpeedSignDistanceFromRoadEnd)
            {
                changed = true;
                road.SpeedSignDistanceFromRoadEnd = _speedSignDistanceFromRoadEnd.floatValue;
            }
            
            if (_shouldSpawnLampPoles.boolValue != road.ShouldSpawnLampPoles)
            {
                changed = true;
                road.ShouldSpawnLampPoles = _shouldSpawnLampPoles.boolValue;
            }
            
            if (_lampPoleIntervalDistance.floatValue != road.LampPoleIntervalDistance)
            {
                changed = true;
                road.LampPoleIntervalDistance = _lampPoleIntervalDistance.floatValue;
            }
            
            if (_lampPoleSideDistanceOffset.floatValue != road.LampPoleSideDistanceOffset)
            {
                changed = true;
                road.LampPoleSideDistanceOffset = _lampPoleSideDistanceOffset.floatValue;
            }
            
            if (_defaultTrafficSignOffset.floatValue != road.DefaultTrafficSignOffset)
            {
                changed = true;
                road.DefaultTrafficSignOffset = _defaultTrafficSignOffset.floatValue;
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

            if (_isOneWay.boolValue != road.IsOneWay)
            {
                changed = true;
                road.IsOneWay = _isOneWay.boolValue;
            }

            if (_isGeneratingOSM.boolValue != road.IsGeneratingOSM)
                road.IsGeneratingOSM = _isGeneratingOSM.boolValue;

            serializedObject.ApplyModifiedProperties();

            if(changed)
                road.OnChange();
        }
    }
}
