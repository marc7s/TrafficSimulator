using UnityEngine;

enum State{RED, TOGO, TOSTOP, GREEN};

public class TrafficLight : MonoBehaviour
{
    private GameObject cube;
    public GameObject redLight;
    public GameObject yellowLight;
    public GameObject greenLight;

    float timer;
    float lastSwitchTime;
    public float switchTime = 3f;

    private State currentState = State.RED;
    private State lastState = State.RED;

    [Range(-10, 10)] public int offset;
    
    void Start()
    {
        // Get the traffic light position
        Vector3 trafficLightPosition = transform.position;

        // Create a cube at the traffic light position
        cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cube.transform.localScale = new Vector3(0.25f, 1.25f, 4.5f);

        // Disable the cube's physics to only act as a trigger
        cube.GetComponent<BoxCollider>().isTrigger = true;
        cube.GetComponent<BoxCollider>().size = new Vector3(0.25f, 1.25f, 4.5f);
        
    }


    void Update()
    {
        // Move the cube to the traffic light position
        Vector3 trafficLightPosition = transform.position;
        cube.transform.position = trafficLightPosition + new Vector3(offset, 0.69f, 3);

        // Get the light sources
        GameObject redLight = GameObject.Find("RedLight");
        GameObject yellowLight = GameObject.Find("YellowLight");
        GameObject greenLight = GameObject.Find("GreenLight");

        timer += Time.deltaTime;

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

    public void Go() {
        switch (currentState)
        {
            case State.RED:
            case State.TOSTOP:
                setState(State.TOGO);
                break;
        }
    }

    public void Stop() {
        switch (currentState)
        {
            case State.GREEN:
            case State.TOGO:
                setState(State.TOSTOP);
                break;
        }
    }

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
