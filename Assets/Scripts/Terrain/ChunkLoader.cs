using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AYellowpaper.SerializedCollections;
using NaughtyAttributes;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Terrain
{
    public class TerrainChunk : IEquatable<TerrainChunk>
    {
        public GameObject gameObject;
        public readonly float[,] heights;
        public readonly BiomeData[,] biomes;
        public readonly Vector2Int coords;

        public TerrainChunk(Vector2Int coords, float[,] heights, BiomeData[,] biomes)
        {
            this.coords = coords;
            this.heights = heights;
            this.biomes = biomes;
        }

        public void SetGameObject(GameObject go) => gameObject = go;

        public bool Equals(TerrainChunk other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return coords.Equals(other.coords);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((TerrainChunk)obj);
        }

        public override int GetHashCode()
        {
            return coords.GetHashCode();
        }
    }

    public class ChunkLoader : MonoBehaviour
    {
        [BoxGroup("Chunk Settings")]
        public int chunkSize = 64;
        [BoxGroup("Chunk Settings")] [Tooltip("Measured in chunks")]
        public int renderDistance = 10;
        [BoxGroup("Chunk Settings")]
        [Range(0, 256)] public int detailFactor = 128;
        [BoxGroup("Chunk Settings")]
        public GameObject chunkPrefab;
        [BoxGroup("Chunk Settings")]
        public Transform chunkParent;
    
        [BoxGroup("Randomness Settings")]
        public string seed = "Azeroth";

        [BoxGroup("Biome Data")]
        [SerializeField] private SerializedDictionary<Vector2Int, BiomeData> biomes = new();

        [BoxGroup("Player Data")]
        public Transform player;
    
        private readonly List<Vector2Int> dirs = new()
        {
            new Vector2Int( 1,  0), 
            new Vector2Int( 0,  1),
            new Vector2Int(-1,  0),
            new Vector2Int( 0, -1),
        };
        private List<Vector2Int> _chunkOrder;

        private readonly HashSet<TerrainChunk> loadedChunks = new HashSet<TerrainChunk>();

        private CancellationTokenSource _cancellationTokenSource;

        private ConcurrentQueue<TerrainChunk> _chunkCreationQueue = new ConcurrentQueue<TerrainChunk>();

        private Vector2Int lastPlayerChunk = new Vector2Int(999, 999);

        public event Action<TerrainChunk> OnChunkLoaded;
        public event Action<TerrainChunk> OnChunkUnloaded;

        private Vector2 _terrainGenerationOffset;
        private Vector2 _temperatureGenerationOffset;
        private Vector2 _rainfallGenerationOffset;
    
        void Awake()
        {
            GenerateChunkOrder();
            StartCoroutine(CheckChunkGenerationQueue());
            
            // Set the seed
            Random.InitState(seed.GetHashCode());
            _terrainGenerationOffset = new Vector2(Random.Range(-9999, 9999), Random.Range(-9999, 9999));
            _temperatureGenerationOffset = new Vector2(Random.Range(-9999, 9999), Random.Range(-9999, 9999));
            _rainfallGenerationOffset = new Vector2(Random.Range(-9999, 9999), Random.Range(-9999, 9999));
        }

        void Update()
        {
            // Check if the position of the player has moved into a different chunk
            Vector2Int currentPlayerChunk = WorldPositionToChunkCoords(player.position);
        
            if (currentPlayerChunk != lastPlayerChunk) {
                StartTerrainGenerationTask(currentPlayerChunk);
                lastPlayerChunk = currentPlayerChunk;
            }
        }

        private void GenerateChunkOrder()
        {
            // Generate the order of chunks (BFS and add the chunk offsets to a list)
            _chunkOrder = new List<Vector2Int>();

            Vector2Int origin = new Vector2Int(0, 0);
        
            Queue<Vector2Int> nextCoordsQueue = new Queue<Vector2Int>();
            nextCoordsQueue.Enqueue(origin);

            HashSet<Vector2Int> visited = new HashSet<Vector2Int> { origin };

            while (nextCoordsQueue.Count > 0)
            {
                // Get the next item
                Vector2Int currChunkCoords = nextCoordsQueue.Dequeue();
                _chunkOrder.Add(currChunkCoords);
            
                foreach (Vector2Int dir in dirs)
                {
                    Vector2Int nextChunkCoords = currChunkCoords + dir * chunkSize;
                    if (visited.Contains(nextChunkCoords))
                        continue;
                        
                    visited.Add(nextChunkCoords);

                    // Ignore far away chunks
                    if (!ChunkInRange(origin, nextChunkCoords))
                        continue;
                
                    nextCoordsQueue.Enqueue(nextChunkCoords);
                }
            }
        }
    
        private void GenerateTerrainData(UnityEngine.Terrain terrain, TerrainChunk chunk, float[,] heights)
        {
            TerrainData terrainData = terrain.terrainData;
            
            // Create the terrain shape
            terrainData.heightmapResolution = chunkSize + 1;
            terrainData.size = new Vector3(chunkSize, 100, chunkSize);

            terrainData.SetHeights(0, 0, heights);
            
            terrain.Flush();
            
            // Change the terrain texture
            var alphaMaps = terrainData.GetAlphamaps(0, 0, terrainData.alphamapWidth, terrainData.alphamapHeight);
            for (int y = 0; y < terrainData.alphamapHeight; ++y)
            {
                for (int x = 0; x < terrainData.alphamapWidth; ++x)
                {
                    int newX = Mathf.FloorToInt(1.0f * x / terrainData.alphamapWidth * (chunkSize + 1));
                    int newY = Mathf.FloorToInt(1.0f * y / terrainData.alphamapHeight * (chunkSize + 1));

                    // var layerContributions = chunk.biomes[newY, newX].layerContributions;
                    alphaMaps[y, x, 0] = 0;
                    alphaMaps[y, x, chunk.biomes[newY, newX].biomeLayerIndex] = 256;
                    // alphaMaps[y, x, 0] = 256 * layerContributions[0];
                    // alphaMaps[y, x, 1] = 256 * layerContributions[1];
                    // alphaMaps[y, x, 2] = 256 * layerContributions[2];
                    // alphaMaps[y, x, 3] = 256 * layerContributions[3];
                }
            }
            
            terrainData.SetAlphamaps(0, 0, alphaMaps);
            
            terrain.Flush();
            
            // Add grass (following: https://discussions.unity.com/t/how-to-spawn-grass-at-runtime/949270 )
            terrainData.SetDetailResolution(32, 16);
            terrainData.RefreshPrototypes();
            
            terrain.Flush();
            
            int detailResolution = terrainData.detailResolution;

            List<int[,]> detailLayers = new List<int[,]>();
            detailLayers.Add(terrainData.GetDetailLayer(0, 0, detailResolution, detailResolution, 0));
            detailLayers.Add(terrainData.GetDetailLayer(0, 0, detailResolution, detailResolution, 1));
            detailLayers.Add(terrainData.GetDetailLayer(0, 0, detailResolution, detailResolution, 2));
            
            int numberOfGrass = Random.Range(100, 300);

            for (int i = 0; i < numberOfGrass; i++)
            {
                int layerIndex = Random.Range(0, 3);
                
                Vector3 randomPosition = new Vector3(Random.Range(0f, chunkSize), 0, Random.Range(0f, chunkSize));
                int x = Mathf.FloorToInt(randomPosition.x / chunkSize * detailResolution);
                int z = Mathf.FloorToInt(randomPosition.z / chunkSize * detailResolution);

                // Skip if grass already exists at this location
                if (detailLayers[layerIndex][x, z] > 0)
                    continue; 
                
                // Skip if not plains
                if (chunk.biomes[Mathf.FloorToInt(randomPosition.x), Mathf.FloorToInt(randomPosition.z)].biomeLayerIndex != 0)
                    continue;

                // Fill a 3x3 area around the calculated position
                for (int dz = -1; dz <= 1; ++dz)
                {
                    for (int dx = -1; dx <= 1; ++dx)
                    {
                        int newX = x + dx, newZ = z + dz;
                        
                        if (newX < 0 || newX >= detailResolution || newZ < 0 || newZ >= detailResolution)
                            continue;
                        
                        detailLayers[layerIndex][newX, newZ] = detailFactor;
                    }
                }
            }

            // Apply changes to the detail layers
            terrainData.SetDetailLayer(0, 0, 0, detailLayers[0]);
            terrainData.SetDetailLayer(0, 0, 1, detailLayers[1]);
            terrainData.SetDetailLayer(0, 0, 2, detailLayers[2]);
            
            // Force terrain to refresh its details
            terrain.Flush();
        }

        private IEnumerator CheckChunkGenerationQueue()
        {
            while (true)
            {
                if (_chunkCreationQueue.TryDequeue(out TerrainChunk chunkToCreate))
                {
                    if (loadedChunks.Contains(chunkToCreate))
                    {
                        yield return null;
                        continue;
                    }

                    loadedChunks.Add(chunkToCreate);
                
                    GameObject chunkGameObject = Instantiate(
                        chunkPrefab, 
                        new Vector3(chunkToCreate.coords.x, 0, chunkToCreate.coords.y), 
                        Quaternion.identity, 
                        chunkParent
                    );

                    chunkGameObject.name = $"Chunk {chunkToCreate.coords.x},{chunkToCreate.coords.y}";
                    chunkToCreate.SetGameObject(chunkGameObject);
                
                    // Create the terrain data
                    UnityEngine.Terrain terrain = chunkGameObject.GetComponent<UnityEngine.Terrain>();
                    TerrainData terrainData = new TerrainData();
                    
                    // Transfer some properties of the chunk prefab's Terrain component
                    terrainData.detailPrototypes = terrain.terrainData.detailPrototypes;
                    terrainData.terrainLayers = terrain.terrainData.terrainLayers;
                    
                    chunkGameObject.GetComponent<UnityEngine.Terrain>().terrainData = terrainData;
                    chunkGameObject.GetComponent<TerrainCollider>().terrainData = terrainData;
                    
                    GenerateTerrainData(terrain, chunkToCreate, chunkToCreate.heights);
                    
                    OnChunkLoaded?.Invoke(chunkToCreate);
                }
            
                yield return null;
            }
        }

        private void StartTerrainGenerationTask(Vector2Int currentChunk)
        {
            // Stop the async task
            _cancellationTokenSource?.Cancel();
            _cancellationTokenSource?.Dispose();
            _cancellationTokenSource = null;
        
            // Should the the chunk creation queue be cleared? Big overhead + might look bad?
            // I left this uncommented to avoid creating duplicate chunks
            _chunkCreationQueue.Clear();
        
            // Delete far away chunks
            List<TerrainChunk> chunksToUnload = new List<TerrainChunk>();
            foreach (TerrainChunk chunk in loadedChunks)
                if (!ChunkInRange(currentChunk, chunk.coords))
                    chunksToUnload.Add(chunk);

            foreach (TerrainChunk chunk in chunksToUnload)
            {
                if (chunk.gameObject != null)
                    Destroy(chunk.gameObject);
            
                loadedChunks.Remove(chunk);
                
                OnChunkUnloaded?.Invoke(chunk);
            }
        
            // Shallow copy the loaded chunks
            List<TerrainChunk> loadedChunksCopy = new List<TerrainChunk>(loadedChunks);

            _cancellationTokenSource = new CancellationTokenSource();
            var terrainGenerationTask = Task.Run(
                () => UpdateLoadedChunks(
                    currentChunk,
                    _terrainGenerationOffset,
                    loadedChunksCopy,
                    _cancellationTokenSource.Token
                ),
                _cancellationTokenSource.Token
            );
        }

        private void UpdateLoadedChunks(Vector2Int center, Vector2 randOffset, List<TerrainChunk> loadedChunksCopy, CancellationToken token)
        {
            try
            {
                // Go through the sorted chunks list, skip the loaded ones and create the new ones
                foreach (Vector2Int nextChunkOffset in _chunkOrder)
                {
                    Vector2Int nextChunkCoords = center + nextChunkOffset;

                    if (loadedChunksCopy.Find(chunk => chunk.coords == nextChunkCoords) != null)
                        continue;
            
                    // Generate the chunk
                    var kvp = TerrainGenerator.GenerateHeights(chunkSize, nextChunkCoords, randOffset, biomes, _temperatureGenerationOffset, _rainfallGenerationOffset);
                    
                    if (token.IsCancellationRequested)
                        return;
            
                    _chunkCreationQueue.Enqueue(new TerrainChunk(nextChunkCoords, kvp.Key, kvp.Value));

                    if (token.IsCancellationRequested)
                        return;
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[ChunkLoader] Terrain generation thread error: {e}");
                throw;
            }
        }

        private bool ChunkInRange_Euclidean(Vector2Int center, Vector2Int chunkCoords)
        {
            float distance = Vector2Int.Distance(center, chunkCoords);
            return distance - 0.0001f <= chunkSize * renderDistance;
        }

        private bool ChunkInRange_Manhattan(Vector2Int center, Vector2Int chunkCoords)
        {
            int distance = Math.Abs(center.x - chunkCoords.x) + Math.Abs(center.y - chunkCoords.y);
            return distance <= chunkSize * renderDistance;
        }

        private bool ChunkInRange(Vector2Int center, Vector2Int chunkCoords)
        {
            return ChunkInRange_Manhattan(center, chunkCoords);
        }

        private Vector2Int WorldPositionToChunkCoords(Vector3 worldPosition)
        {
            Vector2 xOzPos = new Vector2(worldPosition.x, worldPosition.z);
        
            // Bring a chunk to [0, 1]
            Vector2 xOzNormalizedPos = new Vector2(xOzPos.x / chunkSize + 0.5f, xOzPos.y / chunkSize + 0.5f);
        
            // Find the chunk coords
            Vector2Int chunkCoordsNormalized = new Vector2Int(Mathf.FloorToInt(xOzNormalizedPos.x), Mathf.FloorToInt(xOzNormalizedPos.y));

            return new Vector2Int(chunkCoordsNormalized.x * chunkSize - chunkSize / 2, chunkCoordsNormalized.y * chunkSize - chunkSize / 2);
        }

        private void OnValidate()
        {
            // Stop the async task
            _cancellationTokenSource?.Cancel();
            _cancellationTokenSource?.Dispose();
            _cancellationTokenSource = null;
        
            // Should the the chunk creation queue be cleared? Big overhead + might look bad?
            // I left this uncommented to avoid creating duplicate chunks
            _chunkCreationQueue.Clear();
        
            // Delete all loaded chunks
            foreach (TerrainChunk chunk in loadedChunks)
                if (chunk.gameObject != null)
                    Destroy(chunk.gameObject);
        
            loadedChunks.Clear();
        
            // Regenerate the chunk order
            GenerateChunkOrder();
        }
    }
}