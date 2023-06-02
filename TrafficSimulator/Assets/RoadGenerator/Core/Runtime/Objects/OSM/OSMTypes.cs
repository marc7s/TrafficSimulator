using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace RoadGenerator
{
    public enum WayType
    {
        Road,
        Rail,
        Building,
        Terrain,
        Water,
    }

    public enum RoadWayType
    {
        Residential,
        Path,
        Footway,
        Cycleway,
        Motorway,
        Primary,
        Secondary,
        Trunk,
        Steps,
        Tertiary,
        Service,
        RaceWay,
        Unclassified
    }

    public enum RailWayType
    {
        RailTrain,
        RailTram
    }

    public enum ServiceType
    {
        DriveWay,
        Alley
    }

    public enum SideWalkType
    {
        None,
        Left,
        Right,
        Both
    }

    public enum ParkingType
    {
        None,
        Left,
        Right,
        Both
    }

    public enum TerrainType
    {
        Water = -1,
        Grass = 1,
        Sand = -2,
        Concrete = -3,
        Forest = 3,
        Default = 0
    }
}