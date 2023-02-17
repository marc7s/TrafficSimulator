using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System;

namespace RoadGenerator
{
    public enum DrivingSide
    {
        Left = 1,
        Right = -1
    }
    
    [ExecuteInEditMode()]
    [Serializable]
	public class RoadSystem : MonoBehaviour
	{
        [Header("Connections")]       
        [SerializeField] private GameObject _roadContainer;
        [SerializeField] private GameObject _intersectionContainer;
        [SerializeField] private GameObject _roadPrefab;
        [SerializeField] private GameObject _intersectionPrefab;

        [Header("Road system settings")]
        public DrivingSide DrivingSide = DrivingSide.Right;
        [SerializeField] private bool _spawnRoadsAtOrigin = false;

        [SerializeField][HideInInspector] private List<Road> _roads = new List<Road>();

        public List<Intersection> Intersections {get; private set;} = new List<Intersection>();

        public void AddIntersection(Intersection intersection) => Intersections.Add(intersection);
        public void RemoveIntersection(Intersection intersection) => Intersections.Remove(intersection);
        public void AddRoad(Road road) => _roads.Add(road);

        public void RemoveRoad(Road road) => _roads.Remove(road);
        public void AddNewRoad()
        {
            Vector3 spawnPoint = Vector3.zero;
            if(!_spawnRoadsAtOrigin)
            {
                RaycastHit hit;
                SceneView sceneView = SceneView.lastActiveSceneView;
                Camera camera = sceneView.camera;
                
                // Get the nearest point on the surface the camera is looking at
                if(!camera || !Physics.Raycast(camera.transform.position, camera.transform.forward, out hit))
                {
                    Debug.LogError("No surface found in line of sight to spawn road. Make sure the surface you are looking at has a collider");
                    return;
                }
                spawnPoint = hit.point;
            }
            

            // Instantiate a new road prefab
            GameObject roadObj = Instantiate(_roadPrefab, Vector3.zero, Quaternion.identity);
            
            // Set the name of the road
            roadObj.name = "Road" + RoadCount;
            
            // Set the road as a child of the road container
            roadObj.transform.parent = _roadContainer.transform;
            
            // Get the road from the prefab
            Road road = roadObj.GetComponent<Road>();

            // Move the road to the spawn point
            PathCreator pathCreator = roadObj.GetComponent<PathCreator>();
            pathCreator.bezierPath = new BezierPath(spawnPoint);
            
            // Set the road pointers
            road.RoadObject = roadObj;
            road.RoadSystem = this;
            
            // Update the road to display it
            road.OnChange();

            AddRoad(road);
        }

        // Since serialization did not work, this sets up the road system by locating all its roads and intersections
        public void Setup()
        {
            // Find roads
            foreach(Transform roadT in _roadContainer.transform)
            {
                Road road = roadT.GetComponent<Road>();
                road.RoadSystem = this;
                
                AddRoad(road);
            }

            // Find intersections
            foreach(Transform intersectionT in _intersectionContainer.transform)
            {
                Intersection intersection = intersectionT.GetComponent<Intersection>();
                intersection.RoadSystem = this;
                
                AddIntersection(intersection);
            }
        }

        public Intersection AddNewIntersection(IntersectionPointData intersectionPointData, Road road1, Road road2)
        {
            Vector3 intersectionPosition = new Vector3(intersectionPointData.Position.x, 0, intersectionPointData.Position.y);
            GameObject intersectionObject = Instantiate(_intersectionPrefab, intersectionPosition, intersectionPointData.Rotation);
            intersectionObject.name = "Intersection" + IntersectionCount;
            intersectionObject.transform.parent = _intersectionContainer.transform;
            
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
            
            road1.AddIntersection(intersection);
            road2.AddIntersection(intersection);
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