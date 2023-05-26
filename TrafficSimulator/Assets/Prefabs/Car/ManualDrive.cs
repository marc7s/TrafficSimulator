using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;
using RoadGenerator;
using DataModel;
using EVP;
using User;
using Simulation;
using System;

namespace VehicleBrain
{
    [RequireComponent(typeof(VehicleController))]
    [RequireComponent(typeof(BoxCollider))]
    [RequireComponent(typeof(Vehicle))]
    [RequireComponent(typeof(Rigidbody))]
    public abstract class ManualDrive : MonoBehaviour
    {
        [SerializeField] protected bool _showOccupiedNodes = false;
        protected VehicleController _vehicleController;
        protected BoxCollider _collider;
        protected Rigidbody _rigidbody;
        protected List<LaneNode> _occupiedNodes = new List<LaneNode>();
        protected Vehicle _vehicle = null;
        protected LineRenderer _lineRenderer = null;
        protected DefaultRoad _currentRoad = null;
        protected Intersection _currentIntersection = null;

        protected void Start()
        {
            _vehicleController = GetComponent<VehicleController>();
            _collider = GetComponent<BoxCollider>();
            _rigidbody = GetComponent<Rigidbody>();
            
            _vehicle = GetComponent<Vehicle>();
            _vehicle.CurrentSpeedFunction = GetCurrentSpeed;
            
            _lineRenderer = GetComponent<LineRenderer>();
            _lineRenderer.positionCount = 0;
            _lineRenderer.sharedMaterial = new Material(Shader.Find("Standard"));
            _lineRenderer.sharedMaterial.SetColor("_Color", Color.green);
            _lineRenderer.startWidth = 0.3f;
            _lineRenderer.endWidth = 0.3f;
        }

        public float GetCurrentSpeed()
        {
            return _rigidbody.velocity.magnitude;
        }

