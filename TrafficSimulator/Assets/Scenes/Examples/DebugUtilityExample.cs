using UnityEngine;
using RoadGenerator;
using System.Collections.Generic;

namespace Examples
{
    public class DebugUtilityExample : MonoBehaviour
    {
        private Dictionary<string, (Vector3[], Quaternion[])> _positionGroups = new Dictionary<string, (Vector3[], Quaternion[])>();
        private Vector3[] _linePositions = new Vector3[]{ Vector3.zero, new Vector3(10, 5, 10), new Vector3(15, 0, 10) };

        
        private void Start()
        {
            Vector3 diff = new Vector3(1, 0, 2);
            Vector3 curr = new Vector3(0, 0, 0);
            Quaternion rot = Quaternion.identity;
            for(int i = 0; i < 3; i++)
            {
                List<Vector3> positions = new List<Vector3>();
                List<Quaternion> rotations = new List<Quaternion>();
                for(int j = 0; j < 10; j++)
                {
                    positions.Add(curr);
                    rotations.Add(rot);
                    curr += diff;
                    rot *= Quaternion.Euler(Vector3.up * 10);
                }
                _positionGroups.Add($"Positions {i}", (positions.ToArray(), rotations.ToArray()));
                diff += new Vector3(3, 0, -2);
            }

            DebugUtility.AddMarkGroups(_positionGroups);
            DebugUtility.DrawLine(_linePositions, true);
        }
        private void Update()
        {

        }
    }
}