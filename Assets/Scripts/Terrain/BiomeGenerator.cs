using System;
using AYellowpaper.SerializedCollections;
using NaughtyAttributes;
using Unity.Mathematics;
using UnityEngine;

namespace Terrain
{
    [System.Serializable]
    public class BiomeData
    {
        public string biomeName;
        public int biomeLayerIndex;
        public float[] layerContributions = new float[4];
        
        [Min(0.0f)] public float Scale = 20.0f;
    
        [Min(0)] public int Octaves = 4;
        [Min(0.0f)] public float Frequency = 0.02f;
        [Min(0.0f)] public float Amplitude = 10.0f;
        [Min(0.0f)] public float Persistence = 1.0f;
        [Min(0.0f)] public float Lacunarity = 1.0f;
    }
    
    // Inspired by: https://www.youtube.com/watch?v=aZyrimErjJ0
    public static class NoiseFunction
    {
        public static Vector2 FinalNoise(float x, float y, int octaves, Vector2 tempOffset, Vector2 rainOffset, float noiseScale)
        {
            // Get rainfall and temperature noise
            float t = TempNoise(x, y, octaves, tempOffset, noiseScale);
            float r = RainNoise(x, y, octaves, rainOffset, noiseScale);

            // Return the computed values
            return new Vector2(t, r);
        }

        private static float TempNoise(float x, float y, int octaves, Vector2 origin, float noiseScale)
        {
            return WarpedNoise(x, y, octaves, origin, noiseScale);
        }
        private static float RainNoise(float x, float y, int octaves, Vector2 origin, float noiseScale)
        {
            return WarpedNoise(x, y, octaves, origin, noiseScale);
        }
        private static float WarpedNoise(float x, float y, int octaves, Vector2 origin, float noiseScale)
        {
            // warp the noise by generating multiple instances of noise
            var q  = Noise(x + 5.3f, y + 0.8f, octaves, origin, noiseScale);

            return Noise(x + 80.0f * q, y + 80.0f * q, octaves, origin, noiseScale);
        }

        private static float Noise(float x, float y, int octaves, Vector2 origin, float noiseScale)
        {
            // Just generate the noise map 
            float a = 0, opacity = 1, maxValue = 0;

            // Loop for octaves
            for (int octave = 0; octave < octaves; octave++)
            {
                // find sample position on xy axis
                float xVal = (x / noiseScale) + origin.x;
                float yVal = (y / noiseScale) + origin.y;
                float z = (noise.snoise(new float2(xVal, yVal)) + 1) * 0.5f;
                a += (z / opacity);
                maxValue += 1f / opacity;

                // Change opacity and scale
                noiseScale *= 2f;
                opacity *= 2f;
            }

            // divide by max value to normalize the noise value between 0 and 1
            a /= maxValue;

            return a;
        }
    }
    
    public static class BiomeGenerator
    {
        private static readonly Vector2Int[,] biomeIndices = new Vector2Int[,]
        {
            { new Vector2Int(0, 0), new Vector2Int(0, 1) },
            { new Vector2Int(1, 0), new Vector2Int(1, 1) }
        };
        
        private static float AbruptLerp(float a, float b, float t)
        {
            const float aMax = 0.45f, bMin = 0.55f;
            if (t < aMax) return a;
            if (t > bMin) return b;
            return Mathf.Lerp(a, b, (t - aMax) / (bMin - aMax));
        }

        private static float BiomeBlendProperty(SerializedDictionary<Vector2Int, BiomeData> biomes, float t, float r, Func<BiomeData, float> getProperty)
        {
            float h1 = AbruptLerp(getProperty(biomes[biomeIndices[0, 0]]), getProperty(biomes[biomeIndices[0, 1]]), t);
            float h2 = AbruptLerp(getProperty(biomes[biomeIndices[1, 0]]), getProperty(biomes[biomeIndices[1, 1]]), t);
            return AbruptLerp(h1, h2, r);
        }
        
        public static void GetBiome(SerializedDictionary<Vector2Int, BiomeData> biomes, int x, int y,
            Vector2 temperatureOffset, Vector2 rainfallOffset, ref BiomeData biome)
        {
            Vector2 noiseData = NoiseFunction.FinalNoise(x, y, 4, temperatureOffset, rainfallOffset, 1000.0f);
            
            // Get the coordinates in the biome matrix
            float t = noiseData.x, r = noiseData.y;
            int tInt = Mathf.RoundToInt(t);
            int rInt = Mathf.RoundToInt(r);
            
            // Get the biome texture from the most matching biome
            biome.biomeLayerIndex = biomes[biomeIndices[tInt, rInt]].biomeLayerIndex;

            // BiomeData refBiome = biomes[biomeIndices[tInt, rInt]];
            // biome.Scale = refBiome.Scale;
            // biome.Frequency = refBiome.Frequency;
            // biome.Amplitude = refBiome.Amplitude;
            // biome.Persistence = refBiome.Persistence;
            // biome.Lacunarity = refBiome.Lacunarity;
            
            
            // Blend all the other attributes
            biome.Scale = BiomeBlendProperty(biomes, t, r, b => b.Scale);
            biome.Frequency = BiomeBlendProperty(biomes, t, r, b => b.Frequency);
            biome.Amplitude = BiomeBlendProperty(biomes, t, r, b => b.Amplitude);
            biome.Persistence = BiomeBlendProperty(biomes, t, r, b => b.Persistence);
            // biome.Lacunarity = BiomeBlendProperty(biomes, t, r, b => b.Lacunarity);
        }
    }
}
