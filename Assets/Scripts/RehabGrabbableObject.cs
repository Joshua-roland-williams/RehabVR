// File: Assets/Scripts/RehabGrabbableObject.cs
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using System.Collections;

[System.Serializable]
public class TherapeuticProperties
{
    [Header("Clinical Settings")]
    public string objectID = "default_object";
    public float difficultyLevel = 1f;
    public bool requiresTwoHands = false;
    public bool trackMovementQuality = true;
    
    [Header("Physical Properties")]
    public float weightSimulation = 0.1f;
    public Vector3 preferredGripPosition = Vector3.zero;
    public float gripForceRequired = 0.5f;
    
    [Header("Therapeutic Goals")]
    public bool trainsReaching = true;
    public bool trainsGrasping = true;
    public bool trainsManipulation = false;
    public bool trainsBimanual = false;
}

public class RehabGrabbableObject : MonoBehaviour
{
    [Header("Rehabilitation Configuration")]
    public TherapeuticProperties therapySettings;
    
    [Header("Visual Feedback")]
    public GameObject highlightObject;
    public Material defaultMaterial;
    public Material highlightMaterial;
    public Material selectedMaterial;
    
    [Header("Audio Feedback")]
    public AudioClip contactSound;
    public AudioClip grabSound;
    public AudioClip releaseSound;
    public AudioClip successSound;
    
    [Header("Interaction Settings")]
    [SerializeField] private InteractionLayerMask interactionLayerMask = 1; // Use InteractionLayerMask directly
    public float grabDistance = 0.1f;
    public bool enableRayInteraction = true;
    public bool enableDirectInteraction = true;
    
    // Private variables
    private Renderer objectRenderer;
    private AudioSource audioSource;
    private MovementAnalyzer movementAnalyzer;
    private HapticFeedbackController hapticController;
    private UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable grabInteractable;
    private Vector3 initialPosition;
    private Quaternion initialRotation;
    private float gripStartTime;
    private bool isBeingTracked = false;
    private bool isGrabbed = false;
    private Transform grabbingHand;
    
    // Events for clinical data collection
    public System.Action<string, float, Vector3> OnObjectGrabbed;
    public System.Action<string, float, bool> OnObjectReleased;
    public System.Action<string, MovementQualityData> OnMovementAnalyzed;
    
    void Awake()
    {
        // Initialize therapy settings if null
        if (therapySettings == null)
        {
            therapySettings = new TherapeuticProperties();
        }
        
        // Store initial transform
        initialPosition = transform.position;
        initialRotation = transform.rotation;
        
        // Get or add required components
        objectRenderer = GetComponent<Renderer>();
        if (objectRenderer == null)
        {
            objectRenderer = GetComponentInChildren<Renderer>();
        }
        
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.spatialBlend = 1f; // 3D sound
            audioSource.volume = 0.5f;
        }
        
        // Setup XR Grab Interactable for proper VR interaction
        grabInteractable = GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable>();
        if (grabInteractable == null)
        {
            grabInteractable = gameObject.AddComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRGrabInteractable>();
            ConfigureGrabInteractable();
        }
        
        // Get movement analyzer if tracking is enabled
        if (therapySettings.trackMovementQuality)
        {
            movementAnalyzer = GetComponent<MovementAnalyzer>();
            if (movementAnalyzer == null)
            {
                movementAnalyzer = gameObject.AddComponent<MovementAnalyzer>();
            }
        }
        
        // Get haptic feedback controller
        hapticController = GetComponent<HapticFeedbackController>();
        if (hapticController == null)
        {
            hapticController = gameObject.AddComponent<HapticFeedbackController>();
        }
        
        // Configure highlight system
        if (highlightObject == null)
            highlightObject = gameObject;
        
