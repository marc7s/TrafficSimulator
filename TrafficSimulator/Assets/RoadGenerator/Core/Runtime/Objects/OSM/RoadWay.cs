using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Xml;
using UnityEngine;
using System.Collections;

namespace RoadGenerator
{
    public class RoadWay : Way
    {
        public RoadWayType? RoadType;
        public bool IsOneWay = false;
        public bool IsLit = false;
        public int? MaxSpeed;
        public int? LaneAmount;
        public ServiceType? ServiceType;
        public SideWalkType? SideWalkType;
        public ParkingType? ParkingType;

        public RoadWay(XmlNode node, List<Vector3> points, XmlNode typeNode) : base(node, points)
        {
            RoadType = GetRoadType(typeNode);
            IEnumerator ienum = node.GetEnumerator();

            while (ienum.MoveNext())
            {
                XmlNode currentNode = (XmlNode) ienum.Current;

                if (currentNode.Name != "tag") 
                    continue;

                try
                {
                    string key = currentNode.Attributes["k"].Value;
                    string value = currentNode.Attributes["v"].Value;
                    switch (key)
                    {
                        case "name":
                            Name = value;
                            break;
                        case "maxspeed":
                            MaxSpeed = int.Parse(value);
                            break;
                        case "service":
                            if (value == "driveway")
                                ServiceType = RoadGenerator.ServiceType.DriveWay;
                            break;
                        case "lit":
                            IsLit = value == "yes";
                            break;
                        case "oneway":
                            IsOneWay = value == "yes";
                            break;
                        case "parking:right":
                            if (value == "lane" || value == "street_side")
                                ParkingType = RoadGenerator.ParkingType.Right;
                            break;
                        case "parking:left":
                            if (value == "lane" || value == "street_side")
                                ParkingType = RoadGenerator.ParkingType.Left;
                            break;
                        case "parking:both":
                            if (value == "lane" || value == "street_side")
                                ParkingType = RoadGenerator.ParkingType.Both;
                            break;
                    }
                }
                catch
                {
                    Debug.Log("Error parsing way data");
                }
            }
        }

        //https://wiki.openstreetmap.org/wiki/Map_features#Highway
        private static RoadWayType? GetRoadType(XmlNode node)
        {
            string value = node.Attributes["v"].Value;
            switch (value)
            {
                case "motorway":
                    return RoadWayType.Motorway;
                case "residential":
                    return RoadWayType.Residential;
                case "tertiary":
                    return RoadWayType.Tertiary;
                case "secondary":
                    return RoadWayType.Secondary;
                case "primary":
                    return RoadWayType.Primary;
                case "trunk":
                    return RoadWayType.Trunk;
                case "service":
                    return RoadWayType.Service;
                case "footway":
                    return RoadWayType.Footway;
                case "path":
                    return RoadWayType.Path;
                case "unclassified":
                    return null;
                case "raceway":
                    return RoadWayType.RaceWay;
                default:
                    return null;
            }
        }
    }
}