using AYellowpaper.SerializedCollections;
using UnityEngine;

namespace Items
{
    public enum ItemType
    {
        Sword,
        Staff,
        Bow,
        Roller
    }
    
    public enum ItemRarity
    {
        Common,
        Uncommon,
        Rare,
        Epic,
        Legendary
    }
    
    [CreateAssetMenu(fileName = "NewItemData", menuName = "Procedural Generation/Item Data")]
    public class ItemData : ScriptableObject
    {
        public SerializedDictionary<ItemType, GameObject> itemTypeModels;
        public SerializedDictionary<ItemRarity, Color> rarityColors;
    }
}
