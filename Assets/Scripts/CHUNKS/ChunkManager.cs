using UnityEngine;
using System.Collections.Generic;

public class ChunkManager : MonoBehaviour
{
    [SerializeField] private Transform trainTransform;
    [SerializeField] private BackgroundChunk[] landscapeChunkPrefabs;
    [SerializeField] private BackgroundChunk stationChunkPrefab;
    [SerializeField] private float spawnDistance = 1000f; // Spawn when train gets this close
    [SerializeField] private float despawnDistance = 200f; // Delete when this far behind
    [SerializeField] private int stationInterval = 5; // Station every 5 chunks
    
    private List<BackgroundChunk> activeChunks = new List<BackgroundChunk>();
    private float nextChunkXPosition = 0f;
    private int chunkCount = 0;
    
    private void Start()
    {
        // Spawn 2-3 initial chunks so train doesn't start in empty space
        for (int i = 0; i < 3; i++)
        {
            SpawnNextChunk();
        }
    }
    
    private void Update()
    {
        // Continuously spawn chunks ahead as train moves
        while (nextChunkXPosition - trainTransform.position.x < spawnDistance)
        {
            SpawnNextChunk();
        }
        
        // Remove chunks that are far behind (off-screen)
        for (int i = activeChunks.Count - 1; i >= 0; i--)
        {
            if (activeChunks[i].transform.position.x - trainTransform.position.x < -despawnDistance)
            {
                Destroy(activeChunks[i].gameObject);
                activeChunks.RemoveAt(i);
            }
        }
    }
    
  private void SpawnNextChunk()
{
    BackgroundChunk chunkToSpawn;
    
    // Only spawn stations if prefab is assigned AND interval is met
    if (stationChunkPrefab != null && chunkCount > 0 && chunkCount % stationInterval == 0)
    {
        chunkToSpawn = Instantiate(stationChunkPrefab);
        chunkToSpawn.chunkType = BackgroundChunk.ChunkType.Station;
    }
    else
    {
            // Random landscape chunk from your prefab array
            BackgroundChunk prefab = landscapeChunkPrefabs[Random.Range(0, landscapeChunkPrefabs.Length)];
            chunkToSpawn = Instantiate(prefab);
            chunkToSpawn.chunkType = BackgroundChunk.ChunkType.Landscape;
        }
        
        // Place it at the end of the chunk chain
        chunkToSpawn.transform.position = new Vector3(nextChunkXPosition, 0f, 0f);
        
        activeChunks.Add(chunkToSpawn);
        nextChunkXPosition += chunkToSpawn.ChunkWidth; // Move spawn point for next chunk
        chunkCount++;
    }
}