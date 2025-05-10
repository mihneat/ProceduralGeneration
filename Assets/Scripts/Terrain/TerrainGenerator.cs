using System.Collections.Generic;
using AYellowpaper.SerializedCollections;
using Unity.Mathematics;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Terrain
{
    public static class TerrainGenerator
    {
        private static float CalculatePerlinNoise(int chunkSize, BiomeData biome, int x, int y, Vector2 randOffset)
        {
            float amplitude = biome.Amplitude;
            float frequency = biome.Frequency;
            
            float noiseHeight = 0.0f;
            for (int i = 0; i < biome.Octaves; i++)
            {
                float xCoord = (float)x / chunkSize * biome.Scale * frequency + randOffset.x;
                float yCoord = (float)y / chunkSize * biome.Scale * frequency + randOffset.y;

                float perlinValue = Mathf.PerlinNoise(xCoord, yCoord) * 2 - 1; // noise.snoise(new Vector2(xCoord, yCoord)) / 2 + 0.5f;
                noiseHeight += perlinValue * amplitude;

                amplitude *= biome.Persistence;
                frequency *= biome.Lacunarity;
            }

            return (noiseHeight + 1) / 2; // Normalize to 0 - 1
        }
        
        public static KeyValuePair<float[,], BiomeData[,]> GenerateHeights(int chunkSize, Vector2Int chunkOffset, Vector2 terrainOffset,
            SerializedDictionary<Vector2Int, BiomeData> biomes, Vector2 temperatureOffset, Vector2 rainfallOffset)
        {
            float[,] heights = new float[chunkSize + 1, chunkSize + 1];
            BiomeData[,] biomeData = new BiomeData[chunkSize + 1, chunkSize + 1];
            
            for (int i = 0; i <= chunkSize; ++i)
            {
                for (int j = 0; j <= chunkSize; ++j)
                {
                    // Get the biome
                    biomeData[i, j] = new BiomeData();
                    BiomeGenerator.GetBiome(biomes, j + chunkOffset.x, i + chunkOffset.y, temperatureOffset, rainfallOffset, ref biomeData[i, j]);
                    
                    // Compute the height
                    heights[i, j] = CalculatePerlinNoise(chunkSize, biomeData[i, j], j + chunkOffset.x, i + chunkOffset.y, terrainOffset);
                }
            }

            return new KeyValuePair<float[,], BiomeData[,]>(heights, biomeData);
        }
    }
}
