using UnityEngine;
using RoadGenerator;

namespace POIs
{
    public class ParkingLot : Parking
    {
        protected override void ParkingSetup() {}
        protected override Vector3 GetSize()
        {
            return new Vector3(20, 0.1f, 20);
        }

        protected override void GenerateParkingSpots()
        {
            _parkingSpots.Clear();
            float forwardOffset = Size.z / 2;
            Vector3 startPos = transform.position - transform.right * forwardOffset;
            
            int parkingSpotsPerSide = Mathf.FloorToInt(Size.x / _parkingSize.x);
            float sideOffsetDelta = Size.x / parkingSpotsPerSide;

            for(int side = 0; side < 2; side++)
            {
                float sideOffset = _parkingSize.x / 2;
                for(int i = 0; i < parkingSpotsPerSide; i++, sideOffset += sideOffsetDelta)
                {
                    int sideCoef = side == 0 ? -1 : 1;
                    
                    // The amount to offset from the rear edge
                    Vector3 forwardOffsetVector = transform.forward * (forwardOffset + sideCoef * _parkingSize.y / 2);
                    
                    // The amount to offset from the right edge
                    Vector3 sideOffsetVector = transform.right * sideOffset;
                    
                    Quaternion rotation = transform.rotation * (side == 0 ? Quaternion.Euler(Vector3.up * 180) : Quaternion.identity);
                    POINode parkingSpot = new POINode(startPos + forwardOffsetVector + sideOffsetVector, rotation);
                    _parkingSpots.Add(parkingSpot);
                }
                forwardOffset *= -1;
            }
        }
    }
}