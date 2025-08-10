using UnityEngine;
using System.Collections.Generic;
using System;
using System.IO;

[System.Serializable]
public class GripAttempt
{
    public DateTime timestamp;
    public string objectName;
    public float reachTime;
    public float gripDuration;
    public Vector3 gripPosition;
    public float gripAccuracy;
    public bool successful;
    public ObjectDifficulty difficulty;
    public float objectAge; // How long the object existed before being grabbed
}

[System.Serializable]
public class SessionSummary
{
    public int totalAttempts;
    public int successfulGrips;
    public float averageAccuracy;
    public float sessionDuration;
    public Dictionary<ObjectDifficulty, int> difficultyBreakdown;
    public DateTime sessionStart;
    public DateTime sessionEnd;
}

public class PerformanceTracker : MonoBehaviour
{
    [Header("Session Data")]
    public List<GripAttempt> currentSessionData = new List<GripAttempt>();
    
    [Header("Real-time Metrics")]
    public int successfulGrips = 0;
    public int totalAttempts = 0;
    public float averageGripAccuracy = 0f;
    public float sessionDuration = 0f;
    
    [Header("Analysis Settings")]
    public bool enableDetailedLogging = true;
    public bool autoExportOnSessionEnd = true;
    
    private float sessionStartTime;
    private DateTime sessionStartDateTime;
    private RehabilitationObject currentObject;
    private Dictionary<ObjectDifficulty, int> difficultyStats = new Dictionary<ObjectDifficulty, int>();
    
    void Start()
    {
        sessionStartTime = Time.time;
        sessionStartDateTime = DateTime.Now;
        InitializeDifficultyStats();
        
        if (enableDetailedLogging)
            Debug.Log("Performance tracking started at: " + sessionStartDateTime.ToString());
    }
    
    private void InitializeDifficultyStats()
    {
        foreach (ObjectDifficulty difficulty in System.Enum.GetValues(typeof(ObjectDifficulty)))
        {
            difficultyStats[difficulty] = 0;
        }
    }
    
    void Update()
    {
        sessionDuration = Time.time - sessionStartTime;
    }
    
    public void OnObjectGrabbed(RehabilitationObject obj)
    {
        currentObject = obj;
        totalAttempts++;
        
        // Track difficulty statistics
        if (difficultyStats.ContainsKey(obj.difficulty))
            difficultyStats[obj.difficulty]++;
        
        if (obj.wasGrippedCorrectly)
            successfulGrips++;
            
        // Update running averages
        UpdateAverages();
        
        if (enableDetailedLogging)
            Debug.Log($"Object grabbed: {obj.name} (Accuracy: {obj.gripAccuracy:F2}, Success: {obj.wasGrippedCorrectly})");
    }
    
    public void OnObjectReleased(RehabilitationObject obj)
    {
        if (currentObject == obj)
        {
            GripAttempt attempt = new GripAttempt
            {
                timestamp = DateTime.Now,
                objectName = obj.name,
                gripDuration = obj.gripTime,
                gripPosition = obj.gripPosition,
                gripAccuracy = obj.gripAccuracy,
                successful = obj.wasGrippedCorrectly,
                difficulty = obj.difficulty,
                objectAge = obj.GetTimeAlive()
            };
            
            currentSessionData.Add(attempt);
            
            if (enableDetailedLogging)
            {
                Debug.Log($"Object released: {obj.name} (Duration: {obj.gripTime:F2}s, Age: {obj.GetTimeAlive():F2}s)");
            }
            
            // Auto-save periodically
            if (currentSessionData.Count % 10 == 0)
            {
                SaveSessionData();
            }
        }
    }
    
    private void UpdateAverages()
    {
        if (totalAttempts > 0)
        {
            float totalAccuracy = 0f;
            foreach (var attempt in currentSessionData)
            {
                totalAccuracy += attempt.gripAccuracy;
            }
            averageGripAccuracy = totalAccuracy / currentSessionData.Count;
        }
    }
    
    private void SaveSessionData()
    {
        // Save to PlayerPrefs or file system
        string jsonData = JsonUtility.ToJson(new SerializableList<GripAttempt>(currentSessionData));
        PlayerPrefs.SetString("SessionData_" + DateTime.Now.ToString("yyyyMMdd_HHmmss"), jsonData);
    }
    
    public void ExportSessionData()
    {
        try
        {
            // Create session summary
            SessionSummary summary = new SessionSummary
            {
                totalAttempts = totalAttempts,
                successfulGrips = successfulGrips,
                averageAccuracy = averageGripAccuracy,
                sessionDuration = sessionDuration,
                difficultyBreakdown = new Dictionary<ObjectDifficulty, int>(difficultyStats),
                sessionStart = sessionStartDateTime,
                sessionEnd = DateTime.Now
            };
            
            // Export detailed data
            string fileName = "RehabSession_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".json";
            string filePath = Path.Combine(Application.persistentDataPath, fileName);
            
            var exportData = new { 
                summary = summary, 
                attempts = currentSessionData 
            };
            
            string jsonData = JsonUtility.ToJson(exportData, true);
            File.WriteAllText(filePath, jsonData);
            
            Debug.Log($"Session data exported to: {filePath}");
            Debug.Log($"Session Summary - Success Rate: {GetSuccessRate():P1}, Avg Accuracy: {averageGripAccuracy:F2}");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Failed to export session data: {e.Message}");
        }
    }
    
    public float GetSuccessRate()
    {
        return totalAttempts > 0 ? (float)successfulGrips / totalAttempts : 0f;
    }
    
    public Dictionary<ObjectDifficulty, int> GetDifficultyBreakdown()
    {
        return new Dictionary<ObjectDifficulty, int>(difficultyStats);
    }
    
    public void ResetSession()
    {
        currentSessionData.Clear();
        successfulGrips = 0;
        totalAttempts = 0;
        averageGripAccuracy = 0f;
        sessionStartTime = Time.time;
        sessionStartDateTime = DateTime.Now;
        InitializeDifficultyStats();
        
        Debug.Log("Performance tracking session reset");
    }
}

[System.Serializable]
public class SerializableList<T>
{
    public List<T> items;
    public SerializableList(List<T> list) { items = list; }
}
