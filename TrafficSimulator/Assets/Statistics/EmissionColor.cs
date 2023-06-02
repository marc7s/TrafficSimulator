using System.Collections;
using UnityEngine;

namespace Statistics
{
    public class EmissionColor : MonoBehaviour
    {
        private RoadDataGatherer _roadDataGatherer;
        [HideInInspector] public bool EmissionEnabled = false;
        private bool _previousEmissionEnabled;
        public float ColorTransitionDuration = 0.5f; 
        public float MaxRedValue = 1.0f; 

        private MeshRenderer _rend;
        private static readonly int EmissionColor1 = Shader.PropertyToID("_EmissionColor");

        private void Start()
        {
            _rend = GetComponent<MeshRenderer>();
            _roadDataGatherer = GetComponent<RoadDataGatherer>();
            _previousEmissionEnabled = EmissionEnabled;
            StartCoroutine(RoadColorUpdate());
        }

        private void Update()
        {
            if(EmissionEnabled != _previousEmissionEnabled)
            {
                StopCoroutine(RoadColorUpdate());
                StartCoroutine(RoadColorUpdate());
                _previousEmissionEnabled = EmissionEnabled;
            }
        }

        private IEnumerator RoadColorUpdate()
        {
            Color currentColor = Color.green;

            while (true)
            {
                Material[] materials = _rend.materials;
                float colorValue = Mathf.Clamp(_roadDataGatherer.CurrentFuelConsumptionRatio, 0, MaxRedValue);
                Color targetColor = Color.Lerp(Color.green, Color.red, colorValue); 

                float timeElapsed = 0.0f;

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

                // If EmissionEnabled is false, stop the coroutine.
                if (!EmissionEnabled) 
                    break;
            }
        }
    }
}
