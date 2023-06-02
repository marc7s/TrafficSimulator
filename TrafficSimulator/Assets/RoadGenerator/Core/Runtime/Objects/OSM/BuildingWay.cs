using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Xml;
using UnityEngine;
using System.Collections;

namespace RoadGenerator
{
    public class BuildingWay : Way
    {
        public float? Height;
        public int? BuildingLevels;
        public bool IsMultiPolygon;
        public string StreetName;
        public string StreetAddress;
        public GameObject BuildingObject;

        public BuildingWay(XmlNode node, List<Vector3> points) : base(node, points)
        {
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
                        case "height":
                            Height = float.Parse(value.Replace(".", ","));
                            break;
                        case "building:levels":
                            BuildingLevels = int.Parse(value);
                            break;
                        case "addr:street":
                            StreetName = value;
                            break;
                        case "addr:housenumber":
                            StreetAddress = value;
                            break;
                        case "type":
                            if (value == "multipolygon")
                                IsMultiPolygon = true;
                            break;
                    }
                }
                catch{}
            }
        }
    }
}