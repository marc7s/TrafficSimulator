using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;

namespace RoadGenerator
{
    public class TerrainGenerator
    {
        private List<TerrainWay> _terrains;
        private float[,,] _splatmapData;
        private TerrainData _terrainData;
        private float _baseHeight = 10;
        public void GenerateTerrain(Terrain terrain, List<TerrainWay> terrains, Vector3 terrainSize)
        {
            _terrains = terrains;
            _terrainData = terrain.terrainData;
            terrainSize.y = 10f;
            _terrainData.size = terrainSize;
            terrain.gameObject.SetActive(true);
            terrain.gameObject.transform.position = new Vector3(0, -10.01f, 0);
            _splatmapData = new float[_terrainData.alphamapWidth, _terrainData.alphamapHeight, _terrainData.alphamapLayers];

            MapTerrainHeight();
            MapTerrainTextures();
        }

        /// <summary> Mapping the terrain height, water will create holes in the ground </summary>
        private void MapTerrainHeight()
        {
            int res = _terrainData.heightmapResolution;
            float[,] heights = _terrainData.GetHeights(0, 0, res, res);

            for (int x = 0; x < res; x++)
            {
                for (int y = 0; y < res; y++)
                {
                    Vector2 basPos2D = Vector2.zero;
                    Vector2 terrainPosition =  basPos2D + new Vector2(x * _terrainData.size.x / res, y * _terrainData.size.z / res);
                    heights[y, x] = _baseHeight;
                    bool isInsideInnerArea = false;

                    foreach (TerrainWay terrainWay in _terrains)
                    {
                        if (terrainWay.TerrainType == TerrainType.Water && IsPointInPolygon(terrainPosition, Vector3ToVector2(terrainWay.TerrainArea.OuterArea).ToArray()))
                        {
                            if (terrainWay.TerrainArea.InnerAreas != null && terrainWay.TerrainArea.InnerAreas.Count > 0)
                            {
                                List<Vector3> innerArea = new List<Vector3>();

                                foreach (List<Vector3> innerArea2 in terrainWay.TerrainArea.InnerAreas)
                                    innerArea.AddRange(innerArea2);

                                // The terrain point is inside the terrain type area
                                if (IsPointInPolygon(terrainPosition, Vector3ToVector2(innerArea).ToArray()))
                                {
                                    isInsideInnerArea = true;
                                    break;
                                }
                            }

                            if (isInsideInnerArea)
                                break;

                            heights[y, x] = 0;
                            break;
                        }
                    }
                }
            }

            _terrainData.SetHeights(0, 0, heights);
        }

        /// <summary> Mapping the terrain textures according to the OSM texture mapping </summary>
        private void MapTerrainTextures()
        {
            for (int y = 0; y < _terrainData.alphamapHeight; y++)
            {
                for (int x = 0; x < _terrainData.alphamapWidth; x++)
                {
                    Vector2 basePos2D = Vector2.zero;
                    Vector2 terrainPosition =  basePos2D + new Vector2(x * _terrainData.size.x / _terrainData.alphamapWidth, y * _terrainData.size.z / _terrainData.alphamapHeight);
                    // Setup an array to record the mix of texture weights at this point
                    float[] splatWeights = new float[_terrainData.alphamapLayers];

                    bool foundTerrain = false;
                    bool isInsideInnerArea = false;
                    foreach (TerrainWay terrainWay in _terrains)
                    {
                        // The terrain point is inside the terrain type area
                        if (IsPointInPolygon(terrainPosition, Vector3ToVector2(terrainWay.TerrainArea.OuterArea).ToArray()))
                        {
                            foreach (List<Vector3> innerArea in terrainWay.TerrainArea.InnerAreas)
                            {
                                // The terrain point is inside the terrain type area
                                if (IsPointInPolygon(terrainPosition, Vector3ToVector2(innerArea).ToArray()))
                                {
                                    isInsideInnerArea = true;
                                    break;
                                }
                            }

                            if (isInsideInnerArea)
                                break;

                            if (terrainWay.TerrainType == TerrainType.Grass)
                                splatWeights[(int)TerrainType.Grass] = 1f;
                            else if (terrainWay.TerrainType == TerrainType.Forest)
                                splatWeights[(int)TerrainType.Forest] = 1f;
                            else
                                splatWeights[2] = 1f;

                            foundTerrain = true;
                            break;
                        }
                    }

                    if (!foundTerrain)
                        splatWeights[(int)TerrainType.Default] = 1f;

                    // Sum of all textures weights must add to 1, so calculate normalization factor from sum of weights
                    float z = splatWeights.Sum();

                    // Loop through each terrain texture
                    for(int i = 0; i < _terrainData.alphamapLayers; i++)
                    {
                        // Normalize so that sum of all texture weights = 1
                        splatWeights[i] /= z;

                        // Assign this point to the splatmap array
                        _splatmapData[y, x, i] = splatWeights[i];
                    }
                }
            }
            // Finally assign the new splatmap to the terrainData:
            _terrainData.SetAlphamaps(0, 0, _splatmapData);
        }

        private static bool IsPointInPolygon(Vector2 point, Vector2[] polygon)
        {
            if (polygon == null || polygon.Length < 3)
                return false;

            int polygonLength = polygon.Length;
            int i = 0;
            bool inside = false;
            // x, y for tested point.
            float pointX = point.x, pointY = point.y;
            // start / end point for the current polygon segment.
            float startX;
            float startY;
            float endX;
            float endY;
            Vector2 endPoint = polygon[polygonLength - 1];
            endX = endPoint.x;
            endY = endPoint.y;

            while (i < polygonLength)
            {
                startX = endX;
                startY = endY;
                endPoint = polygon[i++];
                endX = endPoint.x;
                endY = endPoint.y;
                inside ^= (endY > pointY ^ startY > pointY) && ((pointX - endX) < (pointY - endY) * (startX - endX) / (startY - endY));
            }

            return inside;
        }

        private static List<Vector2> Vector3ToVector2(List<Vector3> points)
        {
            List<Vector2> vector2Points = new List<Vector2>();

            foreach (Vector3 point in points)
                vector2Points.Add(new Vector2(point.x, point.z));

            return vector2Points;
        }
    }
}