using UnityEngine;
using RoadGenerator;
using System;
using DataModel;

namespace Car
{
    public enum BrakeEventType
    { 
        Vehicle = 0, 
        RoadEnd = 1, 
        TrafficLight = 2
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
            switch(setting.Mode)
            {
                case DrivingMode.Quality:
                    // Calculate the distance it will take to stop
                    return setting.BrakeOffset + (speed / 2) + speed * speed / (setting.VehicleController.tireFriction * 9.81f);

                case DrivingMode.Performance:
                    // Set the distance to the brake offset + the speed divided by 5
                    return setting.BrakeOffset + speed / 5f;

                default:
                    return 0;
            }
        }
        public override Func<LaneNode, bool> EventAssesser(ref AutoDriveAgent agent, BrakeEventType type)
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
                    return (LaneNode node) => false;
            }
        }
    }
}