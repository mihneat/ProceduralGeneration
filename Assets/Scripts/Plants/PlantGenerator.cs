using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using AYellowpaper.SerializedCollections;
using NaughtyAttributes;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Plants
{
    [Flags]
    public enum Plant
    {
        None = 0,
        Plant1 = 1 << 0,
        Plant2 = 1 << 1,
        Plant3 = 1 << 2
    } 
    
    [System.Serializable]
    public class PlantDefinition
    {
        [SerializeField] public string plantName = "plant.png";
        [SerializeField] public Vector2Int textureSize = new Vector2Int(128, 128);
        [SerializeField] public Color plantColor = Color.green;
        
        [SerializeField] public string start = "";
        [SerializeField] public SerializedDictionary<string, string> axioms = new();
        [SerializeField] public int n = 3;
        [SerializeField] public float forwardStep = 0.02f;
        [SerializeField] public float angleStep = 10.0f;
    }
    
    public class PlantGenerator : MonoBehaviour
    {
        struct CursorState
        {
            public Vector2 pos;
            public Vector2 forward;

            public void RotateForwardVector(float degrees)
            {
                float rads = degrees * Mathf.Deg2Rad;

                float cos = Mathf.Cos(rads);
                float sin = Mathf.Sin(rads);
                forward = new Vector2(
                    forward.x * cos - forward.y * sin, 
                    forward.x * sin + forward.y * cos
                );
            }
        }
        
        [SerializeField] private SerializedDictionary<Plant, PlantDefinition> plants = new();
        [SerializeField] private Plant plantsToDraw;

        private readonly string generatedPlantsFolder = "\\Assets\\GeneratedPlants\\";

        private string ApplyAxioms(string sequence, PlantDefinition plant)
        {
            string result = "";

            foreach (char ch in sequence)
            {
                string chString = ch.ToString();
                if (!plant.axioms.ContainsKey(chString))
                {
                    result += ch;
                    continue;
                }

                result += plant.axioms[chString];
            }

            return result;
        }
        
        private string ExpandRule(PlantDefinition plant)
        {
            string sequence = plant.start;

            for (int i = 0; i < plant.n; ++i)
                sequence = ApplyAxioms(sequence, plant);

            return sequence;
        }

        private Vector2Int TexturePosition(Vector2 canvasPosition, Vector2Int textureSize)
        {
            Vector2Int texturePosition = new Vector2Int(
                Mathf.RoundToInt(canvasPosition.x * (textureSize.x - 1)),
                Mathf.RoundToInt(canvasPosition.y * (textureSize.y - 1))
            );

            // Return (-1, -1) if the point is outside the texture
            if (texturePosition.x < 0 || texturePosition.x >= textureSize.x ||
                texturePosition.y < 0 || texturePosition.y >= textureSize.y)
                return new Vector2Int(-1, -1);
            
            return texturePosition;
        }

        private void DrawNeighbouringPixel(Vector2Int texturePosition, Texture2D plantTexture, PlantDefinition plant, Color color)
        {
            if (texturePosition.x < 0 || texturePosition.x >= plant.textureSize.x ||
                texturePosition.y < 0 || texturePosition.y >= plant.textureSize.y)
                return;
            
            plantTexture.SetPixel(texturePosition.x, texturePosition.y, color);
        }

        private bool DrawPixel(Vector2 canvasPosition, PlantDefinition plant, Texture2D plantTexture, HashSet<Vector2Int> drawnPixels, Color color)
        {
            Vector2Int texturePosition = TexturePosition(canvasPosition, plant.textureSize);
            
            // Check if the position is outside the texture
            if (texturePosition is { x: -1, y: -1 })
                return false;

            if (drawnPixels.Contains(texturePosition))
                return false;
            
            // Add color variation?
            plantTexture.SetPixel(texturePosition.x, texturePosition.y, color * Random.Range(0.9f, 1.1f));
            drawnPixels.Add(texturePosition);
            
            // Draw the pixels around
            DrawNeighbouringPixel(texturePosition + Vector2Int.up, plantTexture, plant, color * Random.Range(0.9f, 1.1f));
            DrawNeighbouringPixel(texturePosition + Vector2Int.down, plantTexture, plant, color * Random.Range(0.9f, 1.1f));
            DrawNeighbouringPixel(texturePosition + Vector2Int.left, plantTexture, plant, color * Random.Range(0.9f, 1.1f));
            DrawNeighbouringPixel(texturePosition + Vector2Int.right, plantTexture, plant, color * Random.Range(0.9f, 1.1f));

            return true;
        }

        private void DrawLine(Vector2 start, Vector2 end, PlantDefinition plant, Texture2D plantTexture)
        {
            // Draw the start and end pixels
            HashSet<Vector2Int> drawnPixels = new HashSet<Vector2Int>();
            
            DrawPixel(start, plant, plantTexture, drawnPixels, plant.plantColor);
            DrawPixel(end, plant, plantTexture, drawnPixels, plant.plantColor);
            
            Queue<KeyValuePair<Vector2, Vector2>> queue = new Queue<KeyValuePair<Vector2, Vector2>>();
            queue.Enqueue(new KeyValuePair<Vector2, Vector2>(start, end));

            while (queue.Count > 0)
            {
                // Draw a point at the middle of the current line
                var currLine = queue.Dequeue();
                Vector2 middle = (currLine.Key + currLine.Value) / 2;
                Vector2Int texturePosition = TexturePosition(middle, plant.textureSize);
                
                // Try to draw the pixel
                if (!DrawPixel(middle, plant, plantTexture, drawnPixels, plant.plantColor))
                    continue;
                
                // Process the 2 halves of the current line
                queue.Enqueue(new KeyValuePair<Vector2, Vector2>(start, middle));
                queue.Enqueue(new KeyValuePair<Vector2, Vector2>(middle, end));
            }
        }

        private void DrawPlant(string sequence, PlantDefinition plant, Texture2D plantTexture)
        {
            // Generate the plant in [0, 1] with the corner at the bottom left
            // Set pixels in the texture by transforming the interval to [0, textureSize]

            // Start from the bottom center of the screen, with an angle of 90 degrees
            CursorState currState = new CursorState
            {
                pos = new Vector2(0.5f, 0.0f),
                forward = Vector2.up * plant.forwardStep
            };

            Stack<CursorState> st = new Stack<CursorState>();
            foreach (char ch in sequence)
            {
                switch (ch)
                {
                    case 'F':
                        DrawLine(currState.pos, currState.pos + currState.forward, plant, plantTexture);
                        
                        currState.pos += currState.forward;
                        
                        break;
                    
                    case '+':
                        currState.RotateForwardVector(plant.angleStep);
                        break;
                    
                    case '-':
                        currState.RotateForwardVector(-plant.angleStep);
                        break;
                    
                    case '[':
                        st.Push(currState);
                        break;
                    
                    case ']':
                        currState = st.Pop();
                        break;
                }
            }
        }
        
        private void GeneratePlant(PlantDefinition plant, Texture2D plantTexture)
        {
            // Create the full sequence
            string sequence = ExpandRule(plant);
            DrawPlant(sequence, plant, plantTexture);
        }

        private void GeneratePlantTexture(PlantDefinition plant)
        {
            Debug.Log($"[PlantGenerator] Imma generate a new plant! called '{plant.plantName}'!");
            
            Texture2D plantTexture = new Texture2D(plant.textureSize.x, plant.textureSize.y);
            for (int y = 0; y < plant.textureSize.y; ++y)
                for (int x = 0; x < plant.textureSize.x; ++x)
                    plantTexture.SetPixel(x, y, Color.clear);
            
            GeneratePlant(plant, plantTexture);
                
            // Apply the texture changes
            plantTexture.Apply();

            // Create the file
            byte[] textureBytes = plantTexture.EncodeToPNG();

            string imagePath = Application.dataPath + generatedPlantsFolder + plant.plantName;
            // Debug.Log($"[PlantGenerator] Directory exists: {Directory.Exists(Application.dataPath + generatedPlantsFolder)}");
            // Debug.Log($"[PlantGenerator] File exists: {File.Exists(imagePath)}");

            var fileStream = File.OpenWrite(imagePath);
            fileStream.Write(textureBytes);

            fileStream.Close();
        }
        
        [Button]
        public void GeneratePlantButton()
        {
            foreach (var plantType in Enum.GetValues(typeof(Plant)).Cast<Plant>())
            {
                if (plantType == Plant.None)
                    continue;
                
                if (!plantsToDraw.HasFlag(plantType))
                    continue;

                if (!plants.ContainsKey(plantType))
                {
                    Debug.LogWarning($"[PlantGenerator] Plant definitions dictionary does NOT contain a definition for {Enum.GetName(typeof(Plant), plantType)}");
                    continue;
                }
                
                GeneratePlantTexture(plants[plantType]);
            }
        }
    }
}
