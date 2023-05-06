using UnityEngine;
using System.Collections.Generic;
using DataModel;
using RoadGenerator;

namespace POIs
{
    public enum ParkingLineup
    {
        Random,
        Organized
    }

    public abstract class Parking : POI
    {
        [Header("Connections")]
        public GameObject ParkNodePrefab;

        [Header("Settings")]
        public ParkingLineup ParkingLineup = ParkingLineup.Random;

        [Header("Debug Settings")]
        public bool DrawParkNodes = false;
        
        protected List<POINode> _parkingSpots = new List<POINode>();
        
        [SerializeField][HideInInspector] protected GameObject _parkNodeContainer;
        [HideInInspector] protected static Vector2 _parkingSize = new Vector2(4f, 6f);
        private const string PARK_NODE_CONTAINER_NAME = "Park Nodes";
        private const string PARK_NODE_NAME = "ParkNode";
        
        // Require deriving parking types to implement their behaviour
        protected abstract void ParkingSetup();
        protected abstract void GenerateParkingSpots();
        protected abstract Vector3 GetSize();

        public void Awake()
        {
            Setup();
        }

        protected override void CustomSetup()
        {
            ParkingSetup();
            
            Size = GetSize();
            
            GenerateParkingSpots();
            ShowParkNodes();
        }

        public void ShowParkNodes()
        {
            if(_parkNodeContainer == null)
            {
                // Try to find the lane container if it has already been created
                foreach(Transform child in transform)
                {
                    if(child.name == PARK_NODE_CONTAINER_NAME)
                    {
                        _parkNodeContainer = child.gameObject;
                        break;
                    }
                }
            }

            // Destroy the lane container, and with it all the previous lanes
            if(_parkNodeContainer != null)
                DestroyImmediate(_parkNodeContainer);

            // Create a new empty lane container
            _parkNodeContainer = new GameObject(PARK_NODE_CONTAINER_NAME);
            _parkNodeContainer.transform.parent = transform;

            // Draw the lane nodes if the setting is enabled
            if(DrawParkNodes)
            {
                for(int i = 0; i < _parkingSpots.Count; i++)
                {
                    POINode parkingSpot = _parkingSpots[i];
                    GameObject parkNodeObject = Instantiate(ParkNodePrefab, parkingSpot.Position, parkingSpot.Rotation, _parkNodeContainer.transform);
                    
                    parkNodeObject.name = PARK_NODE_NAME + i;
                }
            }
        }

        public POINode Park(Vehicle vehicle)
        {
            List<POINode> freeParkingSpots = _parkingSpots.FindAll(parkingSpot => !parkingSpot.HasVehicle());
            if (freeParkingSpots.Count < 1)
                return null;

            int spotIndex = ParkingLineup == ParkingLineup.Organized ? 0 : Random.Range(0, freeParkingSpots.Count);
            POINode freeParkingSpot = freeParkingSpots[spotIndex];
            
            freeParkingSpot.SetVehicle(vehicle);
            return freeParkingSpot;
        }

        public void Unpark(Vehicle vehicle)
        {
            POINode parkedSpot = _parkingSpots.Find(parkingSpot => parkingSpot.HasVehicle() && parkingSpot.Vehicle == vehicle);
            if(parkedSpot != null)
                parkedSpot.UnsetVehicle(vehicle);
        }
    }
}