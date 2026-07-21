using NUnit.Framework;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class FenceSpawnerScript : MonoBehaviour
{
    [SerializeField] GameObject fencePrefab;
    [SerializeField] GameObject applePrefab;
    [SerializeField] GameObject coinPrefab;

    [SerializeField] float[] lanePos = {-2.5f, 0f, 2.5f};
    [SerializeField] float appleSpawnChance = 0.3f;
    [SerializeField] float coinSpawnChance = 0.5f;
    [SerializeField] float distanceBetweenCoins = 2f;

    List<int> availableLane = new List<int> { 0, 1, 2 };

    void Start()
    {
        SpawnFence();
        SpawnApple();
        SpawnCoin();
    }

    void SpawnFence()
    {
        int fencesToSpawn = Random.Range(0, lanePos.Length);
        for (int i = 0; i < fencesToSpawn; i++)
        {
            if (availableLane.Count <= 0) break;
            int selectedlane = SelectLane();

            Vector3 spawnPos = new Vector3(lanePos[selectedlane], transform.position.y, transform.position.z);
            Instantiate(fencePrefab, spawnPos, Quaternion.identity, this.transform);
        }
    }
    void SpawnApple()
    {
        if (Random.value > appleSpawnChance || availableLane.Count <= 0) return;
        int selectedlane = SelectLane();
        Vector3 spawnPos = new Vector3(lanePos[selectedlane], transform.position.y, transform.position.z);
        Instantiate(applePrefab, spawnPos, Quaternion.identity, this.transform);
    }

    void SpawnCoin()
    {
        if (Random.value > coinSpawnChance || availableLane.Count <= 0) return;
        int selectedlane = SelectLane();
        int maxCoinsToSpawn = 6;
        int coinsToSpawnCount = Random.Range(1, maxCoinsToSpawn);

        float topOfTheChunk = transform.position.z + (distanceBetweenCoins * 2f);

        for (int i = 0; i < coinsToSpawnCount; i++)
        {
            float spawnPosZ = topOfTheChunk - (i * distanceBetweenCoins);
            Vector3 spawnPos = new Vector3(lanePos[selectedlane], transform.position.y, spawnPosZ);
            Instantiate(coinPrefab, spawnPos, Quaternion.identity, this.transform);
        }
    }

    int SelectLane()
    {
        int randomLane = Random.Range(0, availableLane.Count);
        int selectedlane = availableLane[randomLane];
        availableLane.RemoveAt(randomLane);
        return selectedlane;
    }
}
