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

        public BuildingWay(XmlNode node, List<Vector3> points, Transform buildingTransform, GameObject buildingPrefab) : base(node, points)
        {
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
                        case "height":
                            Height = float.Parse(currentNode.Attributes["v"].Value.Replace(".", ","));
                            break;
                        case "building:levels":
                            BuildingLevels = int.Parse(currentNode.Attributes["v"].Value);
                            break;
                        case "addr:street":
                            StreetName = currentNode.Attributes["v"].Value;
                            break;
                        case "addr:housenumber":
                            StreetAddress = currentNode.Attributes["v"].Value;
                            break;
                        case "type":
                            if (currentNode.Attributes["v"].Value == "multipolygon")
                                IsMultiPolygon = true;
                            break;
                    }
                }
                catch{}
            }
        }
    }
}