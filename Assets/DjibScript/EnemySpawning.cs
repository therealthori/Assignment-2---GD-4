using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class EnemySpawning : MonoBehaviour
{
   [Header("Spawn Settings")]
    public GameObject enemyPrefab;
    public int minEnemies = 2;
    public int maxEnemies = 5;
    public float spawnInterval = 3f;
    [Header("Spawn Logic")]
    public int spawnAttemptsPerFrame = 10;
    public LayerMask enemyLayer = -1;
    [Header("Debug")]
    public bool showDebugLogs = true;

    private List<GameObject> currentEnemies = new List<GameObject>();
    private List<GameObject> playersInArea = new List<GameObject>();
    private BoxCollider spawnArea;
    private Coroutine spawnCoroutine;
    private bool isInitialized = false;

    void Start()
    {
        spawnArea = GetComponent<BoxCollider>();
        if (spawnArea == null || enemyPrefab == null)
        {
            Debug.LogError("EnemySpawnManager: Missing BoxCollider or EnemyPrefab on " + gameObject.name);
            return;
        }
        isInitialized = true;
        Debug.Log("EnemySpawnManager initialized on " + gameObject.name + " with area size " + spawnArea.size);
        StartCoroutine(SpawnLoop());
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            if (!playersInArea.Contains(other.gameObject))
            {
                playersInArea.Add(other.gameObject);
                Debug.Log("Player " + other.name + " entered spawn area. Players in area: " + playersInArea.Count);
            }
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playersInArea.Remove(other.gameObject);
            Debug.Log("Player " + other.name + " exited spawn area. Players in area: " + playersInArea.Count);
        }
    }

    IEnumerator SpawnLoop()
    {
        while (true)
        {
            if (!isInitialized) yield return new WaitForSeconds(spawnInterval);

            UpdateEnemyCount();
            int currentCount = currentEnemies.Count;
            bool hasPlayer = playersInArea.Count > 0;

            string playerStatus = hasPlayer ? "YES" : "NO";
            Debug.Log($"Spawn Check [{gameObject.name}]: Enemies={currentCount}/{minEnemies}-{maxEnemies}, Player in area={playerStatus}, Total spawned ever={totalSpawned}");

            if (currentCount < minEnemies)
            {
                int toSpawn = Mathf.Min(spawnAttemptsPerFrame, minEnemies - currentCount);
                for (int i = 0; i < toSpawn; i++)
                {
                    SpawnEnemy();
                }
                Debug.Log($"Spawned {toSpawn} enemies to reach min ({minEnemies}). Current: {currentEnemies.Count}");
            }
            else if (currentCount < maxEnemies && hasPlayer && Random.value < 0.5f)
            {
                SpawnEnemy();
                Debug.Log("Spawned enemy (within max, player present). Current: " + currentEnemies.Count);
            }

            yield return new WaitForSeconds(spawnInterval);
        }
    }

    private int totalSpawned = 0;

    void SpawnEnemy()
    {
        Vector3 spawnPos = GetRandomPointInBox();
        GameObject newEnemy = Instantiate(enemyPrefab, spawnPos, Quaternion.identity);
        newEnemy.transform.SetParent(transform);
        currentEnemies.Add(newEnemy);
        totalSpawned++;
        Debug.Log($"Spawned enemy #{totalSpawned} at {spawnPos} (local: {transform.InverseTransformPoint(spawnPos)})");
    }

    Vector3 GetRandomPointInBox()
    {
        Vector3 localPos = new Vector3(
            Random.Range(-spawnArea.size.x * 0.5f, spawnArea.size.x * 0.5f),
            Random.Range(-spawnArea.size.y * 0.5f, spawnArea.size.y * 0.5f),
            Random.Range(-spawnArea.size.z * 0.5f, spawnArea.size.z * 0.5f)
        );
        return transform.TransformPoint(localPos);
    }

    void UpdateEnemyCount()
    {
        currentEnemies.RemoveAll(enemy => enemy == null);

        Collider[] enemiesInArea = Physics.OverlapBox(
            transform.position,
            spawnArea.size * 0.5f,
            transform.rotation,
            enemyLayer
        );
        int accurateCount = 0;
        foreach (Collider col in enemiesInArea)
        {
            if (col.CompareTag("Enemy"))
            {
                accurateCount++;
                if (!currentEnemies.Contains(col.gameObject))
                {
                    currentEnemies.Add(col.gameObject);
                }
            }
        }
        while (currentEnemies.Count > accurateCount)
        {
            currentEnemies.RemoveAt(currentEnemies.Count - 1);
        }

        if (showDebugLogs && accurateCount != currentEnemies.Count)
        {
            Debug.LogWarning($"Enemy count mismatch! List: {currentEnemies.Count}, OverlapBox: {accurateCount}");
        }
    }

    public void OnEnemyDestroyed(GameObject enemy)
    {
        if (currentEnemies.Contains(enemy))
        {
            currentEnemies.Remove(enemy);
            Debug.Log("Enemy destroyed. Current enemies: " + currentEnemies.Count);
        }
    }
}
