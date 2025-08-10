using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using System.Collections;

[System.Serializable]
public enum ObjectDifficulty
{
    Easy,
    Medium,
    Hard,
    Expert
}

public class RehabilitationObject : UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable
{
    [Header("Rehabilitation Properties")]
    public ObjectDifficulty difficulty = ObjectDifficulty.Easy;
    public float requiredGripStrength = 10f;
    public bool requiresPrecisionGrip = false;
    public Transform properGripPoint;
    
    [Header("Feedback Systems")]
    public AudioClip grabSuccessSound;
    public AudioClip releaseSound;
    public GameObject visualFeedback;
    public ParticleSystem gripFeedbackParticles;
    
    [Header("Assessment Metrics")]
    [HideInInspector] public float gripTime;
    [HideInInspector] public Vector3 gripPosition;
    [HideInInspector] public float gripAccuracy;
    [HideInInspector] public bool wasGrippedCorrectly;
    [HideInInspector] public float spawnTime;

    protected AudioSource audioSource;  // Changed from 'private' to 'protected'
    private Vector3 initialPosition;
    private Quaternion initialRotation;
    private float grabStartTime;
    protected PerformanceTracker performanceTracker;
    
    protected override void Awake()
    {
        base.Awake();
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();
            
        initialPosition = transform.position;
        initialRotation = transform.rotation;
        spawnTime = Time.time;
        
        // Cache performance tracker reference
        performanceTracker = FindObjectOfType<PerformanceTracker>();
    }
    
    protected override void OnSelectEntered(SelectEnterEventArgs args)
    {
        base.OnSelectEntered(args);
        grabStartTime = Time.time;
        gripPosition = args.interactorObject.transform.position;
        
        // Calculate grip accuracy
        if (properGripPoint != null)
        {
            float distance = Vector3.Distance(args.interactorObject.transform.position, properGripPoint.position);
            gripAccuracy = Mathf.Clamp01(1f - (distance / 0.1f));
            wasGrippedCorrectly = gripAccuracy > 0.7f;
        }
        else
        {
            // Default accuracy if no specific grip point
            gripAccuracy = 1f;
            wasGrippedCorrectly = true;
        }
        
        // Audio feedback
        if (grabSuccessSound != null && audioSource != null)
            audioSource.PlayOneShot(grabSuccessSound);
            
        // Visual feedback
        if (visualFeedback != null)
            visualFeedback.SetActive(true);
            
        // Particle feedback
        if (gripFeedbackParticles != null)
            gripFeedbackParticles.Play();
            
        // Send data to performance tracker
        performanceTracker?.OnObjectGrabbed(this);
    }
    
    protected override void OnSelectExited(SelectExitEventArgs args)
    {
        base.OnSelectExited(args);
        gripTime = Time.time - grabStartTime;
        
        // Audio feedback
        if (releaseSound != null && audioSource != null)
            audioSource.PlayOneShot(releaseSound);
            
        // Visual feedback
        if (visualFeedback != null)
            visualFeedback.SetActive(false);
            
        // Send completion data
        performanceTracker?.OnObjectReleased(this);
    }
    
    public void ResetToInitialPosition()
    {
        transform.position = initialPosition;
        transform.rotation = initialRotation;
        
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }
    }
    
    public float GetTimeAlive()
    {
        return Time.time - spawnTime;
    }
    
    public bool HasBeenGrabbed()
    {
        return gripTime > 0f;
    }
}
