using UnityEngine;
using DataModel;
using System.Collections.Generic;

namespace RoadGenerator 
{
    /// <summary>Represents a single node in a lane</summary>
    public class POINode : Node<POINode>
    {
        protected Vehicle _vehicle;

        /// <summary> Creates a new lane node </summary>
        /// <param name="position"> The position of the node </param>
        /// <param name="rotation"> The rotation of the node </param>
        public POINode(Vector3 position, Quaternion rotation)
        {
            _position = position;
            _rotation = rotation;
        }

        public virtual Vehicle Vehicle => _vehicle;

        public override POINode Copy()
        {
            return new POINode(_position, _rotation);
        }
        
        /// <summary> Tries to assign a vehicle to this node. Returns `true` if it succeded, `false` if there is already a vehicle assigned </summary>
        public virtual bool SetVehicle(Vehicle vehicle)
        {
            if(_vehicle == null || _vehicle == vehicle)
            {
                _vehicle = vehicle;
                return true;
            }
            
            return false;
        }

        /// <summary> Tries to unset a vehicle from this node. Returns `true` if it succeded, `false` if either no vehicle is assigned, or a different vehicle is assigned </summary>
        public virtual bool UnsetVehicle(Vehicle vehicle)
        {
            if(_vehicle == vehicle)
            {
                _vehicle = null;
                return true;
            }
            
            Debug.LogError("Trying to unset a different vehicle");
            return false;
        }

        public virtual bool HasVehicle()
        {
            return Vehicle != null;
        }
    }
}