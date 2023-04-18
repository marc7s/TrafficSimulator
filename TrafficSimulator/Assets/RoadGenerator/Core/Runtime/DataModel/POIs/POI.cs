using UnityEngine;
using RoadGenerator;

namespace POIs
{
    abstract public class POI : MonoBehaviour
    {
        public Road Road;
        public float DistanceAlongRoad;

        public void Awake()
        {
            if(Road != null)
            {
                if(!Road.POIs.Contains(this))
                    Road.POIs.Add(this);
            }
        }
    }
}