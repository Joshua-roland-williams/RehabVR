// File: Assets/Scripts/ClinicalDataManager.cs
using UnityEngine;
using System.Collections.Generic;
using System.IO;
using System.Linq;

[System.Serializable]
public class PatientSession
{
    public string patientID;
    public string sessionID;
    public System.DateTime startTime;
    public System.DateTime endTime;
    public float totalDuration;
    public List<InteractionEvent> interactions = new List<InteractionEvent>();
    public List<MovementQualityData> movementData = new List<MovementQualityData>();
    public SessionMetrics metrics;
}

[System.Serializable]
public class InteractionEvent
{
    public string eventType; // "hover", "grab", "release"
    public string objectID;
    public string handUsed;
    public float timestamp;
    public Vector3 position;
    public bool successful;
    public float duration;
}

[System.Serializable]
public class SessionMetrics
{
    public int totalInteractions;
    public int successfulInteractions;
    public float successRate;
    public float averageTaskDuration;
    public float totalActiveTime;
    public float averageMovementSmoothness;
    public float averageMovementEfficiency;
    public int compensatoryMovements;
    public float improvementScore;
}

public class ClinicalDataManager : MonoBehaviour
{
    public static ClinicalDataManager Instance { get; private set; }
    
    [Header("Patient Configuration")]
    public string currentPatientID = "patient_001";
    public string therapistID = "therapist_001";
    
    [Header("Data Collection Settings")]
    public bool enableDataCollection = true;
    public bool saveToFile = true;
    public bool sendToServer = false;
    public string dataDirectory = "ClinicalData";
    
    [Header("Session Settings")]
    public float sessionTimeoutMinutes = 30f;
    public bool autoStartSession = true;
    
    // Current session data
    private PatientSession currentSession;
    private List<RehabGrabbableObject> registeredObjects = new List<RehabGrabbableObject>();
    private float sessionStartTime;
    private bool sessionActive = false;
    
    // Events for real-time monitoring
    public System.Action<SessionMetrics> OnMetricsUpdated;
    public System.Action<PatientSession> OnSessionCompleted;
    
