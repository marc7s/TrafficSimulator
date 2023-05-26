using UnityEngine;
using System;

namespace DataModel
{
    abstract public class Vehicle : MonoBehaviour
    {
        protected string _id = null;
        public Func<float> CurrentSpeedFunction;
        [SerializeField] public float VehicleLength;

        protected void Init()
        {
            if(_id == null)
                _id = System.Guid.NewGuid().ToString();
        }

        public string ID
        {
            get
            {
                if(_id == null)
                    Init();
                return _id;
            }
        }

        public float CurrentSpeed
        {
            get => CurrentSpeedFunction();
        }

        /// <summary>Override the generic equals for this class</summary>
        public override bool Equals(object other)
        {
            return Equals(other as Vehicle);
        }

        /// <summary>Define a custom equality function between Vehicles</summary>
        public bool Equals(Vehicle other)
        {
            return other != null && other.ID == ID;
        }

        /// <summary>Override the hashcode function for this class</summary>
        public override int GetHashCode()
        {
            return ID.GetHashCode();
        }
    }
}