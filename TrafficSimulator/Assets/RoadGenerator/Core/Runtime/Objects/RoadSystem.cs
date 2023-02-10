using UnityEngine;
using System.Collections.Generic;
using System;
using UnityEditor;

namespace RoadGenerator
{
	public class RoadSystem : MonoBehaviour
	{
        private List<Road> _roads = new List<Road>();

        public GameObject roadContainer;
        public GameObject roadPrefab;

        public void AddRoad(Road road) => _roads.Add(road);
        public void RemoveRoad(Road road) => _roads.Remove(road);

        public void AddNewRoad()
        {
            GameObject roadObj = Instantiate(roadPrefab, roadContainer.transform.position, Quaternion.identity);
            roadObj.name = "Road" + RoadCount;
            roadObj.transform.parent = roadContainer.transform;
            Road road = roadObj.GetComponent<Road>();
            
            road.road = roadObj;
            road.roadSystem = this;
            road.GetComponent<PathSceneTool>().TriggerUpdate();

            AddRoad(road);
        }

        private bool addRoad(UnityEngine.Object instance)
        {
            GameObject roadObject = instance as GameObject;
            Debug.Log(roadObject);
            return true;
        }

        public int RoadCount {
            get => _roads.Count;
        }
    }
}