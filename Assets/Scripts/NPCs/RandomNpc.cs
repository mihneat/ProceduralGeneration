using System;
using NaughtyAttributes;
using TMPro;
using UnityEngine;
using Random = UnityEngine.Random;

namespace NPCs
{
    public class RandomNpc : MonoBehaviour
    {
        [BoxGroup("General NPC Data")]
        [SerializeField] private NpcData npcData;
        
        [BoxGroup("NPC Info")]
        [ReadOnly] public string npcName;
        [BoxGroup("NPC Info")]
        [ReadOnly] public NpcClass npcClass;
        [BoxGroup("NPC Info")]
        [ReadOnly] public NpcTrait trait;
        [BoxGroup("NPC Info")]
        [ReadOnly] public float maxHealth;
        [BoxGroup("NPC Info")]
        [ReadOnly] public float baseDamage;
        [BoxGroup("NPC Info")]
        [ReadOnly] public float size;

        [BoxGroup("References")] 
        [SerializeField] private Transform modelParent;
        [BoxGroup("References")] 
        [SerializeField] private Transform weaponSlot;
        [BoxGroup("References")] 
        [SerializeField] private TMP_Text npcNameText;
        
        private void Awake()
        {
            // Generate a random NPC
            GenerateRandom();
            
            // Spawn the appropriate weapon
            Instantiate(
                npcData.npcWeaponModels[npcClass],
                weaponSlot
            );

            transform.position += new Vector3(Random.Range(-2, 2), 0, Random.Range(-2, 2));
            transform.rotation = Quaternion.Euler(0, Random.Range(0.0f, 360.0f), 0);
            modelParent.localScale = new Vector3(size, size, size);
            
            // Update the NPC's name
            npcNameText.text = npcName;

            Debug.Log($"[RandomItem] New NPC: {Serialize()}");
        }
        
        private string Serialize()
        {
            return $"{npcName}|{(int)npcClass}|{(int)trait}|{maxHealth:F}|{baseDamage:F}|{size:F}";
        }
        
        private void GenerateRandom()
        {
            string[] possibleNames = { "Abraham", "Carol", "Tina", "Aphrodite", "Fred" };

            NpcClass randomClass = NpcClass.Warrior;
            switch (Random.Range(0, 4))
            {
                case 0: 
                    randomClass = NpcClass.Warrior;
                    break;
                
                case 1: 
                    randomClass = NpcClass.Wizard;
                    break;
                
                case 2: 
                    randomClass = NpcClass.Archer;
                    break;
                
                case 3: 
                    randomClass = NpcClass.Baker;
                    break;
            }

            NpcTrait randomTrait = NpcTrait.Shy;
            switch (Random.Range(0, 4))
            {
                case 0: 
                    randomTrait = NpcTrait.Shy;
                    break;
                
                case 1: 
                    randomTrait = NpcTrait.Sleepy;
                    break;
                
                case 2: 
                    randomTrait = NpcTrait.PowerHungry;
                    break;
                
                case 3: 
                    randomTrait = NpcTrait.CheeseEnthusiast;
                    break;
            }
            
            npcName = possibleNames[Random.Range(0, possibleNames.Length)];
            npcClass = randomClass;
            trait = randomTrait;
            maxHealth = Random.Range(50.0f, 200.0f);
            baseDamage = Random.Range(5.0f, 50.0f);
            size = Random.Range(0.7f, 1.4f);
        }
    }
}
