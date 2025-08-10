// File: Assets/Scripts/MovementAnalyzer.cs
using UnityEngine;
using System.Collections.Generic;
using System.Linq;

[System.Serializable]
public class MovementQualityData
{
    public string objectID;
    public float smoothnessScore;
    public float efficiencyScore;
    public bool hasCompensatoryMovements;
    public float tremorLevel;
    public float averageVelocity;
    public float peakVelocity;
    public Vector3[] trajectoryPoints;
    public float gripDuration;
    public bool taskCompleted;
    public System.DateTime timestamp;
}

public class MovementAnalyzer : MonoBehaviour
{
    [Header("Analysis Parameters")]
    public float samplingRate = 50f; // Hz
    public float smoothnessThreshold = 0.1f;
    public float compensationThreshold = 0.3f;
    public float tremorFrequencyRange = 8f; // 4-12 Hz typical tremor range
    
    [Header("Tracking Settings")]
    public bool trackPosition = true;
    public bool trackVelocity = true;
    public bool trackAcceleration = true;
    public bool detectTremor = true;
    
    // Private tracking variables
    private List<Vector3> positionHistory = new List<Vector3>();
    private List<Vector3> velocityHistory = new List<Vector3>();
    private List<Vector3> accelerationHistory = new List<Vector3>();
    private List<float> timeStamps = new List<float>();
    
    private bool isTracking = false;
    private float trackingStartTime;
    private Vector3 lastPosition;
    private Vector3 lastVelocity;
    private Coroutine trackingCoroutine;
    
    public void StartTracking()
    {
        if (isTracking) return;
        
        isTracking = true;
        trackingStartTime = Time.time;
        
        // Clear previous data
        positionHistory.Clear();
        velocityHistory.Clear();
        accelerationHistory.Clear();
        timeStamps.Clear();
        
        // Initialize tracking
        lastPosition = transform.position;
        lastVelocity = Vector3.zero;
        
        // Start sampling coroutine
        trackingCoroutine = StartCoroutine(SampleMovement());
    }
    
    public MovementQualityData StopTracking()
    {
        if (!isTracking) return null;
        
        isTracking = false;
        
        if (trackingCoroutine != null)
        {
            StopCoroutine(trackingCoroutine);
            trackingCoroutine = null;
        }
        
        // Analyze collected data
        return AnalyzeMovement();
    }
    
    private System.Collections.IEnumerator SampleMovement()
    {
        float samplingInterval = 1f / samplingRate;
        
        while (isTracking)
        {
            // Record current state
            Vector3 currentPosition = transform.position;
            float currentTime = Time.time;
            
            positionHistory.Add(currentPosition);
            timeStamps.Add(currentTime);
            
            // Calculate velocity
            if (positionHistory.Count > 1)
            {
                Vector3 velocity = (currentPosition - lastPosition) / samplingInterval;
                velocityHistory.Add(velocity);
                
                // Calculate acceleration
                if (velocityHistory.Count > 1)
                {
                    Vector3 acceleration = (velocity - lastVelocity) / samplingInterval;
                    accelerationHistory.Add(acceleration);
                    lastVelocity = velocity;
                }
            }
            
            lastPosition = currentPosition;
            
            yield return new WaitForSeconds(samplingInterval);
        }
    }
    
    private MovementQualityData AnalyzeMovement()
    {
        if (positionHistory.Count < 10) return null; // Insufficient data
        
        MovementQualityData data = new MovementQualityData
        {
            objectID = GetComponent<RehabGrabbableObject>()?.therapySettings.objectID ?? "unknown",
            timestamp = System.DateTime.Now,
            trajectoryPoints = positionHistory.ToArray()
        };
        
        // Calculate smoothness score
        data.smoothnessScore = CalculateSmoothness();
        
        // Calculate efficiency score
        data.efficiencyScore = CalculateEfficiency();
        
        // Detect compensatory movements
        data.hasCompensatoryMovements = DetectCompensatoryMovements();
        
        // Analyze tremor
        data.tremorLevel = DetectTremor();
        
        // Calculate velocity metrics
        CalculateVelocityMetrics(out data.averageVelocity, out data.peakVelocity);
        
        return data;
    }
    
