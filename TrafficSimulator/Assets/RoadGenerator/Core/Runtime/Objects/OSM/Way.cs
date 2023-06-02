using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Xml;
using UnityEngine;

namespace RoadGenerator
{
    public class Way
    {
        public string ID;
        public string Name;
        public List<Vector3> Points;

        public Way(XmlNode node, List<Vector3> points)
        {
            XmlAttribute IDAttribute = node.Attributes["id"];

            if (IDAttribute != null)
                ID = IDAttribute.Value;

            Points = points;
        }
    }
}