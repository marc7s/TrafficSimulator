using System.Collections;
using UnityEngine;

namespace Statistics
{
    // This class handles the color change of a road based on the current fuel consumption of vehicles on that road.
    public class EmissionColor : MonoBehaviour
    {
        private RoadDataGatherer _roadDataGatherer;
        public float ColorChangeSensitivityFactor = 1.0f; // Sensitivity factor to scale the effect of fuel consumption on color change
        public bool EmissionEnabled = false; // Enable or disable emission shader effect on the road
        public float ColorTransitionDuration = 0.5f; // The color transition duration in seconds
        public float MaxRedValue = 1.0f; // The value representing maximum red color

        private MeshRenderer _rend;
        private static readonly int EmissionColor1 = Shader.PropertyToID("_EmissionColor");

        // Initialization of the road data gatherer and the mesh renderer components
        private void Start()
        {
            _rend = GetComponent<MeshRenderer>();
            _roadDataGatherer = GetComponent<RoadDataGatherer>();
            StartCoroutine(RoadColorUpdate());
        }

        // Coroutine that updates the road color based on fuel consumption
        private IEnumerator RoadColorUpdate()
        {
            Color currentColor = Color.green; // Initialize the road color as green

            while (true)
            {
                Material[] materials = _rend.materials; // Get all materials attached to the road

                // Calculate the colorValue based on the current fuel consumption and the sensitivity factor
                float colorValue = _roadDataGatherer.CurrentFuelConsumption * ColorChangeSensitivityFactor;
                colorValue = Mathf.Clamp(colorValue, 0, MaxRedValue); 
                Color targetColor = Color.Lerp(Color.green, Color.red, colorValue); 

                float timeElapsed = 0.0f;

                // Transition the color smoothly over the specified duration
                while (timeElapsed < ColorTransitionDuration)
                {
                    currentColor = Color.Lerp(currentColor, targetColor, timeElapsed / ColorTransitionDuration);

                    foreach (Material material in materials)
                    {
                        if (EmissionEnabled)
                        {
                            material.SetColor(EmissionColor1, currentColor);
                            material.EnableKeyword("_EMISSION");
                        }
                        else
                        {
                            material.SetColor(EmissionColor1, Color.black);
                            material.DisableKeyword("_EMISSION");
                        }
                    }

                    timeElapsed += Time.deltaTime;
                    yield return null;
                }
            }
        }
    }
}
