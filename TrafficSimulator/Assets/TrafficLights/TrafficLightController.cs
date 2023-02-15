using UnityEngine;

enum Mode{FIRST, SECOND};

public class TrafficLightController : MonoBehaviour
{
    [SerializeField] private TrafficLight[] _trafficLightsPair1; // Starts green
    [SerializeField] private TrafficLight[] _trafficLightsPair2; // Starts red

    private int _trafficLightsCount;

    public float Delay = 10f;
    float lastSwitchTime = 0f;

    private Mode _currentMode = Mode.FIRST;

    void Start()
    {
        _trafficLightsCount = _trafficLightsPair1.Length + _trafficLightsPair2.Length;
    }

    void Update()
    {
        if (Time.time - lastSwitchTime > Delay)
        {
            Controller(_trafficLightsPair1, _trafficLightsPair2);
            lastSwitchTime = Time.time;
        }
    }

    private void Controller(TrafficLight[] _trafficLightsPair1, TrafficLight[] _trafficLightsPair2)
    {
        switch (_trafficLightsCount)
        {
            case 2:
                TwowayController(_trafficLightsPair1, _trafficLightsPair2);
                break;
            case 3:
                ThreewayController(_trafficLightsPair1, _trafficLightsPair2);
                break;
            case 4:
                FourwayController(_trafficLightsPair1, _trafficLightsPair2);
                break;
        }
    }

    private void TwowayController(TrafficLight[] _trafficLightsPair1, TrafficLight[] _trafficLightsPair2)
    {
        switch (_currentMode)
        {
            case Mode.FIRST:
                for (int i = 0; i < _trafficLightsPair1.Length; i++)
                    {
                        _trafficLightsPair1[i].Go();
                    }
                _currentMode = Mode.SECOND;
                break;
            case Mode.SECOND:
                for (int i = 0; i < _trafficLightsPair1.Length; i++)
                    {
                        _trafficLightsPair1[i].Stop();
                    }
                _currentMode = Mode.FIRST;
                break;
        }   
    }

    private void ThreewayController(TrafficLight[] _trafficLightsPair1, TrafficLight[] _trafficLightsPair2)
    {
        switch (_currentMode)
        {
            case Mode.FIRST:
                _trafficLightsPair2[0].Stop();
                for (int i = 0; i < _trafficLightsPair1.Length; i++)
                    {
                        _trafficLightsPair1[i].Go();
                    }
                _currentMode = Mode.SECOND;
                break;
            case Mode.SECOND:
                for (int i = 0; i < _trafficLightsPair1.Length; i++)
                    {
                        _trafficLightsPair1[i].Stop();
                    }
                _trafficLightsPair2[0].Go();
                _currentMode = Mode.FIRST;
                break;
        }
    }

    private void FourwayController(TrafficLight[] _trafficLightsPair1, TrafficLight[] _trafficLightsPair2)
    {
        switch (_currentMode)
        {
            case Mode.FIRST:
                for (int i = 0; i < _trafficLightsPair2.Length; i++)
                    {
                        _trafficLightsPair2[i].Stop();
                    }
                for (int i = 0; i < _trafficLightsPair1.Length; i++)
                    {
                        _trafficLightsPair1[i].Go();
                    }
                _currentMode = Mode.SECOND;
                break;
            case Mode.SECOND:
                for (int i = 0; i < _trafficLightsPair1.Length; i++)
                    {
                        _trafficLightsPair1[i].Stop();
                    }
                for (int i = 0; i < _trafficLightsPair2.Length; i++)
                    {
                        _trafficLightsPair2[i].Go();
                    }
                _currentMode = Mode.FIRST;
                break;
        }
    }


}