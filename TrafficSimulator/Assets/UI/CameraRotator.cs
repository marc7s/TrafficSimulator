using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraRotator : MonoBehaviour
{
    public float _speed = 10f;

    // Update is called once per frame
    void Update()
    {
        transform.Rotate(0, _speed * Time.deltaTime, 0);
    }
}
