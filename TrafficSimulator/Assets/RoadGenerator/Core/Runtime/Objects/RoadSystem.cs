using UnityEngine;
using System.Collections.Generic;
using System;

namespace RoadGenerator
{
    public enum DrivingSide
    {
        Left = 1,
        Right = -1
    }
    //[Serializable]
	public class RoadSystem : MonoBehaviour
	{
        [Header("Connections")]       
        [SerializeField] private GameObject _roadContainer;
        [SerializeField] private GameObject _roadPrefab;
        [SerializeField] private GameObject _intersectionPrefab;

        [Header("Road system settings")]
        public DrivingSide DrivingSide = DrivingSide.Right;

        private List<Road> _roads = new List<Road>();

        public List<Intersection> Intersections {get; private set;} = new List<Intersection>();

        public void AddIntersection(Intersection intersection) => Intersections.Add(intersection);
        public void RemoveIntersection(Intersection intersection) => Intersections.Remove(intersection);
        public void AddRoad(Road road) => _roads.Add(road);

        public void RemoveRoad(Road road) => _roads.Remove(road);
        public void AddNewRoad()
        {
            // Instantiate a new road prefab
            GameObject roadObj = Instantiate(_roadPrefab, _roadContainer.transform.position, Quaternion.identity);
            
            // Set the name of the road
            roadObj.name = "Road" + RoadCount;
            
            // Set the road as a child of the road container
            roadObj.transform.parent = _roadContainer.transform;
            
            // Get the road from the prefab
            Road road = roadObj.GetComponent<Road>();
            
            // Set the road pointers
            road.RoadObject = roadObj;
            road.RoadSystem = this;
            
            // Update the road to display it
            road.Update();

            AddRoad(road);
        }

        public Intersection AddNewIntersection(IntersectionPointData intersectionPointData, Road road1, Road road2){
            Vector3 intersectionPosition = new Vector3(intersectionPointData.IntersectionPosition.x, 0, intersectionPointData.IntersectionPosition.y);
            GameObject intersectionObject = Instantiate(_intersectionPrefab, intersectionPosition, intersectionPointData.Rotation);
            intersectionObject.name = "Intersection" + IntersectionCount;
            intersectionObject.transform.parent = this.transform;
            Intersection intersection = intersectionObject.GetComponent<Intersection>();
            intersection.IntersectionObject = intersectionObject;
            intersection.RoadSystem = this;
            intersection.IntersectionPosition = intersectionPosition;
            intersection.Road1PathCreator = intersectionPointData.Road1PathCreator;
            intersection.Road2PathCreator = intersectionPointData.Road2PathCreator;
            intersection.Road1 = road1;
            intersection.Road2 = road2;
            intersection.Road1AnchorPoint1 = intersectionPointData.Road1AnchorPoint1;
            intersection.Road1AnchorPoint2 = intersectionPointData.Road1AnchorPoint2;
            intersection.Road2AnchorPoint1 = intersectionPointData.Road2AnchorPoint1;
            intersection.Road2AnchorPoint2 = intersectionPointData.Road2AnchorPoint2;
            road1.Intersections.Add(intersection);
            road2.Intersections.Add(intersection);
            AddIntersection(intersection);
            
            return intersection;
        }
        /// <summary> Checks if an intersection already exists at the given position </summary>
        public bool DoesIntersectionExist(Vector3 position)
        {
            foreach (Intersection intersection in Intersections)
            {
                if (Vector3.Distance(position, intersection.IntersectionPosition) < Intersection.IntersectionLength)
                {
                    return true;
                }
            }
            return false;
        }
        public int IntersectionCount 
        {
            get => Intersections.Count;
        }
        /// <summary>Returns the number of roads in the road system</summary>
        public int RoadCount 
        {
            get => _roads.Count;
        }
        public List<Road> Roads 
        {
            get => _roads;
        }
    }
}