using UnityEngine;

namespace RoadGenerator
{
	public class Road : MonoBehaviour
	{
        public GameObject road;
        public RoadSystem roadSystem;
    
        void OnDestroy()
        {
            roadSystem.RemoveRoad(this);
        }
    }
}