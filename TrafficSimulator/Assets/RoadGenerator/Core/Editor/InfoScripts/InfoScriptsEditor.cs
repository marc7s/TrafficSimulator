using UnityEditor;

namespace InfoScriptsEditorBase
{
    public abstract class InfoScriptsEditor : Editor
    {
        public void DrawProperties()
        {
            serializedObject.Update();
            // Do not draw the script field
            DrawPropertiesExcluding(serializedObject, new string[] { "m_Script" });
            serializedObject.ApplyModifiedProperties();
        }
    }
}