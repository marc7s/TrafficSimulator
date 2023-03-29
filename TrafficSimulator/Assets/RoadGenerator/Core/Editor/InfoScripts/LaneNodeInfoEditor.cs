using UnityEditor;
using RoadGenerator;
using InfoScriptsEditorBase;

namespace LaneNodeInfoEditor
{
    [CustomEditor(typeof(LaneNodeInfo))]
    public class LaneNodeInfoEditor : InfoScriptsEditor
    {
        public override void OnInspectorGUI()
        {
            LaneNodeInfo laneNodeInfo = (LaneNodeInfo)target;
            if(laneNodeInfo.IsInitialized)
                DrawProperties();
        }
    }
}
