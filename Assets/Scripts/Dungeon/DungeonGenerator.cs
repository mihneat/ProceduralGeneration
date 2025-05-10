using System;
using System.Collections;
using System.Collections.Generic;
using NaughtyAttributes;
using Player;
using Unity.Mathematics;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Dungeon
{
    public class DungeonGenerator : MonoBehaviour
    {
        public static string seed = "Azeroth";

        [SerializeField] private Vector2Int dungeonSizeRange = new Vector2Int(8, 64);
        [SerializeField] private int iterations = 10;
        [SerializeField] private float tileSize = 3;

        [SerializeField] private Transform dungeonGeometryParent;
        [SerializeField] private GameObject floorPrefab;
        [SerializeField] private GameObject corridorPrefab;
        [SerializeField] private GameObject floorWithEntrancePrefab;
        [SerializeField] private GameObject floorWithLootPrefab;
        [SerializeField] private GameObject floorWithMonsterPrefab;
        [SerializeField] private GameObject floorWithTrapPrefab;
        
        [SerializeField] private GameObject wallPrefab;
        [SerializeField] private GameObject corridorWallPrefab;

        // 0 == wall
        // 1 == floor
        // 2 == corridor
        // 3 == entrance
        // 4 == loot
        // 5 == monster
        // 6 == trap
        private int[,] dungeonGrid;
        private int[,] dungeonGridCopy;

        private int dungeonSize;
        
        private void Awake()
        {
            GenerateDungeon(seed);
        }

        private void GenerateDungeon(string currSeed)
        {
            Debug.Log($"[DungeonGenerator] Generating dungeon for the seed: {currSeed}");
            
            // Set the Random hash
            Random.InitState(currSeed.GetHashCode());
            
            // Generate the random dungeon size
            dungeonSize = Random.Range(dungeonSizeRange.x, dungeonSizeRange.y + 1);

            // Use cellular automata to generate the dungeon
            // Initialize the grids
            dungeonGrid = new int[dungeonSize, dungeonSize];
            dungeonGridCopy = new int[dungeonSize, dungeonSize];
            
            // Randomly fill in the dungeon grid
            for (int i = 0; i < dungeonSize; ++i)
                for (int j = 0; j < dungeonSize; ++j)
                    dungeonGrid[i, j] = Random.Range(0, 2);
            
            // Simulate the game of life
            for (int it = 0; it < iterations; ++it)
                SimulateGameOfLife();
            
            // Make the dungeon buffer
            BeefUpTheDungeon();
            
            // Connect the rooms
            ConnectTheRooms();
            
            // Fill up the dungeon
            PopulateTheDungeon();
            
            // Generate the dungeon pieces based on the resulting grid
            GenerateDungeonGeometry();
            
            // Move the player to the start position
            MovePlayerToEntrance();
        }

        private void SimulateGameOfLife()
        {
            for (int i = 0; i < dungeonSize; ++i)
            {
                for (int j = 0; j < dungeonSize; ++j)
                {
                    int alive = 0;
                        
                    // Count the alive cells in the 3x3 area around the current cell
                    for (int di = -1; di <= 1; ++di)
                    {
                        for (int dj = -1; dj <= 1; ++dj)
                        {
                            int newI = i + di, newJ = j + dj;

                            if (newI == i && newJ == j)
                                continue;

                            if (newI < 0 || newI >= dungeonSize ||
                                newJ < 0 || newJ >= dungeonSize)
                                continue;

                            alive += dungeonGrid[newI, newJ];
                        }
                    }

                    dungeonGridCopy[i, j] = dungeonGrid[i, j];

                    if (dungeonGridCopy[i, j] == 1)
                    {
                        if (alive < 2 || alive > 3)
                            dungeonGridCopy[i, j] = 0;
                    }
                    else
                    {
                        if (alive == 3)
                            dungeonGridCopy[i, j] = 1;
                    }
                }
            }
                
            // Move the copy into the original
            for (int i = 0; i < dungeonSize; ++i)
                for (int j = 0; j < dungeonSize; ++j)
                    dungeonGrid[i, j] = dungeonGridCopy[i, j];
        }

        private void BeefUpTheDungeon()
        {
            int[] dx = { 1, 0, -1, 0 };
            int[] dy = { 0, 1, 0, -1 };
            
            // Clear the copy
            for (int i = 0; i < dungeonSize; ++i)
                for (int j = 0; j < dungeonSize; ++j)
                    dungeonGridCopy[i, j] = 0;
            
            // Add more floor tiles
            for (int i = 0; i < dungeonSize; ++i)
            {
                for (int j = 0; j < dungeonSize; ++j)
                {
                    if (dungeonGrid[i, j] == 0)
                        continue;
                    
                    dungeonGridCopy[i, j] = 1;

                    for (int d = 0; d < dx.Length; ++d)
                    {
                        int newX = i + dy[d], newY = j + dx[d];

                        if (newX < 0 || newX >= dungeonSize ||
                                newY < 0 || newY >= dungeonSize)
                            continue;

                        dungeonGridCopy[newX, newY] = 1;
                    }
                }
            }
                
            // Move the copy into the original
            for (int i = 0; i < dungeonSize; ++i)
                for (int j = 0; j < dungeonSize; ++j)
                    dungeonGrid[i, j] = dungeonGridCopy[i, j];
        }

        private struct CellData
        {
            public bool visited;
            public int room;
            public Vector2Int predecessor;
        }

        private void Fill(CellData[,] gridData, int roomIndex, Vector2Int start)
        {
            int[] dx = { -1, 0, 1, 0 };
            int[] dy = { 0, -1, 0, 1 };
            
            Queue<Vector2Int> queue = new Queue<Vector2Int>();
            queue.Enqueue(start);
            
            gridData[start.x, start.y].visited = true;
            gridData[start.x, start.y].room = roomIndex;
            gridData[start.x, start.y].predecessor = new Vector2Int(-1, -1);

            while (queue.Count > 0)
            {
                Vector2Int currCell = queue.Dequeue();

                for (int d = 0; d < dx.Length; ++d)
                {
                    Vector2Int neigh = currCell + new Vector2Int(dx[d], dy[d]);
                    if (neigh.x < 0 || neigh.x >= dungeonSize ||
                            neigh.y < 0 || neigh.y >= dungeonSize)
                        continue;

                    if (gridData[neigh.x, neigh.y].visited)
                        continue;

                    if (dungeonGrid[neigh.x, neigh.y] == 0)
                        continue;
                    
                    // Add the neighbour to the queue
                    queue.Enqueue(neigh);
            
                    gridData[neigh.x, neigh.y].visited = true;
                    gridData[neigh.x, neigh.y].room = roomIndex;
                    gridData[neigh.x, neigh.y].predecessor = new Vector2Int(-1, -1);
                }
            }
        }

        private int FindRoot(int[] parent, int room)
        {
            int root = room;
            while (parent[root] != root)
            {
                parent[root] = parent[parent[root]];
                root = parent[root];
            }

            return root;
        }

        private bool Merge(int[] parent, int roomA, int roomB)
        {
            int rootA = FindRoot(parent, roomA);
            int rootB = FindRoot(parent, roomB);

            // If the rooms are already connected, return early
            if (rootA == rootB)
                return false;

            if (rootA < rootB)
                parent[rootB] = rootA;
            else
                parent[rootA] = rootB;

            return true;
        }

        private void TraceBackCorridor(CellData[,] gridData, Vector2Int start)
        {
            Vector2Int currPos = start;
            while (dungeonGrid[currPos.x, currPos.y] == 0 && gridData[currPos.x, currPos.y].predecessor.x != -1)
            {
                dungeonGrid[currPos.x, currPos.y] = 2;
                currPos = gridData[currPos.x, currPos.y].predecessor;
            }
        }

        private void ConnectRooms(CellData[,] gridData, Vector2Int startA, Vector2Int startB)
        {
            TraceBackCorridor(gridData, startA);
            TraceBackCorridor(gridData, startB);
        }
        
        private void ConnectTheRooms()
        {
            // BFS through the grid to mark the rooms
            int[] dx = { -1, 0, 1, 0 };
            int[] dy = { 0, -1, 0, 1 };

            CellData[,] gridData = new CellData[dungeonSize, dungeonSize];
            int roomCount = 0;
            for (int i = 0; i < dungeonSize; ++i)
            {
                for (int j = 0; j < dungeonSize; ++j)
                {
                    if (dungeonGrid[i, j] == 0)
                        continue;

                    if (gridData[i, j].visited)
                        continue;
                    
                    // Found a new room, fill it
                    roomCount++;
                    Fill(gridData, roomCount, new Vector2Int(i, j));
                }
            }
            
            // Create a "parent" vector for each room, initialized to themselves
            // This is part of the Union-Find algorithm :)
            int[] parent = new int[roomCount + 1];
            for (int i = 1; i <= roomCount; ++i)
                parent[i] = i;
            
            // Add to a queue every single floor tile on the board
            Queue<Vector2Int> leeQueue = new Queue<Vector2Int>();
            for (int i = 0; i < dungeonSize; ++i)
                for (int j = 0; j < dungeonSize; ++j)
                    if (gridData[i, j].visited)
                        leeQueue.Enqueue(new Vector2Int(i, j));

            while (leeQueue.Count > 0)
            {
                Vector2Int currCell = leeQueue.Dequeue();
                
                for (int d = 0; d < dx.Length; ++d)
                {
                    Vector2Int neigh = currCell + new Vector2Int(dx[d], dy[d]);
                    if (neigh.x < 0 || neigh.x >= dungeonSize ||
                            neigh.y < 0 || neigh.y >= dungeonSize)
                        continue;
                    
                    // If not visited, simply add the cell to the queue
                    if (!gridData[neigh.x, neigh.y].visited)
                    {
                        // Add the neighbour to the queue
                        leeQueue.Enqueue(neigh);
            
                        gridData[neigh.x, neigh.y].visited = true;
                        gridData[neigh.x, neigh.y].room = gridData[currCell.x, currCell.y].room;
                        gridData[neigh.x, neigh.y].predecessor = currCell;
                        
                        continue;
                    }
                    
                    // Check if the two rooms have already been connected, otherwise merge them
                    if (!Merge(parent, gridData[currCell.x, currCell.y].room, gridData[neigh.x, neigh.y].room))
                        continue;
                    
                    ConnectRooms(gridData, currCell, neigh);
                }
            }
        }

        private void PopulateTheDungeon()
        {
            // Choose a random floor tile (not corridor) and create an entrance
            
            // If there are NO empty tiles (RARE!), regenerate the dungeon?
            // Or maybe just create a rare "loot room", that could be very fun :)
            // Nah actually that sounds amazing, imma go with that
            List<Vector2Int> floorTiles = new List<Vector2Int>();
            for (int i = 0; i < dungeonSize; ++i)
                for (int j = 0; j < dungeonSize; ++j)
                    if (dungeonGrid[i, j] == 1)
                        floorTiles.Add(new Vector2Int(i, j));

            if (floorTiles.Count == 0) {
                dungeonSize = 9;
                dungeonGrid = new[,]
                {
                    {0, 1, 1, 1, 1, 1, 1, 1, 0},
                    {0, 1, 4, 4, 4, 4, 4, 1, 0},
                    {0, 1, 4, 4, 4, 4, 4, 1, 0},
                    {0, 1, 4, 4, 4, 4, 4, 1, 0},
                    {0, 1, 1, 1, 1, 1, 1, 1, 0},
                    {0, 0, 0, 1, 1, 1, 0, 0, 0},
                    {0, 0, 0, 1, 1, 1, 0, 0, 0},
                    {0, 0, 0, 1, 3, 1, 0, 0, 0},
                    {0, 0, 0, 1, 1, 1, 0, 0, 0},
                };
                dungeonGridCopy = new int[dungeonSize, dungeonSize];

                return;
            }
            
            Vector2Int entranceTile = floorTiles[Random.Range(0, floorTiles.Count)];
            dungeonGrid[entranceTile.x, entranceTile.y] = 3;

            // Iterate through all remaining floor/corridor tiles and add an element randomly (loot/monster)
            for (int i = 0; i < dungeonSize; ++i)
            {
                for (int j = 0; j < dungeonSize; ++j)
                {
                    if (dungeonGrid[i, j] != 1 && dungeonGrid[i, j] != 2)
                        continue;
                    
                    // If too close to the dungeon entrance, skip
                    if (Mathf.Abs(i - entranceTile.x) + Mathf.Abs(j - entranceTile.y) <= 3)
                        continue;

                    float randValue = Random.Range(0.0f, 100.0f);
                    if (randValue < 70.0f)
                        continue;

                    if (randValue < 85.0f)
                    {
                        // Generate loot
                        dungeonGrid[i, j] = 4;
                        continue;
                    }

                    if (randValue < 95.0f)
                    {
                        // Generate a monster
                        dungeonGrid[i, j] = 5;
                        continue;
                    }
                    
                    // Generate a trap
                    dungeonGrid[i, j] = 6;
                }
            }
        }

        private void GenerateDungeonGeometry()
        {
            // Generate the floor
            for (int i = 0; i < dungeonSize; ++i)
            {
                for (int j = 0; j < dungeonSize; ++j)
                {
                    if (dungeonGrid[i, j] == 0)
                        continue;

                    GameObject prefabToInstantiate;
                    switch (dungeonGrid[i, j])
                    {
                        case 1:
                            // Floor
                            prefabToInstantiate = floorPrefab;
                            break;

                        case 2:
                            // Corridor
                            prefabToInstantiate = corridorPrefab;
                            break;

                        case 3:
                            // Entrance
                            prefabToInstantiate = floorWithEntrancePrefab;
                            break;

                        case 4:
                            // Loot
                            prefabToInstantiate = floorWithLootPrefab;
                            break;

                        case 5:
                            // Monster
                            prefabToInstantiate = floorWithMonsterPrefab;
                            break;

                        case 6:
                            // Trap
                            prefabToInstantiate = floorWithTrapPrefab;
                            break;

                        default:
                            prefabToInstantiate = floorPrefab;
                            break;
                    }

                    Instantiate(
                        prefabToInstantiate,
                        new Vector3(i * tileSize, 0, j * tileSize),
                        Quaternion.identity,
                        dungeonGeometryParent
                    );
                }
            }

            // Generate the walls
            // Go through each floor and check with dx[], dy[] if there exists a wall in that direction
            // If yes, Instantiate the wall prefab with the appropriate rotation
            int[] dx = { 1, 0, -1, 0 };
            int[] dy = { 0, 1, 0, -1 };
            float[] dRot = { 0, 90, 180, 270 };
            for (int i = 0; i < dungeonSize; ++i)
            {
                for (int j = 0; j < dungeonSize; ++j)
                {
                    if (dungeonGrid[i, j] == 0)
                        continue;
                    
                    for (int d = 0; d < dx.Length; ++d)
                    {
                        Vector2Int neighPosition = new Vector2Int(i + dy[d], j + dx[d]);
                        if (neighPosition.x < 0 || neighPosition.x >= dungeonSize ||
                            neighPosition.y < 0 || neighPosition.y >= dungeonSize ||
                            dungeonGrid[neighPosition.x, neighPosition.y] == 0)
                        {
                            // Create a wall
                            Instantiate(
                                wallPrefab,
                                new Vector3(i * tileSize, 0, j * tileSize),
                                Quaternion.Euler(0, dRot[d], 0),
                                dungeonGeometryParent
                            );
                        }
                        
                        // Skip adding a corridor wall if the neighbour is outside the matrix
                        if (neighPosition.x < 0 || neighPosition.x >= dungeonSize ||
                                neighPosition.y < 0 || neighPosition.y >= dungeonSize)
                            continue;

                        if (dungeonGrid[i, j] == 2 && dungeonGrid[neighPosition.x, neighPosition.y] != 0 &&
                            dungeonGrid[neighPosition.x, neighPosition.y] != 2)
                        {
                            // Create a corridor wall
                            Instantiate(
                                corridorWallPrefab,
                                new Vector3(i * tileSize, 0, j * tileSize),
                                Quaternion.Euler(0, dRot[d], 0),
                                dungeonGeometryParent
                            );
                        }
                    }
                }
            }
        }

        private void MovePlayerToEntrance()
        {
            FindFirstObjectByType<PlayerController>().transform.position = FindFirstObjectByType<PortalTrigger>().SpawnPoint.position;
        }
    }
}
