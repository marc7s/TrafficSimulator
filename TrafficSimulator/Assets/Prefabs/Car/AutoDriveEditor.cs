using UnityEngine;
using UnityEditor;
using Car;
using RoadGenerator;

namespace CarGenerator
{
    [CustomEditor(typeof(AutoDrive))]
    public class AutoDriveEditor : Editor 
    {
        #region SerializedProperties
            private SerializedProperty _road;
            private SerializedProperty _laneIndex;
            private SerializedProperty _mode;
            private SerializedProperty _roadEndBehaviour;
            private SerializedProperty _showNavigationPath;
            private SerializedProperty _originalNavigationMode;
            private SerializedProperty _logRepositioningInformation;
            private SerializedProperty _showTargetLines;
            private SerializedProperty _brakeOffset;
            private SerializedProperty _maxRepositioningSpeed;
            private SerializedProperty _maxReverseDistance;
            private SerializedProperty _baseTLD;
            private SerializedProperty _TLDSpeedDivider;
            private SerializedProperty _vehicleOccupancyOffset;
            private SerializedProperty _speed;
            private SerializedProperty _rotationSpeed;
            private SerializedProperty _totalDistance;


        #endregion
        public void OnEnable()
        {
            _road = serializedObject.FindProperty("Road");
            _laneIndex = serializedObject.FindProperty("LaneIndex");
            _mode = serializedObject.FindProperty("Mode");
            _roadEndBehaviour = serializedObject.FindProperty("RoadEndBehaviour");
            _showNavigationPath = serializedObject.FindProperty("ShowNavigationPath");
            _originalNavigationMode = serializedObject.FindProperty("OriginalNavigationMode");
            _logRepositioningInformation = serializedObject.FindProperty("LogRepositioningInformation");
            _showTargetLines = serializedObject.FindProperty("ShowTargetLines");
            _brakeOffset = serializedObject.FindProperty("BrakeOffset");
            _maxRepositioningSpeed = serializedObject.FindProperty("MaxRepositioningSpeed");
            _maxReverseDistance = serializedObject.FindProperty("MaxReverseDistance");
            _baseTLD = serializedObject.FindProperty("BaseTLD");
            _TLDSpeedDivider = serializedObject.FindProperty("TLDSpeedDivider");
            _vehicleOccupancyOffset = serializedObject.FindProperty("VehicleOccupancyOffset");
            _speed = serializedObject.FindProperty("Speed");
            _rotationSpeed = serializedObject.FindProperty("RotationSpeed");
            _totalDistance = serializedObject.FindProperty("TotalDistance");

        }
        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            
            // Uncomment this if you want to change the connections to containers and prefabs
            //DrawDefaultInspector();
            
            AutoDrive autoDrive = (AutoDrive)target;
            
            bool changed = false;

            EditorGUILayout.PropertyField(_road);
            EditorGUILayout.PropertyField(_laneIndex);
            EditorGUILayout.PropertyField(_mode);
            EditorGUILayout.PropertyField(_roadEndBehaviour);
            EditorGUILayout.PropertyField(_showNavigationPath);
            EditorGUILayout.PropertyField(_originalNavigationMode);
            EditorGUILayout.PropertyField(_logRepositioningInformation);
            EditorGUILayout.PropertyField(_showTargetLines);
            EditorGUILayout.PropertyField(_brakeOffset);
            EditorGUILayout.PropertyField(_maxRepositioningSpeed);
            EditorGUILayout.PropertyField(_maxReverseDistance);
            EditorGUILayout.PropertyField(_baseTLD);
            EditorGUILayout.PropertyField(_TLDSpeedDivider);
            EditorGUILayout.PropertyField(_vehicleOccupancyOffset);
            EditorGUILayout.PropertyField(_speed);
            EditorGUILayout.PropertyField(_rotationSpeed);
            EditorGUILayout.PropertyField(_totalDistance);

            if(_road.objectReferenceValue != autoDrive.Road)
            {
                autoDrive.Road = (Road)_road.objectReferenceValue;
            }

            if(_laneIndex.intValue != autoDrive.LaneIndex)
            {
                autoDrive.LaneIndex = _laneIndex.intValue;
            }

            if(_mode.intValue != (int)autoDrive.Mode)
            {
                autoDrive.Mode = (DrivingMode)_mode.intValue;
            }

            if(_roadEndBehaviour.intValue != (int)autoDrive.RoadEndBehaviour)
            {
                autoDrive.RoadEndBehaviour = (RoadEndBehaviour)_roadEndBehaviour.intValue;
            }

            if(_showNavigationPath.boolValue != autoDrive.ShowNavigationPath)
            {
                changed = true;
                autoDrive.ShowNavigationPath = _showNavigationPath.boolValue;
            }

            if(_originalNavigationMode.intValue != (int)autoDrive.OriginalNavigationMode)
            {
                autoDrive.OriginalNavigationMode = (NavigationMode)_originalNavigationMode.intValue;
            }

            if(_logRepositioningInformation.boolValue != autoDrive.LogRepositioningInformation)
            {
                autoDrive.LogRepositioningInformation = _logRepositioningInformation.boolValue;
            }

            if(_showTargetLines.intValue != (int)autoDrive.ShowTargetLines)
            {
                autoDrive.ShowTargetLines = (ShowTargetLines)_showTargetLines.intValue;
            }

            if(_brakeOffset.floatValue != autoDrive.BrakeOffset)
            {
                autoDrive.BrakeOffset = _brakeOffset.floatValue;
            }

            if(_maxRepositioningSpeed.floatValue != autoDrive.MaxRepositioningSpeed)
            {
                autoDrive.MaxRepositioningSpeed = _maxRepositioningSpeed.floatValue;
            }

            if(_maxReverseDistance.floatValue != autoDrive.MaxReverseDistance)
            {
                autoDrive.MaxReverseDistance = _maxReverseDistance.floatValue;
            }

            if(_baseTLD.floatValue != autoDrive.BaseTLD)
            {
                autoDrive.BaseTLD = _baseTLD.floatValue;
            }

            if(_TLDSpeedDivider.intValue != autoDrive.TLDSpeedDivider)
            {
                autoDrive.TLDSpeedDivider = _TLDSpeedDivider.intValue;
            }

            if(_vehicleOccupancyOffset.floatValue != autoDrive.VehicleOccupancyOffset)
            {
                autoDrive.VehicleOccupancyOffset = _vehicleOccupancyOffset.floatValue;
            }

            if(_speed.floatValue != autoDrive.Speed)
            {
                autoDrive.Speed = _speed.floatValue;
            }

            if(_rotationSpeed.floatValue != autoDrive.RotationSpeed)
            {
                autoDrive.RotationSpeed = _rotationSpeed.floatValue;
            }

            if(_totalDistance.floatValue != autoDrive.TotalDistance)
            {
                autoDrive.TotalDistance = _totalDistance.floatValue;
            }

            serializedObject.ApplyModifiedProperties();

            if(changed)
            {
                //roadSystem.UpdateRoadSystemGraph();
            }

        }
    }
}
