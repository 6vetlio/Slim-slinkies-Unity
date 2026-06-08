using UnityEngine;

public class BackgroundChunk : MonoBehaviour
{
    // 640 = the authored chunk width used by the acre/station/city-acre prefabs.
    // The hill / city-hill prefabs don't serialize this field, so they fell back to
    // the old 900 default — leaving a ~260px gap after every hill chunk (tearing).
    // Matching the default to the real width makes every chunk tile seamlessly.
    [SerializeField] private float chunkWidth = 640f;
    
    public float ChunkWidth => chunkWidth;
    
    // Track what type of chunk this is
    public enum ChunkType { Landscape, Station }
    public ChunkType chunkType = ChunkType.Landscape;
}