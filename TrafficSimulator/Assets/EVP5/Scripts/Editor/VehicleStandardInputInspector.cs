//------------------------------------------------------------------------------------------------
// Edy's Vehicle Physics
// (c) Angel Garcia "Edy" - Oviedo, Spain
// http://www.edy.es
//------------------------------------------------------------------------------------------------

using UnityEngine;
using UnityEditor;

namespace EVP
{

[CustomEditor(typeof(VehicleStandardInput))]
public class VehicleStandardInputInspector : Editor
	{
	public override void OnInspectorGUI ()
		{
		InspectorTools.BeginContent();
		serializedObject.Update();

		EditorGUILayout.PropertyField(serializedObject.FindProperty("target"));

		InspectorTools.SetMinLabelWidth(210);
		EditorGUILayout.PropertyField(serializedObject.FindProperty("continuousForwardAndReverse"));
		EditorGUILayout.PropertyField(serializedObject.FindProperty("handbrakeOverridesThrottle"));
		InspectorTools.SetMinLabelWidth(160);

		SerializedProperty propThrottleAndBrakeInput = serializedObject.FindProperty("throttleAndBrakeInput");
		EditorGUILayout.PropertyField(propThrottleAndBrakeInput);

		EditorGUILayout.PropertyField(serializedObject.FindProperty("steerAxis"));

		VehicleStandardInput.ThrottleAndBrakeInput throttleAndBrakeInput = (VehicleStandardInput.ThrottleAndBrakeInput)propThrottleAndBrakeInput.enumValueIndex;

		if (throttleAndBrakeInput == VehicleStandardInput.ThrottleAndBrakeInput.SeparateAxes)
			{
			EditorGUILayout.PropertyField(serializedObject.FindProperty("throttleAxis"));
			EditorGUILayout.PropertyField(serializedObject.FindProperty("brakeAxis"));
			}
		else
			{
			EditorGUILayout.PropertyField(serializedObject.FindProperty("throttleAndBrakeAxis"));
			}

		EditorGUILayout.PropertyField(serializedObject.FindProperty("handbrakeAxis"));
		EditorGUILayout.PropertyField(serializedObject.FindProperty("resetVehicleKey"));

		serializedObject.ApplyModifiedProperties();
		InspectorTools.EndContent();
		}

	}
}
