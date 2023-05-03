using UnityEngine;

namespace POIs
{
    public class Home : POI
    {
        void Awake()
        {
            Setup();
        }

        protected override void CustomSetup()
        {
            if(!_useCustomSize)
                Size = new Vector3(6, 5, 8);
        }
    }
}