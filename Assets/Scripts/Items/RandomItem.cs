using System;
using NaughtyAttributes;
using TMPro;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Items
{
    public class RandomItem : MonoBehaviour
    {
        [BoxGroup("General Item Data")]
        [SerializeField] private ItemData itemData;
        
        [BoxGroup("Item Info")]
        [ReadOnly] public string itemName;
        [BoxGroup("Item Info")]
        [ReadOnly] public ItemType type;
        [BoxGroup("Item Info")]
        [ReadOnly] public ItemRarity rarity;
        [BoxGroup("Item Info")]
        [ReadOnly] public string ability;
        [BoxGroup("Item Info")]
        [ReadOnly] public float damage;
        [BoxGroup("Item Info")]
        [ReadOnly] public float durability;

        [BoxGroup("References")] 
        [SerializeField] private Transform modelParent;
        [BoxGroup("References")] 
        [SerializeField] private TMP_Text itemNameText;
        
        private void Awake()
        {
            // Generate a random item
            GenerateRandom();
            
            // Add the model based on the item data SO
            Instantiate(
                itemData.itemTypeModels[type], 
                transform.position, 
                Quaternion.Euler(0, Random.Range(0.0f, 360.0f), 0),
                modelParent
            );

            transform.position += new Vector3(Random.Range(-2, 2), 0, Random.Range(-2, 2));
            
            // Update the item's name
            itemNameText.text = itemName;

            itemNameText.color = itemData.rarityColors[rarity];
            
            // Append rarity stars
            switch (rarity)
            {
                case ItemRarity.Legendary: itemNameText.text += " \u2605\u2605\u2605\u2605"; break;
                case ItemRarity.Epic: itemNameText.text += " \u2605\u2605\u2605"; break;
                case ItemRarity.Rare: itemNameText.text += " \u2605\u2605"; break;
                case ItemRarity.Uncommon: itemNameText.text += " \u2605"; break;
            }

            Debug.Log($"[RandomItem] New item: {Serialize()}");
        }
        
        private string Serialize()
        {
            return $"{itemName}|{(int)type}|{(int)rarity}|{ability}|{damage:F}|{durability:F}";
        }
        
        private void GenerateRandom()
        {
            string[] possibleAbilities = { "Frostbite", "Shriek of Sorrow", "Unending Dance", "TokiWoTomeru" };
            
            ItemType randomType = ItemType.Sword;
            switch (Random.Range(0, 4))
            {
                case 0: 
                    randomType = ItemType.Sword;
                    break;
                
                case 1: 
                    randomType = ItemType.Staff;
                    break;
                
                case 2: 
                    randomType = ItemType.Bow;
                    break;
                
                case 3: 
                    randomType = ItemType.Roller;
                    break;
            }

            string randomAbility = "";
            ItemRarity randomRarity;
            float rng = Random.Range(0.0f, 100.0f);
            if (rng < 30.0f) randomRarity = ItemRarity.Common;
            else if (rng < 60.0f) randomRarity = ItemRarity.Uncommon;
            else if (rng < 75.0f) { randomRarity = ItemRarity.Rare; randomAbility = possibleAbilities[Random.Range(0, possibleAbilities.Length)]; }
            else if (rng < 90.0f) { randomRarity = ItemRarity.Epic; randomAbility = possibleAbilities[Random.Range(0, possibleAbilities.Length)]; }
            else { randomRarity = ItemRarity.Legendary; randomAbility = possibleAbilities[Random.Range(0, possibleAbilities.Length)]; }


            name = itemName = randomType.ToString();
            type = randomType;
            rarity = randomRarity;
            ability = randomAbility;
            damage = Random.Range(100.0f, 200.0f);
            durability = Random.Range(50.0f, 150.0f);
        }
    }
}
