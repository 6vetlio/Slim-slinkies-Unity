using UnityEngine;
using System.Collections.Generic;

public class ChunkManager : MonoBehaviour
{
    [SerializeField] private Transform trainTransform;
    [SerializeField] private BackgroundChunk[] landscapeChunkPrefabs;
    [SerializeField] private BackgroundChunk stationChunkPrefab;
    [SerializeField] private Transform worldContainer;
    [SerializeField] private float spawnDistance = 1000f;
    [SerializeField] private float despawnDistance = 200f;
    [SerializeField] private int stationInterval = 5;
    
    private List<BackgroundChunk> activeChunks = new List<BackgroundChunk>();
    private float nextChunkLocalX = 0f;
    private int chunkCount = 0;
    
    private void Start()
    {
        for (int i = 0; i < 3; i++)
        {
            SpawnNextChunk();
        }
    }
    
    private void Update()
    {
       
        float nextChunkWorldX = nextChunkLocalX + worldContainer.position.x;
        float distanceToNextChunk = nextChunkWorldX - trainTransform.position.x;
        
       
        if (distanceToNextChunk < spawnDistance)
        {
            SpawnNextChunk();
        }
        
        
        for (int i = activeChunks.Count - 1; i >= 0; i--)
        {
            float chunkWorldX = activeChunks[i].transform.localPosition.x + worldContainer.position.x;
            float distanceBehind = trainTransform.position.x - chunkWorldX;
            
            if (distanceBehind > despawnDistance)
            {
                Destroy(activeChunks[i].gameObject);
                activeChunks.RemoveAt(i);
            }
        }
    }
    
  private void SpawnNextChunk()
{
    BackgroundChunk chunkToSpawn;
    
    if (stationChunkPrefab != null && chunkCount > 0 && chunkCount % stationInterval == 0)
    {
        chunkToSpawn = Instantiate(stationChunkPrefab);
        chunkToSpawn.chunkType = BackgroundChunk.ChunkType.Station;
    }
    else
    {
        BackgroundChunk prefab = landscapeChunkPrefabs[Random.Range(0, landscapeChunkPrefabs.Length)];
        chunkToSpawn = Instantiate(prefab);
        chunkToSpawn.chunkType = BackgroundChunk.ChunkType.Landscape;
    }
    
    chunkToSpawn.transform.SetParent(worldContainer);
    chunkToSpawn.transform.localPosition = new Vector3(nextChunkLocalX, 0f, 0f);
    
    float chunkWorldX = nextChunkLocalX + worldContainer.position.x;
    Debug.Log($"Spawned chunk {chunkCount}: Local X = {nextChunkLocalX}, World X = {chunkWorldX}");
    
    activeChunks.Add(chunkToSpawn);
    nextChunkLocalX += chunkToSpawn.ChunkWidth;
    chunkCount++;
}
}