using UnityEngine;
using System.Collections.Generic;
using System;
using UnityEditor;

namespace RoadGenerator
{
    public enum DrivingSide
    {
        Left = 1,
        Right = -1
    }

	public class RoadSystem : MonoBehaviour
	{
        [Header("Connections")]       
        public GameObject RoadContainer;
        public GameObject RoadPrefab;

        [Header("Road system settings")]
        public DrivingSide DrivingSide = DrivingSide.Right;

        private List<Road> _roads = new List<Road>();

        public void AddRoad(Road road) => _roads.Add(road);
        public void RemoveRoad(Road road) => _roads.Remove(road);

        public void AddNewRoad()
        {
            // Instantiate a new road prefab
            GameObject roadObj = Instantiate(RoadPrefab, RoadContainer.transform.position, Quaternion.identity);
            
            // Set the name of the road
            roadObj.name = "Road" + RoadCount;
            
            // Set the road as a child of the road container
            roadObj.transform.parent = RoadContainer.transform;
            
            // Get the road from the prefab
            Road road = roadObj.GetComponent<Road>();
            
            // Set the road pointers
            road.road = roadObj;
            road.roadSystem = this;
            
            // Update the road to display it
            road.Update();

            AddRoad(road);
        }

        /// <summary>Returns the number of roads in the road system</summary>
        public int RoadCount 
        {
            get => _roads.Count;
        }
    }
}