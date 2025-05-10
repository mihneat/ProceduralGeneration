using AYellowpaper.SerializedCollections;
using UnityEngine;

namespace NPCs
{
    public enum NpcClass
    {
        Warrior,
        Wizard,
        Archer,
        Baker
    }

    public enum NpcTrait
    {
        Shy,
        Sleepy,
        PowerHungry,
        CheeseEnthusiast
    }

    [CreateAssetMenu(fileName = "NewNPCData", menuName = "Procedural Generation/NPC Data")]
    public class NpcData : ScriptableObject
    {
        public SerializedDictionary<NpcClass, GameObject> npcWeaponModels;
    }
}
