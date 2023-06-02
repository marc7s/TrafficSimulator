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
        public ServiceType? ServiceType2;
        public SideWalkType? SideWalkType2;
        public ParkingType? ParkingType2;

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
                    switch (currentNode.Attributes["k"].Value)
                    {
                        case "name":
                            Name = currentNode.Attributes["v"].Value;
                            break;
                        case "maxspeed":
                            MaxSpeed = int.Parse(currentNode.Attributes["v"].Value);
                            break;
                        case "service":
                            if (currentNode.Attributes["v"].Value == "driveway")
                                ServiceType2 = ServiceType.DriveWay;
                            break;
                        case "lit":
                            if (currentNode.Attributes["v"].Value == "yes")
                                IsLit = true;
                            else 
                                IsLit = false;
                            break;
                        case "oneway":
                            IsOneWay = currentNode.Attributes["v"].Value == "yes";
                            break;
                        case "parking:right":
                            if (currentNode.Attributes["v"].Value == "lane")
                                ParkingType2 = ParkingType.Right;
                            else if (currentNode.Attributes["v"].Value == "street_side")
                                ParkingType2 = ParkingType.Right;
                            break;
                        case "parking:left":
                            if (currentNode.Attributes["v"].Value == "lane")
                                ParkingType2 = ParkingType.Left;
                            else if (currentNode.Attributes["v"].Value == "street_side")
                                ParkingType2 = ParkingType.Left;
                            break;
                        case "parking:both":
                            if (currentNode.Attributes["v"].Value == "lane")
                                ParkingType2 = ParkingType.Both;
                            else if (currentNode.Attributes["v"].Value == "street_side")
                                ParkingType2 = ParkingType.Both;
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
            switch (node.Attributes["v"].Value)
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