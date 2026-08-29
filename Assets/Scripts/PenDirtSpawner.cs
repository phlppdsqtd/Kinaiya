using UnityEngine;
using System.Collections.Generic;

public class PenDirtSpawner : MonoBehaviour
{
    [Header("Spawn Settings")]
    [Tooltip("Minimum dirt piles per day.")]
    public int minDirtPerDay = 3; 
    [Tooltip("Maximum dirt piles per day.")]
    public int maxDirtPerDay = 5;
    
    [Header("References")]
    public GameObject[] dirtPrefabs;
    public Transform[] spawnPoints;

    void Start()
    {
        // Spawns dirt for the very first day when the game initially loads
        SpawnDailyDirt(); 
    }

    public void SpawnDailyDirt()
    {
        // Pick a random amount of dirt for today (max is exclusive, so +1 is needed)
        int dirtToSpawn = Random.Range(minDirtPerDay, maxDirtPerDay + 1);

        List<Transform> availablePoints = new List<Transform>(spawnPoints);
        int numToSpawn = Mathf.Min(dirtToSpawn, availablePoints.Count);

        for (int i = 0; i < numToSpawn; i++)
        {
            int randomPointIndex = Random.Range(0, availablePoints.Count);
            Transform chosenPoint = availablePoints[randomPointIndex];

            int randomPrefabIndex = Random.Range(0, dirtPrefabs.Length);
            GameObject chosenPrefab = dirtPrefabs[randomPrefabIndex];

            float randomRotationY = Random.Range(0f, 360f);
            Quaternion randomRotation = Quaternion.Euler(0, randomRotationY, 0);

            Instantiate(chosenPrefab, chosenPoint.position, randomRotation);

            availablePoints.RemoveAt(randomPointIndex);
        }
    }
}