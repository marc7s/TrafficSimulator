using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DataModel;

namespace Bus
{
    public class Bus : Vehicle
    {
        // Start is called before the first frame update
        void Start()
        {
            base.Init();
            Debug.Log(Id + " Im a bus");
        }

        // Update is called once per frame
        void Update()
        {
            
        }
    }
}

