using UnityEngine;
#if UNITY_EDITOR
   using UnityEditor;
#endif
 
namespace CustomProperties
{
    /// <summary>
    /// Serializable Nullable (SN) Does the same as C# System.Nullable, except it's an ordinary
    /// serializable struct, allowing unity to serialize it and show it in the inspector.
    /// </summary>
    [System.Serializable]
    public struct SNReadOnly<T> where T : struct
    {
        public T Value { 
            get {
                if(!HasValue)
                    throw new System.InvalidOperationException("Serializable nullable object must have a value.");
                
                return v;
            }
        }
    
        public bool HasValue { get => hasValue; }
        
        [SerializeField] private T v;
        
        [SerializeField] private bool hasValue;
        
        public SNReadOnly(bool hasValue, T v)
        {
            this.v = v;
            this.hasValue = hasValue;
        }
        
        private SNReadOnly(T v)
        {
            this.v = v;
            this.hasValue = true;
        }
        
        public static implicit operator SNReadOnly<T>(T value)
        {
            return new SNReadOnly<T>(value);
        }
        
        public static implicit operator SNReadOnly<T>(System.Nullable<T> value)
        {
            return value.HasValue ? new SNReadOnly<T>(value.Value) : new SNReadOnly<T>();
        }
        
        public static implicit operator System.Nullable<T>(SNReadOnly<T> value)
        {
            return value.HasValue ? (T?)value.Value : null;
        }
    }
 
 #if UNITY_EDITOR
    [CustomPropertyDrawer(typeof(SNReadOnly<>))]
    internal class SNReadOnlyDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            // Draw label
            position = EditorGUI.PrefixLabel(position, GUIUtility.GetControlID(FocusType.Passive), label);
        
            // Do not indent child fields
            int indent = EditorGUI.indentLevel;
            EditorGUI.indentLevel = 0;
        
            // Calculate rects
            Rect setRect = new Rect(position.x, position.y, 15, position.height);
            float consumed = setRect.width + 5;
            Rect valueRect = new Rect(position.x + consumed, position.y, position.width - consumed, position.height);
        
            // Draw fields - pass GUIContent.none to each so they are drawn without labels
            SerializedProperty hasValueProp = property.FindPropertyRelative("hasValue");
            bool guiEnabled = GUI.enabled;
            GUI.enabled = false;
            
            // Display the value if is has one, otherwise display "null"
            if(hasValueProp.boolValue)
                EditorGUI.PropertyField(valueRect, property.FindPropertyRelative("v"), GUIContent.none);
            else
                EditorGUI.LabelField(valueRect, "null");
            
            GUI.enabled = guiEnabled;
        
            // Set indent back to what it was
            EditorGUI.indentLevel = indent;
        
            EditorGUI.EndProperty();
        }
    }
 #endif
}
 