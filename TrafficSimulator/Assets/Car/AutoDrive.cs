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
        public GameObject LaneObject;
        
        [Header("Settings")]
        public Mode Mode = Mode.Quality;
        
        [Header("Quality mode settings")]
        public bool ShowTargetLine = false;
        public float MaxRepositioningSpeed = 5f;
        [Range(0, 20f)]
        public float TargetLookaheadDistance = 10f;
        
        [Header("Performance mode settings")]
        [Range(0, 100f)]
        public float Speed = 20f;

        // Shared variables

        // Quality variables
        private int _targetIndex = 0;
        private Vector3 _target;
        private WheelController _wheelController;
        private LineRenderer _targetLineRenderer;
        private List<Vector3> _lane = new List<Vector3>();
        private float _originalMaxSpeed;
        private int _repositioningTargetIndex = 0;
        private int _repositioningOffset = 1;
        private Status _status = Status.Driving;

        // Performance variables
        private int _positionIndex = 0;

        void Start()
        {
            // Get the lane positions
            LineRenderer line = LaneObject.GetComponent<LineRenderer>();
            Vector3[] positions = new Vector3[line.positionCount];
            line.GetPositions(positions);
            _lane = positions.ToList();

            if (Mode == Mode.Quality)
            {
                _wheelController = GetComponent<WheelController>();
            
                // Setup target line renderer
                float targetLineWidth = 0.3f;
                _targetLineRenderer = GetComponent<LineRenderer>();
                _targetLineRenderer.sharedMaterial = new Material(Shader.Find("Standard"));
                _targetLineRenderer.sharedMaterial.SetColor("_Color", Color.red);
                _targetLineRenderer.startWidth = targetLineWidth;
                _targetLineRenderer.endWidth = targetLineWidth;

                // Save the original max speed
                _originalMaxSpeed = _wheelController.GetMaxSpeed();

                // Teleport the vehicle to the start of the lane and set the acceleration to the max
                Q_TeleportToLane();
                _target = _lane[0];
                Q_SetAccelerationPercent(1f);
            }
            else if (Mode == Mode.Performance)
            {
                P_MoveToFirstPosition();
            }
        }


        void Update()
        { 
            if (Mode == Mode.Quality)
            {
                Q_SteerTowardsTarget();
                Q_UpdateTarget();
                if (ShowTargetLine)
                {
                    Q_DrawTargetLine();
                }
            }
            else if (Mode == Mode.Performance)
            {
                P_MoveToNextPosition();
            }
        }

        void Q_TeleportToLane()
        {
            // Move it to the first position of the lane, offset in the opposite direction of the lane
            transform.position = _lane[0] - 2 * (_lane[1] - _lane[0]);
        }
        void Q_SteerTowardsTarget()
        {
            Vector3 direction = Q_GetTarget() - transform.position;

            // Calculate the desired steering angle as the angle between our forward vector and the direction to the target, devided by 90 to get a value between -1 and 1 if it is in front of us.
            // If it is behind us, the value will be between (-1, -2) or (1, 2) respectively which will be clamped to +-1 by the SetTurnAnglePercent method
            float steeringAngle = Vector3.SignedAngle(transform.forward, direction.normalized, Vector3.up) / 90;

            // Steer smoothly from the current steering angle to the desired
            _wheelController.SetTurnAnglePercent(Vector3.MoveTowards(new Vector3(_wheelController.GetTurnAnglePercent(), 0, 0), new Vector3(steeringAngle, 0, 0), Time.deltaTime).x);
        }
        void Q_UpdateTarget()
        {
            // Calculate the direction, which is the vector from our current position to the target
            Vector3 direction = Q_GetTarget() - transform.position;
            
            // Calculate the dot product between our forward vector and the direction. If the target is in front of us, the dot product will be positive. If it's behind us, it will be negative
            float dot = Vector3.Dot(transform.forward, direction.normalized);
            
            // If the vehicle is driving and the target is behind us and too far away
            if (_status == Status.Driving && dot < 0 && direction.magnitude > TargetLookaheadDistance + 1f)
            {
                Debug.Log("Repositioning started, slowing down...");
                _status = Status.RepositioningInitiated;

                // Reposition to the point prior to the one we missed
                _repositioningTargetIndex = (_targetIndex - _repositioningOffset) % _lane.Count;

                // Slow down and limit the max speed to the repositioning speed or 30% of the max speed, whichever is lower
                _wheelController.SetBrakingForcePercent(1);
                _wheelController.SetMaxSpeed(Math.Min(_wheelController.GetMaxSpeed() * 0.3f, MaxRepositioningSpeed));
            }
            // If the vehicle has started repositioning and slowed down enough
            else if (_status == Status.RepositioningInitiated && _wheelController.GetCurrentSpeed() <= _wheelController.GetMaxSpeed())
            {
                Debug.Log("Slowed down, releasing brakes and repositioning...");
                _status = Status.Repositioning;

                _wheelController.SetBrakingForcePercent(0);
            }
            // If the vehicle is currently repositioning
            else if (_status == Status.Repositioning) 
            {
                // Allow the vehicle to accelerate and reverse, whatever takes it to the target faster.
                // It will accelerate if the target is in front, and reverse if it's behind
                _wheelController.SetAccelerationPercent(_wheelController.GetMaxAcceleration() * (dot > 0 ? 1 : -1));

                // If the target is in front of us and we are close enough we have successfully repositioned
                if (dot > 0 && direction.magnitude <= TargetLookaheadDistance - 1f) 
                {
                    Debug.Log("Repositioned, speeding back up...");
                    _status = Status.Driving;
                    
                    // Set the target to the one after the target we missed
                    _targetIndex = (_repositioningTargetIndex + _repositioningOffset + 1) % _lane.Count;
                    _target = _lane[_targetIndex];
                    
                    // Reset the max speed to the original, and set the acceleration to the max again
                    _wheelController.SetMaxSpeed(_originalMaxSpeed);
                    _wheelController.SetAccelerationPercent(_wheelController.GetMaxAcceleration());
                }
            }
            // If the vehicle is driving and the target is in front of us and we are close enough
            else if (_status == Status.Driving && dot > 0 && direction.magnitude <= TargetLookaheadDistance)
            {
                // Set the target to the next point in the lane
                _targetIndex = (_targetIndex + 1) % _lane.Count;
                _target = _lane[_targetIndex];
            }
        }

        Vector3 Q_GetTarget()
        {
            return (_status == Status.Driving ? _target : _lane[_repositioningTargetIndex]);
        }

        void Q_SetAccelerationPercent(float accelerationPercent) 
        {
            WheelController wheelController = GetComponent<WheelController>();
            wheelController.SetAccelerationPercent(accelerationPercent);
        }
        
        void Q_DrawTargetLine()
        {
            _targetLineRenderer.SetPositions(new Vector3[] { transform.position, Q_GetTarget() });
        }

        // Performance methods
        void P_MoveToFirstPosition()
        {
            // Move to the first position of the lane
            transform.position = _lane[0];
        }

        void P_MoveToNextPosition()
        {
            Vector3 target = P_GetLerpPosition(_lane[_positionIndex]);
            
            if(transform.position == target) 
            {
                _positionIndex++;
                if(_positionIndex == _lane.Count) 
                {
                    _positionIndex = 0;
                    P_MoveToFirstPosition();
                    return;
                }
            }
            transform.position = target;
        }

        private Vector3 P_GetLerpPosition(Vector3 target)
        {
            return Vector3.MoveTowards(transform.position, target, Speed * Time.deltaTime);
        }
    }
}

