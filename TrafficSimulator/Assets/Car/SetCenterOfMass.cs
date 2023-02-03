using UnityEngine;

namespace Car
{
    public class SetCenterOfMass : MonoBehaviour
    {
        [SerializeField] private Transform _centerOfMass;

        private void Start()
        {
            var rb = GetComponent<Rigidbody>();
            rb.centerOfMass = _centerOfMass.localPosition;
        }
    }
}