        private void Update()
        {
            if(_currentRoad != null || _currentIntersection != null)
            {
                UpdateOccupiedNodes();
                if(_showOccupiedNodes)
                {
                    _lineRenderer.positionCount = _occupiedNodes.Count;
                    _lineRenderer.SetPositions(_occupiedNodes.ConvertAll(node => node.Position).ToArray());
                }
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            DefaultRoad road = other.gameObject.GetComponent<DefaultRoad>();
            Intersection intersection = other.gameObject.GetComponent<Intersection>();

            // Ignore triggers with objects that are not roads or intersections
            if(road == null && intersection == null)
                return;
            
            if(road != null)
                _currentRoad = road;

            if(intersection != null)
                _currentIntersection = intersection;
        }

        private void OnTriggerExit(Collider other)
        {
            DefaultRoad road = other.gameObject.GetComponent<DefaultRoad>();
            Intersection intersection = other.gameObject.GetComponent<Intersection>();

            // Ignore triggers with objects that are not roads or intersections
            if(road == null && intersection == null)
                return;
            
            if(road != null)
            {
                _currentRoad = null;
                ExitNodes(_occupiedNodes.FindAll(node => node.RoadNode.Road == road));
            }

            if(intersection != null)
            {
                _currentIntersection = null;
                ExitNodes(_occupiedNodes.FindAll(node => node is GuideNode));
            }
        }

        private LaneNode FindClosestRoadLaneNode(DefaultRoad road, ref float closestDistance)
        {
            if(road == null)
                return null;
            
            LaneNode closestNode = null;
            
            foreach(Lane lane in road.Lanes)
                UpdateClosestNode(lane.StartNode, ref closestNode, ref closestDistance);

            return closestNode;
        }

        private (LaneNode, LaneNode, bool) FindClosestIntersectionLaneNode(Intersection intersection, ref float closestDistance)
        {
            if(intersection == null)
                return (null, null, false);
            
            LaneNode closestNode = null;
            bool entryIsCloser = true;

            (LaneNode closestEntryNode, LaneNode closestEntryJunction) = (null, null);
            (LaneNode closestExitNode, LaneNode closestExitJunction) = (null, null);
            
            
            foreach(Intersection.Section entrySection in intersection.EntrySections)
            {
                bool updated = UpdateClosestNode(entrySection.Start, ref closestEntryNode, ref closestDistance);
                if(updated)
                {
                    closestEntryJunction = entrySection.JunctionNode;
                    closestNode = closestEntryNode;
                }
            }

            foreach(Intersection.Section exitSection in intersection.ExitSections)
            {
                bool updated = UpdateClosestNode(exitSection.Start, ref closestExitNode, ref closestDistance);
                if(updated)
                {
                    closestExitJunction = exitSection.JunctionNode;
                    closestNode = closestExitNode;
                    entryIsCloser = false;
                }
            }

            return (closestNode, entryIsCloser ? closestEntryJunction : closestExitJunction, entryIsCloser);
        }

        /// <summary> Updates the references if a closer node was found. Returns true if that was the case </summary>
        private bool UpdateClosestNode(LaneNode node, ref LaneNode closestNode, ref float closestDistance)
        {
            LaneNode curr = node;
            bool foundCloser = false;
            
            while(curr != null)
            {
                if(ContainsNode(curr))
                {
                    if(Vector3.Distance(curr.Position, transform.position) < closestDistance)
                    {
                        closestNode = curr;
                        closestDistance = Vector3.Distance(curr.Position, transform.position);
                        foundCloser = true;
                    }
                }
                    
                curr = curr.Next;
            }

            return foundCloser;
        }

        private void ExitNodes(List<LaneNode> nodes)
        {
            foreach (LaneNode node in nodes)
            {
                if (_occupiedNodes.Contains(node))
                {
                    _occupiedNodes.Remove(node);
                    node.UnsetVehicle(_vehicle);
                }
            }
        }

        private void UpdateOccupiedNodes()
        {
            LaneNode closestNode = null;
            LaneNode closestJunction = null;
            bool isEntrySection = false;
            float closestDistance = float.MaxValue;
            bool closestIsRoad = true;
            
            if(_currentRoad != null)
            {
                LaneNode closestRoadNode = FindClosestRoadLaneNode(_currentRoad, ref closestDistance);
                
                if(closestRoadNode != null)
                    closestNode = closestRoadNode;
            }  
            
            if(_currentIntersection != null)
            {
                LaneNode closestIntersectionNode = null;
                closestDistance = float.MaxValue;
                
                (closestIntersectionNode, closestJunction, isEntrySection) = FindClosestIntersectionLaneNode(_currentIntersection, ref closestDistance);
                
                if(closestIntersectionNode != null)
                {
                    closestNode = closestIntersectionNode;
                    closestIsRoad = false;
                }
            }
                
            if(closestNode == null)
                return;

            HashSet<LaneNode> nodesToOccupy = new HashSet<LaneNode>();

            if(closestNode != null)
                nodesToOccupy.Add(closestNode);
            
            LaneNode curr = closestNode?.Next;
            
            // Add current and forward nodes
            while(curr != null && ContainsNode(curr) && (curr.Vehicle == null || curr.Vehicle == _vehicle))
            {
                // Do not occupy nodes in front of a red light
                if(curr.TrafficLight != null && curr.TrafficLight.CurrentState == TrafficLightState.Red)
                    break;
                
                nodesToOccupy.Add(curr);
                curr = curr.Next;
            }

            // Add backward nodes
            curr = closestNode?.Prev;
            while(curr != null && ContainsNode(curr) && (curr.Vehicle == null || curr.Vehicle == _vehicle))
            {
                nodesToOccupy.Add(curr);
                curr = curr.Prev;
                
                // If the closest node was in an intersection entry section, transition from the start of the guide node to the junction node
                if(curr == null && !closestIsRoad && isEntrySection)
                    curr = closestJunction;
            }

            // Unset from all nodes that should not be occupied
            foreach(LaneNode node in _occupiedNodes)
            {
                if(nodesToOccupy.Contains(node))
                    continue;
                
                node.UnsetVehicle(_vehicle);
            }

            _occupiedNodes.Clear();

            if(_currentIntersection != null && ContainsNode(_currentIntersection.IntersectionCenterLaneNode))
            {
                bool claimedCenter = _currentIntersection.IntersectionCenterLaneNode.SetVehicle(_vehicle);
                if(claimedCenter)
                    _occupiedNodes.Add(_currentIntersection.IntersectionCenterLaneNode);
            }

            // Claim all nodes that should be occupied
            foreach(LaneNode node in nodesToOccupy)
            {
                bool claimed = node.SetVehicle(_vehicle);
                if(claimed)
                    _occupiedNodes.Add(node);
            }
        }

        private bool ContainsNode(LaneNode node)
        {
            return _collider.bounds.Contains(node.Position);
        }

        protected virtual void OnThrottle(InputValue value) {}

        protected virtual void OnSteer(InputValue value) {}

        protected virtual void OnReverseGear(InputValue value) {}

        protected virtual void OnBrake(InputValue value) {}

        protected virtual void OnHandbrake(InputValue value) {}

        protected void OnRespawn(InputValue value)
        {
            if(!value.isPressed)
                return;
            
            RigidbodyPause rigidbodyPause = _vehicleController.GetComponent<RigidbodyPause>();
            rigidbodyPause.pause = true;
            transform.position = transform.position + Vector3.up;
            transform.rotation = Quaternion.identity;
        
            TimeManagerEvent unPauseEvent = new TimeManagerEvent(DateTime.Now.AddMilliseconds(1000));
            TimeManager.Instance.AddEvent(unPauseEvent);
            unPauseEvent.OnEvent += () => rigidbodyPause.pause = false;
        }
    }
}