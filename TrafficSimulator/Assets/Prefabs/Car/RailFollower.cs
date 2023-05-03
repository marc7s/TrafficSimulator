using UnityEngine;

namespace VehicleBrain
{
    public class RailFollower : MonoBehaviour
    {
        [Range(0, 100f)] [SerializeField] private float _speed = 1f;
        [SerializeField] private LineRenderer _lineRenderer;
        private int _currentPoint;
        private float _fracTravel;
        private float _startTime;
        private Vector3 _targetDirection;
        private Quaternion _targetRotation;
        private float _travelLength;

        private void Start()
        {
            _startTime = Time.time;
        }

        private void Update()
        {
            CalculateTravelLength();

            LerpTowardsNextPoint();

            SetRotation();
        }

        private void CalculateTravelLength()
        {
            _travelLength = Vector3.Distance(_lineRenderer.GetPosition(_currentPoint),
                _lineRenderer.GetPosition(_currentPoint + 1));
            _fracTravel = (Time.time - _startTime) * _speed / _travelLength;
        }

        private void LerpTowardsNextPoint()
        {
            transform.position = Vector3.Lerp(_lineRenderer.GetPosition(_currentPoint),
                _lineRenderer.GetPosition(_currentPoint + 1), _fracTravel);

            if (_fracTravel >= 1)
            {
                _currentPoint++;
                if (_currentPoint + 1 >= _lineRenderer.positionCount) _currentPoint = 0;
                _startTime = Time.time;
            }
        }

        private void SetRotation()
        {
            _targetDirection = _lineRenderer.GetPosition(_currentPoint + 1) - transform.position;
            _targetRotation = Quaternion.LookRotation(_targetDirection);
            transform.rotation = Quaternion.Lerp(transform.rotation, _targetRotation, Time.deltaTime * _speed);
        }
    }
}