using UnityEngine;

enum State{RED, TOGO, TOSTOP, GREEN};

public class TrafficLight : MonoBehaviour
{
    public GameObject redLight;
    public GameObject yellowLight;
    public GameObject greenLight;

    float lastSwitchTime;
    public float switchTime = 3f;

    private State currentState = State.RED;
    private State lastState = State.RED;
    
    void Start()
    {

    }

    void Update()
    {
        // Traffic light position
        Vector3 trafficLightPosition = transform.position;

        // Get the light sources
        GameObject redLight = GameObject.Find("RedLight");
        GameObject yellowLight = GameObject.Find("YellowLight");
        GameObject greenLight = GameObject.Find("GreenLight");

        // Timer
        if(Time.time - lastSwitchTime > switchTime)
        {
            switch (currentState)
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
        switch (currentState)
        {
            case State.RED:
            case State.TOSTOP:
                setState(State.TOGO);
                break;
        }
    }

    // Green light to red light
    public void Stop() {
        switch (currentState)
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
        lastState = currentState;
        currentState = newState;
        UpdateLightColor(redLight, yellowLight, greenLight);
    }

    // Change the light color
    private void UpdateLightColor(GameObject redLight, GameObject yellowLight, GameObject greenLight)
    {
        switch (currentState)
        {
            case State.RED:
                redLight.GetComponent<Light>().enabled = true;
                yellowLight.GetComponent<Light>().enabled = false;
                greenLight.GetComponent<Light>().enabled = false;
                break;
            case State.TOSTOP:
                redLight.GetComponent<Light>().enabled = false;
                yellowLight.GetComponent<Light>().enabled = true;
                greenLight.GetComponent<Light>().enabled = false;
                break;
            case State.TOGO:
                redLight.GetComponent<Light>().enabled = true;
                yellowLight.GetComponent<Light>().enabled = true;
                greenLight.GetComponent<Light>().enabled = false;
                break;
            case State.GREEN:
                redLight.GetComponent<Light>().enabled = false;
                yellowLight.GetComponent<Light>().enabled = false;
                greenLight.GetComponent<Light>().enabled = true;
                break;
        }
    }
}
