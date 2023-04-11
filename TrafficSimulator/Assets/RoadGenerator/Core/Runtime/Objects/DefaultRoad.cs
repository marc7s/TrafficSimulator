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

    [RequireComponent(typeof(RoadMeshCreator))]
    public class DefaultRoad : Road
    {        
        [SerializeField] protected GameObject _speedSignTenKPH;
        [SerializeField] protected GameObject _speedSignTwentyKPH;
        [SerializeField] protected GameObject _speedSignThirtyKPH;
        [SerializeField] protected GameObject _speedSignFortyKPH;
        [SerializeField] protected GameObject _speedSignFiftyKPH;
        [SerializeField] protected GameObject _speedSignSixtyKPH;
        [SerializeField] protected GameObject _speedSignSeventyKPH;
        [SerializeField] protected GameObject _speedSignEightyKPH;
        [SerializeField] protected GameObject _speedSignNinetyKPH;
        [SerializeField] protected GameObject _speedSignOneHundredKPH;
        [SerializeField] protected GameObject _speedSignOneHundredTenKPH;
        [SerializeField] protected GameObject _speedSignOneHundredTwentyKPH;
        [SerializeField] protected GameObject _speedSignOneHundredThirtyKPH;
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