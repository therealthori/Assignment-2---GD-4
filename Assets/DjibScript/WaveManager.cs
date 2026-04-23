using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using System.Collections.Generic;
using TMPro;
using Unity.Netcode;

public class WaveManager : NetworkBehaviour
{

 [Header("Spawning")]
    public Transform[] spawnPoints;
    public GameObject[] enemyPrefabs;

    [Header("Wave Settings")]
    public float spawnInterval = 60f;
    public int enemiesPerWave = 10;
    public int increasePerWave = 5;

    [Header("Pooling")]
    public int poolSizePerType = 30;

    [Header("Limits")]
    public int maxZombies = 30;
    public ZombieCounter zombieCounter;

    private int currentWave = 0;
    private bool isSinglePlayer = false;

    // Pools per prefab
    private Dictionary<GameObject, List<GameObject>> pools = new Dictionary<GameObject, List<GameObject>>();

    void Start()
    {
        Debug.Log("WaveManager Start()");

        // Create pools
        foreach (GameObject prefab in enemyPrefabs)
        {
            CreatePool(prefab);
        }

        // Detect mode
        if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsListening)
        {
            isSinglePlayer = true;
            StartCoroutine(SpawnLoop());
            return;
        }

        if (!IsServer) return;

        StartCoroutine(SpawnLoop());
    }

    // =========================
    // POOLING
    // =========================
    void CreatePool(GameObject prefab)
    {
        List<GameObject> pool = new List<GameObject>();

        for (int i = 0; i < poolSizePerType; i++)
        {
            GameObject obj = Instantiate(prefab);
            obj.SetActive(false);
            pool.Add(obj);
        }

        pools.Add(prefab, pool);
    }

    GameObject GetFromPool(GameObject prefab)
    {
        List<GameObject> pool = pools[prefab];

        foreach (GameObject obj in pool)
        {
            if (!obj.activeInHierarchy)
                return obj;
        }

        // Expand pool if needed
        GameObject newObj = Instantiate(prefab);
        newObj.SetActive(false);
        pool.Add(newObj);

        return newObj;
    }

    // =========================
    // MAIN LOOP
    // =========================
    IEnumerator SpawnLoop()
    {
        while (true)
        {
            yield return new WaitForSeconds(spawnInterval);

            currentWave++;

            int enemiesToSpawn = enemiesPerWave + (currentWave * increasePerWave);

            Debug.Log($"Wave {currentWave} spawning up to {enemiesToSpawn}");

            StartCoroutine(SpawnWave(enemiesToSpawn));
        }
    }

    IEnumerator SpawnWave(int amount)
    {
        int spawned = 0;

        while (spawned < amount)
        {
            // 🚨 LIMIT CHECK
            if (zombieCounter.currentZombies >= maxZombies)
            {
                yield return null;
                continue;
            }

            SpawnEnemy();
            spawned++;

            yield return new WaitForSeconds(1f); // small delay like COD
        }
    }

    void SpawnEnemy()
    {
        if (spawnPoints.Length == 0 || enemyPrefabs.Length == 0) return;

        Transform spawnPoint = spawnPoints[Random.Range(0, spawnPoints.Length)];
        GameObject prefab = enemyPrefabs[Random.Range(0, enemyPrefabs.Length)];

        GameObject enemy = GetFromPool(prefab);

        enemy.transform.position = spawnPoint.position;
        enemy.transform.rotation = spawnPoint.rotation;

        enemy.SetActive(true);

        // 🌐 Netcode support
        if (!isSinglePlayer)
        {
            NetworkObject netObj = enemy.GetComponent<NetworkObject>();

            if (!netObj.IsSpawned)
                netObj.Spawn();
        }

        // Fix NavMeshAgent
        var agent = enemy.GetComponent<UnityEngine.AI.NavMeshAgent>();
        if (agent != null)
        {
            agent.Warp(spawnPoint.position);
        }
    }
}

