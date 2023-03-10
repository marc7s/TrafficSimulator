using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using RoadGenerator;

namespace Car
{
public class ActiveCarDisplayer : MonoBehaviour
{
    [SerializeField] private Camera _camera;
    [SerializeField] private GameObject _activeCar;
    // Start is called before the first frame update
    void Start()
    {
        AutoDrive autoDrive = _activeCar.GetComponent<AutoDrive>();
        autoDrive.ShowNavigationPath = true;
    }

    // Update is called once per frame
    void Update()
    {

        return;
         if(Input.GetKeyDown(KeyCode.Mouse0)){
            Debug.Log("Mouse0");
        RaycastHit hit;
        Ray ray = _camera.ScreenPointToRay(Input.mousePosition);
        
        if (Physics.Raycast(ray, out hit)) {
            Transform objectHit = hit.transform;
            if (objectHit.gameObject.name == "Car") {
                Debug.Log("Car");
            }
            // Do something with the object that was hit by the raycast.
        }
        }
    }
}
}
