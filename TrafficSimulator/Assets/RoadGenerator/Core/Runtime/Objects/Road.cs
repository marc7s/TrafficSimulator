using UnityEngine;
using System.Collections.Generic;

namespace RoadGenerator
{
    [ExecuteInEditMode()]
	public class Road : MonoBehaviour
	{
        public GameObject RoadObject;
        public RoadSystem RoadSystem;

        public List<Intersection> Intersections = new List<Intersection>();
    
        void OnDestroy()
        {
            RoadSystem.RemoveRoad(this);
            int count = Intersections.Count;
            for (var i = 0; i < count; i++)
            {
                Intersection intersection = Intersections[0];
                Intersections.RemoveAt(0);
                DestroyImmediate(intersection.gameObject);
            }
        }
    }
}