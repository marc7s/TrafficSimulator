using UnityEngine;

namespace DataModel
{
    abstract public class Vehicle : MonoBehaviour
    {
        private string _id;

        protected void Init()
        {
            _id = System.Guid.NewGuid().ToString();
        }

        protected string Id
        {
            get => _id;
        }
    }
}