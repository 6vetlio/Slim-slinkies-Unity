using UnityEngine;

public class BackgroundChunk : MonoBehaviour
{
    [SerializeField] private float chunkWidth = 900f; // Your chunk width
    
    public float ChunkWidth => chunkWidth;
    
    // Track what type of chunk this is
    public enum ChunkType { Landscape, Station }
    public ChunkType chunkType = ChunkType.Landscape;
}