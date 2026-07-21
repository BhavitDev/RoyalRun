using NUnit.Framework;
using UnityEngine;
using System.Collections.Generic;

public class PlatformChunkLoader : MonoBehaviour
{
    [SerializeField] GameObject PlatformChunk;
    [SerializeField] int ChunkCount = 12;
    [SerializeField] GameObject ChunkParent;
    [SerializeField] float ChunkLength = 10f;
    [SerializeField] float chunkMoveSpeed = 8f;
    [SerializeField] float minChunkMoveSpeed = 4f;
    [SerializeField] float maxChunkMoveSpeed = 20f;
    [SerializeField] float minGravityZ = -22f;
    [SerializeField] float maxGravityZ = 2f;
    [SerializeField] CameraController cameraController;

    List<GameObject> chunks = new List<GameObject>();
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
        Vector3 newChunkPos = chunks[chunks.Count - 1].transform.position + ChunkLength * transform.forward;
        GameObject newChunk = Instantiate(PlatformChunk, newChunkPos, Quaternion.identity, ChunkParent.transform);
        chunks.Add(newChunk);
    }
}
