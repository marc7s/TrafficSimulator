using System.Collections.Generic;
using UnityEngine;
using POIs;

namespace DataModel
{
    public class Bus : Vehicle
    {
        public List<BusStop> BusRoute;
        // Start is called before the first frame update
        void Start()
        {
            base.Init();
        }

        // Update is called once per frame
        void Update()
        {
            
        }
    }
}

