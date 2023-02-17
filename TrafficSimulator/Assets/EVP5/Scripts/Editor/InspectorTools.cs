using UnityEngine;
using UnityEditor;


namespace EVP
{

public static class InspectorTools
	{
	static float m_labelWidth;
	static float m_minLabelWidth;

	public static void BeginContent (float minLabelWidth = 0.0f)
		{
		m_labelWidth = EditorGUIUtility.labelWidth;
		m_minLabelWidth = minLabelWidth;
		ResetMinLabelWidth();
		}

	public static void EndContent ()
		{
		EditorGUIUtility.labelWidth = m_labelWidth;
		}

	public static void SetMinLabelWidth (float minLabelWidth = 0.0f)
		{
		EditorGUIUtility.labelWidth = Mathf.Max(EditorGUIUtility.currentViewWidth * 0.4f, minLabelWidth);
		}

	public static void ResetMinLabelWidth ()
		{
		EditorGUIUtility.labelWidth = Mathf.Max(EditorGUIUtility.currentViewWidth * 0.4f, m_minLabelWidth);
		}


	public static void InfoLabel (string label, string text, string hint = null)
		{
		Color currentCol = GUI.contentColor;

		GUI.contentColor = Color.white * 0.8f;

		if (hint == null)
			EditorGUILayout.LabelField(label, text);
		else
			EditorGUILayout.LabelField(new GUIContent(label, hint), new GUIContent(text));

		GUI.contentColor = currentCol;
		}


	public static SerializedProperty PropertyField (SerializedObject serializedObject, string propertyName, string caption = null, string hint = null)
		{
		SerializedProperty property = serializedObject.FindProperty(propertyName);

		if (!string.IsNullOrEmpty(caption))
			{
			if (!string.IsNullOrEmpty(hint))
				EditorGUILayout.PropertyField(property, new GUIContent(caption, hint));
			else
				EditorGUILayout.PropertyField(property, new GUIContent(caption));
			}
		else
			{
			EditorGUILayout.PropertyField(property);
			}

		return property;
		}


	// Convenience methods for a Editor Layout Foldout that respond to clicks on the text also,
	// not only at the fold arrow.

	public static bool LayoutFoldout(bool foldout, string content, string hint)
		{
		Rect rect = EditorGUILayout.GetControlRect();
		return EditorGUI.Foldout(rect, foldout, new GUIContent(content, hint), true);
		}


	public static bool LayoutFoldout(bool foldout, string content)
		{
		Rect rect = EditorGUILayout.GetControlRect();
		return EditorGUI.Foldout(rect, foldout, content, true);
		}
	}
}
