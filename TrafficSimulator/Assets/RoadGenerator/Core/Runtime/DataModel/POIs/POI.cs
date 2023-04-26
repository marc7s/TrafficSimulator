using UnityEngine;
using RoadGenerator;

namespace POIs
{
    abstract public class POI : MonoBehaviour
    {
        public Road Road;
        public RoadNode RoadNode;
        public float DistanceAlongRoad;
        public Vector3 Size;

        public void Setup()
        {
            if(Road != null)
            {
                if(!Road.POIs.Contains(this))
                    Road.POIs.Add(this);
            }
            CustomSetup();
        }

        protected abstract void CustomSetup();
    }
}