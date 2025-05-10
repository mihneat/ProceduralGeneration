using NaughtyAttributes;
using UnityEngine;

namespace Dungeon
{
    public class DungeonEntrance : MonoBehaviour
    {
        public string Seed => seed;
        
        [SerializeField] [ReadOnly] private string seed;
        
        public void Init(string newSeed)
        {
            seed = newSeed;
        }
    }
}
