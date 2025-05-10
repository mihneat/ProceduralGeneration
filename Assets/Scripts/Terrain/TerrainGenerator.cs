using Unity.Mathematics;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Terrain
{
    public static class TerrainGenerator
    {
        private static float CalculateFractalNoise(int chunkSize, HeightmapData heightmapData, int x, int y, Vector2 randOffset)
        {
            float amplitude = heightmapData.Amplitude;
            float frequency = heightmapData.Frequency;
            
            float noiseHeight = 0.0f;
            for (int i = 0; i < heightmapData.Octaves; i++)
            {
                float xCoord = (float)x / chunkSize * heightmapData.Scale * frequency + randOffset.x;
                float yCoord = (float)y / chunkSize * heightmapData.Scale * frequency + randOffset.y;

                float perlinValue = noise.snoise(new Vector2(xCoord, yCoord)) * 2 - 1; // Mathf.PerlinNoise(xCoord, yCoord) * 2 - 1;
                noiseHeight += perlinValue * amplitude;

                amplitude *= heightmapData.Persistence;
                frequency *= heightmapData.Lacunarity;
            }

            return (noiseHeight + 1) / 2; // Normalize to 0 - 1
        }
        
        public static float[,] GenerateHeights(int chunkSize, Vector2Int offset, Vector2 randOffset, HeightmapData heightmapData)
        {
            float[,] heights = new float[chunkSize + 1, chunkSize + 1];
            
            for (int i = 0; i <= chunkSize; ++i)
                for (int j = 0; j <= chunkSize; ++j)
                    heights[i, j] = CalculateFractalNoise(chunkSize, heightmapData, j + offset.x, i + offset.y, randOffset);

            return heights;
        }
    }
}
