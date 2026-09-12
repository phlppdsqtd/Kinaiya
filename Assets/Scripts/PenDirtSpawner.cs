using UnityEngine;
using System.Collections.Generic;

public class PenDirtSpawner : MonoBehaviour
{
    [Header("Spawn Settings")]
    public int minDirtPerDay = 3; 
    public int maxDirtPerDay = 5;
    
    [Header("References")]
    public GameObject[] dirtPrefabs;
    public Transform[] spawnPoints;

    private List<GameObject> spawnedDirtList = new List<GameObject>();

    void Start()
    {
        SpawnDailyDirt(); 
    }

    public void SpawnDailyDirt()
    {
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

            GameObject spawnedDirt = Instantiate(chosenPrefab, chosenPoint.position, randomRotation);
            spawnedDirtList.Add(spawnedDirt);

            availablePoints.RemoveAt(randomPointIndex);
        }
    }

    // Helper method to check if this pen still contains uncleaned dirt
    public bool HasActiveDirt()
    {
        spawnedDirtList.RemoveAll(item => item == null);
        foreach (GameObject dirt in spawnedDirtList)
        {
            if (dirt.activeSelf) return true;
        }
        return false;
    }
}