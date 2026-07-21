using System.Collections;
using UnityEngine;

public class ObstacleSpawnerScript : MonoBehaviour
{
    [SerializeField] GameObject[] obstaclePrefabs;
    [SerializeField] float obstacleSpawnDelay = 1f;
    [SerializeField] float spawnWidth = 4f;
    [SerializeField] Transform obstacleParent;

    void Start()
    {
        StartCoroutine(SpawnObstacleRoutine());
    }
    IEnumerator SpawnObstacleRoutine()
    {
        while (true)
        {
            Vector3 spawnPosition = new Vector3(Random.Range(-spawnWidth, spawnWidth), transform.position.y, transform.position.z);
            GameObject obstacleToSpawn = obstaclePrefabs[Random.Range(0, obstaclePrefabs.Length)];
            yield return new WaitForSeconds(obstacleSpawnDelay);

            Instantiate(obstacleToSpawn, spawnPosition, Random.rotation, obstacleParent);
        }
    }
}
