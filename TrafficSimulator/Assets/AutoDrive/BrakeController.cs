using UnityEngine;
using RoadGenerator;
using System;
using DataModel;

namespace VehicleBrain
{
    public enum BrakeEventType
    { 
        Vehicle, 
        RoadEnd, 
        TrafficLight,
        Yield,
        YieldBlocking
    }

    public class BrakeController : AutoDriveController<BrakeEventType>
    {
        public override bool ShouldAct(ref AutoDriveAgent agent)
        {
            agent.Context.BrakeUndershoot = 0;
            LaneNode curr = agent.Context.CurrentNode;
            LaneNode prev = curr.Prev;
            float distance = 0;
            float brakeDistance = GetBrakeDistance(ref agent) + Vector3.Distance(agent.Context.CurrentNode.Position, agent.Context.VehiclePosition);
            
            // Add an offset if we are braking or are stopped so we do not lose track of the braking target due to deceleration
            float brakingOffset = agent.Setting.Mode == DrivingMode.Quality ? 2f : 10f;
            float offset = agent.Context.IsBrakingOrStopped ? brakingOffset : 0;

            while(curr != null && distance < brakeDistance + offset)
            {
                agent.Context.BrakeTarget = curr;
                agent.Context.BrakeUndershoot = Mathf.Max(0, brakeDistance - distance);
                    
                if(ShouldActAtNode(ref agent, curr))
                    return true;

                prev = curr;
                curr = agent.Next(curr);
                distance += GetNodeDistance(curr, prev);
            }

            return false;
        }

        private static float GetNodeDistance(LaneNode curr, LaneNode prev)
        {
            if(curr == null || prev == null)
                return 0;
            else
                return curr.Type == RoadNodeType.JunctionEdge && curr.Prev.IsIntersection() ? Vector3.Distance(curr.Position, prev.Position) : curr.DistanceToPrevNode;
        }

        private static float GetBrakeDistance(ref AutoDriveAgent agent)
        {
            float speed = agent.Setting.Vehicle.CurrentSpeed;
            const float g = 9.82f;
            const float qualityOffset = 2f;
            
            float performanceBrakeCoef = agent.Context.CurrentAction == DrivingAction.Braking || agent.Context.CurrentAction == DrivingAction.Stopped ? 2f : 1f;
            
            switch(agent.Setting.Mode)
            {
                case DrivingMode.Quality:
                    // Calculate the distance it will take to stop
                    return agent.Setting.BrakeOffset + qualityOffset + speed + speed * speed / (agent.Setting.VehicleController.tireFriction * g);

                case DrivingMode.Performance:
                    /* 
                        Formula explanation: Since we are working with a constant acceleration in the performance mode, two formulas will hold:
                        
                        (1) v = a * t
                        (2) s = v * t
                        
                        Due to the constant acceleration, the distance travelled will be the same as if it travelled with a constant speed equal
                        to the average of the speeds. During braking, it will go from v to 0 so the delta will be v.
                        Therefore:
                        
                        v = a * t = (v - 0) / 2 = v / 2
                        
                        Since a is known, we can calculate t:
                        
                        a * t = v / 2 => t = v / 2a
                        
                        Going back to (2), this gives us the final formula:
                        s = v * v / 2a
                    */
                    return agent.Setting.BrakeOffset + performanceBrakeCoef * speed * speed / (agent.Setting.Acceleration * 2f);

                default:
                    return 0;
            }
        }
        private static bool ShouldYield(AutoDriveAgent agent, ref LaneNode node)
        {
            return node.YieldNodes.Exists(nodePair => ShouldYieldForNode(agent, ref nodePair));
        }

        private static bool ShouldYieldBlocking(AutoDriveAgent agent, ref LaneNode node)
        {
            return node.YieldBlockingNodes.Exists(blockingNode => blockingNode.HasVehicle() && blockingNode.Vehicle != agent.Setting.Vehicle);
        }
        
        private static bool ShouldYieldForNode(AutoDriveAgent agent, ref (LaneNode, LaneNode) yieldForNodePair)
        {
            (LaneNode yieldStart, LaneNode yieldTransition) = yieldForNodePair;
            LaneNode curr = yieldStart;
            LaneNode prev = curr.Next;
            float distanceToYieldNode = Vector3.Distance(agent.Context.CurrentNode.Position, yieldStart.Position);
            float distanceToCurrNode = distanceToYieldNode;

            const float yieldTime = 2f;
            const float maxSpeed = 20f;
            const float maxDistance = maxSpeed * yieldTime;

            while(curr != null && distanceToCurrNode < maxDistance + distanceToYieldNode)
            {
                if(curr.Type == RoadNodeType.End)
                    return false;

                if(curr.HasVehicle() && curr.Vehicle != agent.Setting.Vehicle)
                {
                    float maxSpeedOtherVehicle = distanceToCurrNode / yieldTime;
                    
                    float currentSpeed = Mathf.Max(1, agent.Setting.Vehicle.CurrentSpeed);
                    float otherSpeed = Mathf.Max(1, curr.Vehicle.CurrentSpeed);
                    
                    float timeToNode = distanceToCurrNode / currentSpeed;
                    float otherTimeToNode = distanceToCurrNode / otherSpeed;

                    if(currentSpeed > maxSpeedOtherVehicle && otherTimeToNode < timeToNode)
                        return true;
                }

                distanceToCurrNode += GetNodeDistance(curr, prev);
                
                prev = curr;
                curr = agent.Prev(curr, RoadEndBehaviour.Stop);

                if(curr == null)
                {
                    curr = yieldTransition;
                    
                    if(prev != null)
                        distanceToCurrNode += Vector3.Distance(prev.Position, curr.Position);
                }
            }

            return false;
        }
        public override Func<LaneNode, bool> EventAssessor(ref AutoDriveAgent agent, BrakeEventType type)
        {
            Vehicle vehicle = agent.Setting.Vehicle;
            RoadEndBehaviour endBehaviour = agent.Setting.EndBehaviour;
            bool isEntering = agent.Context.IsEnteringNetwork;
            string prevIntersectionID = agent.Context.PrevIntersection?.ID;
            AutoDriveAgent agentInstance = agent;
            LaneNode currentNode = agent.Context.CurrentNode;

            switch(type)
            {
                case BrakeEventType.Vehicle:
                    return (LaneNode node) => node.HasVehicle() && node.Vehicle != vehicle;
                case BrakeEventType.RoadEnd:
                    return (LaneNode node) => endBehaviour == RoadEndBehaviour.Stop && node.Type == RoadNodeType.End && !(node.First == node && isEntering);
                case BrakeEventType.TrafficLight:
                    return (LaneNode node) => node.TrafficLight != null && node.TrafficLight.CurrentState != TrafficLightState.Green && node.Intersection?.ID != prevIntersectionID;
                case BrakeEventType.Yield:
                    return (LaneNode node) => node != currentNode && ShouldYield(agentInstance, ref node);
                case BrakeEventType.YieldBlocking:
                    return (LaneNode node) => node != currentNode && ShouldYieldBlocking(agentInstance, ref node);
                default:
                    return (LaneNode _) => false;
            }
        }
    }
}