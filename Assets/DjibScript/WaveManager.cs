using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using TMPro;

public class WaveManager : MonoBehaviour
{
   [Header("Spawn Settings")]
    public GameObject[] spawners;              // Drag spawner GameObjects here
    public GameObject[] enemyPrefabs;          // Drag enemy Prefabs here
    public float spawnInterval = 2f;           // Time between spawns in a wave

    [Header("Wave Settings")]
    public TMP_Text waveText;                      // UI Text (e.g. "Wave 1 starting!")
    public float timeBetweenWaves = 5f;        // Wait after wave ends before next wave
    public int enemiesPerWave = 5;             // How many enemies per wave


    private int currentWave = 0;
    private int spawnedThisWave = 0;
    private float nextSpawnTime;
    private bool waveActive = false;

    private void Start()
    {
        if (spawners.Length == 0)
        {
            Debug.LogError("EnemySpawning: no spawners assigned!");
        }
        if (enemyPrefabs.Length == 0)
        {
            Debug.LogError("EnemySpawning: no enemy prefabs assigned!");
        }

        // Start first wave after a short delay
        Invoke(nameof(StartWave), timeBetweenWaves);
    }

    private void Update()
    {
        // Optional: trigger next wave with Space (dev)
        if (Input.GetKeyDown(KeyCode.T))
        {
            if (!waveActive)
                StartWave();
        }

        // Wave spawning loop
        if (waveActive && Time.time >= nextSpawnTime)
        {
            SpawnEnemy();
            spawnedThisWave++;
            nextSpawnTime = Time.time + spawnInterval;

            if (spawnedThisWave >= enemiesPerWave)
            {
                EndWave();
            }
        }
    }

    private void StartWave()
    {
        currentWave++;
        waveActive = true;
        spawnedThisWave = 0;

        // Optional: update UI text
        if (waveText != null)
            waveText.text = "Wave " + currentWave + " starting!";

        Debug.Log("Wave " + currentWave + " starting!");

        // Randomly pick how many enemies this wave spawns (or just keep fixed)
        enemiesPerWave = 5 + (currentWave - 1) * 2;  // Increase per wave

        // Add a new enemy type prefab every wave (if you have them)
        // This is just an example: you can fine‑tune how you grow enemyPrefabs
        // (e.g. by growing a list instead of re‑assigning array)
    }

    private void EndWave()
    {
        waveActive = false;

        if (waveText != null)
            waveText.text = "Wave " + currentWave + " completed!";

        Debug.Log("Wave " + currentWave + " ended.");

        // Schedule next wave after delay
        Invoke(nameof(StartWave), timeBetweenWaves);
    }

    private void SpawnEnemy()
    {
        if (spawners.Length == 0 || enemyPrefabs.Length == 0)
            return;

        // 1. Pick a random spawner
        int spawnerIndex = Random.Range(0, spawners.Length);
        Transform spawner = spawners[spawnerIndex].transform;

        // 2. Pick a random enemy prefab (now it can grow with waves)
        int enemyIndex = Random.Range(0, GetCurrentEnemyCount());
        GameObject enemyPrefab = enemyPrefabs[enemyIndex];

        // 3. Instantiate
        Instantiate(enemyPrefab, spawner.position, spawner.rotation);
    }

    // Let the number of available enemy types grow with the wave
    private int GetCurrentEnemyCount()
    {
        // Clamp so we don’t overflow the array
        return Mathf.Min(currentWave, enemyPrefabs.Length);
    }
}
