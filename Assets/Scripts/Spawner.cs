using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Spawner : MonoBehaviour
{
    public GameObject logPrefab;      // Prefab for logs
    public GameObject stonePrefab;    // Prefab for stones
    public Transform[] spawnPoints;   // Array of spawn points

    // Method to spawn either a log or stone based on the signal received
    public void SpawnObject(bool isLog)
    {
        Debug.Log("Signal received. Spawning object. IsLog: " + isLog);

        if (isLog)
        {
            SpawnLog();
        }
        else
        {
            SpawnStone();
        }
    }

    // Method to spawn a log at specific spawn points (2, 3, 5, 6)
    private void SpawnLog()
    {
        int[] validIndices = { 1, 2, 4, 5 }; // Log spawn points: 2 (1), 3 (2), 5 (4), 6 (5)
        int randomIndex = validIndices[Random.Range(0, validIndices.Length)];
        Transform spawnPoint = spawnPoints[randomIndex];
        GameObject log = Instantiate(logPrefab, spawnPoint.position, spawnPoint.rotation);
        Debug.Log("Log spawned at spawn point: " + randomIndex);
    }

    // Method to spawn a stone at specific spawn points (1, 4, 5, 6)
    private void SpawnStone()
    {
        int[] validIndices = { 0, 3, 4, 5 }; // Rock spawn points: 1 (0), 4 (3), 5 (4), 6 (5)
        int randomIndex = validIndices[Random.Range(0, validIndices.Length)];
        Transform spawnPoint = spawnPoints[randomIndex];
        GameObject stone = Instantiate(stonePrefab, spawnPoint.position, spawnPoint.rotation);
        Debug.Log("Stone spawned at spawn point: " + randomIndex);
    }
}
