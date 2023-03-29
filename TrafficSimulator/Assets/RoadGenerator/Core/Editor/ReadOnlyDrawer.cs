using UnityEngine;
using UnityEditor;

namespace CustomProperties
{
    [CustomPropertyDrawer(typeof(ReadOnlyAttribute))]
    public class ReadOnlyDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            // Draw the prefix separately to avoid the title being grayed out
            position = EditorGUI.PrefixLabel(position, GUIUtility.GetControlID(FocusType.Passive), label);
            
            bool enabled = GUI.enabled;
            GUI.enabled = false;
            EditorGUI.PropertyField(position, property, GUIContent.none);
            GUI.enabled = enabled;
            
            EditorGUI.EndProperty();
        }
    }
}