using System.Collections;
using UnityEngine;
using RoadGenerator;

namespace Statistics
{
    public class EmissionColor : MonoBehaviour
    {
        private RoadDataGatherer _roadDataGatherer;
        [HideInInspector] public StatisticsType EmissionType = StatisticsType.None;
        private StatisticsType _previousEmissionType;
        public float ColorTransitionDuration = 0.5f; 
        public float MaxRedValue = 1.0f; 

        private MeshRenderer _rend;
        private static readonly int EmissionColor1 = Shader.PropertyToID("_EmissionColor");

        private void Start()
        {
            _rend = GetComponent<MeshRenderer>();
            _roadDataGatherer = GetComponent<RoadDataGatherer>();
            _previousEmissionType = EmissionType;
            StartCoroutine(RoadColorUpdate());
        }

        private void Update()
        {
            if(EmissionType != _previousEmissionType)
            {
                _previousEmissionType = EmissionType;
                StopCoroutine(RoadColorUpdate());
                StartCoroutine(RoadColorUpdate());
            }
        }

        private float GetColorRatio()
        {
            switch(EmissionType)
            {
                case StatisticsType.Emissions:
                    return _roadDataGatherer.CurrentFuelConsumptionRatio;
                case StatisticsType.Congestion:
                    return _roadDataGatherer.CurrentCongestionRatio;
                default:
                    return 0.0f;
            }
        }

        private IEnumerator RoadColorUpdate()
        {
            Color currentColor = Color.green;

            while (true)
            {
                Material[] materials = _rend.materials;
                float colorValue = Mathf.Clamp(GetColorRatio(), 0, MaxRedValue);
                Color targetColor = Color.Lerp(Color.green, Color.red, colorValue); 

                float timeElapsed = 0.0f;

                while (timeElapsed < ColorTransitionDuration)
                {
                    currentColor = Color.Lerp(currentColor, targetColor, timeElapsed / ColorTransitionDuration);

                    foreach (Material material in materials)
                    {
                        if (EmissionType != StatisticsType.None)
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

                // If Emission is disabled, stop the coroutine.
                if (EmissionType == StatisticsType.None) 
                    break;
            }
        }
    }
}
