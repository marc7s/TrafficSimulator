using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;

namespace RoadGenerator
{
    public enum PathType
    {
        Road,
        Rail
    }

    [ExecuteInEditMode()]
    [Serializable]
    [RequireComponent(typeof(RoadMeshCreator))]
    public class DefaultRoad : Road
    {        
        [SerializeField] private GameObject _speedSignTenKPH;
        [SerializeField] private GameObject _speedSignTwentyKPH;
        [SerializeField] private GameObject _speedSignThirtyKPH;
        [SerializeField] private GameObject _speedSignFortyKPH;
        [SerializeField] private GameObject _speedSignFiftyKPH;
        [SerializeField] private GameObject _speedSignSixtyKPH;
        [SerializeField] private GameObject _speedSignSeventyKPH;
        [SerializeField] private GameObject _speedSignEightyKPH;
        [SerializeField] private GameObject _speedSignNinetyKPH;
        [SerializeField] private GameObject _speedSignOneHundredKPH;
        [SerializeField] private GameObject _speedSignOneHundredTenKPH;
        [SerializeField] private GameObject _speedSignOneHundredTwentyKPH;
        [SerializeField] private GameObject _speedSignOneHundredThirtyKPH;
        public GameObject LampPostPrefab;

        public override TrafficSignAssessor GetNewTrafficSignAssessor()
        {
            return new DefaultRoadTrafficSignAssessor();
        }

        public override void UpdateMesh()
        {
           RoadMeshCreator roadMeshCreator = RoadObject.GetComponent<RoadMeshCreator>();
           roadMeshCreator.UpdateMesh();
        }

        /// <summary> Returns the speed sign type for the current speed limit </summary>
        public TrafficSignType GetSpeedSignType()
        {
            switch (SpeedLimit)
            {
                case SpeedLimit.TenKPH: return TrafficSignType.SpeedSignTenKPH;
                case SpeedLimit.TwentyKPH: return TrafficSignType.SpeedSignTwentyKPH;
                case SpeedLimit.ThirtyKPH: return TrafficSignType.SpeedSignThirtyKPH;
                case SpeedLimit.FortyKPH: return TrafficSignType.SpeedSignFortyKPH;
                case SpeedLimit.FiftyKPH: return TrafficSignType.SpeedSignFiftyKPH;
                case SpeedLimit.SixtyKPH: return TrafficSignType.SpeedSignSixtyKPH;
                case SpeedLimit.SeventyKPH: return TrafficSignType.SpeedSignSeventyKPH;
                case SpeedLimit.EightyKPH: return TrafficSignType.SpeedSignEightyKPH;
                case SpeedLimit.NinetyKPH: return TrafficSignType.SpeedSignNinetyKPH;
                case SpeedLimit.OneHundredKPH: return TrafficSignType.SpeedSignOneHundredKPH;
                case SpeedLimit.OneHundredTenKPH: return TrafficSignType.SpeedSignOneHundredTenKPH;
                case SpeedLimit.OneHundredTwentyKPH: return TrafficSignType.SpeedSignOneHundredTwentyKPH;
                case SpeedLimit.OneHundredThirtyKPH: return TrafficSignType.SpeedSignOneHundredThirtyKPH;
                default:
                    Debug.LogError("Speed sign type mapping for speed limit " + SpeedLimit + " not found");
                    return TrafficSignType.SpeedSignFiftyKPH;
            }
        }

        /// <summary> Returns the speed sign prefab for the current speed limit </summary>
        public GameObject GetSpeedSignPrefab()
        {
            switch (SpeedLimit)
            {
                case SpeedLimit.TenKPH: return _speedSignTenKPH;
                case SpeedLimit.TwentyKPH: return _speedSignTwentyKPH;
                case SpeedLimit.ThirtyKPH: return _speedSignThirtyKPH;
                case SpeedLimit.FortyKPH: return _speedSignFortyKPH;
                case SpeedLimit.FiftyKPH: return _speedSignFiftyKPH;
                case SpeedLimit.SixtyKPH: return _speedSignSixtyKPH;
                case SpeedLimit.SeventyKPH: return _speedSignSeventyKPH;
                case SpeedLimit.EightyKPH: return _speedSignEightyKPH;
                case SpeedLimit.NinetyKPH: return _speedSignNinetyKPH;
                case SpeedLimit.OneHundredKPH: return _speedSignOneHundredKPH;
                case SpeedLimit.OneHundredTenKPH: return _speedSignOneHundredTenKPH;
                case SpeedLimit.OneHundredTwentyKPH: return _speedSignOneHundredTwentyKPH;
                case SpeedLimit.OneHundredThirtyKPH: return _speedSignOneHundredThirtyKPH;
                default:
                    Debug.LogError("Speed sign prefab mapping for speed limit " + SpeedLimit + " not found");
                    return null;
            }
        }
        
    }
}