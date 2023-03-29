using UnityEngine;
using RoadGenerator;
using System;
using DataModel;

namespace Car
{
    public enum BrakeEventType
    { 
        Vehicle, 
        RoadEnd, 
        TrafficLight
    }

    public class BrakeController : AutoDriveController<BrakeEventType>
    {
        public override bool ShouldAct(ref AutoDriveAgent agent)
        {
            LaneNode curr = agent.Context.CurrentNode;
            float distance = 0;
            float brakeDistance = GetBrakeDistance(agent.Setting) + Vector3.Distance(agent.Context.CurrentNode.Position, agent.Context.VehiclePosition);

            while(curr != null && distance < brakeDistance)
            {
                agent.Context.BrakeTarget = curr;
                    
                if(ShouldActAtNode(ref agent, curr))
                    return true;

                curr = agent.Next(curr);
                distance += curr.DistanceToPrevNode;
            }
            
            return false;
        }

        private static float GetBrakeDistance(AutoDriveSetting setting)
        {
            float speed = setting.Mode == DrivingMode.Quality ? setting.VehicleController.speed : setting.Speed;
            const float g = 9.82f;
            switch(setting.Mode)
            {
                case DrivingMode.Quality:
                    // Calculate the distance it will take to stop
                    return setting.BrakeOffset + speed / 2 + speed * speed / (setting.VehicleController.tireFriction * g);

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
                    return setting.BrakeOffset + speed * speed / (setting.Acceleration * 2f);

                default:
                    return 0;
            }
        }
        public override Func<LaneNode, bool> EventAssessor(ref AutoDriveAgent agent, BrakeEventType type)
        {
            Vehicle vehicle = agent.Setting.Vehicle;
            RoadEndBehaviour endBehaviour = agent.Setting.EndBehaviour;
            bool isEntering = agent.Context.IsEnteringNetwork;
            string prevIntersectionID = agent.Context.PrevIntersection?.ID;
            
            switch(type)
            {
                case BrakeEventType.Vehicle:
                    return (LaneNode node) => node.HasVehicle() && node.Vehicle != vehicle;
                case BrakeEventType.RoadEnd:
                    return (LaneNode node) => endBehaviour == RoadEndBehaviour.Stop && node.Type == RoadNodeType.End && !(node.First == node && isEntering);
                case BrakeEventType.TrafficLight:
                    return (LaneNode node) => node.TrafficLight != null && node.TrafficLight.CurrentState != TrafficLightState.Green && node.Intersection?.ID != prevIntersectionID;
                default:
                    return (LaneNode _) => false;
            }
        }
    }
}