    void Awake()
    {
        // Singleton pattern
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            InitializeDataManager();
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    void Start()
    {
        if (autoStartSession)
        {
            StartSession();
        }
    }
    
    private void InitializeDataManager()
    {
        // Create data directory if it doesn't exist
        if (saveToFile)
        {
            string fullPath = Path.Combine(Application.persistentDataPath, dataDirectory);
            if (!Directory.Exists(fullPath))
            {
                Directory.CreateDirectory(fullPath);
            }
        }
    }
    
    public void StartSession()
    {
        if (sessionActive) return;
        
        sessionActive = true;
        sessionStartTime = Time.time;
        
        currentSession = new PatientSession
        {
            patientID = currentPatientID,
            sessionID = System.Guid.NewGuid().ToString(),
            startTime = System.DateTime.Now,
            interactions = new List<InteractionEvent>(),
            movementData = new List<MovementQualityData>(),
            metrics = new SessionMetrics()
        };
        
        Debug.Log($"Clinical session started for patient: {currentPatientID}");
        
        // Start session timeout
        Invoke(nameof(EndSession), sessionTimeoutMinutes * 60f);
    }
    
    public void EndSession()
    {
        if (!sessionActive) return;
        
        sessionActive = false;
        currentSession.endTime = System.DateTime.Now;
        currentSession.totalDuration = Time.time - sessionStartTime;
        
        // Calculate final metrics
        CalculateSessionMetrics();
        
        // Save session data
        if (saveToFile)
        {
            SaveSessionToFile();
        }
        
        // Trigger completion event
        OnSessionCompleted?.Invoke(currentSession);
        
        Debug.Log($"Clinical session ended. Duration: {currentSession.totalDuration:F1}s");
        
        // Cancel timeout
        CancelInvoke(nameof(EndSession));
    }
    
    public void RegisterObject(RehabGrabbableObject obj)
    {
        if (registeredObjects.Contains(obj)) return;
        
        registeredObjects.Add(obj);
        
        // Subscribe to object events
        obj.OnObjectGrabbed += LogGrabInteraction;
        obj.OnObjectReleased += LogReleaseInteraction;
        obj.OnMovementAnalyzed += LogMovementData;
    }
    
    public void LogHoverEvent(string objectID, string handUsed)
    {
        if (!enableDataCollection || !sessionActive) return;
        
        InteractionEvent interaction = new InteractionEvent
        {
            eventType = "hover",
            objectID = objectID,
            handUsed = handUsed,
            timestamp = Time.time - sessionStartTime,
            position = Vector3.zero,
            successful = true,
            duration = 0f
        };
        
        currentSession.interactions.Add(interaction);
    }
    
    public void LogGrabEvent(string objectID, string handUsed, float timestamp)
    {
        if (!enableDataCollection || !sessionActive) return;
        
        Debug.Log($"Grab initiated: {objectID} with {handUsed}");
    }
    
    public void LogReleaseEvent(string objectID, float duration, bool successful)
    {
        if (!enableDataCollection || !sessionActive) return;
        
        Debug.Log($"Release logged: {objectID}, Duration: {duration:F2}s, Success: {successful}");
    }
    
    private void LogGrabInteraction(string objectID, float timestamp, Vector3 position)
    {
        if (!enableDataCollection || !sessionActive) return;
        
        InteractionEvent interaction = new InteractionEvent
        {
            eventType = "grab",
            objectID = objectID,
            handUsed = "unknown",
            timestamp = timestamp - sessionStartTime,
            position = position,
            successful = false,
            duration = 0f
        };
        
        currentSession.interactions.Add(interaction);
    }
    
    private void LogReleaseInteraction(string objectID, float duration, bool successful)
    {
        if (!enableDataCollection || !sessionActive) return;
        
        // Find the corresponding grab event and update it
        for (int i = currentSession.interactions.Count - 1; i >= 0; i--)
        {
            var interaction = currentSession.interactions[i];
            if (interaction.objectID == objectID && interaction.eventType == "grab" && interaction.duration == 0f)
            {
                interaction.duration = duration;
                interaction.successful = successful;
                break;
            }
        }
        
        // Update real-time metrics
        UpdateRealTimeMetrics();
    }
    
    private void LogMovementData(string objectID, MovementQualityData data)
    {
        if (!enableDataCollection || !sessionActive) return;
        
        data.objectID = objectID;
        currentSession.movementData.Add(data);
        
        // Update real-time metrics
        UpdateRealTimeMetrics();
    }
    
    private void UpdateRealTimeMetrics()
    {
        if (currentSession == null) return;
        
        // Calculate current session metrics
        var completedInteractions = currentSession.interactions.Where(i => i.duration > 0).ToList();
        
        currentSession.metrics.totalInteractions = completedInteractions.Count;
        currentSession.metrics.successfulInteractions = completedInteractions.Count(i => i.successful);
        currentSession.metrics.successRate = completedInteractions.Count > 0 ? 
            (float)currentSession.metrics.successfulInteractions / completedInteractions.Count : 0f;
        
        if (completedInteractions.Count > 0)
        {
            currentSession.metrics.averageTaskDuration = completedInteractions.Average(i => i.duration);
        }
        
        if (currentSession.movementData.Count > 0)
        {
            currentSession.metrics.averageMovementSmoothness = currentSession.movementData.Average(m => m.smoothnessScore);
            currentSession.metrics.averageMovementEfficiency = currentSession.movementData.Average(m => m.efficiencyScore);
            currentSession.metrics.compensatoryMovements = currentSession.movementData.Count(m => m.hasCompensatoryMovements);
        }
        
        // Trigger metrics update event
        OnMetricsUpdated?.Invoke(currentSession.metrics);
    }
    
    private void CalculateSessionMetrics()
    {
        if (currentSession == null) return;
        
        UpdateRealTimeMetrics();
        
        // Calculate improvement score (simplified)
        if (currentSession.movementData.Count > 5)
        {
            var firstHalf = currentSession.movementData.Take(currentSession.movementData.Count / 2);
            var secondHalf = currentSession.movementData.Skip(currentSession.movementData.Count / 2);
            
            float firstHalfSmoothness = firstHalf.Average(m => m.smoothnessScore);
            float secondHalfSmoothness = secondHalf.Average(m => m.smoothnessScore);
            
            currentSession.metrics.improvementScore = secondHalfSmoothness - firstHalfSmoothness;
        }
        
        currentSession.metrics.totalActiveTime = currentSession.totalDuration;
    }
    
    private void SaveSessionToFile()
    {
        try
        {
            string fileName = $"session_{currentSession.patientID}_{currentSession.startTime:yyyyMMdd_HHmmss}.json";
            string fullPath = Path.Combine(Application.persistentDataPath, dataDirectory, fileName);
            
            string jsonData = JsonUtility.ToJson(currentSession, true);
            File.WriteAllText(fullPath, jsonData);
            
            Debug.Log($"Session data saved to: {fullPath}");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Failed to save session data: {e.Message}");
        }
    }
    
    // Public methods for external access
    public SessionMetrics GetCurrentMetrics()
    {
        return currentSession?.metrics ?? new SessionMetrics();
    }
    
    public List<InteractionEvent> GetSessionInteractions()
    {
        return currentSession?.interactions ?? new List<InteractionEvent>();
    }
    
    public void SetPatientID(string patientID)
    {
        currentPatientID = patientID;
    }
    
    public bool IsSessionActive()
    {
        return sessionActive;
    }
    
    // Cleanup
    void OnDestroy()
    {
        if (sessionActive)
        {
            EndSession();
        }
    }
    
    void OnApplicationPause(bool pauseStatus)
    {
        if (pauseStatus && sessionActive)
        {
            EndSession();
        }
    }
}
