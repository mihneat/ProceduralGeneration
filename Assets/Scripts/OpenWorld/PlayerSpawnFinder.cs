using Player;
using UnityEngine;

namespace OpenWorld
{
    public class PlayerSpawnFinder : MonoBehaviour
    {
        public static Vector3 spawnPosition = new Vector3(0, 55, 0);

        [SerializeField] private PlayerController player;
        
        private void Awake()
        {
            player.transform.position = spawnPosition;
        }
    }
}
