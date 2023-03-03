using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class cameraScript : MonoBehaviour
{
    private float x;
    private float y;
    public float sensitivity = -1f;
    private Vector3 rotate;

    public float speed = 2;

    // Start is called before the first frame update
    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
    }

    // Update is called once per frame
    void Update()
    {
        y = Input.GetAxis("Mouse X");
        x = Input.GetAxis("Mouse Y");
        rotate = new Vector3(x, y * sensitivity, 0);
        transform.eulerAngles = transform.eulerAngles - rotate;


    float x2 = Input.GetAxis("Horizontal");
    float z = Input.GetAxis("Vertical");
    Vector3 movement = new Vector3(x2, 0, z);
    transform.Translate(movement * speed * Time.deltaTime);
    }
}