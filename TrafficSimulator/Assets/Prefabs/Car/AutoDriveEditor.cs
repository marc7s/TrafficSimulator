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
            
            if(!Application.isPlaying)
            {
                EditorGUILayout.PropertyField(_road);
                EditorGUILayout.PropertyField(_laneIndex);
                EditorGUILayout.PropertyField(_mode);
                EditorGUILayout.PropertyField(_roadEndBehaviour);
            }

            EditorGUILayout.PropertyField(_showNavigationPath);

            if(!Application.isPlaying)
            {
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
            }

            EditorGUILayout.PropertyField(_totalDistance);

            if(_showNavigationPath.boolValue != autoDrive.ShowNavigationPath)
            {
                autoDrive.ShowNavigationPath = _showNavigationPath.boolValue;
            }

            serializedObject.ApplyModifiedProperties();

            autoDrive.SetNavigationPathVisibilty(_showNavigationPath.boolValue);
        }
    }
}
