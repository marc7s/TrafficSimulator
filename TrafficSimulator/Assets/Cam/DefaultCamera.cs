using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Cam
{
    public class DefaultCamera : CameraState
    {
        [SerializeField] private bool _enableEdgeScrolling;
        [SerializeField] private float _movementSpeed = 50f;
        [SerializeField] private float _rotationSpeed = 50f;
        [SerializeField] private float _minZoom = 40f;
        [SerializeField] private float _maxZoom = 10f;
        [SerializeField] private float _zoomSpeed = 5f;
    
        // Should be set by a function of the current screen size
        private const int _edgeScrollSize = 20;
        private float _targetZoom = 50f;

        private GameObject _toggledGameObject;
        private Vector3 _moveDirection;

        private void Awake()
        {
            IsDefault = true;
            base.Awake();
        }
    
        private void Update()
        {
            if (_toggledGameObject != null) FollowTransform.position = _toggledGameObject.transform.position;
        }
    
        public override void HandlePointInput(Vector2 userPoint)
        {
            if (!_enableEdgeScrolling) return;
        
            float forward = 0;
            float sideways = 0;
            if (userPoint.y < _edgeScrollSize)
                forward = -1f;
            else if (userPoint.y > Screen.height - _edgeScrollSize) forward = 1f;

            if (userPoint.x < _edgeScrollSize)
                sideways = -1f;
            else if (userPoint.x > Screen.width - _edgeScrollSize) sideways = 1f;

            _moveDirection = TranslateDirectionToForward(forward, sideways);
        }

        public override void HandleClickInput(InputAction.CallbackContext ctx)
        {
            throw new NotImplementedException();
        }

        public override void HandleDoubleClickInput(InputAction.CallbackContext ctx)
        {
            var ray = Camera.main.ScreenPointToRay(Mouse.current.position.ReadValue());
            if (Physics.Raycast(ray, out var hitInfo) && hitInfo.transform.gameObject.Equals(_toggledGameObject))
            {
                CameraSwitcher.ToggleThirdPersonCamera();
            }
        }

        private void OnClickInput(InputAction.CallbackContext ctx)
        {
            var ray = Camera.main.ScreenPointToRay(Mouse.current.position.ReadValue());
            if (Physics.Raycast(ray, out var hitInfo) && hitInfo.transform.CompareTag("Vehicle"))
            {
                _toggledGameObject = hitInfo.collider.gameObject;
            }
            else
            {
                _toggledGameObject = null;
            }
        }
        //
        // private void OnZoomInput(InputAction.CallbackContext ctx)
        // {
        //     _playerZoom = Mathf.Clamp(ctx.ReadValue<float>(), -1f, 1f);
        // }

        private void OnMovementInput(InputAction.CallbackContext ctx)
        {
            _toggledGameObject = null;
            _moveDirection = ctx.ReadValue<Vector2>();
            _moveDirection = TranslateDirectionToForward(_moveDirection.y, _moveDirection.x);
        }
    
        public override void Move(Vector3 direction)
        {
            Vector3 translatedDirection = TranslateDirectionToForward(direction.y, direction.x);
            FollowTransform.position += translatedDirection * (_movementSpeed * Time.deltaTime);
        }

        public override void RotateHorizontal(float horizontalRotation)
        {
            throw new NotImplementedException();
        }

        public override void Zoom(float zoomValue)
        {
            throw new NotImplementedException();
        }


        /*private void HandleRotationInput()
    {
        FollowTransform.eulerAngles += new Vector3(0, _rotateDirection * _rotationSpeed * Time.deltaTime, 0);
    }

    private void HandleZoom()
    {
        _targetZoom -= _playerZoom;
        _targetZoom = Mathf.Clamp(_targetZoom, _maxZoom, _minZoom);
        _cmVirtualCamera.m_Lens.FieldOfView = Mathf.Lerp(_cmVirtualCamera.m_Lens.FieldOfView,
            _targetZoom, Time.deltaTime * _zoomSpeed);
    }*/

        private Vector3 TranslateDirectionToForward(float forwardScalar, float sidewaysScalar)
        {
            return FollowTransform.forward * forwardScalar + transform.right * sidewaysScalar;
        }

        public override void SetInactive(CameraSwitcher cameraSwitcher)
        {
            throw new System.NotImplementedException();
        }
    }
}