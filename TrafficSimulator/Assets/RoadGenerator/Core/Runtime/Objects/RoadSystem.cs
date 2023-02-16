using UnityEngine;
using System.Collections.Generic;
using System;

namespace RoadGenerator
{
    [Serializable]
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
            
            road.RoadObject = roadObj;
            road.RoadSystem = this;
            road.GetComponent<PathSceneTool>().TriggerUpdate();

            AddRoad(road);
        }

        public Intersection AddNewIntersection(IntersectionPointData intersectionPointData, Road road1, Road road2){

            Vector3 position = new Vector3(intersectionPointData.IntersectionPosition.x, 0, intersectionPointData.IntersectionPosition.y);
            GameObject intersectionObject = Instantiate(_intersectionPrefab, position, intersectionPointData.rotation);
            intersectionObject.name = "Intersection" + IntersectionCount;
            intersectionObject.transform.parent = this.transform;
            Intersection intersection = intersectionObject.GetComponent<Intersection>();
            intersection.IntersectionObject = intersectionObject;
            intersection.RoadSystem = this;
            intersection.IntersectionPosition = position;
            intersection.Road1PathCreator = intersectionPointData.Road1PathCreator;
            intersection.Road2PathCreator = intersectionPointData.Road2PathCreator;
            intersection.Road1 = road1;
            intersection.Road2 = road2;
            road1.Intersections.Add(intersection);
            road2.Intersections.Add(intersection);
            AddIntersection(intersection);
            
            return intersection;
        }

        public int RoadCount {
            get => Roads.Count;
        }
        public int IntersectionCount {
            get => Intersections.Count;
        }
    }
}