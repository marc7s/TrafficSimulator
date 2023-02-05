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
    public class AutoDrive : MonoBehaviour
    {
        public GameObject laneObject;
        public bool showTargetLine = false;
        public float maxRepositioningSpeed = 5f;
        private int targetIndex = 0;
        private Vector3 target;
        private WheelController wheelController;
        private LineRenderer targetLineRenderer;
        private List<Vector3> lane = new List<Vector3>();
        private float originalMaxSpeed;
        private int repositioningTargetIndex = 0;
        private Status status = Status.Driving;

        void Start()
        {
            wheelController = GetComponent<WheelController>();
            
            // Setup target line renderer
            float targetLineWidth = 0.3f;
            targetLineRenderer = GetComponent<LineRenderer>();
            targetLineRenderer.sharedMaterial = new Material(Shader.Find("Standard"));
            targetLineRenderer.sharedMaterial.SetColor("_Color", Color.red);
            targetLineRenderer.startWidth = targetLineWidth;
            targetLineRenderer.endWidth = targetLineWidth;

            // Get the lane positions
            LineRenderer line = laneObject.GetComponent<LineRenderer>();
            Vector3[] positions = new Vector3[line.positionCount];
            line.GetPositions(positions);
            lane = positions.ToList();

            // Save the original max speed
            originalMaxSpeed = wheelController.GetMaxSpeed();

            // Teleport the vehicle to the start of the lane and set the acceleration to the max
            TeleportToLane();
            target = lane[0];
            SetAccelerationPercent(1f);
        }


        void Update()
        { 
            SteerTowardsTarget();
            UpdateTarget();
            if (showTargetLine)
            {
                DrawTargetLine();
            }
        }

        void TeleportToLane()
        {
            // Move it to the first position of the lane, offset in the opposite direction of the lane
            transform.position = lane[0] - 2 * (lane[1] - lane[0]);
        }
        void SteerTowardsTarget()
        {
            Vector3 direction = GetTarget() - transform.position;
            float steeringAngle = Vector3.SignedAngle(transform.forward, direction.normalized, Vector3.up) / 90;

            // Steer smoothly from the current steering angle to the desired
            wheelController.SetTurnAnglePercent(Vector3.MoveTowards(new Vector3(wheelController.GetTurnAnglePercent(), 0, 0), new Vector3(steeringAngle, 0, 0), Time.deltaTime).x);
        }
        void UpdateTarget()
        {
            Vector3 direction = GetTarget() - transform.position;
            float dot = Vector3.Dot(transform.forward, direction.normalized);
            
            // The distance from the vehicle to the next point
            float targetVicinityThreshold = 10f;
            // If the vehicle is driving and we are in front of the target, but too far away
            if (status == Status.Driving && dot < 0 && direction.magnitude > targetVicinityThreshold + 1f)
            {
                Debug.Log("Repositioning started, slowing down...");
                status = Status.RepositioningInitiated;

                // Reposition to the point prior to the one we missed
                repositioningTargetIndex = (targetIndex - 1) % lane.Count;

                // Slow down and limit the max speed to the repositioning speed
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
                // Allow the vehicle to accelerate and reverse, whatever takes it to the target faster
                wheelController.SetAccelerationPercent(wheelController.GetMaxAcceleration() * (dot > 0 ? 1 : -1));

                // If the target is in front of us and we are close enough we have successfully repositioned
                if (Math.Abs(Vector3.SignedAngle(transform.forward, direction.normalized, Vector3.up)) < 90 && direction.magnitude <= targetVicinityThreshold - 1f) {
                    Debug.Log("Repositioned, speeding back up...");
                    status = Status.Driving;
                    
                    // Set the target to the one after the target we missed
                    targetIndex = (repositioningTargetIndex + 2) % lane.Count;
                    target = lane[targetIndex];
                    
                    // Reset the max speed to the original, and set the acceleration to the max again
                    wheelController.SetMaxSpeed(originalMaxSpeed);
                    wheelController.SetAccelerationPercent(wheelController.GetMaxAcceleration());
                }
            }
            // If the vehicle is driving and the target is in front of us and we are close enough
            else if (status == Status.Driving && Math.Abs(Vector3.SignedAngle(transform.forward, direction.normalized, Vector3.up)) < 90 && direction.magnitude <= targetVicinityThreshold)
            {
                // Set the target to the next point in the lane
                targetIndex = (targetIndex + 1) % lane.Count;
                target = lane[targetIndex];
            }
        }

        Vector3 GetTarget()
        {
            return (status == Status.Driving ? target : lane[repositioningTargetIndex]);
        }

        void SetAccelerationPercent(float accelerationPercent) 
        {
            WheelController wheelController = GetComponent<WheelController>();
            wheelController.SetAccelerationPercent(accelerationPercent);
        }
        
        void DrawTargetLine()
        {
            targetLineRenderer.SetPositions(new Vector3[] { transform.position, GetTarget() });
        }
    }
}

