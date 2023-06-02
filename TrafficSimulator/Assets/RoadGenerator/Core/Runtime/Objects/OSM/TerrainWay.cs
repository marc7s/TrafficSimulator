using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Xml;
using UnityEngine;

namespace RoadGenerator
{
    public class TerrainWay : Way
    {
        public TerrainType TerrainType;
        public TerrainArea TerrainArea;

        public TerrainWay(XmlNode node, List<Vector3> outerPoints, List<List<Vector3>> innerAreas = null, TerrainType? terrainType = null) : base(node, outerPoints)
        {
            TerrainType = terrainType ?? GetTerrainType(node);
            TerrainArea = new TerrainArea(TerrainType, outerPoints, innerAreas);
        }

        // https://wiki.openstreetmap.org/wiki/Key:landuse
        private static TerrainType GetTerrainType(XmlNode node)
        {
            switch (node.Attributes["v"].Value)
            {
                case "grass":
                    return TerrainType.Grass;
                case "sand":
                    return TerrainType.Sand;
                case "forest":
                    return TerrainType.Forest;
                default:
                    return TerrainType.Default;
            }
        }
    }
}