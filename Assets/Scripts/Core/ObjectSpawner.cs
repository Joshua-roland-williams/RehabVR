using UnityEngine;
using System.Collections.Generic;
using System.Collections;

public class ObjectSpawner : MonoBehaviour
{
    [Header("Spawn Settings")]
    public Transform spawnArea;
    public float spawnHeight = 1.2f;
    public Vector2 spawnAreaSize = new Vector2(2f, 1.6f);
    
    [Header("Object Prefabs")]
    public List<GameObject> level1Objects = new List<GameObject>();
    public List<GameObject> level2Objects = new List<GameObject>();
    public List<GameObject> cookingTools = new List<GameObject>();
    
    [Header("Spawn Control")]
    public int maxObjectsAtOnce = 5;
    public float respawnDelay = 2f;
    public bool autoRespawn = true;
    public float objectLifetime = 30f; // Auto-cleanup after 30 seconds
    
    private List<GameObject> activeObjects = new List<GameObject>();
    private List<ObjectDifficulty> spawnHistory = new List<ObjectDifficulty>();
    
    void Start()
    {
        Debug.Log("ObjectSpawner initialized");
        ValidateSpawnArea();
        
        if (autoRespawn)
        {
            StartCoroutine(AutoCleanupRoutine());
        }
    }
    
    private void ValidateSpawnArea()
    {
        if (spawnArea == null)
        {
            spawnArea = transform;
            Debug.LogWarning("Spawn area not set, using spawner's transform as spawn area");
        }
    }
    
    private IEnumerator AutoCleanupRoutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(5f); // Check every 5 seconds
            CleanupDestroyedObjects();
            CleanupOldObjects();
        }
    }
    
    private void CleanupOldObjects()
    {
        for (int i = activeObjects.Count - 1; i >= 0; i--)
        {
            if (activeObjects[i] != null)
            {
                RehabilitationObject rehabObj = activeObjects[i].GetComponent<RehabilitationObject>();
                if (rehabObj != null && Time.time - rehabObj.spawnTime > objectLifetime)
                {
                    DestroyImmediate(activeObjects[i]);
                    activeObjects.RemoveAt(i);
                }
            }
        }
    }
    
    public void SpawnRandomObject(ObjectDifficulty difficulty)
    {
        CleanupDestroyedObjects();
        
        if (activeObjects.Count >= maxObjectsAtOnce)
        {
            Debug.Log("Max objects reached, removing oldest object");
            RemoveOldestObject();
        }
            
        GameObject prefabToSpawn = SelectRandomPrefab(difficulty);
        if (prefabToSpawn != null)
        {
            Vector3 spawnPos = GetRandomSpawnPosition();
            Quaternion spawnRot = GetRandomRotation();
            GameObject spawned = Instantiate(prefabToSpawn, spawnPos, spawnRot);
            
            // Initialize rehabilitation object
            RehabilitationObject rehabObj = spawned.GetComponent<RehabilitationObject>();
            if (rehabObj != null)
            {
                rehabObj.spawnTime = Time.time;
                rehabObj.difficulty = difficulty;
            }
            
            activeObjects.Add(spawned);
            spawnHistory.Add(difficulty);
            Debug.Log($"Spawned: {spawned.name} (Difficulty: {difficulty})");
        }
        else
        {
            Debug.LogWarning("No prefab found for difficulty: " + difficulty);
        }
    }
    
    private void RemoveOldestObject()
    {
        if (activeObjects.Count > 0 && activeObjects[0] != null)
        {
            DestroyImmediate(activeObjects[0]);
            activeObjects.RemoveAt(0);
        }
    }
    
    private Quaternion GetRandomRotation()
    {
        return Quaternion.Euler(
            Random.Range(-15f, 15f),
            Random.Range(0f, 360f),
            Random.Range(-15f, 15f)
        );
    }
    
    private GameObject SelectRandomPrefab(ObjectDifficulty difficulty)
    {
        List<GameObject> targetList = null;
        
        switch (difficulty)
        {
            case ObjectDifficulty.Easy:
                targetList = level1Objects;
                break;
            case ObjectDifficulty.Medium:
            case ObjectDifficulty.Hard:
                targetList = level2Objects;
                break;
            case ObjectDifficulty.Expert:
                targetList = cookingTools;
                break;
        }
        
        if (targetList != null && targetList.Count > 0)
            return targetList[Random.Range(0, targetList.Count)];
            
        return null;
    }
    
    private Vector3 GetRandomSpawnPosition()
    {
        Vector3 basePosition = spawnArea.position;
        float x = Random.Range(-spawnAreaSize.x / 2f, spawnAreaSize.x / 2f);
        float z = Random.Range(-spawnAreaSize.y / 2f, spawnAreaSize.y / 2f);
        
        // Apply local offset to spawn area transform
        Vector3 localOffset = new Vector3(x, 0f, z);
        Vector3 worldOffset = spawnArea.TransformDirection(localOffset);
        
        return basePosition + worldOffset + Vector3.up * spawnHeight;
    }
    
    public void CleanupDestroyedObjects()
    {
        activeObjects.RemoveAll(obj => obj == null);
    }
    
    public void ClearAllObjects()
    {
        foreach (GameObject obj in activeObjects)
        {
            if (obj != null)
                DestroyImmediate(obj);
        }
        activeObjects.Clear();
        spawnHistory.Clear();
    }
    
    public int GetActiveObjectCount()
    {
        CleanupDestroyedObjects();
        return activeObjects.Count;
    }
    
    public List<ObjectDifficulty> GetSpawnHistory()
    {
        return new List<ObjectDifficulty>(spawnHistory);
    }
    
    // Visualization for spawn area in Scene view
    void OnDrawGizmosSelected()
    {
        if (spawnArea != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.matrix = spawnArea.localToWorldMatrix;
            Vector3 center = Vector3.up * spawnHeight;
            Vector3 size = new Vector3(spawnAreaSize.x, 0.1f, spawnAreaSize.y);
            Gizmos.DrawWireCube(center, size);
        }
    }
}
