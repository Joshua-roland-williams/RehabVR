using UnityEngine;
using System.Collections;
using UnityEngine.Events;

public class GrabSystemManager : MonoBehaviour
{
    [Header("Core Components")]
    public ObjectSpawner spawner;
    public PerformanceTracker tracker;
    
    [Header("Integration Settings")]
    public bool autoStartSession = true;
    public ObjectDifficulty startingDifficulty = ObjectDifficulty.Easy;
    public float sessionDuration = 300f; // 5 minutes default
    
    [Header("Events")]
    public UnityEvent OnSessionStarted;
    public UnityEvent OnSessionEnded;
    public UnityEvent<ObjectDifficulty> OnDifficultyChanged;
    
    private bool sessionActive = false;
    private float sessionTimer = 0f;
    
    void Start()
    {
        ValidateComponents();
        
        if (autoStartSession)
        {
            StartTherapySession();
        }
    }
    
    void Update()
    {
        if (sessionActive)
        {
            sessionTimer += Time.deltaTime;
            
            // Auto-end session after duration
            if (sessionTimer >= sessionDuration)
            {
                EndTherapySession();
            }
        }
    }
    
    private void ValidateComponents()
    {
        if (spawner == null)
        {
            spawner = FindObjectOfType<ObjectSpawner>();
            if (spawner == null)
                Debug.LogError("ObjectSpawner not found! Please assign it in the inspector.");
        }
        
        if (tracker == null)
        {
            tracker = FindObjectOfType<PerformanceTracker>();
            if (tracker == null)
                Debug.LogError("PerformanceTracker not found! Please assign it in the inspector.");
        }
    }
    
    public void StartTherapySession()
    {
        if (sessionActive)
        {
            Debug.LogWarning("Session already active!");
            return;
        }
        
        if (spawner != null)
        {
            sessionActive = true;
            sessionTimer = 0f;
            spawner.SpawnRandomObject(startingDifficulty);
            OnSessionStarted?.Invoke();
            Debug.Log("Therapy session started with difficulty: " + startingDifficulty);
        }
        else
        {
            Debug.LogError("ObjectSpawner not assigned!");
        }
    }
    
    public void EndTherapySession()
    {
        if (!sessionActive)
        {
            Debug.LogWarning("No active session to end!");
            return;
        }
        
        sessionActive = false;
        
        if (spawner != null)
        {
            spawner.ClearAllObjects();
        }
        
        if (tracker != null)
        {
            tracker.ExportSessionData();
        }
        
        OnSessionEnded?.Invoke();
        Debug.Log($"Therapy session ended after {sessionTimer:F1} seconds");
    }
    
    public void PauseSession()
    {
        sessionActive = false;
    }
    
    public void ResumeSession()
    {
        sessionActive = true;
    }
    
    public void SetDifficulty(ObjectDifficulty difficulty)
    {
        // Called by Amulya's kitchen system
        ObjectDifficulty previousDifficulty = startingDifficulty;
        startingDifficulty = difficulty;
        
        if (previousDifficulty != difficulty)
        {
            OnDifficultyChanged?.Invoke(difficulty);
            Debug.Log($"Difficulty changed from {previousDifficulty} to {difficulty}");
        }
    }
    
    public void SpawnNextObject()
    {
        if (sessionActive && spawner != null)
        {
            spawner.SpawnRandomObject(startingDifficulty);
        }
    }
    
    public bool IsSessionActive()
    {
        return sessionActive;
    }
    
    public float GetSessionProgress()
    {
        return sessionDuration > 0 ? sessionTimer / sessionDuration : 0f;
    }
    
    public PerformanceData GetCurrentPerformance()
    {
        if (tracker == null)
        {
            Debug.LogError("PerformanceTracker not assigned!");
            return new PerformanceData();
        }
        
        // Return data for kitchen system integration
        return new PerformanceData
        {
            successRate = tracker.totalAttempts > 0 ? (float)tracker.successfulGrips / tracker.totalAttempts : 0f,
            averageAccuracy = tracker.averageGripAccuracy,
            sessionTime = tracker.sessionDuration
        };
    }
}

[System.Serializable]
public class PerformanceData
{
    public float successRate;
    public float averageAccuracy;
    public float sessionTime;
}
