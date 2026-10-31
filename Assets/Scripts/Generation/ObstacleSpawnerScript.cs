using System.Collections;
using UnityEngine;

public class ObstacleSpawnerScript : MonoBehaviour
{
    [SerializeField] GameObject[] obstaclePrefabs;
    [SerializeField] float obstacleSpacing = 22f;
    [SerializeField] float[] spawnLanes = { -3f, 0f, 3f };
    [SerializeField] float spawnAheadDistance = 52f;
    [SerializeField] float dropHeightAboveTrack = 7f;
    [SerializeField] Transform obstacleParent;

    PlatformChunkLoader platformChunkLoader;
    int previousLaneIndex = -1;

    void Start()
    {
        platformChunkLoader = FindFirstObjectByType<PlatformChunkLoader>();
        if (obstacleParent == null)
            obstacleParent = transform;
        StartCoroutine(SpawnObstacleRoutine());
    }

    IEnumerator SpawnObstacleRoutine()
    {
        while (true)
        {
            if (obstaclePrefabs == null || obstaclePrefabs.Length == 0 || spawnLanes == null || spawnLanes.Length == 0)
            {
                yield return null;
                continue;
            }

            var currentSpeed = platformChunkLoader != null ? platformChunkLoader.CurrentChunkMoveSpeed : 8f;
            var delay = obstacleSpacing / Mathf.Max(currentSpeed, 0.1f);
            yield return new WaitForSeconds(delay);

            var laneIndex = Random.Range(0, spawnLanes.Length);
            if (spawnLanes.Length > 1 && laneIndex == previousLaneIndex)
                laneIndex = (laneIndex + Random.Range(1, spawnLanes.Length)) % spawnLanes.Length;
            previousLaneIndex = laneIndex;

            var trackOrigin = platformChunkLoader != null ? platformChunkLoader.transform : transform;
            var spawnPosition = trackOrigin.position + trackOrigin.forward * spawnAheadDistance;
            spawnPosition.y = trackOrigin.position.y + dropHeightAboveTrack;
            spawnPosition.x = spawnLanes[laneIndex];
            var obstacleToSpawn = obstaclePrefabs[Random.Range(0, obstaclePrefabs.Length)];
            if (obstacleToSpawn == null)
                continue;

            var rotation = Quaternion.Euler(0f, Random.Range(0f, 360f), 0f);
            Instantiate(obstacleToSpawn, spawnPosition, rotation, obstacleParent);
        }
    }
}
