using UnityEngine;
using System.Collections.Generic;
using System;
using UnityEditor;

namespace RoadGenerator
{
	public class RoadSystem : MonoBehaviour
	{
        public List<Intersection> Intersections {get; private set;} = new List<Intersection>();
        public List<Road> Roads {get; private set;} = new List<Road>();
        [SerializeField]
        private GameObject _roadContainer;
        [SerializeField]
        private GameObject _roadPrefab;
        [SerializeField]
        public GameObject _intersectionPrefab;

        public void AddRoad(Road road) => Roads.Add(road);
        public void RemoveRoad(Road road) => Roads.Remove(road);

        public void AddIntersection(Intersection intersection) => Intersections.Add(intersection);
        public void RemoveIntersection(Intersection intersection) => Intersections.Remove(intersection);

        public void AddNewRoad()
        {
            GameObject roadObj = Instantiate(_roadPrefab, _roadContainer.transform.position, Quaternion.identity);
            roadObj.name = "Road" + RoadCount;
            roadObj.transform.parent = _roadContainer.transform;
            Road road = roadObj.GetComponent<Road>();
            
            road.road = roadObj;
            road.roadSystem = this;
            road.GetComponent<PathSceneTool>().TriggerUpdate();

            AddRoad(road);
        }

        public Intersection AddNewIntersection(Vector3 position, Quaternion rotation){
            GameObject intersectionObject = Instantiate(_intersectionPrefab, position, rotation);
            intersectionObject.name = "Intersection" + IntersectionCount;
            intersectionObject.transform.parent = this.transform;
            Intersection intersection = intersectionObject.GetComponent<Intersection>();
            intersection.IntersectionObject = intersectionObject;
            intersection.SetConnectionPoints();
            AddIntersection(intersection);
            
            return intersection;
        }

        private bool addRoad(UnityEngine.Object instance)
        {
            GameObject roadObject = instance as GameObject;
            Debug.Log(roadObject);
            return true;
        }

        public int RoadCount {
            get => Roads.Count;
        }
        public int IntersectionCount {
            get => Intersections.Count;
        }
    }
}