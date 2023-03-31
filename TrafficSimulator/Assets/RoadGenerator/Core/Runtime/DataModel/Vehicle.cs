using UnityEngine;

namespace DataModel
{
    abstract public class Vehicle : MonoBehaviour
    {
        protected string _id;

        protected void Init()
        {
            _id = System.Guid.NewGuid().ToString();
        }

        public string ID
        {
            get => _id;
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
            return _id.GetHashCode();
        }
    }
}