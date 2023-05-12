using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum BenchmarkMode 
{
    Running,
    Stopped
}

public class Benchmarker : MonoBehaviour
{
    [SerializeField] private GameObject camera;

    List<Vector3> _cameraPositions = new List<Vector3>();
    List<float> _fps = new List<float>();

    float _currentAvg = 0f;
    float _timer = 0f;
    int cameraIndex = 0;

    BenchmarkMode _mode = BenchmarkMode.Running;

    // Start is called before the first frame update
    void Start()
    {
        Vector3 cameraPosition1 = new Vector3(1939, 96, 664);
        Vector3 cameraPosition2 = new Vector3(1024, 96, 664);
        Vector3 cameraPosition3 = new Vector3(1170, 96, 1337);

        _cameraPositions = new List<Vector3>(){cameraPosition1, cameraPosition2, cameraPosition3};
    }

    // Update is called once per frame
    void Update()
    {
        if(_mode == BenchmarkMode.Stopped)
            Destroy(camera);

        _currentAvg += ((Time.deltaTime / Time.timeScale) - _currentAvg) * 0.03f; 
        
        _timer += Time.deltaTime;

        if (_timer >= 5f)
        {
            _timer = 0f;
            Benchmark(cameraIndex, _currentAvg);
            cameraIndex++;
        }
    }

    private void Benchmark(int cameraIndex, float fps)
    {
        if(cameraIndex >= _cameraPositions.Count)
        {
            float avg = GetAverageFPS();
            Debug.LogError("Average FPS: " + avg);
            _mode = BenchmarkMode.Stopped;
            
            // Make development console visible
            Debug.developerConsoleVisible = true;

            return;
        }

        MoveCamera(camera, _cameraPositions[cameraIndex]);
       
        _fps.Add((1f / _currentAvg));

        _currentAvg = 0f;
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
            sum += fps;
        }

        return sum / _fps.Count;
    }
}
