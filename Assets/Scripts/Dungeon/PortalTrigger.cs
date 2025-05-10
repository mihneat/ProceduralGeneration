using System;
using System.Collections;
using NaughtyAttributes;
using OpenWorld;
using Player;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Dungeon
{
    public enum PortalType
    {
        EnterDungeon,
        ExitDungeon
    }
    
    public class PortalTrigger : MonoBehaviour
    {
        public Transform SpawnPoint => spawnPoint;
        
        [SerializeField] private PortalType portalType;
        [SerializeField] private Transform spawnPoint;
        [ShowIf("portalType", PortalType.EnterDungeon)] [SerializeField] private DungeonEntrance dungeonEntrance;
        
        private void OnTriggerEnter(Collider other)
        {
            if (!other.CompareTag("Player"))
                return;

            if (portalType == PortalType.EnterDungeon)
                EnterDungeon();
            else
                ExitDungeon();
        }

        private void EnterDungeon()
        {
            Debug.Log($"[PortalTrigger] Player entered the portal! Generating the dungeon with the seed: {dungeonEntrance.Seed}");
            DungeonGenerator.seed = dungeonEntrance.Seed;
            
            // Save the coordinates to return to
            PlayerSpawnFinder.spawnPosition = spawnPoint.position;
            
            // Load the Dungeon scene
            SceneManager.LoadScene("Dungeon", LoadSceneMode.Single);
        }

        private void ExitDungeon()
        {
            Debug.Log($"[PortalTrigger] Player exited the dungeon! Returning to the real world at coordinates: {PlayerSpawnFinder.spawnPosition}");
            
            // Load the Open World scene
            SceneManager.LoadScene("OpenWorld", LoadSceneMode.Single);
        }
    }
}
