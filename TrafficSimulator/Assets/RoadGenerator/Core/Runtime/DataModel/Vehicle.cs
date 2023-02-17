using UnityEngine;

namespace DataModel
{
    public class Vehicle
    {
        private string _id;
        private GameObject _vehicle;
        public Vehicle(string id, GameObject vehicle) => (_id, _vehicle) = (id, vehicle);
    }
}