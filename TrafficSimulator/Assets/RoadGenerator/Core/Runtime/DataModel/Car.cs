using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DataModel;

namespace Car 
{
    public class Car : Vehicle
    {
        // Start is called before the first frame update
        void Start()
        {
            base.Init();
            Debug.Log(Id + " Im a car");
        }

        // Update is called once per frame
        void Update()
        {
            
        }
    }
}