        // Setup interaction events
        SetupInteractionEvents();
    }
    
    private void ConfigureGrabInteractable()
    {
        // FIX: Direct assignment of InteractionLayerMask
        grabInteractable.interactionLayers = interactionLayerMask;
        grabInteractable.selectMode = UnityEngine.XR.Interaction.Toolkit.Interactables.InteractableSelectMode.Single;
        grabInteractable.movementType = UnityEngine.XR.Interaction.Toolkit.Interactables.XRBaseInteractable.MovementType.VelocityTracking;
        grabInteractable.trackPosition = true;
        grabInteractable.trackRotation = true;
        grabInteractable.smoothPosition = true;
        grabInteractable.smoothRotation = true;
        grabInteractable.smoothPositionAmount = 10f;
        grabInteractable.smoothRotationAmount = 10f;
        grabInteractable.tightenPosition = 0.5f;
        grabInteractable.tightenRotation = 0.5f;
        
        // Configure attach transform if preferred grip position is set
        if (therapySettings.preferredGripPosition != Vector3.zero)
        {
            GameObject attachPoint = new GameObject("AttachPoint");
            attachPoint.transform.SetParent(transform);
            attachPoint.transform.localPosition = therapySettings.preferredGripPosition;
            grabInteractable.attachTransform = attachPoint.transform;
        }
    }
    
    // Alternative method if you need to convert from LayerMask to InteractionLayerMask
    public void SetInteractionLayersFromLayerMask(LayerMask layerMask)
    {
        // Convert LayerMask to InteractionLayerMask using the value property
        InteractionLayerMask interactionMask = new InteractionLayerMask();
        interactionMask.value = layerMask.value;
        
        if (grabInteractable != null)
        {
            grabInteractable.interactionLayers = interactionMask;
        }
        
        // Store the converted mask
        interactionLayerMask = interactionMask;
    }
    
    private void SetupInteractionEvents()
    {
        // Subscribe to XR interaction events
        grabInteractable.hoverEntered.AddListener(OnHoverEntered);
        grabInteractable.hoverExited.AddListener(OnHoverExited);
        grabInteractable.selectEntered.AddListener(OnSelectEntered);
        grabInteractable.selectExited.AddListener(OnSelectExited);
    }
    
    void Start()
    {
        // Register with clinical data manager if it exists
        if (ClinicalDataManager.Instance != null)
        {
            ClinicalDataManager.Instance.RegisterObject(this);
        }
        
        // Apply initial material
        if (objectRenderer && defaultMaterial)
        {
            objectRenderer.material = defaultMaterial;
        }
        
        // Initialize object properties based on difficulty
        UpdateObjectProperties();
        
        // Validate setup
        ValidateSetup();
    }
    
    private void OnHoverEntered(HoverEnterEventArgs args)
    {
        // Visual feedback
        if (objectRenderer && highlightMaterial)
        {
            objectRenderer.material = highlightMaterial;
        }
        
        // Audio feedback
        PlaySound(contactSound);
        
        // Haptic feedback
        if (hapticController != null)
        {
            hapticController.TriggerContactFeedback(args.interactorObject);
        }
        
        // Clinical logging
        if (ClinicalDataManager.Instance != null)
        {
            ClinicalDataManager.Instance.LogHoverEvent(therapySettings.objectID, args.interactorObject.transform.name);
        }
        
        Debug.Log($"Hovering over: {therapySettings.objectID}");
    }
    
    private void OnHoverExited(HoverExitEventArgs args)
    {
        // Reset visual feedback only if not grabbed
        if (!isGrabbed && objectRenderer && defaultMaterial)
        {
            objectRenderer.material = defaultMaterial;
        }
        
        Debug.Log($"Stopped hovering over: {therapySettings.objectID}");
    }
    
    private void OnSelectEntered(SelectEnterEventArgs args)
    {
        isGrabbed = true;
        grabbingHand = args.interactorObject.transform;
        
        // Record grip start time
        gripStartTime = Time.time;
        
        // Visual feedback
        if (objectRenderer && selectedMaterial)
        {
            objectRenderer.material = selectedMaterial;
        }
        
        // Audio feedback
        PlaySound(grabSound);
        
        // Start movement tracking
        if (movementAnalyzer && therapySettings.trackMovementQuality)
        {
            movementAnalyzer.StartTracking();
            isBeingTracked = true;
        }
        
        // Haptic feedback
        if (hapticController != null)
        {
            hapticController.TriggerGrabFeedback(args.interactorObject);
        }
        
        // Apply weight simulation
        ApplyWeightSimulation(true);
        
        // Clinical event logging
        OnObjectGrabbed?.Invoke(therapySettings.objectID, gripStartTime, args.interactorObject.transform.position);
        
        if (ClinicalDataManager.Instance != null)
        {
            ClinicalDataManager.Instance.LogGrabEvent(therapySettings.objectID, args.interactorObject.transform.name, gripStartTime);
        }
        
        Debug.Log($"Grabbed: {therapySettings.objectID} at time: {gripStartTime}");
    }
    
    private void OnSelectExited(SelectExitEventArgs args)
    {
        isGrabbed = false;
        grabbingHand = null;
        
        float gripDuration = Time.time - gripStartTime;
        bool taskCompleted = CheckTaskCompletion();
        
        // Visual feedback
        if (objectRenderer && defaultMaterial)
        {
            objectRenderer.material = defaultMaterial;
        }
        
        // Audio feedback
        PlaySound(taskCompleted ? successSound : releaseSound);
        
        // Stop movement tracking and analyze
        if (isBeingTracked && movementAnalyzer)
        {
            MovementQualityData qualityData = movementAnalyzer.StopTracking();
            if (qualityData != null)
            {
                qualityData.taskCompleted = taskCompleted;
                qualityData.gripDuration = gripDuration;
                
                OnMovementAnalyzed?.Invoke(therapySettings.objectID, qualityData);
            }
            isBeingTracked = false;
        }
        
        // Success haptic feedback
        if (taskCompleted && hapticController != null)
        {
            hapticController.TriggerSuccessFeedback(args.interactorObject);
        }
        
        // Remove weight simulation
        ApplyWeightSimulation(false);
        
        // Clinical event logging
        OnObjectReleased?.Invoke(therapySettings.objectID, gripDuration, taskCompleted);
        
        if (ClinicalDataManager.Instance != null)
        {
            ClinicalDataManager.Instance.LogReleaseEvent(therapySettings.objectID, gripDuration, taskCompleted);
        }
        
        // Reset position if configured
        if (ShouldResetPosition())
        {
            StartCoroutine(ResetPositionAfterDelay(2f));
        }
        
        Debug.Log($"Released: {therapySettings.objectID}, Duration: {gripDuration:F2}s, Success: {taskCompleted}");
    }
    
    private void ApplyWeightSimulation(bool enable)
    {
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            if (enable)
            {
                rb.mass = therapySettings.weightSimulation;
                rb.linearDamping = therapySettings.weightSimulation * 2f;
            }
            else
            {
                rb.mass = 1f;
                rb.linearDamping = 0f;
            }
        }
    }
    
    private bool CheckTaskCompletion()
    {
        return true;
    }
    
    private bool ShouldResetPosition()
    {
        return Vector3.Distance(transform.position, initialPosition) > 2f;
    }
    
    private IEnumerator ResetPositionAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        
        float resetTime = 1f;
        Vector3 startPos = transform.position;
        Quaternion startRot = transform.rotation;
        
        for (float t = 0; t < resetTime; t += Time.deltaTime)
        {
            float progress = t / resetTime;
            transform.position = Vector3.Lerp(startPos, initialPosition, progress);
            transform.rotation = Quaternion.Lerp(startRot, initialRotation, progress);
            yield return null;
        }
        
        transform.position = initialPosition;
        transform.rotation = initialRotation;
        
        Debug.Log($"Reset position for: {therapySettings.objectID}");
    }
    
    private void PlaySound(AudioClip clip)
    {
        if (audioSource && clip)
        {
            audioSource.clip = clip;
            audioSource.Play();
        }
    }
    
    public void SetDifficultyLevel(float newDifficulty)
    {
        therapySettings.difficultyLevel = newDifficulty;
        UpdateObjectProperties();
        Debug.Log($"Updated difficulty for {therapySettings.objectID} to: {newDifficulty}");
    }
    
    public void UpdateObjectProperties()
    {
        float scaleFactor = Mathf.Lerp(1.2f, 0.8f, therapySettings.difficultyLevel / 5f);
        transform.localScale = Vector3.one * scaleFactor;
        
        therapySettings.weightSimulation = Mathf.Lerp(0.1f, 1f, therapySettings.difficultyLevel / 5f);
        therapySettings.gripForceRequired = Mathf.Lerp(0.3f, 0.9f, therapySettings.difficultyLevel / 5f);
    }
    
    public TherapeuticProperties GetTherapySettings()
    {
        return therapySettings;
    }
    
    public bool IsCurrentlyGrabbed()
    {
        return isGrabbed && grabInteractable != null && grabInteractable.isSelected;
    }
    
    public float GetCurrentGripDuration()
    {
        if (IsCurrentlyGrabbed())
        {
            return Time.time - gripStartTime;
        }
        return 0f;
    }
    
    public Vector3 GetInitialPosition()
    {
        return initialPosition;
    }
    
    public void ForceReset()
    {
        StopAllCoroutines();
        
        if (IsCurrentlyGrabbed() && grabInteractable != null)
        {
            grabInteractable.interactionManager.CancelInteractableSelection((UnityEngine.XR.Interaction.Toolkit.Interactables.IXRSelectInteractable)grabInteractable);
        }
        
        transform.position = initialPosition;
        transform.rotation = initialRotation;
        
        if (objectRenderer && defaultMaterial)
        {
            objectRenderer.material = defaultMaterial;
        }
        
        if (isBeingTracked && movementAnalyzer)
        {
            movementAnalyzer.StopTracking();
            isBeingTracked = false;
        }
        
        isGrabbed = false;
        grabbingHand = null;
        
        Debug.Log($"Force reset: {therapySettings.objectID}");
    }
    
    public bool ValidateSetup()
    {
        bool isValid = true;
        
        if (GetComponent<Collider>() == null)
        {
            Debug.LogWarning($"RehabGrabbableObject '{therapySettings.objectID}' is missing a Collider component!");
            isValid = false;
        }
        
        if (GetComponent<Rigidbody>() == null)
        {
            Debug.LogWarning($"RehabGrabbableObject '{therapySettings.objectID}' is missing a Rigidbody component!");
            isValid = false;
        }
        
        if (objectRenderer == null)
        {
            Debug.LogWarning($"RehabGrabbableObject '{therapySettings.objectID}' is missing a Renderer component!");
            isValid = false;
        }
        
        return isValid;
    }
    
    void Reset()
    {
        if (therapySettings == null)
        {
            therapySettings = new TherapeuticProperties();
        }
        
        therapySettings.objectID = gameObject.name.ToLower().Replace(" ", "_");
        
        if (GetComponent<Rigidbody>() == null)
        {
            gameObject.AddComponent<Rigidbody>();
        }
        
        if (GetComponent<Collider>() == null)
        {
            gameObject.AddComponent<BoxCollider>();
        }
    }
    
    void OnDestroy()
    {
        if (grabInteractable != null)
        {
            grabInteractable.hoverEntered.RemoveListener(OnHoverEntered);
            grabInteractable.hoverExited.RemoveListener(OnHoverExited);
            grabInteractable.selectEntered.RemoveListener(OnSelectEntered);
            grabInteractable.selectExited.RemoveListener(OnSelectExited);
        }
        
        StopAllCoroutines();
        
        if (isBeingTracked && movementAnalyzer)
        {
            movementAnalyzer.StopTracking();
        }
    }
}
