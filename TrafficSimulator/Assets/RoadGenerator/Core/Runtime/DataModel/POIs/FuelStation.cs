using UnityEngine;

namespace POIs
{
    public class FuelStation : POI
    {
        void Awake()
        {
            Setup();
        }

        protected override void CustomSetup()
        {
            if(!_useCustomSize)
                Size = new Vector3(12, 7, 12);
        }
    }
}