using UnityEngine;
using System;

namespace DataModel
{
    abstract public class Vehicle : MonoBehaviour
    {
        protected string _id = null;
        protected string _licensePlate = null;
        public Func<float> CurrentSpeedFunction;
        [SerializeField] public float VehicleLength;

        protected void Init()
        {
            if(_id == null)
                _id = System.Guid.NewGuid().ToString();
            
            if(_licensePlate == null)
                _licensePlate = GetRandomLicensePlate();
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

        public string LicensePlate
        {
            get
            {
                if(_licensePlate == null)
                    Init();
                
                return _licensePlate;
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

        /// <summary> Generates a random sequence of `length` capital letters </summary>
        private string GenRandCharSeq(int length)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
            string sequence = "";
            
            for(int i = 0; i < length; i++)
                sequence += chars[UnityEngine.Random.Range(0, chars.Length)];

            return sequence;
        }

        /// <summary> Generates a random sequence of `length` numbers </summary>
        private string GenRandNumSeq(int length)
        {
            const string chars = "0123456789";
            string sequence = "";
            
            for(int i = 0; i < length; i++)
                sequence += chars[UnityEngine.Random.Range(0, chars.Length)];

            return sequence;
        }

        /// <summary> Generates a random license plate following the two Swedish formats `ABC123` and the newer `ABC12D`</summary>
        private string GetRandomLicensePlate()
        {
            
            const float newFormatChance = 0.15f;
            
            return UnityEngine.Random.value <= newFormatChance ? $"{GenRandCharSeq(3)}{GenRandNumSeq(2)}{GenRandCharSeq(1)}" : $"{GenRandCharSeq(3)}{GenRandNumSeq(3)}";
        }
    }
}