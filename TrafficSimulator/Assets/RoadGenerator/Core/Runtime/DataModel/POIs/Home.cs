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
            Size = new Vector3(6, 5, 8);
        }
    }
}