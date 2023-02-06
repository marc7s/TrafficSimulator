using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System;

namespace Car {
    enum Status {
        Driving,
        RepositioningInitiated,
        Repositioning
    }
    public enum Mode {
        Performance,
        Quality
    }
    public class AutoDrive : MonoBehaviour
    {
        [Header("Connections")]
        public GameObject laneObject;
        
        [Header("Settings")]
        public Mode mode = Mode.Quality;
        
        [Header("Quality mode settings")]
        public bool showTargetLine = false;
        public float maxRepositioningSpeed = 5f;
        [Range(0, 20f)]
        public float targetLookaheadDistance = 10f;
        
        [Header("Performance mode settings")]
        [Range(0, 100f)]
        public float speed = 20f;

        // Shared variables

        // Quality variables
        private int targetIndex = 0;
        private Vector3 target;
        private WheelController wheelController;
        private LineRenderer targetLineRenderer;
        private List<Vector3> lane = new List<Vector3>();
        private float originalMaxSpeed;
        private int repositioningTargetIndex = 0;
        private int repositioningOffset = 1;
        private Status status = Status.Driving;

        // Performance variables
        private int positionIndex = 0;

        void Start()
        {
            // Get the lane positions
            LineRenderer line = laneObject.GetComponent<LineRenderer>();
            Vector3[] positions = new Vector3[line.positionCount];
            line.GetPositions(positions);
            lane = positions.ToList();

            if (mode == Mode.Quality)
            {
                wheelController = GetComponent<WheelController>();
            
                // Setup target line renderer
                float targetLineWidth = 0.3f;
                targetLineRenderer = GetComponent<LineRenderer>();
                targetLineRenderer.sharedMaterial = new Material(Shader.Find("Standard"));
                targetLineRenderer.sharedMaterial.SetColor("_Color", Color.red);
                targetLineRenderer.startWidth = targetLineWidth;
                targetLineRenderer.endWidth = targetLineWidth;

                // Save the original max speed
                originalMaxSpeed = wheelController.GetMaxSpeed();

                // Teleport the vehicle to the start of the lane and set the acceleration to the max
                Q_TeleportToLane();
                target = lane[0];
                Q_SetAccelerationPercent(1f);
            }
            else if (mode == Mode.Performance)
            {
                P_MoveToFirstPosition();
            }
        }


        void Update()
        { 
            if (mode == Mode.Quality)
            {
                Q_SteerTowardsTarget();
                Q_UpdateTarget();
                if (showTargetLine)
                {
                    Q_DrawTargetLine();
                }
            }
            else if (mode == Mode.Performance)
            {
                P_MoveToNextPosition();
            }
        }

        void Q_TeleportToLane()
        {
            // Move it to the first position of the lane, offset in the opposite direction of the lane
            transform.position = lane[0] - 2 * (lane[1] - lane[0]);
        }
        void Q_SteerTowardsTarget()
        {
            Vector3 direction = Q_GetTarget() - transform.position;

            // Calculate the desired steering angle as the angle between our forward vector and the direction to the target, devided by 90 to get a value between -1 and 1 if it is in front of us.
            // If it is behind us, the value will be between (-1, -2) or (1, 2) respectively which will be clamped to +-1 by the SetTurnAnglePercent method
            float steeringAngle = Vector3.SignedAngle(transform.forward, direction.normalized, Vector3.up) / 90;

            // Steer smoothly from the current steering angle to the desired
            wheelController.SetTurnAnglePercent(Vector3.MoveTowards(new Vector3(wheelController.GetTurnAnglePercent(), 0, 0), new Vector3(steeringAngle, 0, 0), Time.deltaTime).x);
        }
        void Q_UpdateTarget()
        {
            // Calculate the direction, which is the vector from our current position to the target
            Vector3 direction = Q_GetTarget() - transform.position;
            
            // Calculate the dot product between our forward vector and the direction. If the target is in front of us, the dot product will be positive. If it's behind us, it will be negative
            float dot = Vector3.Dot(transform.forward, direction.normalized);
            
            // If the vehicle is driving and the target is behind us and too far away
            if (status == Status.Driving && dot < 0 && direction.magnitude > targetLookaheadDistance + 1f)
            {
                Debug.Log("Repositioning started, slowing down...");
                status = Status.RepositioningInitiated;

                // Reposition to the point prior to the one we missed
                repositioningTargetIndex = (targetIndex - repositioningOffset) % lane.Count;

                // Slow down and limit the max speed to the repositioning speed or 30% of the max speed, whichever is lower
                wheelController.SetBrakingForcePercent(1);
                wheelController.SetMaxSpeed(Math.Min(wheelController.GetMaxSpeed() * 0.3f, maxRepositioningSpeed));
            }
            // If the vehicle has started repositioning and slowed down enough
            else if (status == Status.RepositioningInitiated && wheelController.GetCurrentSpeed() <= wheelController.GetMaxSpeed())
            {
                Debug.Log("Slowed down, releasing brakes and repositioning...");
                status = Status.Repositioning;

                wheelController.SetBrakingForcePercent(0);
            }
            // If the vehicle is currently repositioning
            else if (status == Status.Repositioning) 
            {
                // Allow the vehicle to accelerate and reverse, whatever takes it to the target faster.
                // It will accelerate if the target is in front, and reverse if it's behind
                wheelController.SetAccelerationPercent(wheelController.GetMaxAcceleration() * (dot > 0 ? 1 : -1));

                // If the target is in front of us and we are close enough we have successfully repositioned
                if (dot > 0 && direction.magnitude <= targetLookaheadDistance - 1f) 
                {
                    Debug.Log("Repositioned, speeding back up...");
                    status = Status.Driving;
                    
                    // Set the target to the one after the target we missed
                    targetIndex = (repositioningTargetIndex + repositioningOffset + 1) % lane.Count;
                    target = lane[targetIndex];
                    
                    // Reset the max speed to the original, and set the acceleration to the max again
                    wheelController.SetMaxSpeed(originalMaxSpeed);
                    wheelController.SetAccelerationPercent(wheelController.GetMaxAcceleration());
                }
            }
            // If the vehicle is driving and the target is in front of us and we are close enough
            else if (status == Status.Driving && dot > 0 && direction.magnitude <= targetLookaheadDistance)
            {
                // Set the target to the next point in the lane
                targetIndex = (targetIndex + 1) % lane.Count;
                target = lane[targetIndex];
            }
        }

        Vector3 Q_GetTarget()
        {
            return (status == Status.Driving ? target : lane[repositioningTargetIndex]);
        }

        void Q_SetAccelerationPercent(float accelerationPercent) 
        {
            WheelController wheelController = GetComponent<WheelController>();
            wheelController.SetAccelerationPercent(accelerationPercent);
        }
        
        void Q_DrawTargetLine()
        {
            targetLineRenderer.SetPositions(new Vector3[] { transform.position, Q_GetTarget() });
        }

        // Performance methods
        void P_MoveToFirstPosition()
        {
            // Move to the first position of the lane
            transform.position = lane[0];
        }

        void P_MoveToNextPosition()
        {
            Vector3 target = P_GetLerpPosition(lane[positionIndex]);
            
            if(transform.position == target) 
            {
                positionIndex++;
                if(positionIndex == lane.Count) 
                {
                    positionIndex = 0;
                    P_MoveToFirstPosition();
                    return;
                }
            }
            transform.position = target;
        }

        private Vector3 P_GetLerpPosition(Vector3 target)
        {
            return Vector3.MoveTowards(transform.position, target, speed * Time.deltaTime);
        }
    }
}

