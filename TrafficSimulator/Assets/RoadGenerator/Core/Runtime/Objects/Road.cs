using UnityEngine;

namespace RoadGenerator
{
    [ExecuteInEditMode()]
	public class Road : MonoBehaviour
	{
        public GameObject RoadObject;
        public RoadSystem RoadSystem;
    
        void OnDestroy()
        {
            RoadSystem.RemoveRoad(this);
        }
    }
}