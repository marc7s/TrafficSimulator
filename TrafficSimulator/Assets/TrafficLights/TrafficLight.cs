using UnityEngine;

enum State{RED, TOGO, TOSTOP, GREEN};

public class TrafficLight : MonoBehaviour
{
    public GameObject RedLight;
    public GameObject YellowLight;
    public GameObject GreenLight;

    float lastSwitchTime;
    public float SwitchTime = 3f;

    private State _currentState = State.RED;
    private State _lastState = State.RED;
    
    void Start()
    {

    }

    void Update()
    {
        // Traffic light position
        Vector3 trafficLightPosition = transform.position;

        // Get the light sources
        GameObject RedLight = GameObject.Find("redLight");
        GameObject YellowLight = GameObject.Find("yellowLight");
        GameObject GreenLight = GameObject.Find("greenLight");

        // Timer
        if(Time.time - lastSwitchTime > SwitchTime)
        {
            switch (_currentState)
            {
                case State.TOGO:
                    setState(State.GREEN);
                    break;
                case State.TOSTOP:
                    setState(State.RED);
                    break;
            }
        }
    }

    // Red light to green light
    public void Go() {
        switch (_currentState)
        {
            case State.RED:
            case State.TOSTOP:
                setState(State.TOGO);
                break;
        }
    }

    // Green light to red light
    public void Stop() {
        switch (_currentState)
        {
            case State.GREEN:
            case State.TOGO:
                setState(State.TOSTOP);
                break;
        }
    }

    // Change the state
    private void setState(State newState)
    {
        lastSwitchTime = Time.time;
        _lastState = _currentState;
        _currentState = newState;
        UpdateLightColor(RedLight, YellowLight, GreenLight);
    }

    // Change the light color
    private void UpdateLightColor(GameObject RedLight, GameObject YellowLight, GameObject GreenLight)
    {
        switch (_currentState)
        {
            case State.RED:
                RedLight.GetComponent<Light>().enabled = true;
                YellowLight.GetComponent<Light>().enabled = false;
                GreenLight.GetComponent<Light>().enabled = false;
                break;
            case State.TOSTOP:
                RedLight.GetComponent<Light>().enabled = false;
                YellowLight.GetComponent<Light>().enabled = true;
                GreenLight.GetComponent<Light>().enabled = false;
                break;
            case State.TOGO:
                RedLight.GetComponent<Light>().enabled = true;
                YellowLight.GetComponent<Light>().enabled = true;
                GreenLight.GetComponent<Light>().enabled = false;
                break;
            case State.GREEN:
                RedLight.GetComponent<Light>().enabled = false;
                YellowLight.GetComponent<Light>().enabled = false;
                GreenLight.GetComponent<Light>().enabled = true;
                break;
        }
    }
}