    private float CalculateSmoothness()
    {
        if (accelerationHistory.Count < 5) return 0f;
        
        // Calculate normalized jerk (smoothness metric)
        float totalJerk = 0f;
        float totalTime = timeStamps[timeStamps.Count - 1] - timeStamps[0];
        
        for (int i = 2; i < accelerationHistory.Count; i++)
        {
            Vector3 jerk = (accelerationHistory[i] - accelerationHistory[i-1]) / (1f / samplingRate);
            totalJerk += jerk.magnitude;
        }
        
        // Normalize jerk score (lower jerk = higher smoothness)
        float normalizedJerk = totalJerk / (totalTime * totalTime);
        return Mathf.Exp(-normalizedJerk * 10f); // Convert to 0-1 score
    }
    
    private float CalculateEfficiency()
    {
        if (positionHistory.Count < 2) return 0f;
        
        // Calculate path efficiency (straight line distance / actual path length)
        Vector3 startPos = positionHistory[0];
        Vector3 endPos = positionHistory[positionHistory.Count - 1];
        float straightLineDistance = Vector3.Distance(startPos, endPos);
        
        float actualPathLength = 0f;
        for (int i = 1; i < positionHistory.Count; i++)
        {
            actualPathLength += Vector3.Distance(positionHistory[i-1], positionHistory[i]);
        }
        
        return actualPathLength > 0f ? straightLineDistance / actualPathLength : 0f;
    }
    
    private bool DetectCompensatoryMovements()
    {
        if (positionHistory.Count < 10) return false;
        
        // Analyze movement patterns for compensation
        Vector3 movementVector = positionHistory[positionHistory.Count - 1] - positionHistory[0];
        float primaryMovementMagnitude = movementVector.magnitude;
        
        // Check for excessive lateral movements
        float maxLateralDeviation = 0f;
        Vector3 primaryDirection = movementVector.normalized;
        
        foreach (Vector3 pos in positionHistory)
        {
            Vector3 relativePos = pos - positionHistory[0];
            float projectedLength = Vector3.Dot(relativePos, primaryDirection);
            Vector3 projectedPoint = positionHistory[0] + primaryDirection * projectedLength;
            float lateralDeviation = Vector3.Distance(pos, projectedPoint);
            
            maxLateralDeviation = Mathf.Max(maxLateralDeviation, lateralDeviation);
        }
        
        // Compensation detected if lateral deviation exceeds threshold
        return maxLateralDeviation > compensationThreshold * primaryMovementMagnitude;
    }
    
    private float DetectTremor()
    {
        if (velocityHistory.Count < 20) return 0f;
        
        // Simple tremor detection using velocity oscillations
        List<float> velocityMagnitudes = new List<float>();
        foreach (Vector3 vel in velocityHistory)
        {
            velocityMagnitudes.Add(vel.magnitude);
        }
        
        // Calculate frequency content (simplified FFT approach)
        float tremorScore = 0f;
        int windowSize = Mathf.Min(velocityMagnitudes.Count, 50);
        
        for (int i = 0; i < velocityMagnitudes.Count - windowSize; i += 10)
        {
            float localVariance = 0f;
            float localMean = 0f;
            
            // Calculate variance in window
            for (int j = i; j < i + windowSize; j++)
            {
                localMean += velocityMagnitudes[j];
            }
            localMean /= windowSize;
            
            for (int j = i; j < i + windowSize; j++)
            {
                localVariance += Mathf.Pow(velocityMagnitudes[j] - localMean, 2);
            }
            localVariance /= windowSize;
            
            tremorScore = Mathf.Max(tremorScore, localVariance);
        }
        
        return Mathf.Clamp01(tremorScore * 100f); // Normalize to 0-1
    }
    
    private void CalculateVelocityMetrics(out float averageVelocity, out float peakVelocity)
    {
        averageVelocity = 0f;
        peakVelocity = 0f;
        
        if (velocityHistory.Count == 0) return;
        
        float totalVelocity = 0f;
        foreach (Vector3 vel in velocityHistory)
        {
            float magnitude = vel.magnitude;
            totalVelocity += magnitude;
            peakVelocity = Mathf.Max(peakVelocity, magnitude);
        }
        
        averageVelocity = totalVelocity / velocityHistory.Count;
    }
    
    // Visualization for debugging
    void OnDrawGizmos()
    {
        if (!isTracking || positionHistory.Count < 2) return;
        
        // Draw movement trajectory
        Gizmos.color = Color.blue;
        for (int i = 1; i < positionHistory.Count; i++)
        {
            Gizmos.DrawLine(positionHistory[i-1], positionHistory[i]);
        }
        
        // Draw start and end points
        if (positionHistory.Count > 0)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawSphere(positionHistory[0], 0.02f);
            
            Gizmos.color = Color.red;
            Gizmos.DrawSphere(positionHistory[positionHistory.Count - 1], 0.02f);
        }
    }
}
