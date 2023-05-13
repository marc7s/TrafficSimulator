using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Benchmark
{
    public enum BenchmarkMode 
    {
        Running,
        Stopped
    }

    public class Benchmarker : MonoBehaviour
    {
        [SerializeField] private GameObject _camera;

        List<Vector3> _cameraPositions = new List<Vector3>();
        List<float> _fps = new List<float>();

        const float fpsMeasurePeriod = 10f;
        private int _fpsAccumulator = 0;
        private float _fpsNextPeriod = 0;
        private int _currentFps = 0;

        int cameraIndex = 0;

        BenchmarkMode _mode = BenchmarkMode.Running;

        void Start()
        {
            Vector3 cameraPosition1 = new Vector3(1939, 96, 664);
            Vector3 cameraPosition2 = new Vector3(1024, 96, 664);
            Vector3 cameraPosition3 = new Vector3(1170, 96, 1337);

            _cameraPositions = new List<Vector3>(){cameraPosition1, cameraPosition2, cameraPosition3};
            MoveCamera(_camera, _cameraPositions[cameraIndex]);

            _fpsNextPeriod = Time.realtimeSinceStartup + fpsMeasurePeriod;
        }

        void Update()
        {
            if(_mode == BenchmarkMode.Stopped)
                Destroy(GetComponent<Camera>());

            _fpsAccumulator++;

            if (Time.time > _fpsNextPeriod)
            {
                _currentFps = (int) (_fpsAccumulator / fpsMeasurePeriod);
                _fpsAccumulator = 0;
                _fpsNextPeriod += fpsMeasurePeriod;
                _fps.Add(_currentFps);

                cameraIndex++;

                // Check if we're done
                if(cameraIndex >= _cameraPositions.Count)
                {
                    float avg = GetAverageFPS();
                    Debug.LogError("Average FPS: " + avg);
                    _mode = BenchmarkMode.Stopped;
                    
                    Debug.developerConsoleVisible = true;

                    return;
                }

                MoveCamera(_camera, _cameraPositions[cameraIndex]);
            }
        }

        private void MoveCamera(GameObject camera, Vector3 position)
        {
            camera.transform.position = position;
        }

        private float GetAverageFPS()
        {
            float sum = 0f;

            foreach (float fps in _fps)
            {
                Debug.Log(fps);
                sum += fps;
            }

            return sum / _fps.Count;
        }
    }
}