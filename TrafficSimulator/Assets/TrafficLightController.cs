using UnityEngine;

enum Mode{FIRST, SECOND};

public class TrafficLightController : MonoBehaviour
{
    [SerializeField] private TrafficLight[] trafficLightsPair1; // Starts green
    [SerializeField] private TrafficLight[] trafficLightsPair2; // Starts red

    private int trafficLightsCount;

    public float delay = 10f;
    float lastSwitchTime = 0f;

    private Mode currentMode = Mode.FIRST;


    void Start()
    {
        trafficLightsCount = trafficLightsPair1.Length + trafficLightsPair2.Length;
    }

    void Update()
    {
        if (Time.time - lastSwitchTime > delay)
        {
            Debug.Log("Switching traffic lights : " + Time.time);
            Controller(trafficLightsPair1, trafficLightsPair2);
            lastSwitchTime = Time.time;
        }
    }

    private void Controller(TrafficLight[] trafficLightsPair1, TrafficLight[] trafficLightsPair2)
    {
        switch (trafficLightsCount)
        {
            case 2:
                TwowayController(trafficLightsPair1, trafficLightsPair2);
                break;
            case 3:
                ThreewayController(trafficLightsPair1, trafficLightsPair2);
                break;
            case 4:
                FourwayController(trafficLightsPair1, trafficLightsPair2);
                break;
        }
    }

    private void TwowayController(TrafficLight[] trafficLightsPair1, TrafficLight[] trafficLightsPair2)
    {
        switch (currentMode)
        {
            case Mode.FIRST:
                for (int i = 0; i < trafficLightsPair1.Length; i++)
                    {
                        trafficLightsPair1[i].Go();
                        //Debug.Log("Traffic light " + i + " is turning green");
                    }
                currentMode = Mode.SECOND;
                break;
            case Mode.SECOND:
                for (int i = 0; i < trafficLightsPair1.Length; i++)
                    {
                        trafficLightsPair1[i].Stop();
                        //Debug.Log("Traffic light " + i + " is turning red");
                    }
                currentMode = Mode.FIRST;
                break;
        }   
    }

    private void ThreewayController(TrafficLight[] trafficLightsPair1, TrafficLight[] trafficLightsPair2)
    {
        switch (currentMode)
        {
            case Mode.FIRST:
                for (int i = 0; i < trafficLightsPair1.Length; i++)
                    {
                        trafficLightsPair1[i].Go();
                        //Debug.Log("Traffic light " + i + " is turning green");
                    }
                trafficLightsPair2[0].Stop();
                currentMode = Mode.SECOND;
                break;
            case Mode.SECOND:
                for (int i = 0; i < trafficLightsPair1.Length; i++)
                    {
                        trafficLightsPair1[i].Stop();
                        //Debug.Log("Traffic light " + i + " is turning red");
                    }
                trafficLightsPair2[0].Go();
                currentMode = Mode.FIRST;
                break;
        }
    }

    private void FourwayController(TrafficLight[] trafficLightsPair1, TrafficLight[] trafficLightsPair2)
    {
        switch (currentMode)
        {
            case Mode.FIRST:
                for (int i = 0; i < trafficLightsPair1.Length; i++)
                    {
                        trafficLightsPair1[i].Go();
                        //Debug.Log("Traffic light " + i + " is turning green");
                    }
                for (int i = 0; i < trafficLightsPair2.Length; i++)
                    {
                        trafficLightsPair2[i].Stop();
                        //Debug.Log("Traffic light " + i + " is turning red");
                    }
                currentMode = Mode.SECOND;
                break;
            case Mode.SECOND:
                for (int i = 0; i < trafficLightsPair1.Length; i++)
                    {
                        trafficLightsPair1[i].Stop();
                        //Debug.Log("Traffic light " + i + " is turning red");
                    }
                for (int i = 0; i < trafficLightsPair2.Length; i++)
                    {
                        trafficLightsPair2[i].Go();
                        //Debug.Log("Traffic light " + i + " is turning green");
                    }
                currentMode = Mode.FIRST;
                break;
        }
    }


}