using UnityEngine;

namespace POIs
{
    public class BusStop : POI
    {
        void Awake()
        {
            Setup();
        }

        protected override void CustomSetup()
        {
            if(!_useCustomSize)
                Size = new Vector3(1, 2.75f, 1.5f);

            TextMesh text = gameObject.GetComponentInChildren<TextMesh>();
            text.text = gameObject.name;
        }
    }
}