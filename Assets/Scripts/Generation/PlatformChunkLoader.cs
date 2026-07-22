using NUnit.Framework;
using UnityEngine;
using System.Collections.Generic;

public class PlatformChunkLoader : MonoBehaviour
{
    [SerializeField] GameObject[] PlatformChunks;
    [SerializeField] GameObject ChunkParent;
    [SerializeField] CameraController cameraController;
    [SerializeField] GameObject CheckPointChunk;

    [SerializeField] int ChunkCount = 12;
    [SerializeField] float ChunkLength = 10f;
    [SerializeField] float chunkMoveSpeed = 8f;
    [SerializeField] float minChunkMoveSpeed = 4f;
    [SerializeField] float maxChunkMoveSpeed = 20f;
    [SerializeField] float minGravityZ = -22f;
    [SerializeField] float maxGravityZ = 2f;
    [SerializeField] int checkPointInterval = 8;

    GameObject PlatformChunk;
    List<GameObject> chunks = new List<GameObject>();
    
    int chunkIndex = 0;
    void Start()
    {
        SpawnStartingChunk();
    }
    void Update()
    {
        MoveChunk();

    }

    void SpawnStartingChunk()
    {
        for (int i = 0; i < ChunkCount; i++)
        {
            chunkIndex++;
            PlatformChunk = PlatformChunks[Random.Range(0, PlatformChunks.Length)];
            if (chunkIndex % checkPointInterval == 0)
            {
                PlatformChunk = CheckPointChunk;
            }
            Vector3 ChunkPos = transform.position + Vector3.forward * (ChunkLength * i);
            GameObject newChunk = Instantiate(PlatformChunk, ChunkPos, Quaternion.identity, ChunkParent.transform);

            chunks.Add(newChunk);
            
        }
    }

    public void ChangeChunkMoveSpeed(float newChunkSpeed)
    {
        float clampedMoveSpeed = chunkMoveSpeed + newChunkSpeed;
        clampedMoveSpeed = Mathf.Clamp(clampedMoveSpeed, minChunkMoveSpeed, maxChunkMoveSpeed);
        chunkMoveSpeed = clampedMoveSpeed;

        float clampedGravityZ = Physics.gravity.z - newChunkSpeed;
        clampedGravityZ = Mathf.Clamp(clampedGravityZ, minGravityZ, maxGravityZ);
        Physics.gravity = new Vector3(Physics.gravity.x, Physics.gravity.y, clampedGravityZ);

        cameraController.ChangeCameraFOV(newChunkSpeed);
    }
    void MoveChunk()
    {
        for (int i = 0; i < chunks.Count; i++)
        {
            GameObject chunk = chunks[i];
            chunk.transform.Translate(-transform.forward * (chunkMoveSpeed * Time.deltaTime));

            if(chunk.transform.position.z < Camera.main.transform.position.z - ChunkLength)
            {
                ManageNewChunkCreation();
                ManageOldChunkRemoval(chunk);
            }
        }
    }

    private void ManageOldChunkRemoval(GameObject chunk)
    {
        chunks.Remove(chunk);
        Destroy(chunk);
    }

    private void ManageNewChunkCreation()
    {
        chunkIndex++;
        PlatformChunk = PlatformChunks[Random.Range(0, PlatformChunks.Length)];
        if (chunkIndex % checkPointInterval == 0)
        {
            PlatformChunk = CheckPointChunk;
        }
        Vector3 newChunkPos = chunks[chunks.Count - 1].transform.position + ChunkLength * transform.forward;
        GameObject newChunk = Instantiate(PlatformChunk, newChunkPos, Quaternion.identity, ChunkParent.transform);
        chunks.Add(newChunk);
        

    }
}
