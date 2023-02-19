using UnityEngine;
using System.Collections;

public class editor : MonoBehaviour
{
    [SerializeField] public Terrain terrain;

    // Terrain: 0 = Grass, 1 = Water

    void Start()
    {
        //setHeight(0f, 0, 0);
    }

    void Update()
    {

    }

    // Change terrain height on specific coordinate (x, z)
    private void setHeight(float height, int x, int z)
    {
        float xRes = terrain.terrainData.heightmapResolution;
        float yRes = terrain.terrainData.heightmapResolution;

        float[,] heights = terrain.terrainData.GetHeights(0, 0, (int)xRes, (int)yRes);

        heights[x,z] = height;

        terrain.terrainData.SetHeights(0, 0, heights);
    }

    // Change terrain height on specific coordinate (x, z) with a radius
    private void createCube(float height, int x, int z, int radius)
    {
        float xRes = terrain.terrainData.heightmapResolution;
        float yRes = terrain.terrainData.heightmapResolution;

        float[,] heights = terrain.terrainData.GetHeights(0, 0, (int)xRes, (int)yRes);

        for (int i = x - radius; i < x + radius; i++)
        {
            for (int j = z - radius; j < z + radius; j++)
            {
                heights[i, j] = height;
            }
        }

        terrain.terrainData.SetHeights(0, 0, heights);
    }

    // Change terrain texture on specific coordinate (x, z)
    private void updateTerrainTextureCoordinate(int textureNumberFrom, int textureNumberTo, int x, int z)
    {
        float[, ,] alphas = terrain.terrainData.GetAlphamaps(0, 0, terrain.terrainData.alphamapWidth, terrain.terrainData.alphamapHeight);

        alphas[x, z, textureNumberTo] = Mathf.Max(alphas[x, z, textureNumberFrom], alphas[x, z, textureNumberTo]);
        alphas[x, z, textureNumberFrom] = 0f;

        terrain.terrainData.SetAlphamaps(0, 0, alphas);
    }

    // Change the entire terrains texture
    private void resetTerrainTexture(int textureNumberFrom, int textureNumberTo)
    {
        float[, ,] alphas = terrain.terrainData.GetAlphamaps(0, 0, terrain.terrainData.alphamapWidth, terrain.terrainData.alphamapHeight);

        for (int i = 0; i < terrain.terrainData.alphamapWidth; i++)
        {
            for (int j = 0; j < terrain.terrainData.alphamapHeight; j++)
            {
                alphas[i, j, textureNumberTo] = Mathf.Max(alphas[i, j, textureNumberFrom], alphas[i, j, textureNumberTo]);

                alphas[i, j, textureNumberFrom] = 0f;
            }
        }

        terrain.terrainData.SetAlphamaps(0, 0, alphas);
    }

    /*
    // Reset the entire terrain texture
    private void reset(int texture)
    {
        float[, ,] alphas = terrain.terrainData.GetAlphamaps(0, 0, terrain.terrainData.alphamapWidth, terrain.terrainData.alphamapHeight);

        for (int i = 0; i < terrain.terrainData.alphamapWidth; i++)
        {
            for (int j = 0; j < terrain.terrainData.alphamapHeight; j++)
            {
                alphas[i, j, texture] = alphas[i, j, texture];
            }
        }

        terrain.terrainData.SetAlphamaps(0, 0, alphas);
    }
    */
}
