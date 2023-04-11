using UnityEngine;

namespace RoadGenerator
{
    [RequireComponent(typeof(TramRailMeshCreator))]
    public class TramRail : Road
    {
        public float RailWidth = 0.1f;
        public float RailDepth = 0.1f;
        public float RailSpacing = 0.75f;
        public float RailPadding = 0.2f;

        public override TrafficSignAssessor GetNewTrafficSignAssessor()
        {
            return new TramRailTrafficSignAssessor();
        }

        public override void UpdateMesh()
        {
           TramRailMeshCreator tramRailMeshCreator = RoadObject.GetComponent<TramRailMeshCreator>();
           tramRailMeshCreator.UpdateMesh();
        }
    }
}