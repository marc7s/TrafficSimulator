using UnityEditor;
using RoadGenerator;
using InfoScriptsEditorBase;

namespace RoadNodeInfoEditor
{
    [CustomEditor(typeof(RoadNodeInfo))]
    public class RoadNodeInfoEditor : InfoScriptsEditor
    {
        public override void OnInspectorGUI()
        {
            RoadNodeInfo roadNodeInfo = (RoadNodeInfo)target;
            if(roadNodeInfo.IsInitialized)
                DrawProperties();
        }
    }
}
