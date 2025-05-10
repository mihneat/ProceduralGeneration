using System;
using System.Collections.Generic;
using Terrain;
using Unity.Mathematics;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Dungeon
{
    public class DungeonPlacer : MonoBehaviour
    {
        [SerializeField] private GameObject dungeonEntrancePrefab;
        [SerializeField] private ChunkLoader chunkLoader;

        [SerializeField] [Range(0.1f, 100.0f)] private float spawnChance = 5;

        private Vector2Int hashOffset;
        private Vector2 xPositionNoiseOffset;
        private Vector2 zPositionNoiseOffset;
        private Vector2 rotationNoiseOffset;

        [SerializeField] [Min(1)] private int seedLength = 32;
        private readonly string seedAlphabet = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ"; 
        private readonly List<Vector2> dungeonSeedNoiseOffsets = new();

        private readonly Dictionary<Vector2Int, GameObject> loadedDungeonEntrances = new();
        
        private void Awake()
        {
            // Generate offsets in the random generation functions, based on the seed
            Random.InitState(chunkLoader.seed.GetHashCode());

            hashOffset = new Vector2Int(Random.Range(-9999, 9999), Random.Range(-9999, 9999));
            xPositionNoiseOffset = new Vector2(Random.Range(-9999, 9999), Random.Range(-9999, 9999));
            zPositionNoiseOffset = new Vector2(Random.Range(-9999, 9999), Random.Range(-9999, 9999));
            rotationNoiseOffset = new Vector2(Random.Range(-9999, 9999), Random.Range(-9999, 9999));

            for (int i = 0; i < seedLength; ++i)
                dungeonSeedNoiseOffsets.Add(new Vector2(Random.Range(-9999, 9999), Random.Range(-9999, 9999)));
        }

        private bool ChunkHasDungeon(TerrainChunk chunk)
        {
            // Even more randomness could be created if, for each chunk, we add more consistent noise
            // based on perlin noise (extract other coordinates from the chunk, at random)
            
            // For now, use Vector2Int's internal has function, whose main purpose is to try to
            // uniformly distribute values across a range, such that a hashmap stays balanced
            
            Vector2Int normalizedChunkCoords = new Vector2Int(
                (chunk.coords.x + chunkLoader.chunkSize / 2) / chunkLoader.chunkSize,
                (chunk.coords.y + chunkLoader.chunkSize / 2) / chunkLoader.chunkSize
            );
            return ((normalizedChunkCoords + hashOffset).GetHashCode() % Mathf.RoundToInt(100.0f / spawnChance)) == 0;
        }

        private void OnEnable()
        {
            chunkLoader.OnChunkLoaded += HandleOnChunkLoaded;
            chunkLoader.OnChunkUnloaded += HandleOnChunkUnloaded;
        }

        private void OnDisable()
        {
            chunkLoader.OnChunkLoaded -= HandleOnChunkLoaded;
            chunkLoader.OnChunkUnloaded -= HandleOnChunkUnloaded;
        }

        private void HandleOnChunkLoaded(TerrainChunk chunk)
        {
            // Check if a dungeon entrance should be created
            if (!ChunkHasDungeon(chunk))
                return;

            // Choose a random position in the chunk and a random rotation around the Y axis
            // noise.snoise is [-1, 1], convert to [0, chunkSize - 1]
            int xOffset = Mathf.FloorToInt((noise.snoise(xPositionNoiseOffset + chunk.coords) + 1) * chunkLoader.chunkSize / 2.0f);
            int zOffset = Mathf.FloorToInt((noise.snoise(zPositionNoiseOffset + chunk.coords) + 1) * chunkLoader.chunkSize / 2.0f);
            Vector2Int randChunkPosition = new Vector2Int(
                chunk.coords.x + xOffset, 
                chunk.coords.y + zOffset
            );

            // noise.snoise is [-1, 1], convert to [0, 360]
            float randRotation = (noise.snoise(rotationNoiseOffset + randChunkPosition) + 1) * 180.0f;

            // Sample the terrain height at that position
            float height = chunk.gameObject.GetComponent<UnityEngine.Terrain>().SampleHeight(
                new Vector3(randChunkPosition.x, 0, randChunkPosition.y)
            );

            // Instantiate the dungeon prefab
            GameObject dungeonEntrance = Instantiate(
                dungeonEntrancePrefab,
                new Vector3(randChunkPosition.x, height, randChunkPosition.y),
                Quaternion.Euler(0, randRotation, 0),
                transform
            );

            loadedDungeonEntrances[chunk.coords] = dungeonEntrance;
            
            // Generate the dungeon's seed
            string seed = "";
            foreach (Vector2 noiseOffset in dungeonSeedNoiseOffsets)
                seed += GetRandomLetter(randChunkPosition + noiseOffset);
            
            // Set the dungeon's seed
            dungeonEntrance.GetComponent<DungeonEntrance>().Init(seed);
        }
        
        private char GetRandomLetter(Vector2 samplePos)
        {
            // Random value between [0, 1]
            float normalizedNoise = (noise.snoise(samplePos) + 1.0f) / 2.0f;
            
            // Random value in [0, alphabetSize)
            int randomIndex = Mathf.FloorToInt(normalizedNoise * seedAlphabet.Length);
            
            // For safety, modulo by the alphabet size
            return seedAlphabet[randomIndex % seedAlphabet.Length];
        }

        private void HandleOnChunkUnloaded(TerrainChunk chunk)
        {
            // Unload the chunk's dungeon, in case one exists
            if (loadedDungeonEntrances.Remove(chunk.coords, out GameObject loadedEntrance))
                Destroy(loadedEntrance);
        }
    }
}
