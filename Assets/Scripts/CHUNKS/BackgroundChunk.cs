using UnityEngine;

public class BackgroundChunk : MonoBehaviour
{
    [SerializeField] private float chunkWidth = 900f;
    
    public float ChunkWidth => chunkWidth;
    
    public enum ChunkType { Landscape, Station }
    public ChunkType chunkType = ChunkType.Landscape;
}